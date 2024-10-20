﻿using System;
using System.Linq;
using JetBrains.Annotations;
using OneOf.Types;
using SelectParser.Queries;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SelectParser;

public class Parser
{
    private static readonly TokenListParser<SelectToken, (string Identifier, bool CaseSensitive)> Identifier =
        Token.EqualTo(SelectToken.Identifier)
            .Select(ParseIdentifier);

    private static readonly TokenListParser<SelectToken, decimal> Number =
        Token.EqualTo(SelectToken.NumberLiteral)
            .Select(x => decimal.Parse(x.ToStringValue()));

    private static readonly TokenListParser<SelectToken, string> String =
        Token.EqualTo(SelectToken.StringLiteral)
            .Select(ParseString);

    private static readonly TokenListParser<SelectToken, bool> Boolean =
        Token.EqualTo(SelectToken.BooleanLiteral)
            .Select(x => bool.Parse(x.ToStringValue()));


    private static readonly TokenListParser<SelectToken, string> Alias =
        Token.Sequence(SelectToken.As, SelectToken.Identifier).Select(x => ParseIdentifier(x[1]).Identifier)
            .Or(Identifier.Select(x => x.Identifier));

    #region function

    private static readonly TokenListParser<SelectToken, Function> AvgFunction = SingleParameterFunction(SelectToken.Avg, x => new AggregateFunction.Average(x));
    private static readonly TokenListParser<SelectToken, Function> CountFunction = SingleParameterFunction(SelectToken.Count, BuildCountFunction);
    private static readonly TokenListParser<SelectToken, Function> MaxFunction = SingleParameterFunction(SelectToken.Max, x => new AggregateFunction.Max(x));
    private static readonly TokenListParser<SelectToken, Function> MinFunction = SingleParameterFunction(SelectToken.Min, x => new AggregateFunction.Min(x));
    private static readonly TokenListParser<SelectToken, Function> SumFunction = SingleParameterFunction(SelectToken.Sum, x => new AggregateFunction.Sum(x));

    private static readonly TokenListParser<SelectToken, Function> AggregateFunction = AvgFunction.Or(CountFunction).Or(MaxFunction).Or(MinFunction).Or(SumFunction);

    private static AggregateFunction BuildCountFunction(Expression expression)
    {
        if (expression.Value is Expression.Identifier { Name: "*" })
        {
            return new AggregateFunction.Count(new None());
        }
            
        return new AggregateFunction.Count(expression);
    }

    private static readonly TokenListParser<SelectToken, Function> ScalarFunction =
        from name in Token.EqualTo(SelectToken.Identifier)
        from begin in Token.EqualTo(SelectToken.LeftBracket)
        from expr in Superpower.Parse.Ref(() => Expression)
        from end in Token.EqualTo(SelectToken.RightBracket)
        select (Function) new ScalarFunction(new Expression.Identifier(name.ToStringValue(), false), new [] { expr });

    private static readonly TokenListParser<SelectToken, Function> Function = AggregateFunction.Or(ScalarFunction.Try());
    private static readonly TokenListParser<SelectToken, Expression> FunctionExpression = Function.Select(x => (Expression) new Expression.FunctionExpression(x));

    private static TokenListParser<SelectToken, Function> SingleParameterFunction(SelectToken nameToken, Func<Expression, AggregateFunction> constructor) => SingleParameterFunction(nameToken, x => (Function) constructor(x));
    private static TokenListParser<SelectToken, Function> SingleParameterFunction(SelectToken nameToken, Func<Expression, Function> constructor) =>
        from name in Token.EqualTo(nameToken)
        from begin in Token.EqualTo(SelectToken.LeftBracket)
        from expr in Superpower.Parse.Ref(() => Expression)
        from end in Token.EqualTo(SelectToken.RightBracket)
        select constructor(expr);

    #endregion
        
    #region expression 

    private static readonly TokenListParser<SelectToken, Expression> QualifiedIdentifier =
        Identifier
            .Or(Token.EqualTo(SelectToken.Star).Select(_ => ("*", false)))
            .Select(x => new Expression.Identifier(x.Item1, x.Item2))
            .AtLeastOnceDelimitedBy(Token.EqualTo(SelectToken.Dot))
            .Select(BuildQualified);
    private static Expression BuildQualified(Expression.Identifier[] identifiers)
    {
        switch (identifiers.Length)
        {
            case 0: throw new InvalidOperationException("Cannot build a qualified expression from no identifiers");
            case 1: return identifiers[0];
        }

        Expression result = identifiers[identifiers.Length - 1];
        for (var i = identifiers.Length - 2; i >= 0; i--)
        {
            result = new Expression.Qualified(identifiers[i], result);
        }
        return result;
    }

