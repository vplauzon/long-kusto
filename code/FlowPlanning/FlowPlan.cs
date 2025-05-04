using FlowPlanning.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            var firstDraft = StepPlanNode.BuildFirstDraft(script.Statements);
            var steps = firstDraft
                .Select(n => n.StepPlan)
                .ToArray();
            var flowPlan = new FlowPlan(steps);

            return flowPlan;
        }
    }
}