using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPlanning
{
    public class PlanningException : Exception
    {
        public PlanningException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}