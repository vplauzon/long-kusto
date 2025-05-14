using FlowPlanning;
using Kusto;
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
        private readonly string _operationId;
        private readonly string _text;
        private readonly FlowPlan _plan;

        public ProcedureRuntime(
            RowGateway rowGateway,
            DbClientCache dbClientCache,
            Uri clusterUri,
            string database,
            string operationId)
        {
            _rowGateway = rowGateway;
            _clusterUri = clusterUri;
            _database = database;
            _dbClientCache = dbClientCache;
            _operationId = operationId;

            if (!_rowGateway.InMemoryCache.ProcedureRunMap.ContainsKey(operationId))
            {
                throw new InvalidDataException($"Operation ID '{operationId}' doesn't exist");
            }

            var run = _rowGateway.InMemoryCache.ProcedureRunMap[operationId];

            if (run.Text == null)
            {
                throw new InvalidDataException($"Operation ID '{operationId}' doesn't have text");
            }
            if (run.Plan == null)
            {
                throw new InvalidDataException($"Operation ID '{operationId}' doesn't have plan");
            }
            _text = run.Text!.Text;
            _plan = run.Plan!.Plan;
        }

        /// <summary>Runs a procedure.</summary>
        /// <param name="ct"></param>
        /// <returns>Stored query result if there was a return value</returns>
        public async Task<string?> RunProcedureAsync(CancellationToken ct)
        {
            await Task.CompletedTask;

            throw new NotImplementedException();
        }
    }
}