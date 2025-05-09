
namespace FlowPlanning
{
    public record StepPlan(
        string Id,
        QueryPlan? QueryPlan = null,
        UnionPlan? UnionPlan = null,
        ShowCommandPlan? ShowCommandPlan = null,
        CommandPlan? CommandPlan = null,
        string? IdReference = null,
        string? ReturnIdReference = null)
    {
        /// <summary><c>true</c> iif the plan doesn't alter the state of a database.</summary>
        public bool IsReadOnly => QueryPlan != null
            || UnionPlan != null
            || ShowCommandPlan != null
            || IdReference != null
            || ReturnIdReference != null;
    }
}