﻿using FlowPlanning.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlowPlanning
{
    internal class StepPlanNode
    {
        private StepPlanNode? _parent = null;

        public StepPlanNode(StepPlan stepPlan)
        {
            StepPlan = stepPlan;
            DependsOn = ImmutableArray<StepPlanNode>.Empty;
            DependedBy = ImmutableArray<StepPlanNode>.Empty;
            Children = ImmutableArray<StepPlanNode>.Empty;
        }

        public StepPlan StepPlan { get; private set; }

        public IImmutableList<StepPlanNode> DependsOn { get; private set; }

        public IImmutableList<StepPlanNode> DependedBy { get; private set; }

        public IImmutableList<StepPlanNode> Children { get; private set; }

        public IEnumerable<StepPlanNode> AllRecursiveChildren => ListAllRecursiveChildren();

        public IEnumerable<StepPlanNode> AllRecursiveDependedBy => ListAllRecursiveDependedBy();

        #region Recursive gets
        private IEnumerable<StepPlanNode> ListAllRecursiveChildren()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var subChild in child.ListAllRecursiveChildren())
                {
                    yield return subChild;
                }
            }
        }

        private IEnumerable<StepPlanNode> ListAllRecursiveDependedBy()
        {
            foreach (var node in DependedBy)
            {
                yield return node;
                foreach (var subNode in node.ListAllRecursiveDependedBy())
                {
                    yield return subNode;
                }
            }
        }
        #endregion

        #region Update Nodes
        public void UpdatePlan(StepPlan stepPlan)
        {
            StepPlan = stepPlan;
        }

        public void AddDependencies(params IEnumerable<StepPlanNode> dependantNodes)
        {
            DependsOn = DependsOn.AddRange(dependantNodes);
            foreach (var dependantNode in dependantNodes)
            {
                dependantNode.DependedBy = DependedBy.Add(this);
            }
        }

        public void AddChildren(params IEnumerable<StepPlanNode> childrenNodes)
        {
            Children = Children.AddRange(childrenNodes);
            foreach (var childNode in childrenNodes)
            {
                childNode._parent = this;
            }
        }
        #endregion

        #region Remove Node
        public void Remove()
        {
            foreach (var node in DependedBy)
            {
                node.DependsOn = node.DependsOn.Remove(this);
            }
            foreach (var node in DependsOn)
            {
                node.DependedBy = node.DependedBy.Remove(this);
            }
            foreach (var child in Children.ToArray())
            {
                child.Remove();
            }
            if (_parent != null)
            {
                _parent.Children = _parent.Children.Remove(this);
            }
        }
        #endregion

        #region First Draft
        public static StepPlanNode BuildFirstDraft(StatementScript[] statements)
        {
            var draftFirstLevelNodes = BuildFirstDraft(
                statements,
                ImmutableDictionary<string, StepPlanNode>.Empty);
            var draftRootNode = new StepPlanNode(new StepPlan("$root", new ActionPlan()));

            draftRootNode.AddChildren(draftFirstLevelNodes);

            return draftRootNode;
        }

        private static IEnumerable<StepPlanNode> BuildFirstDraft(
            StatementScript[] statements,
            IImmutableDictionary<string, StepPlanNode> accessibleNodes)
        {
            var nodes = new List<StepPlanNode>();

            foreach (var statement in statements)
            {
                StepPlanNode? newNode = null;

                if (statement.InnerStatement.Query != null)
                {
                    newNode = NewQuery(accessibleNodes, statement);
                }
                else if (statement.InnerStatement.Union != null)
                {
                    newNode = NewUnion(accessibleNodes, statement);
                }
                else if (statement.InnerStatement.ShowCommand != null)
                {
                    newNode = NewShowCommand(accessibleNodes, statement);
                }
                else if (statement.InnerStatement.Command != null)
                {
                    newNode = NewCommand(accessibleNodes, statement);
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
                    nodes.Add(newNode);
                }
            }

            return nodes;
        }

        private static StepPlanNode NewQuery(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            var queryPlan = new QueryPlan(
                statement.InnerStatement.Query!.Text,
                GetKustoType(statement.InnerStatement.Query!.Type),
                statement.InnerStatement.Query!.Using);
            var stepPlan = new StepPlan(
                statement.Prefix.LetIdPrefix!,
                new ActionPlan(QueryPlan: queryPlan));
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
                    stepPlanNode.AddDependencies(referencedNode);
                }
            }

            return stepPlanNode;
        }

        private static StepPlanNode? NewUnion(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            var concurrency = PropertyAssignationScript.GetLongProperty(
                statement.InnerStatement.Union!.Properties,
                "concurrency");
            var resultSet = statement.InnerStatement.Union!.ResultSet;

            if (!accessibleNodes.TryGetValue(resultSet, out var referencedNode))
            {
                throw new PlanningException(
                    $"Referenced identifier doesn't exist:  '{resultSet}");
            }
            else
            {
                var childrenNodes = BuildFirstDraft(
                    statement.InnerStatement.Union!.Statements,
                    //  Add iterator as an accessible dummy node
                    accessibleNodes.Add(
                        statement.InnerStatement.Union!.Iterator,
                        new StepPlanNode(new StepPlan(
                            statement.InnerStatement.Union!.Iterator,
                            new ActionPlan()))));
                var unionPlan = new UnionPlan(
                    statement.InnerStatement.Union!.Iterator,
                    resultSet,
                    GetKustoType(statement.InnerStatement.Union!.Type)!.Value,
                    concurrency,
                    Array.Empty<StepPlan>());
                var stepPlan = new StepPlan(
                    statement.Prefix.LetIdPrefix!,
                    new ActionPlan(UnionPlan: unionPlan));
                var stepPlanNode = new StepPlanNode(stepPlan);

                stepPlanNode.AddDependencies(referencedNode);
                stepPlanNode.AddChildren(childrenNodes);

                return stepPlanNode;
            }
        }

        private static KustoType? GetKustoType(string? type)
        {
            switch (type)
            {
                case null:
                    return null;
                case "dynamic":
                    return KustoType.Dynamic;
                case "string":
                    return KustoType.String;
                case "long":
                    return KustoType.Long;

                default:
                    throw new NotSupportedException($"Kusto type '{type}'");
            }
        }

        private static StepPlanNode? NewShowCommand(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            var showCommandPlan = new ShowCommandPlan(statement.InnerStatement.ShowCommand!.Text);
            var stepPlan = new StepPlan(
                statement.Prefix.LetIdPrefix!,
                new ActionPlan(ShowCommandPlan: showCommandPlan));
            var stepPlanNode = new StepPlanNode(stepPlan);

            return stepPlanNode;
        }

        private static StepPlanNode? NewCommand(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            CommandPlan commandPlan = new CommandPlan(statement.InnerStatement.Command!.Text);
            var stepPlan = new StepPlan(
                statement.Prefix.LetIdPrefix!,
                new ActionPlan(CommandPlan: commandPlan));
            var stepPlanNode = new StepPlanNode(stepPlan);

            return stepPlanNode;
        }

        private static StepPlanNode NewReferencedIdentifier(
            IImmutableDictionary<string, StepPlanNode> accessibleNodes,
            StatementScript statement)
        {
            var referencedId = statement.InnerStatement.ReferencedIdentifier!;

            if (!accessibleNodes.TryGetValue(referencedId, out var referencedNode))
            {
                throw new PlanningException(
                    $"Referenced identifier doesn't exist:  '{referencedId}");
            }
            else
            {
                var stepPlan = statement.Prefix.ReturnPrefix
                    ? new StepPlan("$return", new ActionPlan(ReturnIdReference: referencedId))
                    : new StepPlan(
                        statement.Prefix.LetIdPrefix!,
                        new ActionPlan(IdReference: referencedId));
                var stepPlanNode = new StepPlanNode(stepPlan);

                stepPlanNode.AddDependencies(referencedNode);

                return stepPlanNode;
            }
        }
        #endregion

        #region Clean unreferenced read only steps
        /// <summary>
        /// We keep only the nodes that is a return or from which the return depends
        /// (directly or not) upon or non-readonly nodes depend on it.
        /// </summary>
        public void CleanUnreferencedReadonlySteps()
        {   //  Initialize with the return and with all non-readonly steps
            var nodesKept = new HashSet<StepPlanNode>(AllRecursiveChildren
                .Where(n => n.StepPlan.ActionPlan.ReturnIdReference != null
                || !n.StepPlan.IsReadOnly));

            foreach (var readOnlyNode in AllRecursiveChildren.Where(n => n.StepPlan.IsReadOnly))
            {
                var anyDependencyKept = readOnlyNode.AllRecursiveDependedBy
                    .Where(nodesKept.Contains)
                    .Any();

                if (anyDependencyKept)
                {
                    nodesKept.Add(readOnlyNode);
                }
            }

            var nodesToRemove = AllRecursiveChildren
                .Where(n => !nodesKept.Contains(n));

            foreach (var nodeToRemove in nodesToRemove)
            {
                nodeToRemove.Remove();
            }
        }
        #endregion

        #region MakeReturnStoredQuery
        public void MakeReturnStoredQuery()
        {
            var returnNode = Children.LastOrDefault();

            if (returnNode != null && returnNode.StepPlan.ActionPlan.ReturnIdReference != null)
            {
                var currentNode = returnNode;

                while (true)
                {
                    if (currentNode.StepPlan.ActionPlan.ReturnIdReference != null)
                    {   //  Climb back the dependency graph
                        currentNode = currentNode.DependsOn.First();
                    }
                    else if (currentNode.StepPlan.ActionPlan.IdReference != null)
                    {   //  Climb back the dependency graph
                        currentNode = currentNode.DependsOn.First();
                    }
                    else if (currentNode.StepPlan.ActionPlan.UnionPlan != null)
                    {
                        MaterializeUnion(returnNode, currentNode);

                        return;
                    }
                    else if (currentNode.StepPlan.ActionPlan.QueryPlan != null)
                    {   //  Switch the persistency to stored query
                        currentNode.UpdatePlan(new StepPlan(
                            currentNode.StepPlan.Id,
                            new ActionPlan(
                                QueryPlan: currentNode.StepPlan.ActionPlan.QueryPlan.ToStoredQuery())));

                        return;
                    }
                    else if (currentNode.StepPlan.ActionPlan.ShowCommandPlan != null)
                    {   //  Switch the persistency to stored query
                        currentNode.UpdatePlan(new StepPlan(
                            currentNode.StepPlan.Id,
                            new ActionPlan(
                                ShowCommandPlan: currentNode.StepPlan.ActionPlan.ShowCommandPlan.ToStoredQuery())));

                        return;
                    }
                    else if (currentNode.StepPlan.ActionPlan.CommandPlan != null)
                    {   //  Switch the persistency to stored query
                        currentNode.UpdatePlan(new StepPlan(
                            currentNode.StepPlan.Id,
                            new ActionPlan(
                                CommandPlan: currentNode.StepPlan.ActionPlan.CommandPlan.ToStoredQuery())));

                        return;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }

        private void MaterializeUnion(StepPlanNode returnNode, StepPlanNode currentNode)
        {
            var materializationPlanNode = new StepPlanNode(
                new StepPlan(
                    "$materialize",
                    new ActionPlan(
                        QueryPlan: new QueryPlan(
                            currentNode.StepPlan.Id!,
                            null,
                            [currentNode.StepPlan.Id!],
                            PersistanceMode.StoredQuery))));
            var newReturnPlanNode = new StepPlanNode(
                new StepPlan(
                    "$return",
                    new ActionPlan(ReturnIdReference: materializationPlanNode.StepPlan.Id)));

            materializationPlanNode.AddDependencies(currentNode);
            newReturnPlanNode.AddDependencies(materializationPlanNode);
            returnNode.Remove();
            AddChildren(materializationPlanNode, returnNode);
        }
        #endregion

        #region AssignChildrenPlans
        public void AssignChildrenPlans()
        {
            var childrenPlans = Children
                .Select(n => n.StepPlan)
                .ToArray();

            if (StepPlan.ActionPlan.UnionPlan != null)
            {
                UpdatePlan(new StepPlan(
                    StepPlan.Id,
                    new ActionPlan(
                        UnionPlan: new UnionPlan(
                            StepPlan.ActionPlan.UnionPlan!.Iterator,
                            StepPlan.ActionPlan.UnionPlan!.ResultSet,
                            StepPlan.ActionPlan.UnionPlan!.Type,
                            StepPlan.ActionPlan.UnionPlan!.Concurrency,
                            childrenPlans))));
            }
            foreach (var childNode in Children)
            {
                childNode.AssignChildrenPlans();
            }
        }
        #endregion

        #region Object methods
        public override string ToString()
        {
            return StepPlan.ToString();
        }
        #endregion
    }
}