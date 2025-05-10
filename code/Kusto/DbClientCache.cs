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

        public async Task<DbQueryClient> GetDbQueryClientAsync(
            Uri clusterUri,
            string database,
            CancellationToken ct)
        {
            var key = new QueueKey(clusterUri, QueueType.Query);

            if (!_queueMap.TryGetValue(key, out var queueValue))
            {
                _queueMap.TryAdd(key, new QueueValue(
                    DateTime.Now,
                    new ExecutionQueue(MAX_CONCURRENT_QUERY)));
            }
            queueValue = _queueMap[key];

            //  We ignore capacity refresh for queries

            var queryProvider = _providerCache.GetQueryProvider(clusterUri);

            await Task.CompletedTask;

            return new DbQueryClient(queryProvider, queueValue.Queue, database);
        }

        public async Task<DbCommandClient> GetDbCommandClientAsync(
            Uri clusterUri,
            string database,
            CancellationToken ct)
        {
            var key = new QueueKey(clusterUri, QueueType.Command);

            if (!_queueMap.TryGetValue(key, out var queueValue))
            {
                _queueMap.TryAdd(key, new QueueValue(
                    DateTime.Now,
                    new ExecutionQueue(MAX_CONCURRENT_COMMAND)));
            }
            queueValue = _queueMap[key];

            //  We ignore capacity refresh for commands

            var commandProvider = _providerCache.GetCommandProvider(clusterUri);

            await Task.CompletedTask;

            return new DbCommandClient(commandProvider, queueValue.Queue, database);
        }

        public async Task<DbStoredQueryClient> GetDbStoredQueryClientAsync(
            Uri clusterUri,
            string database,
            CancellationToken ct)
        {
            var key = new QueueKey(clusterUri, QueueType.StoredQuery);

            if (!_queueMap.TryGetValue(key, out var queueValue))
            {
                _queueMap.TryAdd(key, new QueueValue(
                    DateTime.Now - 2 * REFRESH_CAPACITY_PERIOD,
                    new ExecutionQueue(1)));
            }
            queueValue = _queueMap[key];

            var commandProvider = _providerCache.GetCommandProvider(clusterUri);

            if (queueValue.LastCapacityRefresh <= DateTime.Now - REFRESH_CAPACITY_PERIOD)
            {   //  Refresh capacity
                var capacity = await GetCapacityAsync(commandProvider, "stored-query-results", ct);

                queueValue.Queue.MaxParallelRunCount = capacity;
            }

            return new DbStoredQueryClient(commandProvider, queueValue.Queue, database);
        }

        public async Task<DbExportClient> GetDbExportClientAsync(
            Uri clusterUri,
            string database,
            CancellationToken ct)
        {
            var key = new QueueKey(clusterUri, QueueType.Export);

            if (!_queueMap.TryGetValue(key, out var queueValue))
            {
                _queueMap.TryAdd(key, new QueueValue(
                    DateTime.Now - 2 * REFRESH_CAPACITY_PERIOD,
                    new ExecutionQueue(1)));
            }
            queueValue = _queueMap[key];

            var commandProvider = _providerCache.GetCommandProvider(clusterUri);

            if (queueValue.LastCapacityRefresh <= DateTime.Now - REFRESH_CAPACITY_PERIOD)
            {   //  Refresh capacity
                var capacity = await GetCapacityAsync(commandProvider, "data-export", ct);

                queueValue.Queue.MaxParallelRunCount = capacity;
            }

            return new DbExportClient(commandProvider, queueValue.Queue, database);
        }

        public async Task<DbIngestClient> GetDbIngestClientAsync(
            Uri clusterUri,
            string database,
            string table,
            CancellationToken ct)
        {
            var key = new QueueKey(clusterUri, QueueType.Ingest);

            if (!_queueMap.TryGetValue(key, out var queueValue))
            {
                _queueMap.TryAdd(key, new QueueValue(
                    DateTime.Now,
                    new ExecutionQueue(MAX_CONCURRENT_INGEST_QUEUING)));
            }
            queueValue = _queueMap[key];

            //  We ignore capacity refresh for ingest queuing

            var ingestProvider = _providerCache.GetIngestProvider(clusterUri);

            await Task.CompletedTask;

            return new DbIngestClient(ingestProvider, queueValue.Queue, database, table);
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