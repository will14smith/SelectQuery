using System.Collections.Generic;
using SelectParser.Queries;
using Xunit;

namespace SelectParser.Tests;

public class NewParserSelectTests
{
    [Fact]
    public void ParsingSelectStar()
    {
        var input = "SELECT *";

        var result = NewParserTestHelpers.Parse(NewParser.SelectClause, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        Assert.True(select.IsT0);
    }
    [Fact]
    public void ParsingSelectColumn()
    {
        var input = "SELECT test";

        var result = NewParserTestHelpers.Parse(NewParser.SelectClause, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        var columns = AssertColumns(select);
        var column = Assert.Single(columns);
        NewParserTestHelpers.AssertIdentifier("test", column.Expression);
        NewParserTestHelpers.AssertNone(column.Alias);
    }
    [Fact]
    public void ParsingSelectColumnWithAlias()
    {
        var input = "SELECT test AS abc";

        var result = NewParserTestHelpers.Parse(NewParser.SelectClause, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        var columns = AssertColumns(select);
        var column = Assert.Single(columns);
        NewParserTestHelpers.AssertIdentifier("test", column.Expression);
        var alias = NewParserTestHelpers.AssertSome(column.Alias);
        Assert.Equal("abc", alias);
    }
    [Fact]
    public void ParsingSelectMultipleColumns()
    {
        var input = "SELECT test1, test2 AS abc, test3";

        var result = NewParserTestHelpers.Parse(NewParser.SelectClause, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        var columns = AssertColumns(select);
        Assert.Collection(columns,
            column =>
            {
                NewParserTestHelpers.AssertIdentifier("test1", column.Expression);
                NewParserTestHelpers.AssertNone(column.Alias);
            },
            column =>
            {
                NewParserTestHelpers.AssertIdentifier("test2", column.Expression);
                var alias = NewParserTestHelpers.AssertSome(column.Alias);
                Assert.Equal("abc", alias);
            },
            column =>
            {
                NewParserTestHelpers.AssertIdentifier("test3", column.Expression);
                NewParserTestHelpers.AssertNone(column.Alias);
            });
    }
    [Fact]
    public void ParsingSelectInvalid()
    {
        var input = "SELECT";

        var result = NewParserTestHelpers.Parse(NewParser.SelectClause, input);

        NewParserTestHelpers.AssertFailed(result);
    }

    private IReadOnlyCollection<Column> AssertColumns(SelectClause clause)
    {
        Assert.True(clause.IsT1);
        return clause.AsT1.Columns;
    }
}