using FlowPlanning.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPlanning
{
    internal record FlowPlan(StepPlan[] StepPlans)
    {
        public static FlowPlan CreatePlan(Script script)
        {
            script.StaticAnalysis();

            throw new NotImplementedException();
        }
    }
}