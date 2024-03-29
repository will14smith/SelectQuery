using Xunit;

namespace SelectParser.Tests;

public class NewQueryTests
{
    [Fact]
    public void ToString_ShouldReturnQuery()
    {
        var input = "SELECT id, name FROM test WHERE col1 ORDER BY col2 LIMIT 10";
        var query = NewParserTestHelpers.AssertSuccess(NewParserTestHelpers.Parse(NewParser.Query, input));

        Assert.Equal(input, query.ToString());
    }
}