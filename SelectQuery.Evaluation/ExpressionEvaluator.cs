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
    public Option<T?> Evaluate<T>(Expression expression, object? obj)
    {
        var value = expression.Match(
            strLiteral => (T?)(object?)EvaluateStringLiteral(strLiteral),
            numLiteral => (T?)(object?)EvaluateNumberLiteral(numLiteral),
            boolLiteral => (T?)(object?)EvaluateBooleanLiteral(boolLiteral),
            identifier => EvaluateIdentifier<T>(identifier, obj),
            qualified => EvaluateQualified<T>(qualified, obj),
            function => EvaluateFunction<T>(function.Function, obj),
            unary => EvaluateUnary<T>(unary, obj),
            binary => EvaluateBinary<T>(binary, obj),
            between => (T?)(object?)EvaluateBetween(between, obj),
            isNull => (T?)(object?)EvaluateIsNull(isNull, obj),
            presence => (T?)(object?)EvaluatePresence(presence, obj),
            inExpr => (T?)(object?)EvaluateIn(inExpr, obj),
            like => (T?)(object?)EvaluateLike(like, obj)
        );

        // normalize result values, utf8json parses numbers are decimal but we want doubles
        if (value.Value is double dbl) return (T?)(object?)Convert.ToDecimal(dbl);
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

    private Option<T?> EvaluateIdentifier<T>(Expression.Identifier identifier, object? obj)
    {
        if (obj is null)
        {
            return default;
        }

        if (obj is IReadOnlyDictionary<string, object?> dict)
        {
            // fast: match exact key
            if (dict.TryGetValue(identifier.Name, out var result))
                return (T?) result;

            if (identifier.CaseSensitive)
            {
                return new None();
            }

            // slow: try match case-insensitive key, ideally the dictionary would be case-insensitive but need some custom deserialization logic to support that
            var entry = dict.FirstOrDefault(x => string.Equals(x.Key, identifier.Name, StringComparison.OrdinalIgnoreCase));
            return entry.Key != null ? (T?) entry.Value : new None();
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
            return entry.Value.ValueKind != JsonValueKind.Undefined ? (T?) (object?) entry.Value : new None();
        }
        
        throw new NotImplementedException($"don't know how to get identifier ({identifier.Name}) value from {obj?.GetType().FullName ?? "null"}");
    }

    private Option<T?> EvaluateQualified<T>(Expression.Qualified qualified, object? obj)
    {
        var target = EvaluateIdentifier<object>(qualified.Qualification, obj);

        return target.SelectMany(obj => Evaluate<T>(qualified.Expression, obj));
    }
    
    private Option<T?> EvaluateFunction<T>(Function function, object? obj)
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

    private Option<T?> EvaluateUnary<T>(Expression.Unary unary, object? obj) =>
        unary.Operator switch
        {
            UnaryOperator.Not => Evaluate<bool>(unary.Expression, obj).Select(value => (T?) (object?) !value),
            UnaryOperator.Negate => Evaluate<decimal>(unary.Expression, obj).Select(value => (T?) (object?) -value),

            _ => throw new ArgumentOutOfRangeException()
        };

    private T? EvaluateBinary<T>(Expression.Binary binary, object? obj)
    {
        var leftOpt = Evaluate<object>(binary.Left, obj);
        var rightOpt = Evaluate<object>(binary.Right, obj);

        if (binary.Operator == BinaryOperator.Add)
        {
            return (T?) EvaluateAddition(leftOpt, rightOpt);
        }

        var left = leftOpt.Value;
        var right = rightOpt.Value;

        // propagate nulls
        if (leftOpt.IsNone || left == null || rightOpt.IsNone || right == null) return default;

        return (T?) (object?) (binary.Operator switch
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
            
            BinaryOperator.Concat => ConvertToString(left) + ConvertToString(right),

            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private object? EvaluateAddition(Option<object?> left, Option<object?> right)
    {
        if (left.Value is decimal leftNum && right.Value is decimal rightNum)
        {
            return leftNum + rightNum;
        }

        if (left.IsNone || left.Value is null) return right.Value?.ToString();
        if (right.IsNone || right.Value is null) return null;

        return $"{left.Value}{right.Value}";
    }

    private bool EvaluateEquality(object? left, object? right)
    {
        left = NormaliseValue(left);
        right = NormaliseValue(right);
        
        return Equals(left, right);
    }
    
    private bool EvaluateBetween(Expression.Between between, object? obj)
    {
        throw new NotImplementedException();
    }

    private bool EvaluateIsNull(Expression.IsNull isNull, object? obj)
    {
        var value = Evaluate<object>(isNull.Expression, obj);

        var hasValue = value.IsSome && NormaliseValue(value.AsT0) is not null;

        return hasValue == isNull.Negate;
    }
        
    private bool EvaluatePresence(Expression.Presence presence, object? obj)
    {
        var value = Evaluate<object>(presence.Expression, obj);

        var isMissing = value.IsNone;

        return isMissing == presence.Negate;
    }

    private bool EvaluateIn(Expression.In inExpr, object? obj)
    {
        if (inExpr.StringMatches is not null)
        {
            var value = EvaluateToString(inExpr.Expression, obj);
            if (value.IsNone) return false;

            foreach (var matchExpr in inExpr.Matches)
            {
                var matchValue = Evaluate<object>(matchExpr, obj).Select(NormaliseValue);

                return inExpr.StringMatches.Contains(value.AsT0);
            }

            return false;
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

    private bool EvaluateLike(Expression.Like like, object? obj)
    {
        var pattern = EvaluateToString(like.Pattern, obj);
        var escape = like.Escape.SelectMany(x => EvaluateToString(x, obj));
        var value = EvaluateToString(like.Expression, obj);

        if (pattern.IsNone || value.IsNone)
        {
            return false;
        }

        if (escape.IsSome && escape.AsT0?.Length != 1)
        {
            throw new InvalidOperationException($"Escape should be a single character, was '{escape.AsT0}'");
        }
        var escapeChar = escape.SelectMany(x => x is not null ? (Option<char>) x[0] : new None());
            
        return LikeMatcher.IsMatch(pattern.AsT0 ?? string.Empty, escapeChar, value.AsT0 ?? string.Empty);
    }

    internal static bool ConvertToBoolean(object? obj)
    {
        return obj switch
        {
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => true,
            bool b => b,
            _ => throw new ArgumentException($"Expected a boolean argument but got {obj?.GetType().Name ?? "null"}")
        };
    }

    internal static decimal ConvertToDecimal(object? obj)
    {
        return obj switch
        {
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetDecimal(),
            decimal num => num,
            _ => throw new ArgumentException($"Expected a decimal argument but got {obj?.GetType().Name ?? "null"}")
        };
    }
    
    internal static string? ConvertToString(object? obj)
    {
        return obj switch
        {
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            string str => str,
            null => null,
            _ => throw new ArgumentException($"Expected a string argument but got {obj?.GetType().Name ?? "null"}")
        };
    }
    
    private Option<string?> EvaluateToString(Expression expr, object? obj)
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
    
    internal static object? NormaliseValue(object? value) =>
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
    public static Option<T?> EvaluateOnTable<T>(this ExpressionEvaluator evaluator, Expression expression, FromClause from, object? obj)
    {
        var tableName = from.Alias.Match(alias => alias, _ => "s3object");
        
        var input = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            { tableName, obj }
        };

        return evaluator.Evaluate<T>(expression, input);
    }
}