using SelectParser.Queries;
using Xunit;

namespace SelectParser.Tests;

public static class ParserTestHelpers
{
    public delegate Parser.Result<T> ParseFn<T>(ref SelectTokenizer tokenizer);

    public static Parser.Result<T> Parse<T>(ParseFn<T> parser, string input)
    {
        var tokenizer = new SelectTokenizer(input);
        var result = parser(ref tokenizer);

        var final = SelectTokenizer.Read(ref tokenizer);
        if (final.Type != SelectToken.Eof)
        {
            if (!result.Success)
            {
                Assert.Fail($"Expected EOF, but got error: {result.Error}");
            }
            
            Assert.Fail($"Expected EOF, but got {final.Type}");
        }

        return result;
    }
        
    public static T AssertSuccess<T>(Parser.Result<T> result)
    {
        Assert.True(result.Success, result.Error ?? "unknown error");
        return result.Value!;
    }

    public static void AssertFailed<T>(Parser.Result<T> result)
    {
        Assert.False(result.Success, "parse succeeded but shouldn't have");
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