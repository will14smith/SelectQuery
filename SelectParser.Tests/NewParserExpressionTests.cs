using SelectParser.Queries;
using Xunit;

namespace SelectParser.Tests;

public class NewParserExpressionTests
{
    #region BooleanOr

    [Fact]
    public void ParsingBooleanOr()
    {
        var input = "a AND b OR c";

        var result = NewParserTestHelpers.Parse(NewParser.BooleanOr, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Or, binary.Operator);
        var left = Assert.IsType<Expression.Binary>(binary.Left.Value);
        Assert.Equal(BinaryOperator.And, left.Operator);
        NewParserTestHelpers.AssertIdentifier("c", binary.Right);
    }
    [Fact]
    public void ParsingNopBooleanOr()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.BooleanOr, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region BooleanAnd

    [Fact]
    public void ParsingBooleanAnd()
    {
        var input = "NOT a AND b";

        var result = NewParserTestHelpers.Parse(NewParser.BooleanAnd, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.And, binary.Operator);
        var unary = Assert.IsType<Expression.Unary>(binary.Left.Value);
        Assert.Equal(UnaryOperator.Not, unary.Operator);
        NewParserTestHelpers.AssertIdentifier("b", binary.Right);
    }
    [Fact]
    public void ParsingNopBooleanAnd()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.BooleanAnd, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region BooleanUnary

