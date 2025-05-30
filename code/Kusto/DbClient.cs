﻿using Kusto.Data.Common;
using System.Collections.Immutable;
using System.Data;

namespace Kusto
{
    public class DbClient
    {
        private static readonly ClientRequestProperties EMPTY_PROPERTIES = new();

        private readonly ICslQueryProvider _queryProvider;
        private readonly ICslAdminProvider _commandProvider;
        private readonly ExecutionQueue _queryQueue;
        private readonly ExecutionQueue _storedQueryQueue;
        private readonly ExecutionQueue _exportQueue;
        private readonly ExecutionQueue _commandQueue;
        private readonly OperationAwaiter _operationAwaiter;

        public DbClient(
            string databaseName,
            ICslQueryProvider queryProvider,
            ICslAdminProvider commandProvider,
            ExecutionQueue queryQueue,
            ExecutionQueue storedQueryQueue,
            ExecutionQueue exportQueue,
            ExecutionQueue commandQueue,
            OperationAwaiter operationAwaiter)
        {
            DatabaseName = databaseName;
            _queryProvider = queryProvider;
            _commandProvider = commandProvider;
            _queryQueue = queryQueue;
            _storedQueryQueue = storedQueryQueue;
            _exportQueue = exportQueue;
            _commandQueue = commandQueue;
            _operationAwaiter = operationAwaiter;
        }

        public string DatabaseName { get; }

        public async Task<object> QueryScalarAsync(string queryText, CancellationToken ct)
        {
            return await _queryQueue.RequestRunAsync(
                async () =>
                {
                    var reader = await _queryProvider.ExecuteQueryAsync(
                        DatabaseName,
                        queryText,
                        EMPTY_PROPERTIES,
                        ct);
                    var scalar = reader
                        .ToEnumerable(r => r[0])
                        .FirstOrDefault();

                    return scalar!;
                });
        }

        public async Task<StoredQueryOutput> ExecuteStoredQueryResultAsync(
            string text,
            CancellationToken ct)
        {
            return await _storedQueryQueue.RequestRunAsync(
                async () =>
                {
                    var storedQueryResultName = $"lk_sqr_{Guid.NewGuid().ToString("N")}";
                    var commandText = $@"
.set async stored_query_result {storedQueryResultName} <|
{text}
";
                    var reader = await _commandProvider.ExecuteControlCommandAsync(
                        DatabaseName,
                        commandText);
                    var operationId = reader
                    .ToEnumerable(r => (Guid)r[0])
                    .First()
                    .ToString();

                    return new StoredQueryOutput(operationId, storedQueryResultName);
                });
        }

        public async Task AwaitStoredQueryResultAsync(string operationId)
        {
            await _operationAwaiter.AwaitOperationCompletionAsync(operationId);
        }
    }
}