using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests
{
    public class ParserLimitTests
    {
        [Fact]
        public void ParsingLimit()
        {
            var input = "LIMIT 10";

            var result = Parse(Parser.LimitClause, input);

            var limit = AssertSuccess(result);
            Assert.Equal(10, limit.Limit);
            AssertNone(limit.Offset);
        }

        [Fact]
        public void ParsingOffset()
        {
            var input = "LIMIT 10 OFFSET 20";

            var result = Parse(Parser.LimitClause, input);

            var limit = AssertSuccess(result);
            Assert.Equal(10, limit.Limit);
            var offset = AssertSome(limit.Offset);
            Assert.Equal(20, offset);
        }

        [Fact]
        public void ParsingInvalidLimit()
        {
            var input = "LIMIT a";

            var result = Parse(Parser.LimitClause, input);

            AssertFailed(result);
        }
    }
}