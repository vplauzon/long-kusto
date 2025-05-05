using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record UnionPlan(
        PersistanceMode PersistanceMode,
        string Iterator,
        string ResultSet,
        KustoType Type,
        long? Concurrency);
}