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
        private readonly Uri _clusterUri;
        private readonly string _database;
        private readonly DbClientCache _dbClientCache;
        private readonly string _operationId;

        public ProcedureRuntime(
            Uri clusterUri,
            string database,
            DbClientCache dbClientCache,
            string operationId)
        {
            _clusterUri = clusterUri;
            _database = database;
            _dbClientCache = dbClientCache;
            _operationId = operationId;
        }

        /// <summary>
        /// Runs a procedure.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>Stored query result if there was a return value</returns>
        public async Task<string?> RunProcedureAsync(CancellationToken ct)
        {
            await Task.CompletedTask;

            throw new NotImplementedException();
        }
    }
}