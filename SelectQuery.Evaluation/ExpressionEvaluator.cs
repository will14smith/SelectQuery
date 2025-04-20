using System;
using System.Collections.Generic;
using System.Linq;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class ExpressionEvaluator
{
    public Option<T> Evaluate<T>(Expression expression, object obj)
    {
        var value = expression switch
        {
            Expression.StringLiteral strLiteral => (T)(object)EvaluateStringLiteral(strLiteral),
            Expression.NumberLiteral numLiteral => (T)(object)EvaluateNumberLiteral(numLiteral),
            Expression.BooleanLiteral boolLiteral => (T)(object)EvaluateBooleanLiteral(boolLiteral),
            Expression.Identifier identifier => EvaluateIdentifier<T>(identifier, obj),
            Expression.Qualified qualified => EvaluateQualified<T>(qualified, obj),
            Expression.FunctionExpression function => EvaluateFunction<T>(function.Function, obj),
            Expression.Unary unary => EvaluateUnary<T>(unary, obj),
            Expression.Binary binary => EvaluateBinary<T>(binary, obj),
            Expression.Between between => (T)(object)EvaluateBetween(between, obj),
            Expression.IsNull isNull => (T)(object)EvaluateIsNull(isNull, obj),
            Expression.Presence presence => (T)(object)EvaluatePresence(presence, obj),
            Expression.In inExpr => (T)(object)EvaluateIn(inExpr, obj),
            Expression.Like like => (T)(object)EvaluateLike(like, obj),

            _ => throw new ArgumentOutOfRangeException(),
        };

        // normalize result values, utf8json parses numbers are decimal but we want doubles
        if (value.Value is double dbl) return (T)(object)Convert.ToDecimal(dbl);
        return value;
    }
    private string EvaluateStringLiteral(Expression.StringLiteral strLiteral)
    {
        return strLiteral.Value;
    }

    private decimal EvaluateNumberLiteral(Expression.NumberLiteral numLiteral)
    {
        return numLiteral.Value;
    }

    private bool EvaluateBooleanLiteral(Expression.BooleanLiteral boolLiteral)
    {
        return boolLiteral.Value;
    }

    private Option<T> EvaluateIdentifier<T>(Expression.Identifier identifier, object obj)
    {
        if (obj is null)
        {
            return default;
        }

        if (obj is IReadOnlyDictionary<string, object> dict)
        {
            // fast: match exact key
            if (dict.TryGetValue(identifier.Name, out var result))
                return (T) result;

            if (identifier.CaseSensitive)
            {
                return new None();
            }

            // slow: try match case-insensitive key, ideally the dictionary would be case-insensitive but need some custom deserialization logic to support that
            var entry = dict.FirstOrDefault(x => string.Equals(x.Key, identifier.Name, StringComparison.OrdinalIgnoreCase));
            return entry.Key != null ? (T) entry.Value : new None();
        }

        throw new NotImplementedException($"don't know how to get identifier ({identifier.Name}) value from {obj?.GetType().FullName ?? "null"}");
    }

    private Option<T> EvaluateQualified<T>(Expression.Qualified qualified, object obj)
    {
        for (var index = 0; index < qualified.Identifiers.Count; index++)
        {
            var identifier = qualified.Identifiers[index];
            
            var context = EvaluateIdentifier<object>(identifier, obj);
            if (context.IsNone)
            {
                return new None();
            }

            obj = context.AsT0;
        }
        
        return (T) obj;
    }
    
    private Option<T> EvaluateFunction<T>(Function function, object obj)
    {
        if (function is not ScalarFunction scalar)
        {
            throw new InvalidOperationException("cannot evaluate a non-scalar function in this way");
        }
        
        var name = scalar.Identifier.Name;
        var arguments = scalar.Arguments.Select(argument => Evaluate<object>(argument, obj)).ToList();

        return FunctionEvaluator.Evaluate<T>(name, arguments);
    }

    private Option<T> EvaluateUnary<T>(Expression.Unary unary, object obj)
    {
        return (unary.Operator switch
        {
            UnaryOperator.Not => Evaluate<bool>(unary.Expression, obj).Select(value => (T) (object) !value),
            UnaryOperator.Negate => Evaluate<decimal>(unary.Expression, obj).Select(value => (T) (object) -value),

            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private T EvaluateBinary<T>(Expression.Binary binary, object obj)
    {
        var leftOpt = Evaluate<object>(binary.Left, obj);
        var rightOpt = Evaluate<object>(binary.Right, obj);

        if (binary.Operator == BinaryOperator.Add)
        {
            return (T) EvaluateAddition(leftOpt, rightOpt);
        }

        var left = leftOpt.Value;
        var right = rightOpt.Value;

        // propagate nulls
        if (leftOpt.IsNone || left == null || rightOpt.IsNone || right == null) return default;

        return (T) (object) (binary.Operator switch
        {
            BinaryOperator.And => (bool)left && (bool)right,
            BinaryOperator.Or => (bool)left || (bool)right,
            BinaryOperator.Lesser => (decimal)left < (decimal)right,
            BinaryOperator.Greater => (decimal)left > (decimal)right,
            BinaryOperator.LesserOrEqual => (decimal)left <= (decimal)right,
            BinaryOperator.GreaterOrEqual => (decimal)left >= (decimal)right,

            BinaryOperator.Equal => EvaluateEquality(left, right),
            BinaryOperator.NotEqual => !EvaluateEquality(left, right),

            BinaryOperator.Subtract => (decimal) left - (decimal) right,
            BinaryOperator.Multiply => (decimal) left * (decimal) right,
            BinaryOperator.Divide => (decimal) left / (decimal) right,
            BinaryOperator.Modulo => (decimal) left % (decimal) right,
            
            BinaryOperator.Concat => (string) left + (string) right,

            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private object EvaluateAddition(Option<object> left, Option<object> right)
    {
        if (left.Value is decimal leftNum && right.Value is decimal rightNum)
        {
            return leftNum + rightNum;
        }

        if (left.IsNone || left.Value is null) return right.Value?.ToString();
        if (right.IsNone || right.Value is null) return null;

        return $"{left.Value}{right.Value}";
    }

    private bool EvaluateEquality(object left, object right)
    {
        return Equals(left, right);
    }

    private bool EvaluateBetween(Expression.Between between, object obj)
    {
        throw new NotImplementedException();
    }

    private bool EvaluateIsNull(Expression.IsNull isNull, object obj)
    {
        var value = Evaluate<object>(isNull.Expression, obj);

        var hasValue = value is { IsSome: true, AsT0: not null };

        return hasValue == isNull.Negate;
    }
        
    private bool EvaluatePresence(Expression.Presence presence, object obj)
    {
        var value = Evaluate<object>(presence.Expression, obj);

        var isMissing = value.IsNone;

        return isMissing == presence.Negate;
    }

    private bool EvaluateIn(Expression.In inExpr, object obj)
    {
        if (inExpr.StringMatches is not null)
        {
            var value = EvaluateToString(inExpr.Expression, obj);
            if (value.IsNone) return false;

            return inExpr.StringMatches.Contains(value.AsT0);
        }
        else
        {
            var value = Evaluate<object>(inExpr.Expression, obj);

            foreach (var matchExpr in inExpr.Matches)
            {
                var matchValue = Evaluate<object>(matchExpr, obj);

                if (EvaluateEquality(value, matchValue))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private bool EvaluateLike(Expression.Like like, object obj)
    {
        var pattern = EvaluateToString(like.Pattern, obj);
        var escape = like.Escape.SelectMany(x => EvaluateToString(x, obj));
        var value = EvaluateToString(like.Expression, obj);

        if (pattern.IsNone || value.IsNone)
        {
            return false;
        }

        if (escape.IsSome && escape.AsT0.Length != 1)
        {
            throw new InvalidOperationException($"Escape should be a single character, was '{escape.AsT0}'");
        }
        var escapeChar = escape.Select(x => x[0]);
            
        return LikeMatcher.IsMatch(pattern.AsT0, escapeChar, value.AsT0);
    }

    private Option<string> EvaluateToString(Expression expr, object obj)
    {
        var valueObj = Evaluate<object>(expr, obj);
        if (valueObj.IsNone)
        {
            return new None();
        }

        if (valueObj.Value is not string valueStr)
        {
            throw new InvalidCastException($"'{expr}' did not evaluate to a string");
        }

        return valueStr;
    }
}

public static class ExpressionEvaluatorExtensions
{
    public static Option<T> EvaluateOnTable<T>(this ExpressionEvaluator evaluator, Expression expression, FromClause from, object obj)
    {
        var tableName = from.Alias.Match(alias => alias, _ => "s3object");
        var input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { tableName, obj }
        };

        return evaluator.Evaluate<T>(expression, input);
    }
}