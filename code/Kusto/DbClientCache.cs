using Azure.Core;
using Kusto.Cloud.Platform.Data;
using Kusto.Data.Common;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;

namespace Kusto
{
    public class DbClientCache : IDisposable
    {
        #region Inner Types
        private enum QueueType
        {
            Query,
            Command,
            Export,
            StoredQuery,
            Ingest
        }

        private record QueueKey(Uri ClusterUri, QueueType QueueType);

        private record QueueValue(DateTime LastCapacityRefresh, ExecutionQueue Queue);
        #endregion

        private const int MAX_CONCURRENT_QUERY = 5;
        private const int MAX_CONCURRENT_COMMAND = 5;
        private const int MAX_CONCURRENT_INGEST_QUEUING = 25;
        private static readonly TimeSpan REFRESH_CAPACITY_PERIOD = TimeSpan.FromMinutes(10);

        private readonly KustoProviderCache _providerCache;
        private readonly ConcurrentDictionary<QueueKey, QueueValue> _queueMap = new();

        public DbClientCache(TokenCredential credentials, string traceApplicationName)
        {
            _providerCache = new KustoProviderCache(credentials, traceApplicationName);
        }

        void IDisposable.Dispose()
        {
            ((IDisposable)_providerCache).Dispose();
        }

        public async Task<DbClient> GetDbClientAsync(
            Uri clusterUri,
            string database,
            CancellationToken ct)
        {   //  Fetch the queues for various queue type
            var queueMap = new[]
            {
                QueueType.Query,
                QueueType.Command,
                QueueType.StoredQuery,
                QueueType.Export
            }
            .Select(qt =>
            {
                var key = new QueueKey(clusterUri, qt);

                if (!_queueMap.TryGetValue(key, out var queueValue))
                {
                    _queueMap.TryAdd(key, new QueueValue(
                        DateTime.Now,
                        new ExecutionQueue(MAX_CONCURRENT_QUERY)));
                }
                queueValue = _queueMap[key];

                return (QueueType: qt, QueueValue: queueValue);
            })
            .ToImmutableDictionary(p => p.QueueType, p => p.QueueValue);
            var queryProvider = _providerCache.GetQueryProvider(clusterUri);
            var commandProvider = _providerCache.GetCommandProvider(clusterUri);

            //  We ignore capacity refresh for queries & commands

            if (queueMap[QueueType.StoredQuery].LastCapacityRefresh
                <= DateTime.Now - REFRESH_CAPACITY_PERIOD)
            {   //  Refresh capacity
                var capacity =
                    await GetCapacityAsync(commandProvider, "stored-query-results", ct);

                queueMap[QueueType.StoredQuery].Queue.MaxParallelRunCount = capacity;
            }
            if (queueMap[QueueType.Export].LastCapacityRefresh
                <= DateTime.Now - REFRESH_CAPACITY_PERIOD)
            {   //  Refresh capacity
                var capacity = await GetCapacityAsync(commandProvider, "data-export", ct);

                queueMap[QueueType.Export].Queue.MaxParallelRunCount = capacity;
            }

            return new DbClient(
                database,
                queryProvider,
                commandProvider,
                queueMap[QueueType.Query].Queue,
                queueMap[QueueType.Command].Queue,
                queueMap[QueueType.Export].Queue,
                queueMap[QueueType.StoredQuery].Queue);
        }

        public async Task<DbIngestClient> GetDbIngestClientAsync(
            Uri clusterUri,
            string database,
            string table,
            CancellationToken ct)
        {
            var ingestQueueKey = new QueueKey(clusterUri, QueueType.Ingest);

            if (!_queueMap.TryGetValue(ingestQueueKey, out var ingestQueueValue))
            {
                _queueMap.TryAdd(ingestQueueKey, new QueueValue(
                    DateTime.Now,
                    new ExecutionQueue(MAX_CONCURRENT_INGEST_QUEUING)));
            }
            ingestQueueValue = _queueMap[ingestQueueKey];

            //  We ignore capacity refresh for ingest queuing

            var ingestProvider = _providerCache.GetIngestProvider(clusterUri);

            await Task.CompletedTask;

            return new DbIngestClient(ingestProvider, ingestQueueValue.Queue, database, table);
        }

        private static async Task<int> GetCapacityAsync(
            ICslAdminProvider provider,
            string capacityName,
            CancellationToken ct)
        {
            var commandText = @$"
.show capacity {capacityName}
| project Total";
            var reader = await provider.ExecuteControlCommandAsync(string.Empty, commandText);
            var capacity = reader.GetScalar<long>();

            return (int)capacity;
        }
    }
}