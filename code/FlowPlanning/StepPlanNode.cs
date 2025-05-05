using FlowPlanning.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPlanning
{
    internal class StepPlanNode
    {
        public StepPlanNode(StepPlan stepPlan)
        {
            StepPlan = stepPlan;
            DependsOn = ImmutableArray<StepPlanNode>.Empty;
            DependedBy = ImmutableArray<StepPlanNode>.Empty;
            Children = ImmutableArray<StepPlanNode>.Empty;
        }

        public StepPlan StepPlan { get; }

        public IImmutableList<StepPlanNode> DependsOn { get; set; }

        public IImmutableList<StepPlanNode> DependedBy { get; set; }

        public IImmutableList<StepPlanNode> Children { get; set; }

        #region Update Nodes
        public void AddDependency(StepPlanNode node)
        {
            DependsOn = DependsOn.Add(node);
            node.DependedBy = DependedBy.Add(node);
        }

        public void AddChild(StepPlanNode node)
        {
            Children.Add(node);
        }
        #endregion

        #region First Draft
        public static IImmutableList<StepPlanNode> BuildFirstDraft(StatementScript[] statements)
        {
            return BuildFirstDraft(
                statements,
                ImmutableDictionary<string, StepPlanNode>.Empty);
        }

        private static IImmutableList<StepPlanNode> BuildFirstDraft(
            StatementScript[] statements,
            IImmutableDictionary<string, StepPlanNode> accessibleNodes)
        {
            var builder = ImmutableArray<StepPlanNode>.Empty.ToBuilder();

            foreach (var statement in statements)
            {
                StepPlanNode? newNode = null;

                if (statement.InnerStatement.Query != null)
                {
                    if (statement.Prefix.LetIdPrefix != null
                        || statement.Prefix.ReturnPrefix)
                    {   //  We don't plan for queries that aren't referenced
                        newNode = NewQuery(accessibleNodes, statement);
                    }
                }
                else if (statement.InnerStatement.ReferencedIdentifier != null)
                {
                    newNode = NewReferencedIdentifier(accessibleNodes, statement);
                }
                else
                {
                    throw new NotImplementedException();
                }
                if (newNode != null)
                {
                    if (statement.Prefix.LetIdPrefix != null)
                    {
                        accessibleNodes = accessibleNodes.Add(
                            statement.Prefix.LetIdPrefix,
                            newNode!);
                    }
                    builder.Add(newNode);
                }
            }

            return builder.ToImmutableArray();
        }

        private static StepPlanNode NewReferencedIdentifier(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            var referencedId = statement.InnerStatement.ReferencedIdentifier!;
            var stepPlan = new StepPlan(
                statement.Prefix.LetIdPrefix,
                PersistanceMode.Blob,
                null,
                referencedId);
            var stepPlanNode = new StepPlanNode(stepPlan);

            if (!accessibleNodes.TryGetValue(referencedId, out var referencedNode))
            {
                throw new PlanningException(
                    $"Referenced identifier doesn't exist:  '{referencedId}");
            }
            else
            {
                stepPlanNode.AddDependency(referencedNode);
            }

            return stepPlanNode;
        }

        private static StepPlanNode NewQuery(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            var queryPlan = new QueryPlan(statement.InnerStatement.Query!);
            var stepPlan = new StepPlan(
                statement.Prefix.LetIdPrefix!,
                PersistanceMode.Blob,
                queryPlan,
                null);
            var stepPlanNode = new StepPlanNode(stepPlan);

            foreach (var referencedId in statement.InnerStatement.Query!.Using)
            {
                if (!accessibleNodes.TryGetValue(referencedId, out var referencedNode))
                {
                    throw new PlanningException(
                        $"Referenced identifier doesn't exist:  '{referencedId}");
                }
                else
                {
                    stepPlanNode.AddDependency(referencedNode);
                }
            }

            return stepPlanNode;
        }
        #endregion
    }
}