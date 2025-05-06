using FlowPlanning;
using FlowPlanning.Parsing;

namespace UnitTest.Planning.SimpleQueryPlanning
{
    public class SimpleQueryPlanningTest : BaseTest
    {
        [Fact]
        public void SimpleQuery()
        {
            var text = GetResource("Planning.SimpleQueryPlanning.SimpleQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("MyQuery", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.Equal("$return", plan.Steps[1].Id);
            Assert.Equal("MyQuery", plan.Steps[1].ReturnIdReference);
        }

        [Fact]
        public void DirectSimpleQuery()
        {
            var text = GetResource("Planning.SimpleQueryPlanning.DirectSimpleQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.NotNull(plan.Steps[1].ReturnIdReference);
        }

        [Fact]
        public void UnnamedQuery()
        {
            var text = GetResource("Planning.SimpleQueryPlanning.UnnamedQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Empty(plan.Steps);
        }
    }
}