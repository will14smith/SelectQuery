using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class ExpressionEvaluator
{
    public static Option<T> Evaluate<T>(Expression expression, object obj)
    {
        var value = expression.Index switch
        {
            0 => (T)(object)EvaluateStringLiteral(expression.AsT0),
            1 => (T)(object)EvaluateNumberLiteral(expression.AsT1),
            2 => (T)(object)EvaluateBooleanLiteral(expression.AsT2),
            3 => EvaluateIdentifier<T>(expression.AsT3, obj),
            4 => EvaluateQualified<T>(expression.AsT4, obj),
            5 => EvaluateFunction<T>(expression.AsT5.Function, obj),
            6 => EvaluateUnary<T>(expression.AsT6, obj),
            7 => EvaluateBinary<T>(expression.AsT7, obj),
            8 => (T)(object)EvaluateBetween(expression.AsT8, obj),
            9 => (T)(object)EvaluateIsNull(expression.AsT9, obj),
            10 => (T)(object)EvaluatePresence(expression.AsT10, obj),
            11 => (T)(object)EvaluateIn(expression.AsT11, obj),
            12 => (T)(object)EvaluateLike(expression.AsT12, obj),
            
            _ => throw new NotImplementedException("unknown expression type"),
        };
        
        // normalize result values, utf8json parses numbers are decimal but we want doubles
        if (value.Value is double dbl) return (T)(object)Convert.ToDecimal(dbl);
        return value;
    }
    private static string EvaluateStringLiteral(Expression.StringLiteral strLiteral)
    {
        return strLiteral.Value;
    }

    private static decimal EvaluateNumberLiteral(Expression.NumberLiteral numLiteral)
    {
        return numLiteral.Value;
    }

    private static bool EvaluateBooleanLiteral(Expression.BooleanLiteral boolLiteral)
    {
        return boolLiteral.Value;
    }

    private static Option<T> EvaluateIdentifier<T>(Expression.Identifier identifier, object obj)
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

            // slow: try match case-insensitive key, ideally the dictionary would be case insensitive but need some custom deserialization logic to support that
            var entry = dict.FirstOrDefault(x => string.Equals(x.Key, identifier.Name, StringComparison.OrdinalIgnoreCase));
            return entry.Key != null ? (Option<T>) (T) entry.Value : new None();
        }

        if (obj is JsonElement { ValueKind: JsonValueKind.Object } json)
        {
            // fast: match exact key
            if (json.TryGetProperty(identifier.Name, out var result))
            {
                return (T) (object) result;
            }
            
            if (identifier.CaseSensitive)
            {
                return new None();
            }

            // slow: try match case-insensitive key, ideally the dictionary would be case insensitive but need some custom deserialization logic to support that
            var entry = json.EnumerateObject().FirstOrDefault(x => string.Equals(x.Name, identifier.Name, StringComparison.OrdinalIgnoreCase));
            return entry.Value.ValueKind != JsonValueKind.Undefined ? (Option<T>) (T) (object) entry.Value : new None();
        }
        
        throw new NotImplementedException($"don't know how to get identifier ({identifier.Name}) value from {obj?.GetType().FullName ?? "null"}");
    }

    private static Option<T> EvaluateQualified<T>(Expression.Qualified qualified, object obj)
    {
        var target = EvaluateIdentifier<object>(qualified.Qualification, obj);

        return target.SelectMany(obj => Evaluate<T>(qualified.Expression, obj));
    }
    
    private static Option<T> EvaluateFunction<T>(Function function, object obj)
    {
        if (!function.IsT1)
        {
            throw new InvalidOperationException("cannot evaluate a non-scalar function in this way");
        }
        
        var scalar = function.AsT1;

        var name = scalar.Identifier.Name;
        var arguments = scalar.Arguments.Select(argument => Evaluate<object>(argument, obj)).ToList();

        return FunctionEvaluator.Evaluate<T>(name, arguments);
    }

    private static Option<T> EvaluateUnary<T>(Expression.Unary unary, object obj) =>
        unary.Operator switch
        {
            UnaryOperator.Not => Evaluate<bool>(unary.Expression, obj).Select(value => (T) (object) !value),
            UnaryOperator.Negate => Evaluate<decimal>(unary.Expression, obj).Select(value => (T) (object) -value),

            _ => throw new ArgumentOutOfRangeException()
        };

    private static T EvaluateBinary<T>(Expression.Binary binary, object obj)
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
            BinaryOperator.And => ConvertToBoolean(left) && ConvertToBoolean(right),
            BinaryOperator.Or => ConvertToBoolean(left) || ConvertToBoolean(right),
            BinaryOperator.Lesser => ConvertToDecimal(left) < ConvertToDecimal(right),
            BinaryOperator.Greater => ConvertToDecimal(left) > ConvertToDecimal(right),
            BinaryOperator.LesserOrEqual => ConvertToDecimal(left) <= ConvertToDecimal(right),
            BinaryOperator.GreaterOrEqual => ConvertToDecimal(left) >= ConvertToDecimal(right),

            BinaryOperator.Equal => EvaluateEquality(left, right),
            BinaryOperator.NotEqual => !EvaluateEquality(left, right),

            BinaryOperator.Subtract => ConvertToDecimal(left) - ConvertToDecimal(right),
            BinaryOperator.Multiply => ConvertToDecimal(left) * ConvertToDecimal(right),
            BinaryOperator.Divide => ConvertToDecimal(left) / ConvertToDecimal(right),
            BinaryOperator.Modulo => ConvertToDecimal(left) % ConvertToDecimal(right),

            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private static object EvaluateAddition(Option<object> left, Option<object> right)
    {
        if (left.Value is decimal leftNum && right.Value is decimal rightNum)
        {
            return leftNum + rightNum;
        }

        if (left.IsNone || left.Value is null) return right.Value?.ToString();
        if (right.IsNone || right.Value is null) return null;

        return $"{left.Value}{right.Value}";
    }

    private static bool EvaluateEquality(Option<object> left, Option<object> right)
    {
        left = left.Select(NormaliseValue);
        right = right.Select(NormaliseValue);
        
        return left.Equals(right);
    }
    private static bool EvaluateEquality(object left, object right)
    {
        left = NormaliseValue(left);
        right = NormaliseValue(right);
        
        return Equals(left, right);
    }
    
    private static bool EvaluateBetween(Expression.Between between, object obj)
    {
        throw new NotImplementedException();
    }

    private static bool EvaluateIsNull(Expression.IsNull isNull, object obj)
    {
        var value = Evaluate<object>(isNull.Expression, obj);

        var hasValue = value.IsSome && NormaliseValue(value.Value) is not null;

        return hasValue == isNull.Negate;
    }
        
    private static bool EvaluatePresence(Expression.Presence presence, object obj)
    {
        var value = Evaluate<object>(presence.Expression, obj);

        var isMissing = value.IsNone;

        return isMissing == presence.Negate;
    }

    private static bool EvaluateIn(Expression.In inExpr, object obj)
    {
        var value = Evaluate<object>(inExpr.Expression, obj).Select(NormaliseValue);

        foreach (var matchExpr in inExpr.Matches)
        {
            var matchValue = Evaluate<object>(matchExpr, obj).Select(NormaliseValue);

            if (EvaluateEquality(value, matchValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EvaluateLike(Expression.Like like, object obj)
    {
        var pattern = EvaluateToString(like.Pattern, obj);
        var escape = like.Escape.SelectMany(x => EvaluateToString(x, obj));
        var value = EvaluateToString(like.Expression, obj);

        if (pattern.IsNone || value.IsNone)
        {
            return false;
        }

        if (escape.IsSome && escape.Value.Length != 1)
        {
            throw new InvalidOperationException($"Escape should be a single character, was '{escape.Value}'");
        }
        var escapeChar = escape.Select(x => x[0]);
            
        return LikeMatcher.IsMatch(pattern.Value, escapeChar, value.Value);
    }

    internal static bool ConvertToBoolean(object obj)
    {
        return obj switch
        {
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => true,
            bool b => b,
            _ => throw new ArgumentException($"Expected a boolean argument but got {obj?.GetType().Name ?? "null"}")
        };
    }

    internal static decimal ConvertToDecimal(object obj)
    {
        return obj switch
        {
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetDecimal(),
            decimal num => num,
            _ => throw new ArgumentException($"Expected a decimal argument but got {obj?.GetType().Name ?? "null"}")
        };
    }
    
    private static Option<string> EvaluateToString(Expression expr, object obj)
    {
        var valueObj = Evaluate<object>(expr, obj);
        if (valueObj.IsNone)
        {
            return new None();
        }

        return valueObj.Value switch
        {
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            string str => str,
            null => null,
            _ => throw new InvalidCastException($"'{expr}' did not evaluate to a string")
        };
    }
    
    internal static object NormaliseValue(object value) =>
        value switch
        {
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetDecimal(),
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            _ => value,
        };
}

public static class ExpressionEvaluatorExtensions
{
    public static Option<T> EvaluateOnTable<T>(this ExpressionEvaluator evaluator, Expression expression, FromClause from, JsonElement obj)
    {
        var tableName = from.Alias.Match(static alias => alias, static () => "s3object");
        
        var input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { tableName, obj }
        };

        return ExpressionEvaluator.Evaluate<T>(expression, input);
    }
}