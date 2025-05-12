using FlowPlanning;

namespace UnitTest.Planning.CleanUnreferencedReadonlySteps
{
    public class CleanUnreferencedReadonlyStepsTests : BaseTest
    {
        [Fact]
        public void UnreferrencedMain()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.UnreferrencedMain.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(2, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].ActionPlan.QueryPlan);
            Assert.Equal("$return", plan.Steps[1].Id);
            Assert.Equal("Result", plan.Steps[1].ActionPlan.ReturnIdReference);
        }

        [Fact]
        public void ReferrencedMain()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.ReferrencedMain.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(5, plan.Steps.Count());
            Assert.Equal("Result", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].ActionPlan.QueryPlan);
            Assert.Equal("$return", plan.Steps[4].Id);
            Assert.Equal("Result4", plan.Steps[4].ActionPlan.ReturnIdReference);
        }

        [Fact]
        public void Union()
        {
            var text = GetResource("Planning.CleanUnreferencedReadonlySteps.Union.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(4, plan.Steps.Count());
            Assert.Equal("Categories", plan.Steps[0].Id);
            Assert.NotNull(plan.Steps[0].ActionPlan.QueryPlan);
            Assert.NotNull(plan.Steps[1].ActionPlan.UnionPlan);
            Assert.NotNull(plan.Steps[2].ActionPlan.QueryPlan);
            Assert.NotNull(plan.Steps[3].ActionPlan.ReturnIdReference);
            Assert.Equal(
                PersistanceMode.StoredQuery,
                plan.Steps[2].ActionPlan.QueryPlan!.PersistanceMode);
        
            Assert.Equal(4, plan.Steps[1].ActionPlan.UnionPlan!.ChildrenPlans.Count());
            Assert.Equal("CategoryPartition2", plan.Steps[1].ActionPlan.UnionPlan!.ChildrenPlans[0].Id);
            Assert.Equal("CategoryPartition3", plan.Steps[1].ActionPlan.UnionPlan!.ChildrenPlans[1].Id);
            Assert.NotNull(
                plan.Steps[1].ActionPlan.UnionPlan!.ChildrenPlans[2].ActionPlan.QueryPlan);
            Assert.NotNull(
                plan.Steps[1].ActionPlan.UnionPlan!.ChildrenPlans[3].ActionPlan.ReturnIdReference);
        }
    }
}