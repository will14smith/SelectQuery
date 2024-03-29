using Xunit;

namespace SelectParser.Tests;

public class NewParserLimitTests
{
    [Fact]
    public void ParsingLimit()
    {
        var input = "LIMIT 10";

        var result = NewParserTestHelpers.Parse(NewParser.LimitClause, input);

        var limit = NewParserTestHelpers.AssertSuccess(result);
        Assert.Equal(10, limit.Limit);
    }
        
    [Fact]
    public void ParsingInvalidLimit()
    {
        var input = "LIMIT a";

        var result = NewParserTestHelpers.Parse(NewParser.LimitClause, input);

        NewParserTestHelpers.AssertFailed(result);
    }
}