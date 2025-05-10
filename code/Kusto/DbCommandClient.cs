using Kusto.Data.Common;
using System.Collections.Immutable;
using System.Data;

namespace Kusto
{
    internal class DbCommandClient
    {
        private readonly ICslAdminProvider _provider;
        private readonly ExecutionQueue _commandQueue;

        public DbCommandClient(
            ICslAdminProvider provider,
            ExecutionQueue commandQueue,
            string databaseName)
        {
            _provider = provider;
            _commandQueue = commandQueue;
            DatabaseName = databaseName;
        }

        public string DatabaseName { get; }

        public async Task StoreQueryResultAsync(
            string queryText,
            CancellationToken ct)
        {
            await _commandQueue.RequestRunAsync(
                async () =>
                {
                    var commandText = @$"
.show capacity
| where Resource == 'DataExport'
| project Total";
                    var reader = await _provider.ExecuteControlCommandAsync(
                        DatabaseName,
                        commandText);
                    var result = reader
                        .ToEnumerable(r => (long)r[0])
                        .First();

                    return (int)result;
                });
        }

        public async Task ExportResultAsync(
            string queryText,
            CancellationToken ct)
        {
            await _commandQueue.RequestRunAsync(
                async () =>
                {
                    var commandText = @$"
.show capacity
| where Resource == 'DataExport'
| project Total";
                    var reader = await _provider.ExecuteControlCommandAsync(
                        DatabaseName,
                        commandText);
                    var result = reader
                        .ToEnumerable(r => (long)r[0])
                        .First();

                    return (int)result;
                });
        }
    }
}