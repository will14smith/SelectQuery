using SelectParser.Queries;
using Superpower;
using Superpower.Model;
using Xunit;

namespace SelectParser.Tests
{
    public class ParserTestHelpers
    {
        public static TokenListParserResult<SelectToken, T> Parse<T>(TokenListParser<SelectToken, T> parser, string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return parser.AtEnd().TryParse(tokens);
        }
        
        public static T AssertSuccess<T>(TokenListParserResult<SelectToken, T> result)
        {
            Assert.True(result.HasValue, result.ToString());
            return result.Value;
        }

        public static void AssertFailed<T>(TokenListParserResult<SelectToken, T> result)
        {
            Assert.False(result.HasValue);
        }

        public static T AssertSome<T>(Option<T> option)
        {
            Assert.True(option.IsSome);
            return option.AsT0;
        }
        public static void AssertNone<T>(Option<T> option)
        {
            Assert.True(option.IsNone);
        }

        public static void AssertIdentifier(string expected, Expression expression)
        {
            var identifier = Assert.IsType<Expression.Identifier>(expression.Value);
            Assert.Equal(expected, identifier.Name);
        }
    }
}