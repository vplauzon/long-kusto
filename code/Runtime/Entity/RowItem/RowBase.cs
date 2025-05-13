using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    /// <summary>
    /// Base class for all row items except <see cref="FileVersionHeader"/>
    /// and <see cref="ViewHeader"/>.
    /// </summary>
    internal abstract class RowBase
    {
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime Updated { get; set; } = DateTime.UtcNow;

        public abstract void Validate();

        public RowBase Clone()
        {
            var clone = (RowBase)MemberwiseClone();

            clone.Updated = DateTime.UtcNow;

            return clone;
        }
    }
}