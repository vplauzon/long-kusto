using FlowPlanning.Parsing;
using static System.Net.Mime.MediaTypeNames;
using System.Net.NetworkInformation;

namespace FlowPlanning
{
    public record CommandPlan(
        string Text,
        PersistanceMode PersistanceMode = PersistanceMode.Blob)
    {
        public CommandPlan ToStoredQuery()
        {
            return new CommandPlan(Text, PersistanceMode.StoredQuery);
        }
    }
}