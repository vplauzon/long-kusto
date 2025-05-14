using Azure;
using Azure.Core;
using FlowPlanning;
using Kusto;
using Runtime.Entity.RowItem;
using Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public class RuntimeGateway
    {
        #region Inner Type
        private record RunningProcedureEntry(
            string OperationId,
            Task RunTask,
            CancellationToken ct);
        #endregion

        private readonly DbClientCache _dbClientCache;
        private readonly RowGateway _rowGateway;
        private readonly IDictionary<string, RunningProcedureEntry> _runningProcedureIndex =
            new Dictionary<string, RunningProcedureEntry>();

        #region Constructors
        private RuntimeGateway(
            DbClientCache dbClientCache,
            RowGateway rowGateway)
        {
            _dbClientCache = dbClientCache;
            _rowGateway = rowGateway;
        }

        /// <summary>Factory method hidding all internal dependencies.</summary>>
        /// <param name="credentials"></param>
        /// <param name="appVersion"></param>
        /// <param name="traceApplicationName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<RuntimeGateway> CreateAsync(
            TokenCredential credentials,
            Version appVersion,
            string traceApplicationName,
            string dataLakeRootUrl,
            CancellationToken ct)
        {
            var fileSystem = new AzureBlobFileSystem(dataLakeRootUrl, credentials);
            var logStorage = await LogStorage.CreateAsync(fileSystem, ct);

            return new RuntimeGateway(
                new DbClientCache(credentials, traceApplicationName),
                await RowGateway.CreateAsync(appVersion, logStorage, ct));
        }
        #endregion

        public ProcedureOutput<string?> RunProcedure(string text, Uri databaseUri)
        {
            (var clusterUri, var database) = ExtractClusterAndDatabase(databaseUri);
            var plan = FlowPlan.CreatePlan(text);
            var operationId = Guid.NewGuid().ToString();
            var procedureRunRow = new ProcedureRunRow
            {
                OperationId = operationId
            };
            var procedureRunTextRow = new ProcedureRunTextRow
            {
                OperationId = operationId,
                Text = text
            };
            var procedureRunPlanRow = new ProcedureRunPlanRow
            {
                OperationId = operationId,
                Plan = plan
            };

            _rowGateway.Append([procedureRunRow, procedureRunTextRow, procedureRunPlanRow]);

            var procedureRuntime = new ProcedureRuntime(
                _rowGateway,
                _dbClientCache,
                clusterUri,
                database,
                operationId);
            var ct = new CancellationToken();
            var runTask = Task.Run(() => procedureRuntime.RunProcedureAsync(ct), ct);

            lock (_runningProcedureIndex)
            {
                _runningProcedureIndex.Add(
                    operationId,
                    new RunningProcedureEntry(operationId, runTask, ct));
            }

            return new ProcedureOutput<string?>(operationId, runTask);
        }

        private static (Uri ClusterUri, string Database) ExtractClusterAndDatabase(
            Uri databaseUri)
        {
            var databaseUriBuilder = new UriBuilder(databaseUri);
            var database = databaseUriBuilder.Path.Trim('/');

            databaseUriBuilder.Path = string.Empty;

            var clusterUri = databaseUriBuilder.Uri;

            return (clusterUri, database);
        }
    }
}