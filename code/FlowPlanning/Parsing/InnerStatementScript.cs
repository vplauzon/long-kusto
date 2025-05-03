namespace FlowPlanning.Parsing
{
    internal record InnerStatementScript(
        QueryScript Query,
        CommandScript Command,
        ForEachScript ForEach,
        UnionScript Union,
        AwaitScript Await,
        AppendScript Append,
        string? Identifier);
}