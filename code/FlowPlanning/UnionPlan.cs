using FlowPlanning.Parsing;

namespace FlowPlanning
{
    public record UnionPlan(
        string Iterator,
        string ResultSet,
        KustoType Type,
        long? Concurrency,
        StepPlan[] ChildrenPlans);
}