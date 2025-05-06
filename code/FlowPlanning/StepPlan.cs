namespace FlowPlanning
{
    internal record StepPlan(
        string Id,
        QueryPlan? QueryPlan = null,
        UnionPlan? UnionPlan = null,
        string? IdReference = null,
        string? ReturnIdReference = null);
}