using QueryPlan.Parsing;

namespace UnitTest
{
    public class ParsingTest
    {
        [Fact]
        public void Test1()
        {
            var text = @"let MyQuery = query:
    """"""
        T
        | where Category==""Red""
        | summarize Cardinality=count() by Category, SubCategory
        | top 50 by Cardinality
    """""";

return MyQuery;";
            var script = ScriptParser.ParseScript(text);

            Assert.Equal(2, script.Statements.Count());
            Assert.Equal("MyQuery", script.Statements[0].Prefix.LetIdPrefix);
            Assert.False(script.Statements[0].Prefix.ReturnPrefix);
            Assert.Empty(script.Statements[0].InnerStatement.Query.Using);
            Assert.True(script.Statements[1].Prefix.ReturnPrefix);
            Assert.Equal("MyQuery", script.Statements[1].InnerStatement.Identifier);
        }
    }
}