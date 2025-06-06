﻿using FlowPlanning;

namespace UnitTest.Planning.UnionQueryPlanning
{
    public class UnionQueryPlanningTest : BaseTest
    {
        [Fact]
        public void AdHocUnionQuery()
        {
            var text = GetResource("Planning.UnionQueryPlanning.AdHocUnionQuery.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(4, plan.Steps.Count());
            Assert.Equal("Query1", plan.Steps[0].Id);
            Assert.Equal("Query2", plan.Steps[1].Id);
            Assert.Equal("UnionQuery", plan.Steps[2].Id);
            Assert.Equal("$return", plan.Steps[3].Id);
        }

        [Fact]
        public void GenUnionQuery()
        {
            var text = GetResource("Planning.UnionQueryPlanning.GenUnionQuery.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(4, plan.Steps.Count());
            Assert.Equal("Categories", plan.Steps[0].Id);
            Assert.Equal("UnionQuery", plan.Steps[1].Id);
            Assert.NotNull(plan.Steps[1].ActionPlan.UnionPlan);
            Assert.Equal("$return", plan.Steps[3].Id);
        }

        [Fact]
        public void MaterializeGenUnionQuery()
        {
            var text = GetResource("Planning.UnionQueryPlanning.MaterializeGenUnionQuery.kql");
            var plan = FlowPlan.CreatePlan(text);

            Assert.Equal(4, plan.Steps.Count());
            Assert.Equal("Categories", plan.Steps[0].Id);
            Assert.Equal("UnionQuery", plan.Steps[1].Id);
            Assert.NotNull(plan.Steps[1].ActionPlan.UnionPlan);
            Assert.NotNull(plan.Steps[2].ActionPlan.QueryPlan);
            Assert.Equal(
                PersistanceMode.StoredQuery,
                plan.Steps[2].ActionPlan.QueryPlan!.PersistanceMode);
            Assert.NotNull(plan.Steps[3].ActionPlan.ReturnIdReference);
        }
    }
}