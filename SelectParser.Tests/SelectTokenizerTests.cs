using Xunit;

namespace SelectParser.Tests;

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
    [InlineData("OFFSET", SelectToken.Offset)]
    [InlineData("ASC", SelectToken.Asc)]
    [InlineData("DESC", SelectToken.Desc)]
    [InlineData("IS", SelectToken.Is)]
    [InlineData("MISSING", SelectToken.Missing)]
    [InlineData("select", SelectToken.Select)]
    public void TestKeywordsAreParsedCorrectly(string input, SelectToken expected)
    {
        var tokenizer = new SelectTokenizer(input);

        var token = SelectTokenizer.Read(ref tokenizer);

        Assert.Equal(expected, token.Type);
    }

    [Theory]
    [InlineData("test123", "test123")]
    [InlineData("TEST", "TEST")]
    [InlineData("_", "_")]
    [InlineData("_1", "_1")]
    [InlineData("\"Test\"", "\"Test\"")]
    [InlineData("\"SELECT\"", "\"SELECT\"")]
    public void TestIdentifiersAreParserCorrectly(string input, string expected)
    {
        var tokenizer = new SelectTokenizer(input);

        var token = SelectTokenizer.Read(ref tokenizer);

        Assert.Equal(SelectToken.Identifier, token.Type);
        Assert.Equal(expected, token.ToStringValue());
    }

    [Theory]
    [InlineData("'Test'", "'Test'")]
    [InlineData("'Te''st'", "'Te''st'")]
    public void TestStringLiteralsAreParserCorrectly(string input, string expected)
    {
        var tokenizer = new SelectTokenizer(input);

        var token = SelectTokenizer.Read(ref tokenizer);

        Assert.Equal(SelectToken.StringLiteral, token.Type);
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
        var tokenizer = new SelectTokenizer(input);

        var token = SelectTokenizer.Read(ref tokenizer);

        Assert.Equal(SelectToken.NumberLiteral, token.Type);
        Assert.Equal(expected, token.ToStringValue());
    }

    [Theory]
    [InlineData("TRUE", "TRUE")]
    [InlineData("true", "true")]
    [InlineData("FALSE", "FALSE")]
    [InlineData("false", "false")]
    public void TestBooleanLiteralsAreParserCorrectly(string input, string expected)
    {
        var tokenizer = new SelectTokenizer(input);

        var token = SelectTokenizer.Read(ref tokenizer);

        Assert.Equal(SelectToken.BooleanLiteral, token.Type);
        Assert.Equal(expected, token.ToStringValue());
    }

    [Theory]

    [InlineData(".", SelectToken.Dot)]
    [InlineData(",", SelectToken.Comma)]
    [InlineData("(", SelectToken.LeftBracket)]
    [InlineData(")", SelectToken.RightBracket)]
    [InlineData("*", SelectToken.Star)]

    [InlineData("NOT", SelectToken.Not)]
    [InlineData("!", SelectToken.Not)]
    [InlineData("-", SelectToken.Negate)]

    [InlineData("AND", SelectToken.And)]
    [InlineData("&&", SelectToken.And)]
    [InlineData("OR", SelectToken.Or)]

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
    [InlineData("||", SelectToken.Concat)]

    [InlineData("BETWEEN", SelectToken.Between)]
    [InlineData("IN", SelectToken.In)]
    [InlineData("LIKE", SelectToken.Like)]
    [InlineData("ESCAPE", SelectToken.Escape)]
    public void TestOperatorsAreParsedCorrectly(string input, SelectToken expected)
    {
        var tokenizer = new SelectTokenizer(input);

        var token = SelectTokenizer.Read(ref tokenizer);

        Assert.Equal(expected, token.Type);
    }

    [Fact]
    public void TestComplexQueryIsParsedCorrectly()
    {
        var input = "SELECT * FROM Test WHERE m = 5";
        var expectedTokens = new[]
        {
            SelectToken.Select, SelectToken.Star,
            SelectToken.From, SelectToken.Identifier,
            SelectToken.Where,SelectToken.Identifier,SelectToken.Equal,SelectToken.NumberLiteral,
            SelectToken.Eof
        };
        var tokenizer = new SelectTokenizer(input);

        foreach (var expectedToken in expectedTokens)
        {
            var token = SelectTokenizer.Read(ref tokenizer);
            Assert.Equal(expectedToken, token.Type);
        }
    }
}