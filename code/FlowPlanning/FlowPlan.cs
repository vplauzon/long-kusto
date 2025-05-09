using FlowPlanning.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPlanning
{
    public record FlowPlan(StepPlan[] Steps)
    {
        public static FlowPlan CreatePlan(string text)
        {
            return CreatePlan(ScriptParser.ParseScript(text));
        }

        internal static FlowPlan CreatePlan(Script script)
        {
            script.StaticAnalysis();
            script = script.TransformToReferenceReturnOnly();

            var draft = StepPlanNode.BuildFirstDraft(script.Statements);

            draft.CleanUnreferencedReadonlySteps();
            draft.MakeReturnStoredQuery();
            draft.AssignChildrenPlans();

            var steps = draft.Children
                .Select(n => n.StepPlan)
                .ToArray();
            var flowPlan = new FlowPlan(steps);

            return flowPlan;
        }
    }
}