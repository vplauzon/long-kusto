using FlowPlanning.Parsing;

namespace FlowPlanning
{
    public record QueryPlan(
        string Text,
        KustoType? Type,
        string[] Using,
        PersistanceMode PersistanceMode = PersistanceMode.Blob)
    {
        public QueryPlan ToStoredQuery()
        {
            return new QueryPlan(Text, Type, Using, PersistanceMode.StoredQuery);
        }
    }
}