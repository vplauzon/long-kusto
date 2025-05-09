using FlowPlanning.Parsing;

namespace FlowPlanning
{
    public record ShowCommandPlan(
        string Text,
        PersistanceMode PersistanceMode = PersistanceMode.Blob)
    {
        public ShowCommandPlan ToStoredQuery()
        {
            return new ShowCommandPlan(Text, PersistanceMode.StoredQuery);
        }
    }
}