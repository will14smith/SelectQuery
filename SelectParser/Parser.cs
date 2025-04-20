using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SelectParser.Queries;

namespace SelectParser;

public class Parser
{
    #region function

    private static Result<Expression> SingleParameterFunction(ref SelectTokenizer tokenizer, Func<Expression, AggregateFunction> constructor)
    {
        var leftBracket = SelectTokenizer.Read(ref tokenizer);
        if(leftBracket.Type != SelectToken.LeftBracket) return Result<Expression>.UnexpectedToken(leftBracket);

        var argument = Expression(ref tokenizer);
        if (!argument.Success) return argument;
        
        var rightBracket = SelectTokenizer.Read(ref tokenizer);
        if(rightBracket.Type != SelectToken.RightBracket) return Result<Expression>.UnexpectedToken(rightBracket);

        var result = constructor(argument.Value!);
        
        return Result<Expression>.Ok(new Expression.FunctionExpression(result));
    }
    
    #endregion
    
    #region expression

    public static Result<Expression> Expression(ref SelectTokenizer tokenizer) => BooleanOr(ref tokenizer);
    public static Result<Expression> BooleanOr(ref SelectTokenizer tokenizer)
    {
        var expr = BooleanAnd(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        while(next.Type == SelectToken.Or)
        {
            tokenizer = peekTokenizer;
            var rightExpr = BooleanAnd(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;
            
            expr = Result<Expression>.Ok(new Expression.Binary(BinaryOperator.Or, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = SelectTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> BooleanAnd(ref SelectTokenizer tokenizer)
    {
        var expr = BooleanUnary(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        while(next.Type == SelectToken.And)
        {
            tokenizer = peekTokenizer;
            var rightExpr = BooleanUnary(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;
            
            expr = Result<Expression>.Ok(new Expression.Binary(BinaryOperator.And, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = SelectTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> BooleanUnary(ref SelectTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type == SelectToken.Not)
        {
            tokenizer = peekTokenizer;
            var term = Equality(ref tokenizer);
            if (!term.Success) return term;
            
            return Result<Expression>.Ok(new Expression.Unary(UnaryOperator.Not, term.Value!));
        }

        return Equality(ref tokenizer);
    }

    public static Result<Expression> Equality(ref SelectTokenizer tokenizer)
    {
        var expr = Comparative(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        while(next.Type is SelectToken.Equal or SelectToken.NotEqual)
        {
            tokenizer = peekTokenizer;
            var rightExpr = Comparative(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                SelectToken.Equal => BinaryOperator.Equal,
                SelectToken.NotEqual => BinaryOperator.NotEqual,
                
                _ => throw new InvalidOperationException("invalid operator"),
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = SelectTokenizer.Read(ref peekTokenizer);
        }

        return expr;
    }

    public static Result<Expression> Comparative(ref SelectTokenizer tokenizer)
    {
        var expr = StringConcatenation(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type is SelectToken.Lesser or SelectToken.LesserOrEqual or SelectToken.Greater or SelectToken.GreaterOrEqual)
        {
            tokenizer = peekTokenizer;

            var rightExpr = StringConcatenation(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                SelectToken.Lesser => BinaryOperator.Lesser,
                SelectToken.LesserOrEqual => BinaryOperator.LesserOrEqual,
                SelectToken.Greater => BinaryOperator.Greater,
                SelectToken.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
                
                _ => throw new InvalidOperationException("invalid operator"),
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!));
        }

        return expr;
    }

    public static Result<Expression> StringConcatenation(ref SelectTokenizer tokenizer)
    {
        var expr = Pattern(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type is SelectToken.Concat)
        {
            tokenizer = peekTokenizer;

            var rightExpr = Pattern(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;
            
            expr = Result<Expression>.Ok(new Expression.Binary(BinaryOperator.Concat, expr.Value!, rightExpr.Value!));
        }

        return expr;
    }
    
    public static Result<Expression> Pattern(ref SelectTokenizer tokenizer)
    {
        var expr = Containment(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var peekNext = SelectTokenizer.Read(ref peekTokenizer);
        
        if (peekNext.Type != SelectToken.Like)
        {
            return expr;
        }

        tokenizer = peekTokenizer;

        var pattern = Expression(ref tokenizer);
        if (!pattern.Success) return pattern;
        
        peekTokenizer = tokenizer;
        peekNext = SelectTokenizer.Read(ref peekTokenizer);
        
        if (peekNext.Type != SelectToken.Escape)
        {
            return Result<Expression>.Ok(new Expression.Like(expr.Value!, pattern.Value!, new Option<Expression>()));
        }

        tokenizer = peekTokenizer;

        var escape = Expression(ref tokenizer);
        if (!escape.Success) return escape;
        
        return Result<Expression>.Ok(new Expression.Like(expr.Value!, pattern.Value!, escape.Value!));
    }

    public static Result<Expression> Containment(ref SelectTokenizer tokenizer)
    {
        var expr = Presence(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        var negate = next.Type is SelectToken.Not;
        if (negate)
        {
            next = SelectTokenizer.Read(ref peekTokenizer);
        }
        
        if (next.Type != SelectToken.Between)
        {
            // don't accept peek tokenizer
            return expr;
        }

        tokenizer = peekTokenizer;
        
        var lower = Presence(ref tokenizer);
        if (!lower.Success) return lower;

        next = SelectTokenizer.Read(ref tokenizer);
        if(next.Type != SelectToken.And) return Result<Expression>.UnexpectedToken(next);
        
        var upper = Expression(ref tokenizer);
        if (!upper.Success) return upper;
        
        return Result<Expression>.Ok(new Expression.Between(negate, expr.Value!, lower.Value!, upper.Value!));
    }

    public static Result<Expression> Presence(ref SelectTokenizer tokenizer)
    {
        var expr = IsNull(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type != SelectToken.Is)
        {
            return expr;
        }
        
        next = SelectTokenizer.Read(ref peekTokenizer);

        var negate = next.Type is SelectToken.Not;
        if (negate)
        {
            next = SelectTokenizer.Read(ref peekTokenizer);
        }

        if (next.Type != SelectToken.Missing)
        {
            // don't accept peek tokenizer
            return expr;
        }

        tokenizer = peekTokenizer;
        return Result<Expression>.Ok(new Expression.Presence(expr.Value!, !negate));
    }

    public static Result<Expression> IsNull(ref SelectTokenizer tokenizer)
    {
        var expr = Membership(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type != SelectToken.Is)
        {
            return expr;
        }
        
        next = SelectTokenizer.Read(ref peekTokenizer);

        var negate = next.Type is SelectToken.Not;
        if (negate)
        {
            next = SelectTokenizer.Read(ref peekTokenizer);
        }

        if (next.Type != SelectToken.Null)
        {
            // don't accept peek tokenizer
            return expr;
        }

        tokenizer = peekTokenizer;
        return Result<Expression>.Ok(new Expression.IsNull(expr.Value!, negate));
    }

    public static Result<Expression> Membership(ref SelectTokenizer tokenizer)
    {
        var expr = Additive(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type != SelectToken.In)
        {
            return expr;
        }

        tokenizer = peekTokenizer;

        next = SelectTokenizer.Read(ref tokenizer);
        if(next.Type != SelectToken.LeftBracket) return Result<Expression>.UnexpectedToken(next);

        var values = new List<Expression>();
        while (true)
        {
            var value = Expression(ref tokenizer);
            if (!value.Success) return expr;

            values.Add(value.Value!);
            
            next = SelectTokenizer.Read(ref tokenizer);
            if (next.Type == SelectToken.RightBracket) break;
            if (next.Type != SelectToken.Comma) return Result<Expression>.UnexpectedToken(next);
        }
        
        return Result<Expression>.Ok(new Expression.In(expr.Value!, values));
    }

    public static Result<Expression> Additive(ref SelectTokenizer tokenizer)
    {
        var expr = Multiplicative(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        while(next.Type is SelectToken.Add or SelectToken.Negate)
        {
            tokenizer = peekTokenizer;
            var rightExpr = Multiplicative(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                SelectToken.Add => BinaryOperator.Add,
                SelectToken.Negate => BinaryOperator.Subtract,
                
                _ => throw new InvalidOperationException("invalid operator"),
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = SelectTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> Multiplicative(ref SelectTokenizer tokenizer)
    {
        var expr = Unary(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        while(next.Type is SelectToken.Star or SelectToken.Divide or SelectToken.Modulo)
        {
            tokenizer = peekTokenizer;
            var rightExpr = Unary(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                SelectToken.Star => BinaryOperator.Multiply,
                SelectToken.Divide => BinaryOperator.Divide,
                SelectToken.Modulo => BinaryOperator.Modulo,
                
                _ => throw new InvalidOperationException("invalid operator"),
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = SelectTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> Unary(ref SelectTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type == SelectToken.Negate)
        {
            tokenizer = peekTokenizer;
            var term = Term(ref tokenizer);
            if (!term.Success) return term;
            
            return Result<Expression>.Ok(new Expression.Unary(UnaryOperator.Negate, term.Value!));
        }

        return Term(ref tokenizer);
    }

    public static Result<Expression> Term(ref SelectTokenizer tokenizer)
    {
        var next = SelectTokenizer.Read(ref tokenizer);

        switch (next.Type)
        {
            case SelectToken.NumberLiteral: return Result<Expression>.Ok(new Expression.NumberLiteral(ParseNumber(next)));
            case SelectToken.StringLiteral: return Result<Expression>.Ok(new Expression.StringLiteral(ParseString(next)));
            case SelectToken.BooleanLiteral: return Result<Expression>.Ok(new Expression.BooleanLiteral(ParseBoolean(next)));
            case SelectToken.Identifier:
            {
                var identifier = ParseIdentifier(next);
                return QualifiedIdentifier(ref tokenizer, identifier);
            }
            case SelectToken.Star: return Result<Expression>.Ok(new Expression.Identifier("*", false));
            case SelectToken.LeftBracket:
            {
                var expr = Expression(ref tokenizer);
                
                next = SelectTokenizer.Read(ref tokenizer);
                if (next.Type != SelectToken.RightBracket)
                {
                    return Result<Expression>.UnexpectedToken(next);
                }

                return expr;
            }
            
            case SelectToken.Avg: return SingleParameterFunction(ref tokenizer, x => new AggregateFunction.Average(x));
            case SelectToken.Count: return SingleParameterFunction(ref tokenizer, x => new AggregateFunction.Count(x is Expression.Identifier { Name: "*" } ? new None() : x));
            case SelectToken.Max: return SingleParameterFunction(ref tokenizer, x => new AggregateFunction.Max(x));
            case SelectToken.Min: return SingleParameterFunction(ref tokenizer, x => new AggregateFunction.Min(x));
            case SelectToken.Sum: return SingleParameterFunction(ref tokenizer, x => new AggregateFunction.Sum(x));

            default: return Result<Expression>.UnexpectedToken(next);
        }
    }
    
    private static Result<Expression> QualifiedIdentifier(ref SelectTokenizer tokenizer, Expression.Identifier initial)
    {
        var peekTokenizer = tokenizer;
        var next = SelectTokenizer.Read(ref peekTokenizer);

        if (next.Type == SelectToken.LeftBracket)
        {
            // scalar function
            tokenizer = peekTokenizer;

            var expr = Expression(ref tokenizer);
            if (!expr.Success) return expr;
            
            next = SelectTokenizer.Read(ref tokenizer);
            if (next.Type != SelectToken.RightBracket) return Result<Expression>.UnexpectedToken(next);
            
            return Result<Expression>.Ok(new Expression.FunctionExpression(new ScalarFunction(initial, [expr.Value!])));
        }
        
        if (next.Type != SelectToken.Dot)
        {
            return Result<Expression>.Ok(initial);
        }
        
        var identifiers = new List<Expression.Identifier>
        {
            initial
        };

        tokenizer = peekTokenizer;
        while (true)
        {
            next = SelectTokenizer.Read(ref tokenizer);

            if (next.Type == SelectToken.Star)
            {
                identifiers.Add(new Expression.Identifier("*", false));
                break;
            }
            
            if (next.Type != SelectToken.Identifier)
            {
                return Result<Expression>.UnexpectedToken(next);
            }

            identifiers.Add(ParseIdentifier(next));
            
            peekTokenizer = tokenizer;
            next = SelectTokenizer.Read(ref peekTokenizer);

            if (next.Type != SelectToken.Dot)
            {
                break;
            }
            
            tokenizer = peekTokenizer;
        }
        
        Expression result = identifiers[identifiers.Count - 1];
        for (var i = identifiers.Count - 2; i >= 0; i--)
        {
            result = new Expression.Qualified(identifiers[i], result);
        }
        
        return Result<Expression>.Ok(result);
    }

    #endregion
    
    #region select
    
    public static Result<SelectClause> SelectClause(ref SelectTokenizer tokenizer)
    {
        var next = SelectTokenizer.Read(ref tokenizer);
        if (next.Type != SelectToken.Select) return Result<SelectClause>.UnexpectedToken(next);
        
        var peekTokenizer = tokenizer;
        var peekNext = SelectTokenizer.Read(ref peekTokenizer);

        if (peekNext.Type == SelectToken.Star)
        {
            tokenizer = peekTokenizer;
            return Result<SelectClause>.Ok(new SelectClause.Star());
        }

        var columns = new List<Column>();
        while (true)
        {
            var expression = Expression(ref tokenizer);
            if(!expression.Success) return Result<SelectClause>.FromError(expression);
            
            var alias = Alias(ref tokenizer);
            if(!alias.Success) return Result<SelectClause>.FromError(expression);

            columns.Add(new Column(expression.Value!, alias.Value));
            
            peekTokenizer = tokenizer;
            peekNext = SelectTokenizer.Read(ref peekTokenizer);

            if (peekNext.Type != SelectToken.Comma)
            {
                break;
            }

            tokenizer = peekTokenizer;
        }

        return Result<SelectClause>.Ok(new SelectClause.List(columns));
    }

    private static Result<Option<string>> Alias(ref SelectTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = SelectTokenizer.Read(ref peekTokenizer);

        switch (peekNext.Type)
        {
            case SelectToken.As:
            {
                tokenizer = peekTokenizer;
            
                var next = SelectTokenizer.Read(ref tokenizer);
                if (next.Type != SelectToken.Identifier) return Result<Option<string>>.UnexpectedToken(next);

                return Result<Option<string>>.Ok(ParseIdentifier(next).Name);
            }
            
            case SelectToken.Identifier:
                tokenizer = peekTokenizer;
                return Result<Option<string>>.Ok(ParseIdentifier(peekNext).Name);
            
            default:
                return Result<Option<string>>.Ok(new Option<string>());
        }
    }
    
    #endregion
    
    #region from

    public static Result<FromClause> FromClause(ref SelectTokenizer tokenizer)
    {
        var next = SelectTokenizer.Read(ref tokenizer);
        if (next.Type != SelectToken.From) return Result<FromClause>.UnexpectedToken(next);

        next = SelectTokenizer.Read(ref tokenizer);
        if (next.Type != SelectToken.Identifier) return Result<FromClause>.UnexpectedToken(next);
        var tableName = ParseIdentifier(next);
        
        var alias = Alias(ref tokenizer);
        if (!alias.Success) return Result<FromClause>.FromError(alias);
        
        return Result<FromClause>.Ok(new FromClause(tableName.Name, alias.Value));
    }
    
    #endregion
    
    #region where
    
    public static Result<WhereClause?> WhereClause(ref SelectTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = SelectTokenizer.Read(ref peekTokenizer);
        if (peekNext.Type != SelectToken.Where) return Result<WhereClause?>.Ok(null);

        tokenizer = peekTokenizer;
        
        var expr = Expression(ref tokenizer);
        if (!expr.Success) return Result<WhereClause?>.FromError(expr);
        
        return Result<WhereClause?>.Ok(new WhereClause(expr.Value!));
    }

    #endregion
    
    #region order by
    
    public static Result<OrderClause?> OrderByClause(ref SelectTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = SelectTokenizer.Read(ref peekTokenizer);
        if (peekNext.Type != SelectToken.Order) return Result<OrderClause?>.Ok(null);
        
        tokenizer = peekTokenizer;

        var next = SelectTokenizer.Read(ref tokenizer);
        if (next.Type != SelectToken.By) return Result<OrderClause?>.UnexpectedToken(next);

        var columns = new List<(Expression, OrderDirection)>();
        
        while (true)
        {
            var expr = Expression(ref tokenizer);
            if (!expr.Success) return Result<OrderClause?>.FromError(expr);

            peekTokenizer = tokenizer;
            peekNext = SelectTokenizer.Read(ref peekTokenizer);

            var order = OrderDirection.Ascending;
            switch (peekNext.Type)
            {
                case SelectToken.Asc:
                    tokenizer = peekTokenizer;

                    order = OrderDirection.Ascending;
                    peekNext = SelectTokenizer.Read(ref peekTokenizer);
                    break;
                case SelectToken.Desc:
                    tokenizer = peekTokenizer;

                    order = OrderDirection.Descending;
                    peekNext = SelectTokenizer.Read(ref peekTokenizer);
                    break;
            }
            
            columns.Add((expr.Value!, order));

            if (peekNext.Type == SelectToken.Comma)
            {
                tokenizer = peekTokenizer;
            }
            else
            {
                break;
            }
        }
        
        return Result<OrderClause?>.Ok(new OrderClause(columns));
    }

    #endregion
    
    #region limit
    
    public static Result<LimitClause?> LimitClause(ref SelectTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = SelectTokenizer.Read(ref peekTokenizer);
        if (peekNext.Type != SelectToken.Limit) return Result<LimitClause?>.Ok(null);
        
        tokenizer = peekTokenizer;
        
        var next = SelectTokenizer.Read(ref tokenizer);
        if(next.Type != SelectToken.NumberLiteral) return Result<LimitClause?>.UnexpectedToken(next);

        var limit = ParseNumber(next);
        
        return Result<LimitClause?>.Ok(new LimitClause((int)limit));
    }

    #endregion
    
    public static Result<Query> Parse(string query)
    {
        var tokenizer = new SelectTokenizer(query.AsSpan());
        var result = Query(ref tokenizer);
        if (!result.Success)
        {
            return result;
        }

        var final = SelectTokenizer.Read(ref tokenizer);
        if (final.Type != SelectToken.Eof)
        {
            return Result<Query>.UnexpectedToken(final);
        }

        return result;
    }
    
    public static Result<Query> Query(ref SelectTokenizer tokenizer)
    {
        var select = SelectClause(ref tokenizer);
        if (!select.Success) return Result<Query>.FromError(select);
        
        var from = FromClause(ref tokenizer);
        if (!from.Success) return Result<Query>.FromError(from);

        var where = WhereClause(ref tokenizer);
        if (!where.Success) return Result<Query>.FromError(where);

        var order = OrderByClause(ref tokenizer);
        if (!order.Success) return Result<Query>.FromError(order);

        var limit = LimitClause(ref tokenizer);
        if (!limit.Success) return Result<Query>.FromError(limit);

        var final = SelectTokenizer.Read(ref tokenizer);
        if(final.Type != SelectToken.Eof) return Result<Query>.UnexpectedToken(final);
        
        var query = new Query(select.Value!, from.Value!, where.Value ?? new Option<WhereClause>(), order.Value ?? new Option<OrderClause>(), limit.Value ?? new Option<LimitClause>());
        
        return Result<Query>.Ok(query);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal ParseNumber(SelectTokenizer.Token next)
    {
#if NETSTANDARD
        return decimal.Parse(next.ToStringValue());
#else
        return decimal.Parse(next.Span);
#endif
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ParseString(SelectTokenizer.Token token)
    {
        // TODO handle escapes
#if NETSTANDARD
        return new string(token.Span.Slice(1, token.Span.Length - 2).ToArray());
#else
        return new string(token.Span[1..^1]);
#endif
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ParseBoolean(SelectTokenizer.Token next)
    {
#if NETSTANDARD
        return bool.Parse(next.ToStringValue());
#else
        return bool.Parse(next.Span);
#endif
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Expression.Identifier ParseIdentifier(SelectTokenizer.Token token)
    {
#if NETSTANDARD
        return token.Span[0] == '"' 
            ? new Expression.Identifier(new string(token.Span.Slice(1, token.Span.Length - 2).ToArray()), true)
            : new Expression.Identifier(new string(token.Span.ToArray()), false);
#else
        return token.Span[0] == '"' 
            ? new Expression.Identifier(new string(token.Span[1..^1]), true)
            : new Expression.Identifier(new string(token.Span), false);
#endif
    }
    
    public readonly struct Result<T>
    {
        public bool Success { get; }
        
        public T? Value { get; }
        public string? Error { get; }

        private Result(bool success, T? value, string? error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        public static Result<T> Ok(T value) => new(true, value, null);
        public static Result<T> UnexpectedToken(SelectTokenizer.Token token) => new(false, default, $"Unexpected token '{token.Type}'");

        public static Result<T> FromError<TOther>(Result<TOther> other)
        {
            if (other.Success) throw new InvalidOperationException();

            return new Result<T>(false, default, other.Error);
        }
    }
}