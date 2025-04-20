using System;
using System.Linq;
using SelectParser.Queries;
using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests;

public class ParserQueryTests
{
    [Fact]
    public void ParsingSelectFrom()
    {
        var input = "SELECT * FROM test";

        var result = Parse(Parser.Query, input);

        var query = AssertSuccess(result);
        Assert.IsType<SelectClause.Star>(query.Select);
        Assert.Equal("test", query.From.Table);
    }

    [Fact]
    public void ParsingSelect_MissingFrom()
    {
        var input = "SELECT *";

        var result = Parse(Parser.Query, input);

        AssertFailed(result);
    }

    [Fact]
    public void ParsingSelectFromWhere()
    {
        var input = "SELECT * FROM test WHERE col";

        var result = Parse(Parser.Query, input);

        var query = AssertSuccess(result);
        Assert.IsType<SelectClause.Star>(query.Select);
        Assert.Equal("test", query.From.Table);
        var where = AssertSome(query.Where);
        AssertIdentifier("col", where.Condition);
    }

    [Fact]
    public void ParsingSelectFromOrder()
    {
        var input = "SELECT * FROM test ORDER BY col";

        var result = Parse(Parser.Query, input);

        var query = AssertSuccess(result);
        Assert.IsType<SelectClause.Star>(query.Select);
        Assert.Equal("test", query.From.Table);
        var order = AssertSome(query.Order);
        var (orderExpression, _) = Assert.Single(order.Columns);
        AssertIdentifier("col", orderExpression);
    }

    [Fact]
    public void ParsingSelectFromLimit()
    {
        var input = "SELECT * FROM test LIMIT 10";

        var result = Parse(Parser.Query, input);

        var query = AssertSuccess(result);
        Assert.IsType<SelectClause.Star>(query.Select);
        Assert.Equal("test", query.From.Table);
        var limit = AssertSome(query.Limit);
        Assert.Equal(10, limit.Limit);
    }

    [Fact]
    public void ParsingSelectFromWhereOrderLimit()
    {
        var input = "SELECT * FROM test WHERE col1 ORDER BY col2 LIMIT 10";

        var result = Parse(Parser.Query, input);

        var query = AssertSuccess(result);
        Assert.IsType<SelectClause.Star>(query.Select);
        Assert.Equal("test", query.From.Table);
        var where = AssertSome(query.Where);
        AssertIdentifier("col1", where.Condition);
        var order = AssertSome(query.Order);
        var (orderExpression, _) = Assert.Single(order.Columns);
        AssertIdentifier("col2", orderExpression);
        var limit = AssertSome(query.Limit);
        Assert.Equal(10, limit.Limit);
    }

    [Fact]
    public void ParsingSelectFromOrderWhere_WrongOrder()
    {
        var input = "SELECT * FROM test ORDER BY col1 WHERE col2";

        var result = Parser.Parse(input);

        AssertFailed(result);
    }
}