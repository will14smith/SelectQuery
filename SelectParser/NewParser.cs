using System;
using System.Collections.Generic;
using SelectParser.Queries;

namespace SelectParser;

public class NewParser
{
    #region function
    
    #endregion
    
    #region expression

    public static Result<Expression> Expression(ref NewTokenizer tokenizer) => BooleanOr(ref tokenizer);
    public static Result<Expression> BooleanOr(ref NewTokenizer tokenizer)
    {
        var expr = BooleanAnd(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        while(next.Type == NewTokenizer.TokenType.Or)
        {
            tokenizer = peekTokenizer;
            var rightExpr = BooleanAnd(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;
            
            expr = Result<Expression>.Ok(new Expression.Binary(BinaryOperator.Or, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = NewTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> BooleanAnd(ref NewTokenizer tokenizer)
    {
        var expr = BooleanUnary(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        while(next.Type == NewTokenizer.TokenType.And)
        {
            tokenizer = peekTokenizer;
            var rightExpr = BooleanUnary(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;
            
            expr = Result<Expression>.Ok(new Expression.Binary(BinaryOperator.And, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = NewTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> BooleanUnary(ref NewTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type == NewTokenizer.TokenType.Not)
        {
            tokenizer = peekTokenizer;
            var term = Equality(ref tokenizer);
            if (!term.Success) return term;
            
            return Result<Expression>.Ok(new Expression.Unary(UnaryOperator.Not, term.Value!));
        }

        return Equality(ref tokenizer);
    }

    public static Result<Expression> Equality(ref NewTokenizer tokenizer)
    {
        var expr = Comparative(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        while(next.Type is NewTokenizer.TokenType.Equal or NewTokenizer.TokenType.NotEqual)
        {
            tokenizer = peekTokenizer;
            var rightExpr = Comparative(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                NewTokenizer.TokenType.Equal => BinaryOperator.Equal,
                NewTokenizer.TokenType.NotEqual => BinaryOperator.NotEqual,
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = NewTokenizer.Read(ref peekTokenizer);
        }

        return expr;
    }

    public static Result<Expression> Comparative(ref NewTokenizer tokenizer)
    {
        var expr = Pattern(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type is NewTokenizer.TokenType.Lesser or NewTokenizer.TokenType.LesserOrEqual or NewTokenizer.TokenType.Greater or NewTokenizer.TokenType.GreaterOrEqual)
        {
            tokenizer = peekTokenizer;

            var rightExpr = Pattern(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                NewTokenizer.TokenType.Lesser => BinaryOperator.Lesser,
                NewTokenizer.TokenType.LesserOrEqual => BinaryOperator.LesserOrEqual,
                NewTokenizer.TokenType.Greater => BinaryOperator.Greater,
                NewTokenizer.TokenType.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!));
        }

        return expr;
    }

    public static Result<Expression> Pattern(ref NewTokenizer tokenizer)
    {
        var expr = Containment(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var peekNext = NewTokenizer.Read(ref peekTokenizer);
        
        if (peekNext.Type != NewTokenizer.TokenType.Like)
        {
            return expr;
        }

        tokenizer = peekTokenizer;

        var pattern = Expression(ref tokenizer);
        if (!pattern.Success) return pattern;
        
        peekTokenizer = tokenizer;
        peekNext = NewTokenizer.Read(ref peekTokenizer);
        
        if (peekNext.Type != NewTokenizer.TokenType.Escape)
        {
            return Result<Expression>.Ok(new Expression.Like(expr.Value!, pattern.Value!, new Option<Expression>()));
        }

        tokenizer = peekTokenizer;

        var escape = Expression(ref tokenizer);
        if (!escape.Success) return escape;
        
        return Result<Expression>.Ok(new Expression.Like(expr.Value!, pattern.Value!, escape.Value!));
    }

    public static Result<Expression> Containment(ref NewTokenizer tokenizer)
    {
        var expr = Presence(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type is NewTokenizer.TokenType.Not or NewTokenizer.TokenType.Between)
        {
            throw new NotImplementedException();
        }

        return expr;
    }

    public static Result<Expression> Presence(ref NewTokenizer tokenizer)
    {
        var expr = IsNull(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type == NewTokenizer.TokenType.Is)
        {
            throw new NotImplementedException();
        }

        return expr;
    }

    public static Result<Expression> IsNull(ref NewTokenizer tokenizer)
    {
        var expr = Membership(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type == NewTokenizer.TokenType.Is)
        {
            throw new NotImplementedException();
        }

        return expr;
    }

    public static Result<Expression> Membership(ref NewTokenizer tokenizer)
    {
        var expr = Additive(ref tokenizer);
        if (!expr.Success) return expr;

        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type != NewTokenizer.TokenType.In)
        {
            return expr;
        }

        tokenizer = peekTokenizer;

        next = NewTokenizer.Read(ref tokenizer);
        if(next.Type != NewTokenizer.TokenType.LeftBracket) return Result<Expression>.UnexpectedToken(next);

        var values = new List<Expression>();
        while (true)
        {
            var value = Expression(ref tokenizer);
            if (!value.Success) return expr;

            values.Add(value.Value!);
            
            next = NewTokenizer.Read(ref tokenizer);
            if (next.Type == NewTokenizer.TokenType.RightBracket) break;
            if (next.Type != NewTokenizer.TokenType.Comma) return Result<Expression>.UnexpectedToken(next);
        }
        
        return Result<Expression>.Ok(new Expression.In(expr.Value!, values));
    }

    public static Result<Expression> Additive(ref NewTokenizer tokenizer)
    {
        var expr = Multiplicative(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        while(next.Type is NewTokenizer.TokenType.Add or NewTokenizer.TokenType.Negate)
        {
            tokenizer = peekTokenizer;
            var rightExpr = Multiplicative(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                NewTokenizer.TokenType.Add => BinaryOperator.Add,
                NewTokenizer.TokenType.Negate => BinaryOperator.Subtract,
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = NewTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> Multiplicative(ref NewTokenizer tokenizer)
    {
        var expr = Unary(ref tokenizer);
        if (!expr.Success) return expr;
        
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        while(next.Type is NewTokenizer.TokenType.Star or NewTokenizer.TokenType.Divide or NewTokenizer.TokenType.Modulo)
        {
            tokenizer = peekTokenizer;
            var rightExpr = Unary(ref tokenizer);
            if (!rightExpr.Success) return rightExpr;

            var op = next.Type switch
            {
                NewTokenizer.TokenType.Star => BinaryOperator.Multiply,
                NewTokenizer.TokenType.Divide => BinaryOperator.Divide,
                NewTokenizer.TokenType.Modulo => BinaryOperator.Modulo,
            };
            
            expr = Result<Expression>.Ok(new Expression.Binary(op, expr.Value!, rightExpr.Value!)); 
            
            peekTokenizer = tokenizer;
            next = NewTokenizer.Read(ref peekTokenizer);
        }
        
        return expr;
    }

    public static Result<Expression> Unary(ref NewTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type == NewTokenizer.TokenType.Negate)
        {
            tokenizer = peekTokenizer;
            var term = Term(ref tokenizer);
            if (!term.Success) return term;
            
            return Result<Expression>.Ok(new Expression.Unary(UnaryOperator.Negate, term.Value!));
        }

        return Term(ref tokenizer);
    }

    public static Result<Expression> Term(ref NewTokenizer tokenizer)
    {
        var next = NewTokenizer.Read(ref tokenizer);

        switch (next.Type)
        {
            case NewTokenizer.TokenType.NumberLiteral: return Result<Expression>.Ok(new Expression.NumberLiteral(ParseNumber(next)));
            case NewTokenizer.TokenType.StringLiteral: return Result<Expression>.Ok(new Expression.StringLiteral(ParseString(next)));
            case NewTokenizer.TokenType.BooleanLiteral: return Result<Expression>.Ok(new Expression.BooleanLiteral(bool.Parse(next.ToStringValue())));
            case NewTokenizer.TokenType.Identifier:
            {
                var identifier = ParseIdentifier(next);
                return QualifiedIdentifier(ref tokenizer, identifier);
            }
            case NewTokenizer.TokenType.LeftBracket:
            {
                var expr = Expression(ref tokenizer);
                
                next = NewTokenizer.Read(ref tokenizer);
                if (next.Type != NewTokenizer.TokenType.RightBracket)
                {
                    return Result<Expression>.UnexpectedToken(next);
                }

                return expr;
            }

            default: return Result<Expression>.UnexpectedToken(next);
        }
    }
    
    private static Result<Expression> QualifiedIdentifier(ref NewTokenizer tokenizer, Expression.Identifier initial)
    {
        var peekTokenizer = tokenizer;
        var next = NewTokenizer.Read(ref peekTokenizer);

        if (next.Type == NewTokenizer.TokenType.LeftBracket)
        {
            // scalar function
            tokenizer = peekTokenizer;

            var expr = Expression(ref tokenizer);
            if (!expr.Success) return expr;
            
            next = NewTokenizer.Read(ref tokenizer);
            if(next.Type != NewTokenizer.TokenType.RightBracket) return Result<Expression>.UnexpectedToken(next);
            
            return Result<Expression>.Ok(new Expression.FunctionExpression(new ScalarFunction(initial, new [] { expr.Value! })));
        }
        
        if (next.Type != NewTokenizer.TokenType.Dot)
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
            next = NewTokenizer.Read(ref tokenizer);

            if (next.Type == NewTokenizer.TokenType.Star)
            {
                identifiers.Add(new Expression.Identifier("*", false));
                break;
            }
            
            if (next.Type != NewTokenizer.TokenType.Identifier)
            {
                return Result<Expression>.UnexpectedToken(next);
            }

            identifiers.Add(ParseIdentifier(next));
            
            peekTokenizer = tokenizer;
            next = NewTokenizer.Read(ref peekTokenizer);

            if (next.Type != NewTokenizer.TokenType.Dot)
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
    
    public static Result<SelectClause> SelectClause(ref NewTokenizer tokenizer)
    {
        var next = NewTokenizer.Read(ref tokenizer);
        if (next.Type != NewTokenizer.TokenType.Select) return Result<SelectClause>.UnexpectedToken(next);
        
        var peekTokenizer = tokenizer;
        var peekNext = NewTokenizer.Read(ref peekTokenizer);

        if (peekNext.Type == NewTokenizer.TokenType.Star)
        {
            tokenizer = peekTokenizer;
            return Result<SelectClause>.Ok(new SelectClause(new SelectClause.Star()));
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
            peekNext = NewTokenizer.Read(ref peekTokenizer);

            if (peekNext.Type != NewTokenizer.TokenType.Comma)
            {
                break;
            }

            tokenizer = peekTokenizer;
        }

        return Result<SelectClause>.Ok(new SelectClause(new SelectClause.List(columns)));
    }

    private static Result<Option<string>> Alias(ref NewTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = NewTokenizer.Read(ref peekTokenizer);

        switch (peekNext.Type)
        {
            case NewTokenizer.TokenType.As:
            {
                tokenizer = peekTokenizer;
            
                var next = NewTokenizer.Read(ref tokenizer);
                if (next.Type != NewTokenizer.TokenType.Identifier) return Result<Option<string>>.UnexpectedToken(next);

                return Result<Option<string>>.Ok(ParseIdentifier(next).Name);
            }
            
            case NewTokenizer.TokenType.Identifier:
                tokenizer = peekTokenizer;
                return Result<Option<string>>.Ok(ParseIdentifier(peekNext).Name);
            
            default:
                return Result<Option<string>>.Ok(new Option<string>());
        }
    }
    
    #endregion
    
    #region from

    public static Result<FromClause> FromClause(ref NewTokenizer tokenizer)
    {
        var next = NewTokenizer.Read(ref tokenizer);
        if (next.Type != NewTokenizer.TokenType.From) return Result<FromClause>.UnexpectedToken(next);

        next = NewTokenizer.Read(ref tokenizer);
        if (next.Type != NewTokenizer.TokenType.Identifier) return Result<FromClause>.UnexpectedToken(next);
        var tableName = ParseIdentifier(next);
        
        var alias = Alias(ref tokenizer);
        if (!alias.Success) return Result<FromClause>.FromError(alias);
        
        return Result<FromClause>.Ok(new FromClause(tableName.Name, alias.Value));
    }
    
    #endregion
    
    #region where
    
    public static Result<WhereClause?> WhereClause(ref NewTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = NewTokenizer.Read(ref peekTokenizer);
        if (peekNext.Type != NewTokenizer.TokenType.Where) return Result<WhereClause?>.Ok(null);

        tokenizer = peekTokenizer;
        
        var expr = Expression(ref tokenizer);
        if (!expr.Success) return Result<WhereClause?>.FromError(expr);
        
        return Result<WhereClause?>.Ok(new WhereClause(expr.Value!));
    }

    #endregion
    
    #region order by
    
    public static Result<OrderClause?> OrderByClause(ref NewTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = NewTokenizer.Read(ref peekTokenizer);
        if (peekNext.Type != NewTokenizer.TokenType.Order) return Result<OrderClause?>.Ok(null);
        
        tokenizer = peekTokenizer;

        var next = NewTokenizer.Read(ref tokenizer);
        if (next.Type != NewTokenizer.TokenType.By) return Result<OrderClause?>.UnexpectedToken(next);

        var columns = new List<(Expression, OrderDirection)>();
        
        while (true)
        {
            var expr = Expression(ref tokenizer);
            if (!expr.Success) return Result<OrderClause?>.FromError(expr);

            peekTokenizer = tokenizer;
            peekNext = NewTokenizer.Read(ref peekTokenizer);

            var order = OrderDirection.Ascending;
            switch (peekNext.Type)
            {
                case NewTokenizer.TokenType.Asc:
                    tokenizer = peekTokenizer;

                    order = OrderDirection.Ascending;
                    peekNext = NewTokenizer.Read(ref peekTokenizer);
                    break;
                case NewTokenizer.TokenType.Desc:
                    tokenizer = peekTokenizer;

                    order = OrderDirection.Descending;
                    peekNext = NewTokenizer.Read(ref peekTokenizer);
                    break;
            }
            
            columns.Add((expr.Value!, order));

            if (peekNext.Type == NewTokenizer.TokenType.Comma)
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
    
    public static Result<LimitClause?> LimitClause(ref NewTokenizer tokenizer)
    {
        var peekTokenizer = tokenizer;
        var peekNext = NewTokenizer.Read(ref peekTokenizer);
        if (peekNext.Type != NewTokenizer.TokenType.Limit) return Result<LimitClause?>.Ok(null);
        
        tokenizer = peekTokenizer;
        
        var next = NewTokenizer.Read(ref tokenizer);
        if(next.Type != NewTokenizer.TokenType.NumberLiteral) return Result<LimitClause?>.UnexpectedToken(next);

        var limit = ParseNumber(next);
        
        return Result<LimitClause?>.Ok(new LimitClause((int)limit));
    }

    #endregion
    
    public static Result<Query> Parse(string query)
    {
        var tokenizer = new NewTokenizer(query.AsSpan());
        return Query(ref tokenizer);
    }
    
    public static Result<Query> Query(ref NewTokenizer tokenizer)
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

        var final = NewTokenizer.Read(ref tokenizer);
        if(final.Type != NewTokenizer.TokenType.Eof) return Result<Query>.UnexpectedToken(final);
        
        var query = new Query(select.Value!, from.Value!, where.Value ?? new Option<WhereClause>(), order.Value ?? new Option<OrderClause>(), limit.Value ?? new Option<LimitClause>());
        
        return Result<Query>.Ok(query);
    }

    private static decimal ParseNumber(NewTokenizer.Token next) => decimal.Parse(next.ToStringValue());
    private static string ParseString(NewTokenizer.Token token)
    {
        // TODO handle escapes
        return new string(token.Span.Slice(1, token.Span.Length - 2).ToArray());
    }
    private static Expression.Identifier ParseIdentifier(NewTokenizer.Token token)
    {
        if (token.Span[0] == '"')
        {
            return new Expression.Identifier(new string(token.Span.Slice(1, token.Span.Length - 2).ToArray()), true);
        }
        else
        {
            return new Expression.Identifier(new string(token.Span.ToArray()), false);
        }
    }
    
    public class Result<T>
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

        public static Result<T> Ok(T value) => new(true, value, default);
        public static Result<T> UnexpectedToken(NewTokenizer.Token token) => new(false, default, $"Unexpected token '{token.Type}'");

        public static Result<T> FromError<TOther>(Result<TOther> other)
        {
            if (other.Success) throw new InvalidOperationException();

            return new Result<T>(false, default, other.Error);
        }
    }
}