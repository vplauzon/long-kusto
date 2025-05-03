using FlowPlanning.Parsing;

namespace UnitTest
{
    public class ParsingTest : BaseTest
    {
        [Fact]
        public void SimpleQuery()
        {
            var text = GetResource("Parsing.SimpleQuery.kql");
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