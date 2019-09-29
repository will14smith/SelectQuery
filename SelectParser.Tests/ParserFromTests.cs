using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests
{
    public class ParserFromTests
    {
        [Fact]
        public void ParsingFrom()
        {
            var input = "FROM test";

            var result = Parse(Parser.FromClause, input);

            var from = AssertSuccess(result);
            Assert.Equal("test", from.Table);
            AssertNone(from.Alias);
        }
        [Fact]
        public void ParsingFromWithAsAlias()
        {
            var input = "FROM test AS abc";

            var result = Parse(Parser.FromClause, input);

            var from = AssertSuccess(result);
            Assert.Equal("test", from.Table);
            var alias = AssertSome(from.Alias);
            Assert.Equal("abc", alias);
        }
        [Fact]
        public void ParsingFromWithAlias()
        {
            var input = "FROM test abc";

            var result = Parse(Parser.FromClause, input);

            var from = AssertSuccess(result);
            Assert.Equal("test", from.Table);
            var alias = AssertSome(from.Alias);
            Assert.Equal("abc", alias);
        }
        [Fact]
        public void ParsingInvalidFrom()
        {
            var input = "FROM 10";

            var result = Parse(Parser.FromClause, input);

            AssertFailed(result);
        }
    }
}