using FlowPlanning;
using FlowPlanning.Parsing;

namespace UnitTest.Planning.CleanUnreferencedReadonlySteps
{
    public class CleanUnreferencedReadonlyStepsTests : BaseTest
    {
        [Fact]
        public void UnreferrencedMain()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.UnreferrencedMain.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.Equal("$return", plan.Steps[1].Id);
            Assert.Equal("Result", plan.Steps[1].ReturnIdReference);
        }

        [Fact]
        public void ReferrencedMain()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.ReferrencedMain.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(5, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.Equal("$return", plan.Steps[4].Id);
            Assert.Equal("Result4", plan.Steps[4].ReturnIdReference);
        }

        [Fact]
        public void Union()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.Union.kql");
            var script = ScriptParser.ParseScript(text);
            var plan = FlowPlan.CreatePlan(script);

            Assert.Equal(4, plan.Steps.Count());
            Assert.Equal("Categories", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].QueryPlan);
            Assert.NotNull(plan.Steps[1].UnionPlan);
            Assert.NotNull(plan.Steps[2].QueryPlan);
            Assert.NotNull(plan.Steps[3].ReturnIdReference);
            Assert.Equal(PersistanceMode.StoredQuery, plan.Steps[2].QueryPlan!.PersistanceMode);
        
            Assert.Equal(4, plan.Steps[1].UnionPlan!.ChildrenPlans.Count());
            Assert.Equal("CategoryPartition2", plan.Steps[1].UnionPlan!.ChildrenPlans[0].Id);
            Assert.Equal("CategoryPartition3", plan.Steps[1].UnionPlan!.ChildrenPlans[1].Id);
            Assert.NotNull(plan.Steps[1].UnionPlan!.ChildrenPlans[2].QueryPlan);
            Assert.NotNull(plan.Steps[1].UnionPlan!.ChildrenPlans[3].ReturnIdReference);
        }
    }
}