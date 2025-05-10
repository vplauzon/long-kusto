using FlowPlanning;
using Kusto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public class ProcedureRuntime
    {
        public ProcedureRuntime(
            DbClientCache dbClientCache,
            FlowPlan flowPlan)
        {
        }

        public async Task RunProcedureAsync(CancellationToken ct)
        {
            await Task.CompletedTask;

            throw new NotImplementedException();
        }
    }
}