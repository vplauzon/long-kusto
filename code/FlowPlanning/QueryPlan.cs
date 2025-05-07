using FlowPlanning.Parsing;

namespace FlowPlanning
{
    internal record QueryPlan(string Text, KustoType? Type, string[] Using);
}