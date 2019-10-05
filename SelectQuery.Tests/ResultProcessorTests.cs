using System.Linq;
using SelectParser.Queries;
using Xunit;
using static SelectQuery.Tests.ResultTestHelpers;

namespace SelectQuery.Tests
{
    public class ResultProcessorTests
    {
        #region order
        [Fact]
        public void OrderedQuery_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Ascending));
            var input = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var expectedResults = new[] { input[1], input[0], input[2] };

            var results = ResultProcessor.Order(order, input);

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Multiple_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Ascending), ("other", OrderDirection.Ascending));
            var input = new[]
            {
                CreateRow(("id", 1), ("name", "a"), ("other", 2)),
                CreateRow(("id", 2), ("name", "a"), ("other", 1)),
                CreateRow(("id", 3), ("name", "b"), ("other", 1)),
            };
            var expectedResults = new[] { input[1], input[0], input[2] };

            var results = ResultProcessor.Order(order, input);

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Desc_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Descending));
            var input = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var expectedResults = new[] { input[2], input[0], input[1] };

            var results = ResultProcessor.Order(order, input);

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Missing_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Ascending));
            var input = new[]
            {
                CreateRow(("id", 1)),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var expectedResults = new[] { input[0], input[1], input[2] };

            var results = ResultProcessor.Order(order, input);

            AssertResultsEqual(expectedResults, results);
        }

        private static OrderClause Order(params (string Key, OrderDirection Direction)[] columns)
        {
            return new OrderClause(columns.Select(x => ((Expression)new Expression.Identifier(x.Key), x.Direction)).ToList());
        }
        #endregion

        #region limit
        [Fact]
        public void Limit_ShouldLimitResults()
        {
            var input = new[] { CreateRow(("id", 1)), CreateRow(("id", 2)) };
            var expectedResults = new[] { input[0] };

            var results = ResultProcessor.Limit(new LimitClause(1), input);

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitHigherThanCount_ShouldLimitResults()
        {
            var input = new[] { CreateRow(("id", 1)) };
            var expectedResults = new[] { input[0] };

            var results = ResultProcessor.Limit(new LimitClause(10), input);

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitOffset_ShouldLimitResults()
        {
            var input = new[] { CreateRow(("id", 1)), CreateRow(("id", 2)), CreateRow(("id", 3)) };
            var expectedResults = new[] { input[1] };

            var results = ResultProcessor.Limit(new LimitClause(1, 1), input);

            AssertResultsEqual(expectedResults, results);
        }
        #endregion
    }
}
