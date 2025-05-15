using FlowPlanning;
using Kusto;
using Runtime.Entity.RowItem;
using Runtime.Entity.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    internal class ProcedureRuntime
    {
        private readonly RowGateway _rowGateway;
        private readonly DbClientCache _dbClientCache;
        private readonly Uri _clusterUri;
        private readonly string _database;
        private readonly string _runOperationId;
        private readonly string _text;
        private readonly FlowPlan _plan;

        public ProcedureRuntime(
            RowGateway rowGateway,
            DbClientCache dbClientCache,
            Uri clusterUri,
            string database,
            string runOperationId)
        {
            _rowGateway = rowGateway;
            _clusterUri = clusterUri;
            _database = database;
            _dbClientCache = dbClientCache;
            _runOperationId = runOperationId;

            if (!_rowGateway.InMemoryCache.ProcedureRunMap.ContainsKey(runOperationId))
            {
                throw new InvalidDataException($"Operation ID '{runOperationId}' doesn't exist");
            }

            var run = _rowGateway.InMemoryCache.ProcedureRunMap[runOperationId];

            if (run.Text == null)
            {
                throw new InvalidDataException($"Operation ID '{runOperationId}' doesn't have text");
            }
            if (run.Plan == null)
            {
                throw new InvalidDataException($"Operation ID '{runOperationId}' doesn't have plan");
            }
            _text = run.Text!.Text;
            _plan = run.Plan!.Plan;
        }

        /// <summary>Runs a procedure.</summary>
        /// <param name="ct"></param>
        /// <returns>Stored query result if there was a return value</returns>
        public async Task<string?> RunProcedureAsync(CancellationToken ct)
        {
            await RunSequenceAsync(string.Empty, _plan.Steps, ct);

            //  Stored result return
            throw new NotImplementedException();
        }

        private async Task RunSequenceAsync(
            string stepPathPrefix,
            IEnumerable<StepPlan> steps,
            CancellationToken ct)
        {
            var index = 0;

            foreach (var step in steps)
            {
                var stepPath = stepPathPrefix == string.Empty
                    ? index.ToString()
                    : $"{stepPathPrefix}.{index}";

                await RunStepAsync(stepPath, step, ct);
                ++index;
            }
        }

        private async Task RunStepAsync(
            string stepPath,
            StepPlan step,
            CancellationToken ct)
        {
            var run = _rowGateway.InMemoryCache.ProcedureRunMap[_runOperationId];
            var stepCache = run.NavigateToStep(stepPath.Split('.').Select(p => long.Parse(p)));
            var stepTemplate = stepCache!=null
                ? stepCache.Row
                : new ProcedureRunStepRow
                {
                    StepPath = stepPath,
                    RunOperationId = _runOperationId
                };

            if (step.ActionPlan.QueryPlan != null)
            {
                await RunQueryPlanAsync(stepTemplate, step, ct);
            }
            else
            {
                throw new NotImplementedException("Action plan");
            }
        }

        private async Task RunQueryPlanAsync(
            ProcedureRunStepRow stepTemplate,
            StepPlan step,
            CancellationToken ct)
        {
            var persistanceMode = step.ActionPlan.QueryPlan!.PersistanceMode;
            var client = await _dbClientCache.GetDbClientAsync(_clusterUri, _database, ct);

            switch (persistanceMode)
            {
                case PersistanceMode.StoredQuery:
                    var output = await client.ExecuteStoredQueryResultAsync(
                        step.ActionPlan.QueryPlan!.Text,
                        ct);
                    var stepRow = stepTemplate.ChangeState(ProcedureRunStepState.Running);

                    stepRow.StoredQueryOperationId = output.OperationId;
                    stepRow.StoredQueryName = output.StoredQueryName;
                    _rowGateway.Append(stepRow);
                    await client.AwaitStoredQueryResultAsync(output.OperationId);
                    stepRow = stepRow.ChangeState(ProcedureRunStepState.Completed);
                    stepRow.StoredQueryOperationId = string.Empty;
                    _rowGateway.Append(stepRow);

                    break;
                case PersistanceMode.Blob:
                default:
                    throw new NotSupportedException(
                        $"{nameof(PersistanceMode)}.{persistanceMode}");
            }
            await Task.CompletedTask;

            throw new NotImplementedException();
        }
    }
}