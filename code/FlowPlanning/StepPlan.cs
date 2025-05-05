namespace FlowPlanning
{
    internal record StepPlan(
        string Id,
        QueryPlan? QueryPlan,
        UnionPlan? UnionPlan,
        string? IdReference);
}