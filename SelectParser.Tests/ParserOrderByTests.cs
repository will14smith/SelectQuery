using SelectParser.Queries;
using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests
{
    public class ParserOrderByTests
    {
        [Fact]
        public void ParsingOrderByASC()
        {
            var input = "ORDER BY test ASC";

            var result = Parse(Parser.OrderByClause, input);

            var order = AssertSuccess(result);
            var column = Assert.Single(order.Columns);
            AssertIdentifier("test", column.Expression);
            Assert.Equal(OrderDirection.Ascending, column.Direction);
        }
        [Fact]
        public void ParsingOrderByDESC()
        {
            var input = "ORDER BY test DESC";

            var result = Parse(Parser.OrderByClause, input);

            var order = AssertSuccess(result);
            var column = Assert.Single(order.Columns);
            AssertIdentifier("test", column.Expression);
            Assert.Equal(OrderDirection.Descending, column.Direction);
        }
        [Fact]
        public void ParsingOrderByWithoutDirection()
        {
            var input = "ORDER BY test";

            var result = Parse(Parser.OrderByClause, input);

            var order = AssertSuccess(result);
            var column = Assert.Single(order.Columns);
            AssertIdentifier("test", column.Expression);
            Assert.Equal(OrderDirection.Ascending, column.Direction);
        }

        [Fact]
        public void ParsingOrderByWithMultipleColumns()
        {
            var input = "ORDER BY test1, test2 DESC";

            var result = Parse(Parser.OrderByClause, input);

            var order = AssertSuccess(result);
            Assert.Collection(order.Columns,
                column =>
                {
                    AssertIdentifier("test1", column.Expression);
                    Assert.Equal(OrderDirection.Ascending, column.Direction);
                },
                column =>
                {
                    AssertIdentifier("test2", column.Expression);
                    Assert.Equal(OrderDirection.Descending, column.Direction);
                });
        }
    }
}
