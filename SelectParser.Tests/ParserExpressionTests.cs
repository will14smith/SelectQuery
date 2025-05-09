﻿using SelectParser.Queries;
using Xunit;
using static SelectParser.Tests.ParserTestHelpers;

namespace SelectParser.Tests;

public class ParserExpressionTests
{
    #region BooleanOr

    [Fact]
    public void ParsingBooleanOr()
    {
        var input = "a AND b OR c";

        var result = Parse(Parser.BooleanOr, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Or, binary.Operator);
        var left = Assert.IsType<Expression.Binary>(binary.Left);
        Assert.Equal(BinaryOperator.And, left.Operator);
        AssertIdentifier("c", binary.Right);
    }
    [Fact]
    public void ParsingNopBooleanOr()
    {
        var input = "test";

        var result = Parse(Parser.BooleanOr, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region BooleanAnd

    [Fact]
    public void ParsingBooleanAnd()
    {
        var input = "NOT a AND b";

        var result = Parse(Parser.BooleanAnd, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.And, binary.Operator);
        var unary = Assert.IsType<Expression.Unary>(binary.Left);
        Assert.Equal(UnaryOperator.Not, unary.Operator);
        AssertIdentifier("b", binary.Right);
    }
    [Fact]
    public void ParsingNopBooleanAnd()
    {
        var input = "test";

        var result = Parse(Parser.BooleanAnd, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region BooleanUnary

    [Fact]
    public void ParsingBooleanUnary()
    {
        var input = "NOT a = b";

        var result = Parse(Parser.BooleanUnary, input);

        var expression = AssertSuccess(result);
        var unary = Assert.IsType<Expression.Unary>(expression);
        Assert.Equal(UnaryOperator.Not, unary.Operator);
        var binary = Assert.IsType<Expression.Binary>(unary.Expression);
        Assert.Equal(BinaryOperator.Equal, binary.Operator);
    }
    [Fact]
    public void ParsingNopBooleanUnary()
    {
        var input = "test";

        var result = Parse(Parser.BooleanUnary, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Equality

    [Fact]
    public void ParsingEquality()
    {
        var input = "a = b";

        var result = Parse(Parser.Equality, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Equal, binary.Operator);
        AssertIdentifier("a", binary.Left);
        AssertIdentifier("b", binary.Right);
    }
    [Fact]
    public void ParsingNopEquality()
    {
        var input = "test";

        var result = Parse(Parser.Equality, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Comparative

    [Fact]
    public void ParsingComparative()
    {
        var input = "a < b";

        var result = Parse(Parser.Comparative, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Lesser, binary.Operator);
        AssertIdentifier("a", binary.Left);
        AssertIdentifier("b", binary.Right);
    }
    [Fact]
    public void ParsingNopComparative()
    {
        var input = "test";

        var result = Parse(Parser.Comparative, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Pattern

    [Fact]
    public void ParsingPattern()
    {
        var input = "test LIKE '%'";

        var result = Parse(Parser.Pattern, input);

        var expression = AssertSuccess(result);
        var like = Assert.IsType<Expression.Like>(expression);
        AssertIdentifier("test", like.Expression);
        var pattern = Assert.IsType<Expression.StringLiteral>(like.Pattern);
        Assert.Equal("%", pattern.Value);
        AssertNone(like.Escape);
    }
    [Fact]
    public void ParsingPatternWithEscape()
    {
        var input = "test LIKE '%' ESCAPE 1";

        var result = Parse(Parser.Pattern, input);

        var expression = AssertSuccess(result);
        var like = Assert.IsType<Expression.Like>(expression);
        AssertIdentifier("test", like.Expression);
        var pattern = Assert.IsType<Expression.StringLiteral>(like.Pattern);
        Assert.Equal("%", pattern.Value);
        var escape = AssertSome(like.Escape);
        var escapeValue = Assert.IsType<Expression.NumberLiteral>(escape);
        Assert.Equal(1, escapeValue.Value);
    }
    [Fact]
    public void ParsingNopPattern()
    {
        var input = "test";

        var result = Parse(Parser.Pattern, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Containment

    [Fact]
    public void ParsingContainment()
    {
        var input = "test BETWEEN 1 AND 2 + 3";

        var result = Parse(Parser.Containment, input);

        var expression = AssertSuccess(result);
        var between = Assert.IsType<Expression.Between>(expression);
        Assert.False(between.Negate);
        AssertIdentifier("test", between.Expression);
        var lower = Assert.IsType<Expression.NumberLiteral>(between.Lower);
        Assert.Equal(1, lower.Value);
        Assert.IsType<Expression.Binary>(between.Upper);
    }
    [Fact]
    public void ParsingNotContainment()
    {
        var input = "test NOT BETWEEN 1 AND 2 + 3";

        var result = Parse(Parser.Containment, input);

        var expression = AssertSuccess(result);
        var between = Assert.IsType<Expression.Between>(expression);
        Assert.True(between.Negate);
        AssertIdentifier("test", between.Expression);
        var lower = Assert.IsType<Expression.NumberLiteral>(between.Lower);
        Assert.Equal(1, lower.Value);
        Assert.IsType<Expression.Binary>(between.Upper);
    }
    [Fact]
    public void ParsingNopContainment()
    {
        var input = "test";

        var result = Parse(Parser.Containment, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion
        
    #region Presence

    [Fact]
    public void ParsingPresence()
    {
        var input = "test IS NOT MISSING";

        var result = Parse(Parser.Presence, input);

        var expression = AssertSuccess(result);
        var presence = Assert.IsType<Expression.Presence>(expression);
        AssertIdentifier("test", presence.Expression);
        Assert.False(presence.Negate);
    }

    [Fact]
    public void ParsingNotPresence()
    {
        var input = "test IS MISSING";

        var result = Parse(Parser.Presence, input);

        var expression = AssertSuccess(result);
        var presence = Assert.IsType<Expression.Presence>(expression);
        Assert.True(presence.Negate);
    }

    [Fact]
    public void ParsingNopPresence()
    {
        var input = "test";

        var result = Parse(Parser.Presence, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region IsNull

    [Fact]
    public void ParsingIsNull()
    {
        var input = "test IS NULL";

        var result = Parse(Parser.IsNull, input);

        var expression = AssertSuccess(result);
        var presence = Assert.IsType<Expression.IsNull>(expression);
        AssertIdentifier("test", presence.Expression);
        Assert.False(presence.Negate);
    }

    [Fact]
    public void ParsingNotIsNull()
    {
        var input = "test IS NOT NULL";

        var result = Parse(Parser.Presence, input);

        var expression = AssertSuccess(result);
        var presence = Assert.IsType<Expression.IsNull>(expression);
        Assert.True(presence.Negate);
    }

    [Fact]
    public void ParsingNoIsNull()
    {
        var input = "test";

        var result = Parse(Parser.Presence, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion
        
    #region Membership

    [Fact]
    public void ParsingMembership()
    {
        var input = "test in (1, 2 + 3)";

        var result = Parse(Parser.Membership, input);

        var expression = AssertSuccess(result);
        var @in = Assert.IsType<Expression.In>(expression);
        AssertIdentifier("test", @in.Expression);
        Assert.Collection(@in.Matches,
            match =>
            {
                var num = Assert.IsType<Expression.NumberLiteral>(match);
                Assert.Equal(1, num.Value);
            },
            match =>
            {
                var binary = Assert.IsType<Expression.Binary>(match);
                Assert.Equal(BinaryOperator.Add, binary.Operator);
            }
        );
    }
    [Fact]
    public void ParsingMembershipAdditive()
    {
        var input = "123 + test IN (1, 2, 3)";

        var result = Parse(Parser.Membership, input);

        var expression = AssertSuccess(result);
        var @in = Assert.IsType<Expression.In>(expression);
        Assert.IsType<Expression.Binary>(@in.Expression);
    }
    [Fact]
    public void ParsingNopMembership()
    {
        var input = "test";

        var result = Parse(Parser.Membership, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Additive 

    [Fact]
    public void ParsingAdditive()
    {
        var input = "test + 123";

        var result = Parse(Parser.Additive, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Add, binary.Operator);
        AssertIdentifier("test", binary.Left);
        var number = Assert.IsType<Expression.NumberLiteral>(binary.Right);
        Assert.Equal(123, number.Value);

    }
    [Fact]
    public void ParsingAdditiveMultiplicative()
    {
        var input = "test * 123 + test";

        var result = Parse(Parser.Additive, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Add, binary.Operator);
        var left = Assert.IsType<Expression.Binary>(binary.Left);
        Assert.Equal(BinaryOperator.Multiply, left.Operator);
    }
    [Fact]
    public void ParsingNopAdditive()
    {
        var input = "test";

        var result = Parse(Parser.Additive, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Multiplicative 

    [Fact]
    public void ParsingMultiplicative()
    {
        var input = "test * 123";

        var result = Parse(Parser.Multiplicative, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Multiply, binary.Operator);
        AssertIdentifier("test", binary.Left);
        var number = Assert.IsType<Expression.NumberLiteral>(binary.Right);
        Assert.Equal(123, number.Value);

    }
    [Fact]
    public void ParsingMultiplicativeUnary()
    {
        var input = "-test * 123";

        var result = Parse(Parser.Multiplicative, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Multiply, binary.Operator);
        Assert.IsType<Expression.Unary>(binary.Left);
    }
    [Fact]
    public void ParsingNopMultiplicative()
    {
        var input = "test";

        var result = Parse(Parser.Multiplicative, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Unary 

    [Fact]
    public void ParsingNegate()
    {
        var input = "-test";

        var result = Parse(Parser.Unary, input);

        var expression = AssertSuccess(result);
        var negate = Assert.IsType<Expression.Unary>(expression);
        Assert.Equal(UnaryOperator.Negate, negate.Operator);
        AssertIdentifier("test", negate.Expression);
    }
    [Fact]
    public void ParsingNopNegate()
    {
        var input = "test";

        var result = Parse(Parser.Unary, input);

        var select = AssertSuccess(result);
        AssertIdentifier("test", select);
    }

    #endregion

    #region Term
        
    [Fact]
    public void ParsingIdentifier()
    {
        var input = "Test";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var identifier = Assert.IsType<Expression.Identifier>(expression);
        Assert.Equal("Test", identifier.Name);
        Assert.False(identifier.CaseSensitive);
    }
    [Fact]
    public void ParsingQuoted()
    {
        var input = "\"Test\"";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var identifier = Assert.IsType<Expression.Identifier>(expression);
        Assert.Equal("Test", identifier.Name);
        Assert.True(identifier.CaseSensitive);
    }
    [Fact]
    public void ParsingQualified()
    {
        var input = "a.b";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression);
        Assert.Equal(2, qualified.Identifiers.Count);
        AssertIdentifier("a", qualified.Identifiers[0]);
        AssertIdentifier("b", qualified.Identifiers[1]);
    }
    [Fact]
    public void ParsingDeepQualified()
    {
        var input = "a.b.c.d.e";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression);
        
        Assert.Equal(5, qualified.Identifiers.Count);
        AssertIdentifier("a", qualified.Identifiers[0]);
        AssertIdentifier("b", qualified.Identifiers[1]);
        AssertIdentifier("c", qualified.Identifiers[2]);
        AssertIdentifier("d", qualified.Identifiers[3]);
        AssertIdentifier("e", qualified.Identifiers[4]);
    }
    [Fact]
    public void ParsingQuotedQualified()
    {
        var input = "\"a\".b";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression);
        Assert.Equal(2, qualified.Identifiers.Count);
        AssertIdentifier("a", qualified.Identifiers[0], true);
        AssertIdentifier("b", qualified.Identifiers[1], false);
    }
    [Fact]
    public void ParsingQualifiedStar()
    {
        var input = "a.*";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression);
        Assert.Equal(2, qualified.Identifiers.Count);
        AssertIdentifier("a", qualified.Identifiers[0]);
        AssertIdentifier("*", qualified.Identifiers[1]);
    }
    [Fact]
    public void ParsingAggregateFunction()
    {
        var input = "AVG(Value)";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression);
        var aggregate = Assert.IsAssignableFrom<AggregateFunction>(function.Function);
        var average = Assert.IsType<AggregateFunction.Average>(aggregate);
        AssertIdentifier("Value", average.Expression);
    }
    [Fact]
    public void ParsingScalarFunction()
    {
        var input = "LOWER(Value)";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression);
        var scalar = Assert.IsType<ScalarFunction>(function.Function);
        AssertIdentifier("LOWER", scalar.Identifier);
        Assert.Single(scalar.Arguments);
        AssertIdentifier("Value", scalar.Arguments[0]);
    }
    [Fact]
    public void ParsingCountColumn()
    {
        var input = "COUNT(Value)";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression);
        var aggregate = Assert.IsAssignableFrom<AggregateFunction>(function.Function);
        var count = Assert.IsType<AggregateFunction.Count>(aggregate);
        Assert.True(count.Expression.IsSome);
        AssertIdentifier("Value", count.Expression.AsT0);
    }
    [Fact]
    public void ParsingCountStar()
    {
        var input = "COUNT(*)";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression);
        var aggregate = Assert.IsAssignableFrom<AggregateFunction>(function.Function);
        var count = Assert.IsType<AggregateFunction.Count>(aggregate);
        Assert.True(count.Expression.IsNone);
    }
    [Fact]
    public void ParsingNumberLiteral()
    {
        var input = "123";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var identifier = Assert.IsType<Expression.NumberLiteral>(expression);
        Assert.Equal(123, identifier.Value);
    }
    [Fact]
    public void ParsingStringLiteral()
    {
        var input = "'test'";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var identifier = Assert.IsType<Expression.StringLiteral>(expression);
        Assert.Equal("test", identifier.Value);
    }
    [Fact]
    public void ParsingBooleanLiteral()
    {
        var input = "true";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var identifier = Assert.IsType<Expression.BooleanLiteral>(expression);
        Assert.True(identifier.Value);
    }

    [Fact]
    public void ParsingBracketedExpression()
    {
        var input = "(a or b)";

        var result = Parse(Parser.Term, input);

        var expression = AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression);
        Assert.Equal(BinaryOperator.Or, binary.Operator);
    }
        
    #endregion
}