using Xunit;

namespace SelectParser.Tests
{
    public class ParserLimitTests
    {
        [Fact]
        public void ParsingLimit()
        {
            var input = "LIMIT 10";

            var result = ParserTestHelpers.Parse(Parser.LimitClause, input);

            var limit = ParserTestHelpers.AssertSuccess(result);
            Assert.Equal(10, limit.Limit);
        }
        [Fact]
        public void ParsingInvalidLimit()
        {
            var input = "LIMIT a";

            var result = ParserTestHelpers.Parse(Parser.LimitClause, input);

            ParserTestHelpers.AssertFailed(result);
        }
    }
}