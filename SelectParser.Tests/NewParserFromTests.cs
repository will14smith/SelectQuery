using Xunit;

namespace SelectParser.Tests;

public class NewParserFromTests
{
    [Fact]
    public void ParsingFrom()
    {
        var input = "FROM test";

        var result = NewParserTestHelpers.Parse(NewParser.FromClause, input);

        var from = NewParserTestHelpers.AssertSuccess(result);
        Assert.Equal("test", from.Table);
        NewParserTestHelpers.AssertNone(from.Alias);
    }
    [Fact]
    public void ParsingFromWithAsAlias()
    {
        var input = "FROM test AS abc";

        var result = NewParserTestHelpers.Parse(NewParser.FromClause, input);

        var from = NewParserTestHelpers.AssertSuccess(result);
        Assert.Equal("test", from.Table);
        var alias = NewParserTestHelpers.AssertSome(from.Alias);
        Assert.Equal("abc", alias);
    }
    [Fact]
    public void ParsingFromWithAlias()
    {
        var input = "FROM test abc";

        var result = NewParserTestHelpers.Parse(NewParser.FromClause, input);

        var from = NewParserTestHelpers.AssertSuccess(result);
        Assert.Equal("test", from.Table);
        var alias = NewParserTestHelpers.AssertSome(from.Alias);
        Assert.Equal("abc", alias);
    }
    [Fact]
    public void ParsingInvalidFrom()
    {
        var input = "FROM 10";

        var result = NewParserTestHelpers.Parse(NewParser.FromClause, input);

        NewParserTestHelpers.AssertFailed(result);
    }
}