    public static readonly TokenListParser<SelectToken, Expression> BracketExpression =
        from begin in Token.EqualTo(SelectToken.LeftBracket)
        from expr in Superpower.Parse.Ref(() => Expression)
        from end in Token.EqualTo(SelectToken.RightBracket)
        select expr;

    public static readonly TokenListParser<SelectToken, Expression> Term =
        FunctionExpression
            .Or(QualifiedIdentifier)
            .Or(Number.Select(x => (Expression)new Expression.NumberLiteral(x)))
            .Or(String.Select(x => (Expression)new Expression.StringLiteral(x)))
            .Or(Boolean.Select(x => (Expression)new Expression.BooleanLiteral(x)))
            .Or(BracketExpression);

    public static readonly TokenListParser<SelectToken, Expression> Unary =
        (
            from op in Token.EqualTo(SelectToken.Negate)
            from term in Term
            select (Expression)new Expression.Unary(UnaryOperator.Negate, term)
        )
        .Or(Term);

    public static readonly TokenListParser<SelectToken, Expression> Multiplicative =
        Superpower.Parse.Chain(
            Token.EqualTo(SelectToken.Star).Or(Token.EqualTo(SelectToken.Divide)).Or(Token.EqualTo(SelectToken.Modulo)),
            Unary,
            CreateMultiplicative
        );
    private static Expression CreateMultiplicative(Token<SelectToken> opToken, Expression left, Expression right)
    {
        var operation = opToken.Kind switch
        {
            SelectToken.Star => BinaryOperator.Multiply,
            SelectToken.Divide => BinaryOperator.Divide,
            SelectToken.Modulo => BinaryOperator.Modulo,
                
            _ => throw new InvalidOperationException("Operation should be a multiplicative token")
        };

        return new Expression.Binary(operation, left, right);
    }

    public static readonly TokenListParser<SelectToken, Expression> Additive =
        Superpower.Parse.Chain(
            Token.EqualTo(SelectToken.Add).Or(Token.EqualTo(SelectToken.Negate)),
            Multiplicative,
            CreateAdditive
        );
    private static Expression CreateAdditive(Token<SelectToken> opToken, Expression left, Expression right)
    {
        var operation = opToken.Kind switch
        {
            SelectToken.Add => BinaryOperator.Add,
            SelectToken.Negate => BinaryOperator.Subtract,
                
            _ => throw new InvalidOperationException("Operation should be a additive token")
        };

        return new Expression.Binary(operation, left, right);
    }

    public static readonly TokenListParser<SelectToken, Expression> Membership = ParseOptionalOrDefaultSuffix(
        Additive,
        (
            from @in in Token.EqualTo(SelectToken.In)
            from begin in Token.EqualTo(SelectToken.LeftBracket)
            from values in Superpower.Parse.Ref(() => Expression).ManyDelimitedBy(
                Token.EqualTo(SelectToken.Comma),
                Token.EqualTo(SelectToken.RightBracket))
            select values
        ),
        (expr, suffix) => new Expression.In(expr, suffix)
    );

    public static readonly TokenListParser<SelectToken, Expression> IsNull = ParseOptionalSuffix(
        Membership,
        (
            from @is in Token.EqualTo(SelectToken.Is)
            from not in Token.EqualTo(SelectToken.Not).Optional()
            from @null in Token.EqualTo(SelectToken.Null)
            select not.HasValue
        ),
        (expr, suffix) => new Expression.IsNull(expr, suffix)
    );

    public static readonly TokenListParser<SelectToken, Expression> Presence = ParseOptionalSuffix(
        IsNull,
        (
            from @is in Token.EqualTo(SelectToken.Is)
            from not in Token.EqualTo(SelectToken.Not).Optional()
            from missing in Token.EqualTo(SelectToken.Missing)
            select not.HasValue
        ),
        (expr, suffix) => new Expression.Presence(expr, !suffix)
    );

    public static readonly TokenListParser<SelectToken, Expression> Containment = ParseOptionalSuffix(
        Presence,
        (
            from not in Token.EqualTo(SelectToken.Not).Optional()
            from between in Token.EqualTo(SelectToken.Between)
            from lower in Presence
            from and in Token.EqualTo(SelectToken.And)
            from upper in Superpower.Parse.Ref(() => Expression)
            select (negate: not.HasValue, lower, upper)
        ),
        (expr, suffix) => new Expression.Between(suffix.negate, expr, suffix.lower, suffix.upper)
    );

