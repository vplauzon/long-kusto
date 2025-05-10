using Kusto.Data.Common;
using System.Collections.Immutable;
using System.Data;

namespace Kusto
{
    public class DbQueryClient
    {
        private static readonly ClientRequestProperties EMPTY_PROPERTIES =
            new ClientRequestProperties();
        private readonly ICslQueryProvider _provider;
        private readonly ExecutionQueue _queryQueue;
        private readonly string _databaseName;

        public DbQueryClient(
            ICslQueryProvider provider,
            ExecutionQueue queryQueue,
            string databaseName)
        {
            _provider = provider;
            _queryQueue = queryQueue;
            _databaseName = databaseName;
        }

        public async Task<object> QueryScalarAsync(string queryText, CancellationToken ct)
        {
            return await _queryQueue.RequestRunAsync(
                async () =>
                {
                    var reader = await _provider.ExecuteQueryAsync(
                        _databaseName,
                        queryText,
                        EMPTY_PROPERTIES,
                        ct);
                    var scalar = reader
                        .ToEnumerable(r => r[0])
                        .FirstOrDefault();

                    return scalar!;
                });
        }
    }
}