using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record QueryPlan(PersistanceMode PersistanceMode, QueryScript QueryScript);
}