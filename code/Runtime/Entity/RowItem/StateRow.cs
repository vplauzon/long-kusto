using Kusto.Cloud.Platform.Utils;
using Runtime.Entity.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    internal abstract class StateRow<ST, T> : RowBase
        where ST : struct, Enum
        where T : StateRow<ST, T>
    {
        public ST State { get; set; }

        public T ChangeState(ST newState)
        {
            var clone = (T)Clone();

            clone.State = newState;

            return clone;
        }
    }
}