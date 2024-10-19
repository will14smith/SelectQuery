using System.Collections.Generic;
using SelectParser.Queries;
using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests;

public class ParserSelectTests
{
    [Fact]
    public void ParsingSelectStar()
    {
        var input = "SELECT *";

        var result = Parse(Parser.SelectClause, input);

        var select = AssertSuccess(result);
        Assert.IsType<SelectClause.Star>(select);
    }
    [Fact]
    public void ParsingSelectColumn()
    {
        var input = "SELECT test";

        var result = Parse(Parser.SelectClause, input);

        var select = AssertSuccess(result);
        var columns = AssertColumns(select);
        var column = Assert.Single(columns);
        AssertIdentifier("test", column.Expression);
        AssertNone(column.Alias);
    }
    [Fact]
    public void ParsingSelectColumnWithAlias()
    {
        var input = "SELECT test AS abc";

        var result = Parse(Parser.SelectClause, input);

        var select = AssertSuccess(result);
        var columns = AssertColumns(select);
        var column = Assert.Single(columns);
        AssertIdentifier("test", column.Expression);
        var alias = AssertSome(column.Alias);
        Assert.Equal("abc", alias);
    }
    [Fact]
    public void ParsingSelectMultipleColumns()
    {
        var input = "SELECT test1, test2 AS abc, test3";

        var result = Parse(Parser.SelectClause, input);

        var select = AssertSuccess(result);
        var columns = AssertColumns(select);
        Assert.Collection(columns,
            column =>
            {
                AssertIdentifier("test1", column.Expression);
                AssertNone(column.Alias);
            },
            column =>
            {
                AssertIdentifier("test2", column.Expression);
                var alias = AssertSome(column.Alias);
                Assert.Equal("abc", alias);
            },
            column =>
            {
                AssertIdentifier("test3", column.Expression);
                AssertNone(column.Alias);
            });
    }
    [Fact]
    public void ParsingSelectInvalid()
    {
        var input = "SELECT";

        var result = Parse(Parser.SelectClause, input);

        AssertFailed(result);
    }

    private static IReadOnlyCollection<Column> AssertColumns(SelectClause clause)
    {
        var list = Assert.IsType<SelectClause.List>(clause);
        return list.Columns;
    }
}