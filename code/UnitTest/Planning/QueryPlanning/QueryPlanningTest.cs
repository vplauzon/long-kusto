using FlowPlanning;
using FlowPlanning.Parsing;

namespace UnitTest.QueryPlanning
{
    public class QueryPlanningTest : BaseTest
    {
        [Fact]
        public void SimpleQuery()
        {
            var text = GetResource("Planning.QueryPlanning.SimpleQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("MyQuery", plan.Steps[0].Id);
            //  The query should be persisted in stored query to optimize return
            Assert.Equal(PersistanceMode.StoredQuery, plan.Steps[0].PersistanceMode);
            Assert.Equal("MyQuery", plan.Steps[1].IdReference);
        }

        [Fact]
        public void DirectSimpleQuery()
        {
            var text = GetResource("Planning.QueryPlanning.DirectSimpleQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.NotNull(plan.Steps[1].IdReference);
        }

        [Fact]
        public void UnnamedQuery()
        {
            var text = GetResource("Planning.QueryPlanning.UnnamedQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Empty(plan.Steps);
        }
    }
}