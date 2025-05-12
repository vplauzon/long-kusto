using FlowPlanning;

namespace UnitTest.Planning.CommandPlanning
{
    public class CommandPlanningTest : BaseTest
    {
        [Fact]
        public void Command()
        {
            var text = GetResource("Planning.CommandPlanning.Command.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Single(plan.Steps);
            Assert.NotNull(plan.Steps[0].CommandPlan);
        }
    }
}