using Kusto.Data.Common;
using Kusto.Ingest;
using System.Collections.Immutable;

namespace Kusto
{
    public class DbIngestClient
    {
        private static readonly IImmutableList<Status> FAILED_STATUS = [
            Status.Skipped,
            Status.Failed,
            Status.PartiallySucceeded];

        private readonly IKustoQueuedIngestClient _ingestProvider;
        private readonly ExecutionQueue _executionQueue;
        private readonly string _database;
        private readonly string _table;

        public DbIngestClient(
            IKustoQueuedIngestClient ingestProvider,
            ExecutionQueue executionQueue,
            string database,
            string table)
        {
            _ingestProvider = ingestProvider;
            _executionQueue = executionQueue;
            _database = database;
            _table = table;
        }

        public async Task<string> QueueBlobAsync(
            Uri blobPath,
            DateTime? creationTime,
            CancellationToken ct)
        {
            var properties = new KustoQueuedIngestionProperties(_database, _table)
            {
                Format = DataSourceFormat.parquet,
                ReportLevel = IngestionReportLevel.FailuresAndSuccesses,
                ReportMethod = IngestionReportMethod.Table
            };

            if (creationTime != null)
            {
                properties.AdditionalProperties.Add(
                    "creationTime",
                    creationTime.Value.ToString("o"));
            }

            return await _executionQueue.RequestRunAsync(
                async () =>
                {
                    var ingestionResult = await _ingestProvider.IngestFromStorageAsync(
                        blobPath.ToString(),
                        properties);
                    var serializedResult = IngestionResultSerializer.Serialize(ingestionResult);

                    return serializedResult;
                });
        }

        public async Task<IngestionFailureDetail?> FetchIngestionFailureAsync(
            string serializedQueuedResult)
        {
            var ingestionResult = IngestionResultSerializer.Deserialize(serializedQueuedResult);
            var status = ingestionResult.GetIngestionStatusCollection();

            await Task.CompletedTask;
            if (status.Count() != 1)
            {
                throw new InvalidOperationException(
                    $"Status count was expected to be 1 but is {status.Count()}");
            }

            var firstStatus = status.First();

            if (FAILED_STATUS.Contains(firstStatus.Status)
                && firstStatus.FailureStatus != FailureStatus.Transient)
            {
                return new IngestionFailureDetail(
                    firstStatus.Status.ToString(),
                    firstStatus.Details);
            }
            else
            {
                return null;
            }
        }
    }
}