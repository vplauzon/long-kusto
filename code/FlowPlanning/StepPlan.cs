namespace FlowPlanning
{
    internal record StepPlan(
        string Id,
        QueryPlan? QueryPlan = null,
        UnionPlan? UnionPlan = null,
        ShowCommandPlan? ShowCommandPlan = null,
        CommandPlan? CommandPlan = null,
        string? IdReference = null,
        string? ReturnIdReference = null);
}