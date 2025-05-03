using QueryPlan.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryPlan.Planning
{
    internal record Plan
    {
        public static Plan CreatePlan(Script script)
        {
            script.StaticAnalysis();

            throw new NotImplementedException();
        }
    }
}