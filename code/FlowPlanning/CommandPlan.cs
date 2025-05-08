using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record CommandPlan(
        CommandScript CommandScript,
        PersistanceMode PersistanceMode = PersistanceMode.Blob);
}