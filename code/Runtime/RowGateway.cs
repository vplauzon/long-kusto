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
        private record QueuedContent(
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
        private readonly ConcurrentQueue<QueuedContent> _contentQueue = new();
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
            var currentShardStorage =
                await NewShardAsync(logStorage, appVersion, lastShard + 1, ct);

            return new RowGateway(
                appVersion,
                logStorage,
                lastShard + 1,
                currentShardStorage,
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
                .AddType<ProcedureRunTextRow>(RowType.ProcedureRunText)
                .AddType<ProcedureRunPlanRow>(RowType.ProcedureRunPlan)
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

        #region Append
        /// <summary>Appends many items atomically.</summary>
        /// <param name="items"></param>
        public void Append(params IEnumerable<RowBase> items)
        {
            var materializedItems = items.ToImmutableArray();

            if (materializedItems.Any())
            {
                AppendInternal(items.ToImmutableArray(), null);
            }
        }

        /// <summary>Appends many items atomically.</summary>
        /// <param name="ct"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task AppendAndPersistAsync(
            CancellationToken ct,
            params IEnumerable<RowBase> items)
        {
            var materializedItems = items.ToImmutableArray();

            if (materializedItems.Any())
            {
                var taskSource = new TaskCompletionSource();

                AppendInternal(materializedItems, taskSource);
                await taskSource.Task;
            }
        }
        private void AppendInternal(
            IImmutableList<RowBase> items,
            TaskCompletionSource? TaskSource)
        {
            var buffers = items
                .Select(i =>
                {
                    i.Validate();

                    var text = _rowSerializer.Serialize(i);
                    var binaryItem = ASCIIEncoding.ASCII.GetBytes(text);

                    if (binaryItem.Length > _currentShardStorage.MaxBufferSize)
                    {
                        throw new InvalidDataException(
                            $"Row bigger than maximum buffer size:  {binaryItem.Length} " +
                            $"in '{text}'");
                    }
                    return binaryItem;
                })
                .ToImmutableArray();
            var atomicBuffers = MakeAtomicContent(buffers);

            lock (_lock)
            {
                var newCache = _inMemoryCache;

                foreach (var item in items)
                {
                    newCache = newCache.AppendItem(item);
                }
                Interlocked.Exchange(ref _inMemoryCache, newCache);
                //  Enqueuing is done under lock for atomicity
                foreach (var buffer in atomicBuffers)
                {
                    _contentQueue.Enqueue(new QueuedContent(DateTime.Now, buffer, TaskSource));
                }
            }
        }

        private IImmutableList<byte[]> MakeAtomicContent(IImmutableList<byte[]> binaryItems)
        {
            if (binaryItems.Count > 1)
            {   //  Only one item:  already is atomic
                return binaryItems;
            }
            else
            {   //  Wrap in transaction
                var transactionId = Guid.NewGuid().ToString();
                var binaryOpen = ASCIIEncoding.ASCII.GetBytes(
                    _rowSerializer.Serialize(new TransactionBracket(transactionId, true)));
                var binaryClose = ASCIIEncoding.ASCII.GetBytes(
                    _rowSerializer.Serialize(new TransactionBracket(transactionId, false)));

                return binaryItems
                    .Prepend(binaryOpen)
                    .Append(binaryClose)
                    .ToImmutableArray();
            }
        }
        #endregion

        #region Background persistance
        private async Task BackgroundPersistanceAsync(CancellationToken ct)
        {
            while (!_backgroundCompletedSource.Task.IsCompleted)
            {
                if (_contentQueue.TryPeek(out var queueItem))
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
                    if (!_contentQueue.TryPeek(out var content)
                        || bufferStream.Length + content.Buffer.Length
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
                            _currentShardStorage = await NewShardAsync(
                                _logStorage,
                                _appVersion,
                                ++_currentShardIndex,
                                ct);
                        }
                    }
                    else
                    {   //  Append to buffer stream
                        if (_contentQueue.TryDequeue(out content))
                        {
                            bufferStream.Write(content.Buffer);
                            if (content.RowPersistedSource != null)
                            {
                                sources.Add(content.RowPersistedSource);
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
        #endregion

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

        private static async Task<IAppendStorage> NewShardAsync(
            LogStorage logStorage,
            Version appVersion,
            long shardIndex,
            CancellationToken ct)
        {
            var newShardStorage = await logStorage.OpenWriteLogShardAsync(shardIndex, ct);
            var versionHeader = new FileVersionHeader(appVersion);

            using (var memoryStream = new MemoryStream())
            {
                JsonSerializer.Serialize(
                    memoryStream,
                    versionHeader,
                    RowJsonContext.Default.FileVersionHeader);
                memoryStream.WriteByte((byte)'\n');
                await newShardStorage.AtomicAppendAsync(memoryStream.ToArray(), ct);
            }

            return newShardStorage;
        }
    }
}