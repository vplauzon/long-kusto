
namespace QueryPlan.Parsing
{
    internal record Script(StatementScript[] Statements)
    {
        internal void StaticAnalysis()
        {
        }
    }
}