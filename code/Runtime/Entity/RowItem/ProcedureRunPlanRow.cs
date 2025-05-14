using FlowPlanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    internal class ProcedureRunPlanRow : RowBase
    {
        public string OperationId { get; set; } = string.Empty;

        public FlowPlan Plan { get; set; } = new FlowPlan(Array.Empty<StepPlan>());

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(OperationId))
            {
                throw new InvalidDataException($"{nameof(OperationId)} shouldn't be empty");
            }
            if (!Plan.Steps.Any())
            {
                throw new InvalidDataException($"{nameof(Plan)} shouldn't be empty");
            }
        }
    }
}