    private static readonly TokenListParser<SelectToken, (Expression Pattern, Option<Expression> Escape)> PatternSuffix = ParseOptionalOrDefaultSuffix(
        (
            from between in Token.EqualTo(SelectToken.Like)
            from pattern in Superpower.Parse.Ref(() => Expression)
            select (pattern, (Option<Expression>)new None())
        ),
        (
            from escape in Token.EqualTo(SelectToken.Escape)
            from escapeExpression in Superpower.Parse.Ref(() => Expression)
            select escapeExpression
        ),
        (pattern, escape) => (pattern.pattern, escape)
    );

    public static readonly TokenListParser<SelectToken, Expression> Pattern = ParseOptionalSuffix(
        Containment,
        PatternSuffix,
        (expr, suffix) => new Expression.Like(expr, suffix.Pattern, suffix.Escape)
    );
    
    public static readonly TokenListParser<SelectToken, Expression> StringConcatenation =
        Superpower.Parse.ChainRight(
            Token.EqualTo(SelectToken.Concat),
            Pattern,
            (_, left, right) => new Expression.Binary(BinaryOperator.Concat, left, right)
        );


    public static readonly TokenListParser<SelectToken, Expression> Comparative = ParseOptionalSuffix(
        StringConcatenation,
        (
            from operation in Token.EqualTo(SelectToken.Lesser).Or(Token.EqualTo(SelectToken.Greater)).Or(Token.EqualTo(SelectToken.LesserOrEqual)).Or(Token.EqualTo(SelectToken.GreaterOrEqual))
            from right in StringConcatenation
            select (operation, right)
        ),
        (expr, suffix) => new Expression.Binary(GetComparativeOperation(suffix.operation), expr, suffix.right)
    );
    private static BinaryOperator GetComparativeOperation(Token<SelectToken> operation) => operation.Kind switch
    {
        SelectToken.Lesser => BinaryOperator.Lesser,
        SelectToken.Greater => BinaryOperator.Greater,
        SelectToken.LesserOrEqual => BinaryOperator.LesserOrEqual,
        SelectToken.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
            
        _ => throw new InvalidOperationException("Operation should be a comparative token")
    };

    public static readonly TokenListParser<SelectToken, Expression> Equality =
        Superpower.Parse.ChainRight(
            Token.EqualTo(SelectToken.Equal).Or(Token.EqualTo(SelectToken.NotEqual)),
            Comparative,
            CreateEquality
        );
    private static Expression CreateEquality(Token<SelectToken> opToken, Expression left, Expression right)
    {
        var operation = opToken.Kind switch
        {
            SelectToken.Equal => BinaryOperator.Equal,
            SelectToken.NotEqual => BinaryOperator.NotEqual,
                
            _ => throw new InvalidOperationException("Operation should be a equality token")
        };

        return new Expression.Binary(operation, left, right);
    }

    public static readonly TokenListParser<SelectToken, Expression> BooleanUnary =
    (
        from operation in Token.EqualTo(SelectToken.Not)
        from expression in Equality
        select (Expression)new Expression.Unary(UnaryOperator.Not, expression)
    ).Or(Equality);

    public static readonly TokenListParser<SelectToken, Expression> BooleanAnd =
        Superpower.Parse.ChainRight(
            Token.EqualTo(SelectToken.And),
            BooleanUnary,
            (_, left, right) => new Expression.Binary(BinaryOperator.And, left, right)
        );

    public static readonly TokenListParser<SelectToken, Expression> BooleanOr =
        Superpower.Parse.ChainRight(
            Token.EqualTo(SelectToken.Or),
            BooleanAnd,
            (_, left, right) => new Expression.Binary(BinaryOperator.Or, left, right)
        );
    
    public static readonly TokenListParser<SelectToken, Expression> Expression = BooleanOr;

    // TODO handle brackets

    #endregion

    #region select
    private static readonly TokenListParser<SelectToken, Column> Column =
        from expr in Expression
        from alias in Alias.Select(x => (Option<string>)x).OptionalOrDefault(new None())
        select new Column(expr, alias);
    private static readonly TokenListParser<SelectToken, SelectClause> ColumnsStar =
        Token.EqualTo(SelectToken.Star).Select(_ => (SelectClause)new SelectClause.Star());
    private static readonly TokenListParser<SelectToken, SelectClause> ColumnsList =
        Superpower.Parse.Chain(Token.EqualTo(SelectToken.Comma), Column.Select(x => new SelectClause.List(new[] { x })), CombineColumnLists)
            .Select(x => (SelectClause)x);

