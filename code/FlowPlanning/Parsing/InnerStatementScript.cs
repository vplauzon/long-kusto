namespace FlowPlanning.Parsing
{
    internal record InnerStatementScript(
        QueryScript? Query = null,
        ShowCommandScript? ShowCommand = null,
        CommandScript? Command = null,
        ForEachScript? ForEach = null,
        UnionScript? Union = null,
        AwaitScript? Await = null,
        AppendScript? Append = null,
        string? ReferencedIdentifier = null);
}