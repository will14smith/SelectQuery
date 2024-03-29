using System;
using SelectParser.Queries;
using Xunit;

namespace SelectParser.Tests;

public class NewParserTestHelpers
{
    public delegate NewParser.Result<T> ParseFn<T>(ref NewTokenizer tokenizer);

    public static NewParser.Result<T> Parse<T>(ParseFn<T> parser, string input)
    {
        var tokenizer = new NewTokenizer(input);
        var result = parser(ref tokenizer);

        var final = NewTokenizer.Read(ref tokenizer);
        if (final.Type != NewTokenizer.TokenType.Eof)
        {
            if (!result.Success)
            {
                Assert.False(true, $"Expected EOF, but got error: {result.Error}");
            }
            
            Assert.False(true, $"Expected EOF, but got {final.Type}");
        }

        return result;
    }
        
    public static T AssertSuccess<T>(NewParser.Result<T> result)
    {
        Assert.True(result.Success, result.Error ?? "unknown error");
        return result.Value!;
    }

    public static void AssertFailed<T>(NewParser.Result<T> result)
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