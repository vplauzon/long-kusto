using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record UnionPlan(
        string Iterator,
        string ResultSet,
        KustoType Type,
        long? Concurrency,
        StepPlan[] ChildrenPlans);
}