using FlowPlanning;

namespace UnitTest.Planning.ShowCommandPlanning
{
    public class ShowCommandPlanningTest : BaseTest
    {
        [Fact]
        public void ShowCommand()
        {
            var text = GetResource("Planning.ShowCommandPlanning.ShowCommand.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].ShowCommandPlan);
            Assert.Equal("$return", plan.Steps[1].Id);
            Assert.Equal("Result", plan.Steps[1].ReturnIdReference);
        }
    }
}