
namespace FlowPlanning
{
    public record StepPlan(string? Id, ActionPlan ActionPlan)
    {
        /// <summary><c>true</c> iif the plan doesn't alter the state of a database.</summary>
        public bool IsReadOnly => ActionPlan.QueryPlan != null
            || ActionPlan.UnionPlan != null
            || ActionPlan.ShowCommandPlan != null
            || ActionPlan.IdReference != null
            || ActionPlan.ReturnIdReference != null;
    }
}