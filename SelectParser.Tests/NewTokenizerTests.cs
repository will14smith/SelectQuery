using Xunit;

namespace SelectParser.Tests;

public class NewTokenizerTests
{
    [Theory]
    [InlineData("SELECT", NewTokenizer.TokenType.Select)]
    [InlineData("AS", NewTokenizer.TokenType.As)]
    [InlineData("FROM", NewTokenizer.TokenType.From)]
    [InlineData("WHERE", NewTokenizer.TokenType.Where)]
    [InlineData("ORDER", NewTokenizer.TokenType.Order)]
    [InlineData("BY", NewTokenizer.TokenType.By)]
    [InlineData("LIMIT", NewTokenizer.TokenType.Limit)]
    [InlineData("OFFSET", NewTokenizer.TokenType.Offset)]
    [InlineData("ASC", NewTokenizer.TokenType.Asc)]
    [InlineData("DESC", NewTokenizer.TokenType.Desc)]
    [InlineData("IS", NewTokenizer.TokenType.Is)]
    [InlineData("MISSING", NewTokenizer.TokenType.Missing)]
    [InlineData("select", NewTokenizer.TokenType.Select)]
    public void TestKeywordsAreParsedCorrectly(string input, NewTokenizer.TokenType expected)
    {
        var tokenizer = new NewTokenizer(input);

        var token = NewTokenizer.Read(ref tokenizer);

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
        var tokenizer = new NewTokenizer(input);

        var token = NewTokenizer.Read(ref tokenizer);

        Assert.Equal(NewTokenizer.TokenType.Identifier, token.Type);
        Assert.Equal(expected, token.ToStringValue());
    }

    [Theory]
    [InlineData("'Test'", "'Test'")]
    [InlineData("'Te''st'", "'Te''st'")]
    public void TestStringLiteralsAreParserCorrectly(string input, string expected)
    {
        var tokenizer = new NewTokenizer(input);

        var token = NewTokenizer.Read(ref tokenizer);

        Assert.Equal(NewTokenizer.TokenType.StringLiteral, token.Type);
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
        var tokenizer = new NewTokenizer(input);

        var token = NewTokenizer.Read(ref tokenizer);

        Assert.Equal(NewTokenizer.TokenType.NumberLiteral, token.Type);
        Assert.Equal(expected, token.ToStringValue());
    }

    [Theory]
    [InlineData("TRUE", "TRUE")]
    [InlineData("true", "true")]
    [InlineData("FALSE", "FALSE")]
    [InlineData("false", "false")]
    public void TestBooleanLiteralsAreParserCorrectly(string input, string expected)
    {
        var tokenizer = new NewTokenizer(input);

        var token = NewTokenizer.Read(ref tokenizer);

        Assert.Equal(NewTokenizer.TokenType.BooleanLiteral, token.Type);
        Assert.Equal(expected, token.ToStringValue());
    }

    [Theory]

    [InlineData(".", NewTokenizer.TokenType.Dot)]
    [InlineData(",", NewTokenizer.TokenType.Comma)]
    [InlineData("(", NewTokenizer.TokenType.LeftBracket)]
    [InlineData(")", NewTokenizer.TokenType.RightBracket)]
    [InlineData("*", NewTokenizer.TokenType.Star)]

    [InlineData("NOT", NewTokenizer.TokenType.Not)]
    [InlineData("!", NewTokenizer.TokenType.Not)]
    [InlineData("-", NewTokenizer.TokenType.Negate)]

    [InlineData("AND", NewTokenizer.TokenType.And)]
    [InlineData("&&", NewTokenizer.TokenType.And)]
    [InlineData("OR", NewTokenizer.TokenType.Or)]
    [InlineData("||", NewTokenizer.TokenType.Or)]

    [InlineData("<", NewTokenizer.TokenType.Lesser)]
    [InlineData(">", NewTokenizer.TokenType.Greater)]
    [InlineData("<=", NewTokenizer.TokenType.LesserOrEqual)]
    [InlineData(">=", NewTokenizer.TokenType.GreaterOrEqual)]
    [InlineData("=", NewTokenizer.TokenType.Equal)]
    [InlineData("==", NewTokenizer.TokenType.Equal)]
    [InlineData("!=", NewTokenizer.TokenType.NotEqual)]
    [InlineData("<>", NewTokenizer.TokenType.NotEqual)]

    [InlineData("+", NewTokenizer.TokenType.Add)]
    [InlineData("/", NewTokenizer.TokenType.Divide)]
    [InlineData("%", NewTokenizer.TokenType.Modulo)]

    [InlineData("BETWEEN", NewTokenizer.TokenType.Between)]
    [InlineData("IN", NewTokenizer.TokenType.In)]
    [InlineData("LIKE", NewTokenizer.TokenType.Like)]
    [InlineData("ESCAPE", NewTokenizer.TokenType.Escape)]
    public void TestOperatorsAreParsedCorrectly(string input, NewTokenizer.TokenType expected)
    {
        var tokenizer = new NewTokenizer(input);

        var token = NewTokenizer.Read(ref tokenizer);

        Assert.Equal(expected, token.Type);
    }

    [Fact]
    public void TestComplexQueryIsParsedCorrectly()
    {
        var input = "SELECT * FROM Test WHERE m = 5";
        var expectedTokens = new[]
        {
            NewTokenizer.TokenType.Select, NewTokenizer.TokenType.Star,
            NewTokenizer.TokenType.From, NewTokenizer.TokenType.Identifier,
            NewTokenizer.TokenType.Where,NewTokenizer.TokenType.Identifier,NewTokenizer.TokenType.Equal,NewTokenizer.TokenType.NumberLiteral,
            NewTokenizer.TokenType.Eof
        };
        var tokenizer = new NewTokenizer(input);

        foreach (var expectedToken in expectedTokens)
        {
            var token = NewTokenizer.Read(ref tokenizer);
            Assert.Equal(expectedToken, token.Type);
        }
    }
}