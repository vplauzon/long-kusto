using Kusto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public class RuntimeGateway
    {
        #region Inner Types
        public record ProcedureOutput(string OperationId, string? StoredResultName);
        #endregion

        public RuntimeGateway(DbClientCache dbClientCache)
        {
        }

        public Task<ProcedureOutput> RunProcedureAsync(string text, Uri databaseUri)
        {
            throw new NotImplementedException();
        }
    }
}