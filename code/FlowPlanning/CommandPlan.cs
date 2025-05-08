using FlowPlanning.Parsing;
using static System.Net.Mime.MediaTypeNames;
using System.Net.NetworkInformation;

namespace FlowPlanning
{
    internal record CommandPlan(
        CommandScript CommandScript,
        PersistanceMode PersistanceMode = PersistanceMode.Blob)
    {
        public CommandPlan ToStoredQuery()
        {
            return new CommandPlan(CommandScript, PersistanceMode.StoredQuery);
        }
    }
}