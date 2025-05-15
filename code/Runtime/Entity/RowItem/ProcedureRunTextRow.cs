using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    internal class ProcedureRunTextRow : RowBase
    {
        public string RunOperationId { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(RunOperationId))
            {
                throw new InvalidDataException($"{nameof(RunOperationId)} shouldn't be empty");
            }
            if (string.IsNullOrWhiteSpace(Text))
            {
                throw new InvalidDataException($"{nameof(Text)} shouldn't be empty");
            }
        }
    }
}