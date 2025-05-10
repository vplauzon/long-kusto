using Azure.Core;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Kusto
{
    internal class KustoProviderCache : IDisposable
    {
        private readonly ConcurrentDictionary<Uri, ICslQueryProvider> _queryProviderMap = new();
        private readonly ConcurrentDictionary<Uri, ICslAdminProvider> _commandProviderMap = new();
        private readonly ConcurrentDictionary<Uri, IKustoQueuedIngestClient> _ingestProviderMap
            = new();
        private readonly TokenCredential _credentials;
        private readonly string _traceApplicationName;

        public KustoProviderCache(
            TokenCredential credentials,
            string traceApplicationName)
        {
            _credentials = credentials;
            _traceApplicationName = traceApplicationName;
        }

        void IDisposable.Dispose()
        {
            var disposables = _commandProviderMap.Values.Cast<IDisposable>()
                .Concat(_queryProviderMap.Values)
                .Concat(_ingestProviderMap.Values);

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public ICslQueryProvider GetQueryProvider(Uri clusterUri)
        {
            if (_queryProviderMap.TryGetValue(clusterUri, out var provider))
            {
                return provider;
            }
            else
            {
                var builder = CreateBuilder(clusterUri);
                var tempProvider = KustoClientFactory.CreateCslQueryProvider(builder);

                _queryProviderMap.TryAdd(clusterUri, tempProvider);

                return _queryProviderMap[clusterUri];
            }
        }

        public ICslAdminProvider GetCommandProvider(Uri clusterUri)
        {
            if (_commandProviderMap.TryGetValue(clusterUri, out var provider))
            {
                return provider;
            }
            else
            {
                var builder = CreateBuilder(clusterUri);
                var tempProvider = KustoClientFactory.CreateCslAdminProvider(builder);

                _commandProviderMap.TryAdd(clusterUri, tempProvider);

                return _commandProviderMap[clusterUri];
            }
        }

        public IKustoQueuedIngestClient GetIngestProvider(Uri clusterUri)
        {
            if (_ingestProviderMap.TryGetValue(clusterUri, out var provider))
            {
                return provider;
            }
            else
            {
                var builder = CreateBuilder(clusterUri);
                var tempProvider = KustoIngestFactory.CreateQueuedIngestClient(builder);

                _ingestProviderMap.TryAdd(clusterUri, tempProvider);

                return _ingestProviderMap[clusterUri];
            }
        }

        private KustoConnectionStringBuilder CreateBuilder(Uri clusterUri)
        {
            var builder = new KustoConnectionStringBuilder(clusterUri.ToString())
                .WithAadAzureTokenCredentialsAuthentication(_credentials);

            builder.ApplicationNameForTracing = _traceApplicationName;

            return builder;
        }
    }
}