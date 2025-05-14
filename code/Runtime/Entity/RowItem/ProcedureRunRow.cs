using Runtime.Entity.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    internal class ProcedureRunRow : StateRow<ProcedureRunState, ProcedureRunRow>
    {
        public string OperationId { get; set; } = string.Empty;

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(OperationId))
            {
                throw new InvalidDataException($"{nameof(OperationId)} shouldn't be empty");
            }
        }
    }
}