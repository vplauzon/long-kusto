namespace FlowPlanning
{
    internal record StepPlan(
        string? Id,
        PersistanceMode PersistanceMode,
        QueryPlan? QueryPlan,
        string? IdReference);
}