    private static SelectClause.List CombineColumnLists(Token<SelectToken> op, SelectClause.List left, SelectClause.List right)
    {
        var columns = left.Columns.Concat(right.Columns).ToList();
        return new SelectClause.List(columns);
    }

    public static readonly TokenListParser<SelectToken, SelectClause> SelectClause =
        from @select in Token.EqualTo(SelectToken.Select)
        from columns in ColumnsStar.Or(ColumnsList)
        select columns;

    #endregion

    #region from
    public static readonly TokenListParser<SelectToken, FromClause> FromClause =
        from @from in Token.EqualTo(SelectToken.From)
        from tableName in Identifier.Select(x => x.Identifier)
        from alias in Alias.Select(x => (Option<string>)x).OptionalOrDefault(new None())
        select new FromClause(tableName, alias);
    #endregion

    #region where
    public static readonly TokenListParser<SelectToken, WhereClause> WhereClause =
        from @where in Token.EqualTo(SelectToken.Where)
        from expression in Expression
        select new WhereClause(expression);
    #endregion

    #region order by

    private static readonly TokenListParser<SelectToken, (Expression Expression, OrderDirection Direction)> OrderByColumn =
        from column in Expression
        from direction in Token.EqualTo(SelectToken.Asc).Select(_ => OrderDirection.Ascending)
            .Or(Token.EqualTo(SelectToken.Desc).Select(_ => OrderDirection.Descending)).OptionalOrDefault(OrderDirection.Ascending)
        select (column, direction);

    public static readonly TokenListParser<SelectToken, OrderClause> OrderByClause =
        from orderBy in Token.Sequence(SelectToken.Order, SelectToken.By)
        from columns in OrderByColumn.ManyDelimitedBy(Token.EqualTo(SelectToken.Comma))
        select new OrderClause(columns);

    #endregion

    #region limit
    public static readonly TokenListParser<SelectToken, LimitClause> LimitClause =
        from limit in Token.EqualTo(SelectToken.Limit)
        from number in Number.Select(x => (int)x)
        select new LimitClause(number);
    #endregion
        
    public static readonly TokenListParser<SelectToken, Query> Query =
        from @select in SelectClause
        from @from in FromClause
        from @where in WhereClause.Select(x => (Option<WhereClause>)x).OptionalOrDefault(new None())
        from order in OrderByClause.Select(x => (Option<OrderClause>)x).OptionalOrDefault(new None())
        from limit in LimitClause.Select(x => (Option<LimitClause>)x).OptionalOrDefault(new None())
        select new Query(@select, @from, @where, order, limit);
    
    [PublicAPI]
    public static TokenListParserResult<SelectToken, Query> Parse(string input)
    {
        var tokenizer = new SelectTokenizer();
        var tokens = tokenizer.Tokenize(input);
        return Parse(tokens);
    }
    [PublicAPI]
    public static TokenListParserResult<SelectToken, Query> Parse(TokenList<SelectToken> input) => Query(input);

    private static (string Identifier, bool CaseSensitive) ParseIdentifier(Token<SelectToken> token)
    {
        var rawValue = token.ToStringValue();

        return rawValue[0] == '\"'
            ? (rawValue.Substring(1, rawValue.Length - 2), true)
            : (rawValue, false);
    }

    private static string ParseString(Token<SelectToken> token)
    {
        var rawValue = token.ToStringValue();
        // TODO handle escapes
        return rawValue.Substring(1, rawValue.Length - 2);
    }

    private static TokenListParser<SelectToken, T1> ParseOptionalSuffix<T1, T2>(TokenListParser<SelectToken, T1> expressionParser, TokenListParser<SelectToken, T2> suffixParser, Func<T1, T2, T1> selector) where T2 : struct =>
        from expression in expressionParser
        from suffix in suffixParser.Try().Optional()
        select suffix.HasValue ? selector(expression, suffix.Value) : expression;
    
    private static TokenListParser<SelectToken, T1> ParseOptionalOrDefaultSuffix<T1, T2>(TokenListParser<SelectToken, T1> expressionParser, TokenListParser<SelectToken, T2> suffixParser, Func<T1, T2, T1> selector) where T2 : class =>
        from expression in expressionParser
        from suffix in suffixParser.Try().OptionalOrDefault()
        select suffix != default ? selector(expression, suffix) : expression;

}