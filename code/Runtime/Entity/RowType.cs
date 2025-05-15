using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity
{
    internal enum RowType
    {
        FileVersionHeader,
        ViewHeader,
        TransactionBracket,
        ProcedureRun,
        ProcedureRunText,
        ProcedureRunPlan,
        ProcedureRunStep
    }
}