namespace FlowPlanning
{
    internal record StepPlan(
        string? ValueName,
        PersistanceMode PersistanceMode,
        QueryPlan? QueryPlan);
}