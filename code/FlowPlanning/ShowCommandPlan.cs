using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record ShowCommandPlan(
        ShowCommandScript ShowCommandScript,
        PersistanceMode PersistanceMode = PersistanceMode.Blob);
}