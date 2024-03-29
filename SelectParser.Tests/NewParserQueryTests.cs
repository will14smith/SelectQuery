using SelectParser.Queries;
using Xunit;

namespace SelectParser.Tests;

public class NewParserQueryTests
{
    [Fact]
    public void ParsingSelectFrom()
    {
        var input = "SELECT * FROM test";

        var result = NewParserTestHelpers.Parse(NewParser.Query, input);

        var query = NewParserTestHelpers.AssertSuccess(result);
        Assert.True(query.Select.IsT0);
        Assert.Equal("test", query.From.Table);
    }

    [Fact]
    public void ParsingSelect_MissingFrom()
    {
        var input = "SELECT *";

        var result = NewParserTestHelpers.Parse(NewParser.Query, input);

        NewParserTestHelpers.AssertFailed(result);
    }

    [Fact]
    public void ParsingSelectFromWhere()
    {
        var input = "SELECT * FROM test WHERE col";

        var result = NewParserTestHelpers.Parse(NewParser.Query, input);

        var query = NewParserTestHelpers.AssertSuccess(result);
        Assert.True(query.Select.IsT0);
        Assert.Equal("test", query.From.Table);
        var where = NewParserTestHelpers.AssertSome(query.Where);
        NewParserTestHelpers.AssertIdentifier("col", where.Condition);
    }

    [Fact]
    public void ParsingSelectFromOrder()
    {
        var input = "SELECT * FROM test ORDER BY col";

        var result = NewParserTestHelpers.Parse(NewParser.Query, input);

        var query = NewParserTestHelpers.AssertSuccess(result);
        Assert.True(query.Select.IsT0);
        Assert.Equal("test", query.From.Table);
        var order = NewParserTestHelpers.AssertSome(query.Order);
        var (orderExpression, _) = Assert.Single(order.Columns);
        NewParserTestHelpers.AssertIdentifier("col", orderExpression);
    }

    [Fact]
    public void ParsingSelectFromLimit()
    {
        var input = "SELECT * FROM test LIMIT 10";

        var result = NewParserTestHelpers.Parse(NewParser.Query, input);

        var query = NewParserTestHelpers.AssertSuccess(result);
        Assert.True(query.Select.IsT0);
        Assert.Equal("test", query.From.Table);
        var limit = NewParserTestHelpers.AssertSome(query.Limit);
        Assert.Equal(10, limit.Limit);
    }

    [Fact]
    public void ParsingSelectFromWhereOrderLimit()
    {
        var input = "SELECT * FROM test WHERE col1 ORDER BY col2 LIMIT 10";

        var result = NewParserTestHelpers.Parse(NewParser.Query, input);

        var query = NewParserTestHelpers.AssertSuccess(result);
        Assert.True(query.Select.IsT0);
        Assert.Equal("test", query.From.Table);
        var where = NewParserTestHelpers.AssertSome(query.Where);
        NewParserTestHelpers.AssertIdentifier("col1", where.Condition);
        var order = NewParserTestHelpers.AssertSome(query.Order);
        var (orderExpression, _) = Assert.Single(order.Columns);
        NewParserTestHelpers.AssertIdentifier("col2", orderExpression);
        var limit = NewParserTestHelpers.AssertSome(query.Limit);
        Assert.Equal(10, limit.Limit);
    }

    [Fact]
    public void ParsingSelectFromOrderWhere_WrongOrder()
    {
        var input = "SELECT * FROM test ORDER BY col1 WHERE col2";

        var tokenizer = new NewTokenizer(input);
        var result = NewParser.Query(ref tokenizer);

        NewParserTestHelpers.AssertFailed(result);
    }
}