
namespace FlowPlanning
{
    public record ActionPlan(
        QueryPlan? QueryPlan = null,
        UnionPlan? UnionPlan = null,
        ShowCommandPlan? ShowCommandPlan = null,
        CommandPlan? CommandPlan = null,
        string? IdReference = null,
        string? ReturnIdReference = null);
}