using System.Linq;
using Xunit;

namespace SelectParser.Tests
{
    public class SelectTokenizerTests
    {
        [Theory]
        [InlineData("SELECT", SelectToken.Select)]
        [InlineData("AS", SelectToken.As)]
        [InlineData("FROM", SelectToken.From)]
        [InlineData("WHERE", SelectToken.Where)]
        [InlineData("ORDER", SelectToken.Order)]
        [InlineData("BY", SelectToken.By)]
        [InlineData("LIMIT", SelectToken.Limit)]
        [InlineData("select", SelectToken.Select)]
        public void TestKeywordsAreParsedCorrectly(string input, SelectToken expected)
        {
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            var token = Assert.Single(output);
            Assert.Equal(expected, token.Kind);
        }

        [Theory]
        [InlineData("test123", "test123")]
        [InlineData("TEST", "TEST")]
        [InlineData("\"Test\"", "\"Test\"")]
        [InlineData("\"SELECT\"", "\"SELECT\"")]
        public void TestIdentifiersAreParserCorrectly(string input, string expected)
        {
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            var token = Assert.Single(output);
            Assert.Equal(SelectToken.Identifier, token.Kind);
            Assert.Equal(expected, token.ToStringValue());
        }

        [Theory]
        [InlineData("'Test'", "'Test'")]
        [InlineData("'Te''st'", "'Te''st'")]
        public void TestStringLiteralsAreParserCorrectly(string input, string expected)
        {
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            var token = Assert.Single(output);
            Assert.Equal(SelectToken.StringLiteral, token.Kind);
            Assert.Equal(expected, token.ToStringValue());
        }

        [Theory]
        [InlineData("1", "1")]
        [InlineData("10", "10")]
        [InlineData("01", "01")]
        [InlineData("1.1", "1.1")]
        [InlineData("10.01", "10.01")]
        [InlineData("0.01", "0.01")]
        [InlineData("0.10", "0.10")]
        public void TestNumberLiteralsAreParserCorrectly(string input, string expected)
        {
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            var token = Assert.Single(output);
            Assert.Equal(SelectToken.NumberLiteral, token.Kind);
            Assert.Equal(expected, token.ToStringValue());
        }

        [Theory]
        [InlineData("TRUE", "TRUE")]
        [InlineData("true", "true")]
        [InlineData("FALSE", "FALSE")]
        [InlineData("false", "false")]
        public void TestBooleanLiteralsAreParserCorrectly(string input, string expected)
        {
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            var token = Assert.Single(output);
            Assert.Equal(SelectToken.BooleanLiteral, token.Kind);
            Assert.Equal(expected, token.ToStringValue());
        }

        [Theory]

        [InlineData(".", SelectToken.Dot)]
        [InlineData("*", SelectToken.Star)]

        [InlineData("!", SelectToken.Not)]
        [InlineData("-", SelectToken.Negate)]

        [InlineData("AND", SelectToken.And)]
        [InlineData("&&", SelectToken.And)]
        [InlineData("OR", SelectToken.Or)]
        [InlineData("||", SelectToken.Or)]

        [InlineData("<", SelectToken.Lesser)]
        [InlineData(">", SelectToken.Greater)]
        [InlineData("<=", SelectToken.LesserOrEqual)]
        [InlineData(">=", SelectToken.GreaterOrEqual)]
        [InlineData("=", SelectToken.Equal)]
        [InlineData("==", SelectToken.Equal)]
        [InlineData("!=", SelectToken.NotEqual)]
        [InlineData("<>", SelectToken.NotEqual)]

        [InlineData("+", SelectToken.Add)]
        [InlineData("/", SelectToken.Divide)]
        [InlineData("%", SelectToken.Modulo)]

        [InlineData("BETWEEN", SelectToken.Between)]
        [InlineData("IN", SelectToken.In)]
        [InlineData("LIKE", SelectToken.Like)]
        public void TestOperatorsAreParsedCorrectly(string input, SelectToken expected)
        {
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            var token = Assert.Single(output);
            Assert.Equal(expected, token.Kind);
        }

        [Fact]
        public void TestComplexQueryIsParsedCorrectly()
        {
            var input = "SELECT * FROM Test WHERE m = 5";
            var expected = new[]
            {
                SelectToken.Select, SelectToken.Star,
                SelectToken.From, SelectToken.Identifier,
                SelectToken.Where,SelectToken.Identifier,SelectToken.Equal,SelectToken.NumberLiteral
            };
            var tokenizer = new SelectTokenizer();

            var output = tokenizer.Tokenize(input).ToList();

            Assert.Equal(expected, output.Select(x => x.Kind));
        }
    }
}
