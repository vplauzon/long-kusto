namespace FlowPlanning.Parsing
{
    internal record UnionScript(
        string Iterator,
        string ResultSet,
        string? Type,
        PropertyAssignationScript[] Properties,
        StatementScript[] Statements);
}