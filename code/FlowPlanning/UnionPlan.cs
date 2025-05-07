using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record UnionPlan(
        bool IsLazyExecuted,
        string Iterator,
        string ResultSet,
        KustoType Type,
        long? Concurrency,
        StepPlan[] ChildrenPlans);
}