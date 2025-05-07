using FlowPlanning;
using FlowPlanning.Parsing;

namespace UnitTest.Planning.CleanUnreferencedReadonlySteps
{
    public class CleanUnreferencedReadonlyStepsTests : BaseTest
    {
        [Fact]
        public void Unreferrenced()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.Unreferrenced.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.Equal("$return", plan.Steps[1].Id);
            Assert.Equal("Result", plan.Steps[1].ReturnIdReference);
        }

        [Fact]
        public void Referrenced()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.Referrenced.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(5, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.Equal("$return", plan.Steps[4].Id);
            Assert.Equal("Result4", plan.Steps[4].ReturnIdReference);
        }
    }
}