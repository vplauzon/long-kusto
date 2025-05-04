using FlowPlanning.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPlanning
{
    internal record FlowPlan(StepPlan[] Steps)
    {
        public static FlowPlan CreatePlan(Script script)
        {
            script.StaticAnalysis();
            script = script.TransformToReferenceReturnOnly();

            //var graph = BuildGraph(script);

            throw new NotImplementedException();
        }
    }
}