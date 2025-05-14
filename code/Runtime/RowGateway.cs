using Runtime.Entity;
using Runtime.Entity.Cache;
using Runtime.Entity.RowItem;
using Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Runtime
{
    internal class RowGateway : IAsyncDisposable
    {
        #region Inner Types
        private record QueuedRow(
            DateTime EnqueueTime,
            byte[] Buffer,
            TaskCompletionSource? RowPersistedSource);
        #endregion

        private static readonly TimeSpan MIN_WAIT_PERIOD = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FLUSH_PERIOD = TimeSpan.FromSeconds(5);

        private static readonly RowSerializer<RowType> _rowSerializer =
            CreateRowSerializer();
        private readonly Version _appVersion;
        private readonly LogStorage _logStorage;
        private readonly ConcurrentQueue<QueuedRow> _rowQueue = new();
        private readonly ConcurrentQueue<Task> _releaseSourceTaskQueue = new();
        private readonly Task _backgroundTask;
        private readonly TaskCompletionSource _backgroundCompletedSource = new();
        private readonly object _lock = new object();
        private IAppendStorage _currentShardStorage;
        private long _currentShardIndex;
        private volatile RowInMemoryCache _inMemoryCache;

        #region Construction
        private RowGateway(
            Version appVersion,
            LogStorage logStorage,
            long currentShardIndex,
            IAppendStorage currentShardStorage,
            RowInMemoryCache cache,
            CancellationToken ct)
        {
            _appVersion = appVersion;
            _logStorage = logStorage;
            _backgroundTask = Task.Run(() => BackgroundPersistanceAsync(ct));
            _currentShardIndex = currentShardIndex;
            _currentShardStorage = currentShardStorage;
            _inMemoryCache = cache;
        }

        public static async Task<RowGateway> CreateAsync(
            Version appVersion,
            LogStorage logStorage,
            CancellationToken ct)
        {
            var cache = new RowInMemoryCache();
            var lastShard = (long)-1;

            //  Read everything in the cache (which resolve evolution)
            await foreach (var pair in ReadCurrentStateAsync(logStorage, ct))
            {
                lastShard = pair.Shard;

                cache = cache.AppendItem(pair.Row);
            }
            if (lastShard >= 0)
            {
                //  Persists into a new view
                await PersistViewAsync(appVersion, logStorage, cache, lastShard, ct);
            }

            return new RowGateway(
                appVersion,
                logStorage,
                lastShard + 1,
                await logStorage.OpenWriteLogShardAsync(lastShard + 1, ct),
                cache,
                ct);
        }

        private static async IAsyncEnumerable<(RowBase Row, long Shard)> ReadCurrentStateAsync(
            LogStorage logStorage,
            [EnumeratorCancellation]
            CancellationToken ct)
        {   //  Start with latest view, then move to remaining shards
            var lastShard = (long)0;

            using (var latestStream = await logStorage.OpenReadLatestViewAsync(ct))
            {
                if (latestStream != null)
                {
                    using (var reader = new StreamReader(latestStream))
                    {
                        var versionHeaderText = await reader.ReadLineAsync(ct);
                        var versionHeader = versionHeaderText != null
                            ? JsonSerializer.Deserialize(
                                versionHeaderText,
                                RowJsonContext.Default.FileVersionHeader)
                            : null;

                        if (versionHeader == null)
                        {
                            throw new InvalidDataException(
                                "Latest view blob doesn't contain file version header");
                        }

                        var viewHeaderText = await reader.ReadLineAsync(ct);
                        var viewHeader = viewHeaderText != null
                            ? JsonSerializer.Deserialize(
                                viewHeaderText,
                                RowJsonContext.Default.ViewHeader)
                            : null;

                        if (viewHeader == null)
                        {
                            throw new InvalidDataException(
                                "Latest view blob doesn't contain view header");
                        }
                        lastShard = viewHeader.LastShard;

                        string? text;

                        while (!string.IsNullOrWhiteSpace(text = await reader.ReadLineAsync(ct)))
                        {
                            var row = (RowBase)_rowSerializer.Deserialize(text);

                            yield return (row, lastShard);
                        }
                    }
                }
            }
            //  Move the remaining shards
            Stream? shardStream;

            while ((shardStream = await logStorage.OpenReadLogShardAsync(++lastShard, ct)) != null)
            {
                using (shardStream)
                using (var reader = new StreamReader(shardStream))
                {
                    var versionHeaderText = await reader.ReadLineAsync(ct);
                    var versionHeader = versionHeaderText != null
                        ? JsonSerializer.Deserialize(
                            versionHeaderText,
                            RowJsonContext.Default.FileVersionHeader)
                        : null;
                    string? text;

                    if (versionHeader == null)
                    {
                        throw new InvalidDataException(
                            "Log shard doesn't contain file version header");
                    }
                    while (!string.IsNullOrWhiteSpace(text = await reader.ReadLineAsync(ct)))
                    {
                        var row = (RowBase)_rowSerializer.Deserialize(text);

                        yield return (row, lastShard);
                    }
                }
            }
        }

        private static RowSerializer<RowType> CreateRowSerializer()
        {
            return new RowSerializer<RowType>(RowJsonContext.Default.GetTypeInfo)
                .AddType<FileVersionHeader>(RowType.FileVersionHeader)
                .AddType<ViewHeader>(RowType.ViewHeader)
                .AddType<ProcedureRunRow>(RowType.ProcedureRun)
                .AddType<ProcedureRunStepRow>(RowType.ProcedureRunStep);
        }
        #endregion

        public RowInMemoryCache InMemoryCache => _inMemoryCache;

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            _backgroundCompletedSource.SetResult();
            await _backgroundTask;
            await Task.WhenAll(_releaseSourceTaskQueue);
        }

        public void Append(RowBase item)
        {
            AppendInternal(new[] { item }, null);
        }

        public void Append(IEnumerable<RowBase> items)
        {
            AppendInternal(items, null);
        }

        public Task AppendAndPersistAsync(RowBase item, CancellationToken ct)
        {
            var taskSource = new TaskCompletionSource();

            AppendInternal(new[] { item }, taskSource);

            return taskSource.Task;
        }

        public async Task AppendAndPersistAsync(IEnumerable<RowBase> items, CancellationToken ct)
        {
            var materializedItems = items.ToImmutableArray();

            if (materializedItems.Any())
            {
                var taskSource = new TaskCompletionSource();

                AppendInternal(items, taskSource);
                await taskSource.Task;
            }
        }

        private void AppendInternal(
            IEnumerable<RowBase> items,
            TaskCompletionSource? TaskSource)
        {
            var materializedItems = items.ToImmutableArray();
            var binaryItems = new List<byte[]>();

            foreach (var item in materializedItems)
            {
                item.Validate();

                var text = _rowSerializer.Serialize(item);
                var binaryItem = ASCIIEncoding.ASCII.GetBytes(text);

                binaryItems.Add(binaryItem);
            }
            lock (_lock)
            {
                var newCache = _inMemoryCache;

                foreach (var item in materializedItems)
                {
                    newCache = newCache.AppendItem(item);
                }
                Interlocked.Exchange(ref _inMemoryCache, newCache);
            }
            foreach (var binaryItem in binaryItems)
            {
                _rowQueue.Enqueue(new QueuedRow(DateTime.Now, binaryItem, TaskSource));
            }
        }

        private async Task BackgroundPersistanceAsync(CancellationToken ct)
        {
            while (!_backgroundCompletedSource.Task.IsCompleted)
            {
                if (_rowQueue.TryPeek(out var queueItem))
                {
                    var delta = DateTime.Now - queueItem.EnqueueTime;
                    var waitTime = FLUSH_PERIOD - delta;

                    if (waitTime < MIN_WAIT_PERIOD)
                    {
                        await PersistBatchAsync(ct);
                    }
                    else
                    {   //  Wait for first item to age to about FLUSH_PERIOD
                        await Task.WhenAny(
                            Task.Delay(waitTime, ct),
                            _backgroundCompletedSource.Task);
                    }
                }
                else
                {   //  Wait for an element to pop in
                    await Task.WhenAny(
                        Task.Delay(FLUSH_PERIOD, ct),
                        _backgroundCompletedSource.Task);
                }
                await CleanReleaseSourceTaskQueueAsync();
            }
        }

        private async Task CleanReleaseSourceTaskQueueAsync()
        {
            while (_releaseSourceTaskQueue.TryPeek(out var task) && task.IsCompleted)
            {
                if (_releaseSourceTaskQueue.TryDequeue(out var task2))
                {
                    await task2;
                }
            }
        }

        private async Task PersistBatchAsync(CancellationToken ct)
        {
            using (var bufferStream = new MemoryStream())
            {
                var sources = new List<TaskCompletionSource>();

                while (true)
                {
                    if (!_rowQueue.TryPeek(out var queueItem)
                        || bufferStream.Length + queueItem.Buffer.Length
                            > _currentShardStorage.MaxBufferSize)
                    {   //  Flush buffer stream
                        if (bufferStream.Length == 0)
                        {
                            throw new InvalidDataException("No buffer to append");
                        }
                        if (await _currentShardStorage.AtomicAppendAsync(
                            bufferStream.ToArray(),
                            ct))
                        {   //  Release tasks
                            foreach (var source in sources)
                            {   //  We run it on another thread not to block the persistance
                                _releaseSourceTaskQueue.Enqueue(
                                    Task.Run(() => source.TrySetResult()));
                            }

                            return;
                        }
                        else
                        {   //  New shard
                            ++_currentShardIndex;
                            _currentShardStorage = await _logStorage.OpenWriteLogShardAsync(
                                _currentShardIndex,
                                ct);
                        }
                    }
                    else
                    {   //  Append to buffer stream
                        if (_rowQueue.TryDequeue(out queueItem))
                        {
                            bufferStream.Write(queueItem.Buffer);
                            if (queueItem.RowPersistedSource != null)
                            {
                                sources.Add(queueItem.RowPersistedSource);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                "We dequeue what we just peeked, this shouldn't fail");
                        }
                    }
                }
            }
        }

        #region Persist view
        private static async Task PersistViewAsync(
            Version appVersion,
            LogStorage logStorage,
            RowInMemoryCache cache,
            long lastShard,
            CancellationToken ct)
        {
            var tempBlobOutput = await logStorage.OpenWriteTempLatestViewAsync(ct);
            var tempStorage = tempBlobOutput.AppendStorage;
            var appendBlock = new List<byte>(tempStorage.MaxBufferSize);

            //  Write view headers
            using (var memoryStream = new MemoryStream())
            {   //  Headers for the view
                var versionHeader = new FileVersionHeader(appVersion);
                var viewHeader = new ViewHeader(lastShard);

                JsonSerializer.Serialize(
                    memoryStream,
                    versionHeader,
                    RowJsonContext.Default.FileVersionHeader);
                memoryStream.WriteByte((byte)'\n');
                JsonSerializer.Serialize(
                    memoryStream,
                    viewHeader,
                    RowJsonContext.Default.ViewHeader);
                memoryStream.WriteByte((byte)'\n');
                await AppendLatestViewBlockAsync(tempStorage, memoryStream.ToArray(), ct);
            }
            //  Content of the view
            foreach (var character in StreamCache(cache))
            {
                appendBlock.Add(character);
                if (appendBlock.Count == tempStorage.MaxBufferSize)
                {
                    await AppendLatestViewBlockAsync(tempStorage, appendBlock, ct);
                    appendBlock.Clear();
                }
            }
            //  Push the remainder of content
            if (appendBlock.Any())
            {
                await AppendLatestViewBlockAsync(tempStorage, appendBlock, ct);
            }
            await tempBlobOutput.MoveToPermanentAsync(ct);
        }

        private static async Task AppendLatestViewBlockAsync(
            IAppendStorage tempStorage,
            IEnumerable<byte> appendBlock,
            CancellationToken ct)
        {
            var isSuccess = await tempStorage.AtomicAppendAsync(appendBlock, ct);

            if (!isSuccess)
            {
                throw new InvalidOperationException("Couldn't complete latest view creation");
            }
        }

        private static IEnumerable<byte> StreamCache(RowInMemoryCache cache)
        {
            foreach (var item in cache.GetItems())
            {
                var text = _rowSerializer.Serialize(item);
                var buffer = ASCIIEncoding.UTF8.GetBytes(text);

                foreach (var character in buffer)
                {
                    yield return character;
                }
            }
        }
        #endregion
    }
}