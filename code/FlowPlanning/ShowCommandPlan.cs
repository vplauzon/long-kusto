using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record ShowCommandPlan(
        ShowCommandScript ShowCommandScript,
        PersistanceMode PersistanceMode = PersistanceMode.Blob)
    {
        public ShowCommandPlan ToStoredQuery()
        {
            return new ShowCommandPlan(ShowCommandScript, PersistanceMode.StoredQuery);
        }
    }
}