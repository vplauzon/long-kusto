using Runtime.Entity.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    internal class ProcedureRunStepRow : StateRow<ProcedureRunStepState, ProcedureRunStepRow>
    {
        public string OperationId { get; set; } = string.Empty;

        public string StepPath { get; set; } = string.Empty;

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(OperationId))
            {
                throw new InvalidDataException($"{nameof(OperationId)} shouldn't be empty");
            }
            if (string.IsNullOrWhiteSpace(StepPath))
            {
                throw new InvalidDataException($"{nameof(StepPath)} shouldn't be empty");
            }

            var invalidParts = StepPath
                .Split('.')
                .Select(part => long.TryParse(part, out var _))
                .Where(v => !v);

            if (invalidParts.Any())
            {
                throw new InvalidDataException(
                    $"{nameof(StepPath)} has invalid parts:  '{StepPath}'");
            }
        }
    }
}