    [Fact]
    public void ParsingBooleanUnary()
    {
        var input = "NOT a = b";

        var result = NewParserTestHelpers.Parse(NewParser.BooleanUnary, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var unary = Assert.IsType<Expression.Unary>(expression.Value);
        Assert.Equal(UnaryOperator.Not, unary.Operator);
        var binary = Assert.IsType<Expression.Binary>(unary.Expression.Value);
        Assert.Equal(BinaryOperator.Equal, binary.Operator);
    }
    [Fact]
    public void ParsingNopBooleanUnary()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.BooleanUnary, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Equality

    [Fact]
    public void ParsingEquality()
    {
        var input = "a = b";

        var result = NewParserTestHelpers.Parse(NewParser.Equality, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Equal, binary.Operator);
        NewParserTestHelpers.AssertIdentifier("a", binary.Left);
        NewParserTestHelpers.AssertIdentifier("b", binary.Right);
    }
    [Fact]
    public void ParsingNopEquality()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Equality, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Comparative

    [Fact]
    public void ParsingComparative()
    {
        var input = "a < b";

        var result = NewParserTestHelpers.Parse(NewParser.Comparative, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Lesser, binary.Operator);
        NewParserTestHelpers.AssertIdentifier("a", binary.Left);
        NewParserTestHelpers.AssertIdentifier("b", binary.Right);
    }
    [Fact]
    public void ParsingNopComparative()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Comparative, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Pattern

    [Fact]
    public void ParsingPattern()
    {
        var input = "test LIKE '%'";

        var result = NewParserTestHelpers.Parse(NewParser.Pattern, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var like = Assert.IsType<Expression.Like>(expression.Value);
        NewParserTestHelpers.AssertIdentifier("test", like.Expression);
        var pattern = Assert.IsType<Expression.StringLiteral>(like.Pattern.Value);
        Assert.Equal("%", pattern.Value);
        NewParserTestHelpers.AssertNone(like.Escape);
    }
    [Fact]
    public void ParsingPatternWithEscape()
    {
        var input = "test LIKE '%' ESCAPE 1";

        var result = NewParserTestHelpers.Parse(NewParser.Pattern, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var like = Assert.IsType<Expression.Like>(expression.Value);
        NewParserTestHelpers.AssertIdentifier("test", like.Expression);
        var pattern = Assert.IsType<Expression.StringLiteral>(like.Pattern.Value);
        Assert.Equal("%", pattern.Value);
        var escape = NewParserTestHelpers.AssertSome(like.Escape);
        var escapeValue = Assert.IsType<Expression.NumberLiteral>(escape.Value);
        Assert.Equal(1, escapeValue.Value);
    }
    [Fact]
    public void ParsingNopPattern()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Pattern, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Containment

    [Fact]
    public void ParsingContainment()
    {
        var input = "test BETWEEN 1 AND 2 + 3";

        var result = NewParserTestHelpers.Parse(NewParser.Containment, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var between = Assert.IsType<Expression.Between>(expression.Value);
        Assert.False(between.Negate);
        NewParserTestHelpers.AssertIdentifier("test", between.Expression);
        var lower = Assert.IsType<Expression.NumberLiteral>(between.Lower.Value);
        Assert.Equal(1, lower.Value);
        Assert.IsType<Expression.Binary>(between.Upper.Value);
    }
    [Fact]
    public void ParsingNotContainment()
    {
        var input = "test NOT BETWEEN 1 AND 2 + 3";

        var result = NewParserTestHelpers.Parse(NewParser.Containment, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var between = Assert.IsType<Expression.Between>(expression.Value);
        Assert.True(between.Negate);
        NewParserTestHelpers.AssertIdentifier("test", between.Expression);
        var lower = Assert.IsType<Expression.NumberLiteral>(between.Lower.Value);
        Assert.Equal(1, lower.Value);
        Assert.IsType<Expression.Binary>(between.Upper.Value);
    }
    [Fact]
    public void ParsingNopContainment()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Containment, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion
        
    #region Presence

    [Fact]
    public void ParsingPresence()
    {
        var input = "test IS NOT MISSING";

        var result = NewParserTestHelpers.Parse(NewParser.Presence, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var presence = Assert.IsType<Expression.Presence>(expression.Value);
        NewParserTestHelpers.AssertIdentifier("test", presence.Expression);
        Assert.False(presence.Negate);
    }

    [Fact]
    public void ParsingNotPresence()
    {
        var input = "test IS MISSING";

        var result = NewParserTestHelpers.Parse(NewParser.Presence, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var presence = Assert.IsType<Expression.Presence>(expression.Value);
        Assert.True(presence.Negate);
    }

    [Fact]
    public void ParsingNopPresence()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Presence, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region IsNull

    [Fact]
    public void ParsingIsNull()
    {
        var input = "test IS NULL";

        var result = NewParserTestHelpers.Parse(NewParser.IsNull, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var presence = Assert.IsType<Expression.IsNull>(expression.Value);
        NewParserTestHelpers.AssertIdentifier("test", presence.Expression);
        Assert.False(presence.Negate);
    }

    [Fact]
    public void ParsingNotIsNull()
    {
        var input = "test IS NOT NULL";

        var result = NewParserTestHelpers.Parse(NewParser.Presence, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var presence = Assert.IsType<Expression.IsNull>(expression.Value);
        Assert.True(presence.Negate);
    }

    [Fact]
    public void ParsingNoIsNull()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Presence, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion
        
    #region Membership

    [Fact]
    public void ParsingMembership()
    {
        var input = "test in (1, 2 + 3)";

        var result = NewParserTestHelpers.Parse(NewParser.Membership, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var @in = Assert.IsType<Expression.In>(expression.Value);
        NewParserTestHelpers.AssertIdentifier("test", @in.Expression);
        Assert.Collection(@in.Matches,
            match =>
            {
                var num = Assert.IsType<Expression.NumberLiteral>(match.Value);
                Assert.Equal(1, num.Value);
            },
            match =>
            {
                var binary = Assert.IsType<Expression.Binary>(match.Value);
                Assert.Equal(BinaryOperator.Add, binary.Operator);
            }
        );
    }
    [Fact]
    public void ParsingMembershipAdditive()
    {
        var input = "123 + test IN (1, 2, 3)";

        var result = NewParserTestHelpers.Parse(NewParser.Membership, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var @in = Assert.IsType<Expression.In>(expression.Value);
        Assert.IsType<Expression.Binary>(@in.Expression.Value);
    }
    [Fact]
    public void ParsingNopMembership()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Membership, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Additive 

    [Fact]
    public void ParsingAdditive()
    {
        var input = "test + 123";

        var result = NewParserTestHelpers.Parse(NewParser.Additive, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Add, binary.Operator);
        NewParserTestHelpers.AssertIdentifier("test", binary.Left);
        var number = Assert.IsType<Expression.NumberLiteral>(binary.Right.Value);
        Assert.Equal(123, number.Value);

    }
    [Fact]
    public void ParsingAdditiveMultiplicative()
    {
        var input = "test * 123 + test";

        var result = NewParserTestHelpers.Parse(NewParser.Additive, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Add, binary.Operator);
        var left = Assert.IsType<Expression.Binary>(binary.Left.Value);
        Assert.Equal(BinaryOperator.Multiply, left.Operator);
    }
    [Fact]
    public void ParsingNopAdditive()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Additive, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Multiplicative 

    [Fact]
    public void ParsingMultiplicative()
    {
        var input = "test * 123";

        var result = NewParserTestHelpers.Parse(NewParser.Multiplicative, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Multiply, binary.Operator);
        NewParserTestHelpers.AssertIdentifier("test", binary.Left);
        var number = Assert.IsType<Expression.NumberLiteral>(binary.Right.Value);
        Assert.Equal(123, number.Value);

    }
    [Fact]
    public void ParsingMultiplicativeUnary()
    {
        var input = "-test * 123";

        var result = NewParserTestHelpers.Parse(NewParser.Multiplicative, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Multiply, binary.Operator);
        Assert.IsType<Expression.Unary>(binary.Left.Value);
    }
    [Fact]
    public void ParsingNopMultiplicative()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Multiplicative, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Unary 

    [Fact]
    public void ParsingNegate()
    {
        var input = "-test";

        var result = NewParserTestHelpers.Parse(NewParser.Unary, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var negate = Assert.IsType<Expression.Unary>(expression.Value);
        Assert.Equal(UnaryOperator.Negate, negate.Operator);
        NewParserTestHelpers.AssertIdentifier("test", negate.Expression);
    }
    [Fact]
    public void ParsingNopNegate()
    {
        var input = "test";

        var result = NewParserTestHelpers.Parse(NewParser.Unary, input);

        var select = NewParserTestHelpers.AssertSuccess(result);
        NewParserTestHelpers.AssertIdentifier("test", select);
    }

    #endregion

    #region Term
        
    [Fact]
    public void ParsingIdentifier()
    {
        var input = "Test";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var identifier = Assert.IsType<Expression.Identifier>(expression.Value);
        Assert.Equal("Test", identifier.Name);
        Assert.False(identifier.CaseSensitive);
    }
    [Fact]
    public void ParsingQuoted()
    {
        var input = "\"Test\"";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var identifier = Assert.IsType<Expression.Identifier>(expression.Value);
        Assert.Equal("Test", identifier.Name);
        Assert.True(identifier.CaseSensitive);
    }
    [Fact]
    public void ParsingQualified()
    {
        var input = "a.b";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression.Value);
        Assert.Equal("a", qualified.Qualification.Name);
        NewParserTestHelpers.AssertIdentifier("b", qualified.Expression);
    }
    [Fact]
    public void ParsingDeepQualified()
    {
        var input = "a.b.c.d.e";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression.Value);
        NewParserTestHelpers.AssertIdentifier("a", qualified.Qualification);
        qualified = qualified.Expression.AsT4;
        NewParserTestHelpers.AssertIdentifier("b", qualified.Qualification);
        qualified = qualified.Expression.AsT4;
        NewParserTestHelpers.AssertIdentifier("c", qualified.Qualification);
        qualified = qualified.Expression.AsT4;
        NewParserTestHelpers.AssertIdentifier("d", qualified.Qualification);
        NewParserTestHelpers.AssertIdentifier("e", qualified.Expression);
    }
    [Fact]
    public void ParsingQuotedQualified()
    {
        var input = "\"a\".b";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression.Value);
        Assert.Equal("a", qualified.Qualification.Name);
        Assert.True(qualified.Qualification.CaseSensitive);
        NewParserTestHelpers.AssertIdentifier("b", qualified.Expression);
    }
    [Fact]
    public void ParsingQualifiedStar()
    {
        var input = "a.*";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var qualified = Assert.IsType<Expression.Qualified>(expression.Value);
        Assert.Equal("a", qualified.Qualification.Name);
        NewParserTestHelpers.AssertIdentifier("*", qualified.Expression);
    }
    [Fact]
    public void ParsingAggregateFunction()
    {
        var input = "AVG(Value)";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression.Value);
        var aggregate = Assert.IsType<AggregateFunction>(function.Function.Value);
        var average = Assert.IsType<AggregateFunction.Average>(aggregate.Value);
        NewParserTestHelpers.AssertIdentifier("Value", average.Expression);
    }
    [Fact]
    public void ParsingScalarFunction()
    {
        var input = "LOWER(Value)";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression.Value);
        var scalar = Assert.IsType<ScalarFunction>(function.Function.Value);
        NewParserTestHelpers.AssertIdentifier("LOWER", scalar.Identifier);
        Assert.Equal(1, scalar.Arguments.Count);
        NewParserTestHelpers.AssertIdentifier("Value", scalar.Arguments[0]);
    }
    [Fact]
    public void ParsingCountColumn()
    {
        var input = "COUNT(Value)";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression.Value);
        var aggregate = Assert.IsType<AggregateFunction>(function.Function.Value);
        var count = Assert.IsType<AggregateFunction.Count>(aggregate.Value);
        Assert.True(count.Expression.IsSome);
        NewParserTestHelpers.AssertIdentifier("Value", count.Expression.AsT0);
    }
    [Fact]
    public void ParsingCountStar()
    {
        var input = "COUNT(*)";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var function = Assert.IsType<Expression.FunctionExpression>(expression.Value);
        var aggregate = Assert.IsType<AggregateFunction>(function.Function.Value);
        var count = Assert.IsType<AggregateFunction.Count>(aggregate.Value);
        Assert.True(count.Expression.IsNone);
    }
    [Fact]
    public void ParsingNumberLiteral()
    {
        var input = "123";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var identifier = Assert.IsType<Expression.NumberLiteral>(expression.Value);
        Assert.Equal(123, identifier.Value);
    }
    [Fact]
    public void ParsingStringLiteral()
    {
        var input = "'test'";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var identifier = Assert.IsType<Expression.StringLiteral>(expression.Value);
        Assert.Equal("test", identifier.Value);
    }
    [Fact]
    public void ParsingBooleanLiteral()
    {
        var input = "true";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var identifier = Assert.IsType<Expression.BooleanLiteral>(expression.Value);
        Assert.True(identifier.Value);
    }

    [Fact]
    public void ParsingBracketedExpression()
    {
        var input = "(a or b)";

        var result = NewParserTestHelpers.Parse(NewParser.Term, input);

        var expression = NewParserTestHelpers.AssertSuccess(result);
        var binary = Assert.IsType<Expression.Binary>(expression.Value);
        Assert.Equal(BinaryOperator.Or, binary.Operator);
    }
        
    #endregion
}