using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests;

public class QueryTests
{
    [Fact]
    public void ToString_ShouldReturnQuery()
    {
        var input = "SELECT id, name FROM test WHERE col1 ORDER BY col2 LIMIT 10";
        var query = AssertSuccess(Parse(Parser.Query, input));

        Assert.Equal(input, query.ToString());
    }
}