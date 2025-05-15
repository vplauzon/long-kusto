using Kusto.Data.Common;

namespace Kusto
{
    public class DbStoredQueryClient
    {
        private readonly ICslAdminProvider _provider;
        private readonly ExecutionQueue _storedQueryQueue;

        public DbStoredQueryClient(
            ICslAdminProvider provider,
            ExecutionQueue storedQueryQueue,
            string databaseName)
        {
            _provider = provider;
            _storedQueryQueue = storedQueryQueue;
            DatabaseName = databaseName;
        }

        public string DatabaseName { get; }

        public async Task<string> ExecuteQueryAsync(string text, CancellationToken ct)
        {
            return await _storedQueryQueue.RequestRunAsync(
                async () =>
                {
                    var storedQueryResultName = $"lk_sqr_{Guid.NewGuid().ToString("N")}";
                    var commandText = $@"
.set async stored_query_result {storedQueryResultName} <|
{text}
";
                    var reader = await _provider.ExecuteControlCommandAsync(
                        DatabaseName,
                        commandText);
                    var operationId = reader
                    .ToEnumerable(r => (Guid)r[0])
                    .First()
                    .ToString();

                    return storedQueryResultName;
                });
        }
    }
}