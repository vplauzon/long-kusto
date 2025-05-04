using FlowPlanning;
using FlowPlanning.Parsing;

namespace UnitTest.QueryPlanning
{
    public class QueryPlanningTest : BaseTest
    {
        [Fact]
        public void SimpleQuery()
        {
            var text = GetResource("QueryPlanning.SimpleQuery.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("MyQuery", plan.Steps[0].Id);
            Assert.Equal(PersistanceMode.Blob, plan.Steps[0].PersistanceMode);
            Assert.Equal("MyQuery", plan.Steps[1].IdReference);
        }
    }
}