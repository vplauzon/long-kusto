using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record QueryPlan(
        string Text,
        KustoType? Type,
        string[] Using,
        PersistanceMode PersistanceMode = PersistanceMode.Blob);
}