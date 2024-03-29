using SelectParser.Queries;
using Xunit;

namespace SelectParser.Tests;

public class NewParserOrderByTests
{
    [Fact]
    public void ParsingOrderByASC()
    {
        var input = "ORDER BY test ASC";

        var result = NewParserTestHelpers.Parse(NewParser.OrderByClause, input);

        var order = NewParserTestHelpers.AssertSuccess(result);
        var column = Assert.Single(order.Columns);
        NewParserTestHelpers.AssertIdentifier("test", column.Expression);
        Assert.Equal(OrderDirection.Ascending, column.Direction);
    }
    [Fact]
    public void ParsingOrderByDESC()
    {
        var input = "ORDER BY test DESC";

        var result = NewParserTestHelpers.Parse(NewParser.OrderByClause, input);

        var order = NewParserTestHelpers.AssertSuccess(result);
        var column = Assert.Single(order.Columns);
        NewParserTestHelpers.AssertIdentifier("test", column.Expression);
        Assert.Equal(OrderDirection.Descending, column.Direction);
    }
    [Fact]
    public void ParsingOrderByWithoutDirection()
    {
        var input = "ORDER BY test";

        var result = NewParserTestHelpers.Parse(NewParser.OrderByClause, input);

        var order = NewParserTestHelpers.AssertSuccess(result);
        var column = Assert.Single(order.Columns);
        NewParserTestHelpers.AssertIdentifier("test", column.Expression);
        Assert.Equal(OrderDirection.Ascending, column.Direction);
    }

    [Fact]
    public void ParsingOrderByWithMultipleColumns()
    {
        var input = "ORDER BY test1, test2 DESC";

        var result = NewParserTestHelpers.Parse(NewParser.OrderByClause, input);

        var order = NewParserTestHelpers.AssertSuccess(result);
        Assert.Collection(order.Columns,
            column =>
            {
                NewParserTestHelpers.AssertIdentifier("test1", column.Expression);
                Assert.Equal(OrderDirection.Ascending, column.Direction);
            },
            column =>
            {
                NewParserTestHelpers.AssertIdentifier("test2", column.Expression);
                Assert.Equal(OrderDirection.Descending, column.Direction);
            });
    }
}