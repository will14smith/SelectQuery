using SelectQuery.Workers;
using Xunit;
using static SelectQuery.Tests.OptionTestHelpers;
using static SelectQuery.Tests.QueryTestHelpers;

namespace SelectQuery.Tests.Workers
{
    public class PlannerTests
    {
        [Fact]
        public void ShouldReturnOriginalQuery()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 ORDER BY y LIMIT 10");
            var planner = new Planner();

            var plan = planner.Plan(query);

            AssertEqual(query, plan.InputQuery);
        }

        [Fact]
        public void QueryWithoutOrderingAndLimits_ShouldBeUnchanged()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1");
            var planner = new Planner();

            var plan = planner.Plan(query);

            AssertEqual(query, plan.UnderlyingQuery);
            AssertNone(plan.Order);
            AssertNone(plan.Limit);
        }

        [Fact]
        public void QueryWithOrderingAndLimits_Underlying_ShouldNotLimit()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name LIMIT 10");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var expectedUnderlyingQuery = ParseQuery("SELECT id, name FROM table");
            AssertEqual(expectedUnderlyingQuery, plan.UnderlyingQuery);
        }

        #region Query with ordering
        [Fact]
        public void QueryWithOrderingOnSelectStar_UnderlyingQuery_ShouldRemoveOrderingAndProjectExtraColumn()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 ORDER BY y");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var expected = ParseQuery("SELECT table.*, y as __internal__order_0 FROM table WHERE x = 1");
            AssertEqual(expected, plan.UnderlyingQuery);
        }
        [Fact]
        public void QueryWithOrderingOnSelectStar_Order_ShouldNotBeNone()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 ORDER BY y");
            var planner = new Planner();

            var plan = planner.Plan(query);

            AssertEqual("ORDER BY __internal__order_0", plan.Order);
        }

        [Fact]
        public void QueryWithOrderingOnProjectedColumn_UnderlyingQuery_ShouldRemoveOrdering()
        {
            var query = ParseQuery("SELECT a, y FROM table WHERE x = 1 ORDER BY y");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var expected = ParseQuery("SELECT a, y FROM table WHERE x = 1");
            AssertEqual(expected, plan.UnderlyingQuery);
        }
        [Fact]
        public void QueryWithOrderingOnProjectedColumn_Order_ShouldNotBeNone()
        {
            var query = ParseQuery("SELECT a, y FROM table WHERE x = 1 ORDER BY y");
            var planner = new Planner();

            var plan = planner.Plan(query);

            AssertEqual("ORDER BY y", plan.Order);
        }

        [Fact]
        public void QueryWithOrderingOnUnprojectedExpression_UnderlyingQuery_ShouldRemoveOrderingAndProjectExtraColumn()
        {
            var query = ParseQuery("SELECT a FROM table WHERE x = 1 ORDER BY y");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var expected = ParseQuery("SELECT a, y as __internal__order_0 FROM table WHERE x = 1");
            AssertEqual(expected, plan.UnderlyingQuery);
        }
        [Fact]
        public void QueryWithOrderingOnUnprojectedExpression_Order_ShouldNotBeNone()
        {
            var query = ParseQuery("SELECT a FROM table WHERE x = 1 ORDER BY y");
            var planner = new Planner();

            var plan = planner.Plan(query);

            AssertEqual("ORDER BY __internal__order_0", plan.Order);
        }
        #endregion

        #region Query with limit
        [Fact]
        public void QueryWithLimit_UnderlyingQuery_ShouldKeepLimit()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 LIMIT 10");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var expected = ParseQuery("SELECT * FROM table WHERE x = 1 LIMIT 10");
            AssertEqual(expected, plan.UnderlyingQuery);
        }
        [Fact]
        public void QueryWithLimit_Limit_ShouldNotBeNone()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 LIMIT 10");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var limit = AssertSome(plan.Limit);
            Assert.Equal(10, limit.Limit);
        }

        [Fact]
        public void QueryWithLimitAndOffset_UnderlyingQuery_ShouldTransformLimit()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 LIMIT 10 OFFSET 30");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var expected = ParseQuery("SELECT * FROM table WHERE x = 1 LIMIT 40");
            AssertEqual(expected, plan.UnderlyingQuery);
        }
        [Fact]
        public void QueryWithLimitAndOffset_Limit_ShouldNotBeNone()
        {
            var query = ParseQuery("SELECT * FROM table WHERE x = 1 LIMIT 10 OFFSET 30");
            var planner = new Planner();

            var plan = planner.Plan(query);

            var limit = AssertSome(plan.Limit);
            Assert.Equal(10, limit.Limit);
            Assert.Equal(30, limit.Offset);
        }
        #endregion
    }
}
