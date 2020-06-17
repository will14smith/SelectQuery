using System.Linq;
using System.Threading.Tasks;
using SelectParser.Queries;
using Xunit;
using static SelectQuery.Tests.ResultTestHelpers;

namespace SelectQuery.Tests
{
    public class ResultProcessorTests
    {
        #region order
        [Fact]
        public async Task OrderedQuery_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Ascending));
            var input = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var expectedResults = new[] { input[1], input[0], input[2] };

            var results = await ResultProcessor.Order(order, input.ToAsyncEnumerable()).ToListAsync();

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public async Task OrderedQuery_Multiple_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Ascending), ("other", OrderDirection.Ascending));
            var input = new[]
            {
                CreateRow(("id", 1), ("name", "a"), ("other", 2)),
                CreateRow(("id", 2), ("name", "a"), ("other", 1)),
                CreateRow(("id", 3), ("name", "b"), ("other", 1)),
            };
            var expectedResults = new[] { input[1], input[0], input[2] };

            var results = await ResultProcessor.Order(order, input.ToAsyncEnumerable()).ToListAsync();

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public async Task OrderedQuery_Desc_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Descending));
            var input = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var expectedResults = new[] { input[2], input[0], input[1] };

            var results = await ResultProcessor.Order(order, input.ToAsyncEnumerable()).ToListAsync();

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public async Task OrderedQuery_Missing_ShouldOrderResults()
        {
            var order = Order(("name", OrderDirection.Ascending));
            var input = new[]
            {
                CreateRow(("id", 1)),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var expectedResults = new[] { input[0], input[1], input[2] };

            var results = await ResultProcessor.Order(order, input.ToAsyncEnumerable()).ToListAsync();

            AssertResultsEqual(expectedResults, results);
        }

        private static OrderClause Order(params (string Key, OrderDirection Direction)[] columns)
        {
            return new OrderClause(columns.Select(x => ((Expression)new Expression.Identifier(x.Key, false), x.Direction)).ToList());
        }
        #endregion

        #region limit
        [Fact]
        public async Task Limit_ShouldLimitResults()
        {
            var limit = new LimitClause(1);
            var input = new[] { CreateRow(("id", 1)), CreateRow(("id", 2)) };
            var expectedResults = new[] { input[0] };

            var results = await ResultProcessor.Limit(limit, input.ToAsyncEnumerable()).ToListAsync();

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public async Task LimitHigherThanCount_ShouldLimitResults()
        {
            var limit = new LimitClause(10);
            var input = new[] { CreateRow(("id", 1)) };
            var expectedResults = new[] { input[0] };

            var results = await ResultProcessor.Limit(limit, input.ToAsyncEnumerable()).ToListAsync();

            AssertResultsEqual(expectedResults, results);
        }
        #endregion
    }
}
