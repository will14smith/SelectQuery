using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class ExpressionEvaluator(IReadOnlyDictionary<string, JsonElement> tables)
{
    private static readonly JsonElement True; 
    private static readonly JsonElement False; 
    private static readonly JsonElement Null;

    static ExpressionEvaluator()
    {
        True = Create("true"u8);
        False = Create("false"u8);
        Null = Create("null"u8);
        
        return;

        JsonElement Create(ReadOnlySpan<byte> buffer)
        {
            var reader = new Utf8JsonReader(buffer);
            return JsonElement.ParseValue(ref reader);
        }
    }
    
    public static Option<JsonElement> EvaluateOnTable(Expression expression, FromClause from, JsonElement obj)
    {
        var tableName = from.Alias.Match(alias => alias, _ => "s3object");
        
        var tables = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            { tableName, obj }
        };

        var evaluator = new ExpressionEvaluator(tables);
        
        return evaluator.Evaluate(expression, new None());
    }
    
    public Option<JsonElement> Evaluate(Expression expression, Option<JsonElement> context)
    {
        var value = expression.Match<Option<JsonElement>>(
            strLiteral => CreateElement(strLiteral.Value),
            numLiteral => CreateElement(numLiteral.Value),
            boolLiteral => CreateElement(boolLiteral.Value),
            identifier => EvaluateIdentifier(identifier, context),
            qualified => EvaluateQualified(qualified, context),
            function => EvaluateFunction(function.Function, context),
            unary => EvaluateUnary(unary, context),
            binary => EvaluateBinary(binary, context),
            between => CreateElement(EvaluateBetween(between, context)),
            isNull => CreateElement(EvaluateIsNull(isNull, context)),
            presence => CreateElement(EvaluatePresence(presence, context)),
            inExpr => CreateElement(EvaluateIn(inExpr, context)),
            like => CreateElement(EvaluateLike(like, context))
        );

        return value;
    }
    
    internal static JsonElement CreateElement(string? value)
    {
        if (value is null) return Null;
        
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStringValue(value);
        }

        var reader = new Utf8JsonReader(stream.ToArray());
        return JsonElement.ParseValue(ref reader);
    }
    internal static JsonElement CreateElement(decimal value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteNumberValue(value);
        }

        var reader = new Utf8JsonReader(stream.ToArray());
        return JsonElement.ParseValue(ref reader);
    }
    internal static JsonElement CreateElement(bool value) => value ? True : False;
    internal static JsonElement CreateNullElement() => Null;

    private Option<JsonElement> EvaluateIdentifier(Expression.Identifier identifier, Option<JsonElement> context)
    {
        if (context.IsNone)
        {
            if (identifier.CaseSensitive)
            {
                throw new NotImplementedException();
            }

            return tables.TryGetValue(identifier.Name, out var table) ? table : default;
        }

        if (context.AsT0 is { ValueKind: JsonValueKind.Object })
        {
            // fast: match exact key
            if (context.AsT0.TryGetProperty(identifier.Name, out var result))
            {
                return result;
            }
            
            if (identifier.CaseSensitive)
            {
                return new None();
            }
            
            // slow: try match case-insensitive key
            var entry = context.AsT0.EnumerateObject().FirstOrDefault(x => string.Equals(x.Name, identifier.Name, StringComparison.OrdinalIgnoreCase));
            return entry.Value.ValueKind != JsonValueKind.Undefined ? entry.Value : new None();
        }
        
        throw new NotImplementedException($"don't know how to get identifier ({identifier.Name}) value from {context.AsT0.ValueKind}");
    }

    private Option<JsonElement> EvaluateQualified(Expression.Qualified qualified, Option<JsonElement> context)
    {
        for (var index = 0; index < qualified.Identifiers.Count; index++)
        {
            var identifier = qualified.Identifiers[index];
            context = EvaluateIdentifier(identifier, context);
            
            if (context.IsNone)
            {
                return context;
            }
        }
        
        return context;
    }
    
    private Option<JsonElement> EvaluateFunction(Function function, Option<JsonElement> context)
    {
        if (!function.IsT1)
        {
            throw new InvalidOperationException("cannot evaluate a non-scalar function in this way");
        }
        
        var scalar = function.AsT1;

        var name = scalar.Identifier.Name;
        var arguments = scalar.Arguments.Select(argument => Evaluate(argument, context)).ToList();

        return FunctionEvaluator.Evaluate(name, arguments);
    }

    private Option<JsonElement> EvaluateUnary(Expression.Unary unary, Option<JsonElement> context)
    {
        var value = Evaluate(unary.Expression, context);
        
        // propagate none
        if (value.IsNone)
        {
            return value;
        }
        
        switch (unary.Operator)
        {
            case UnaryOperator.Not:
            {
                var boolValue = ConvertToBoolean(value.AsT0);
                return CreateElement(!boolValue);
            }
            case UnaryOperator.Negate:
            {
                var boolValue = ConvertToDecimal(value.AsT0);
                return CreateElement(-boolValue);
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Option<JsonElement> EvaluateBinary(Expression.Binary binary, Option<JsonElement> context)
    {
        var leftOpt = Evaluate(binary.Left, context);
        var rightOpt = Evaluate(binary.Right, context);
        
        if (binary.Operator == BinaryOperator.Add)
        {
            return EvaluateAddition(leftOpt, rightOpt);
        }
        
        // propagate nulls
        if (leftOpt.IsNone || rightOpt.IsNone) return new None();
        
        var left = leftOpt.AsT0;
        var right = rightOpt.AsT0;
        if (left is { ValueKind: JsonValueKind.Null } || right is { ValueKind: JsonValueKind.Null }) return new None();
        
        return binary.Operator switch
        {
            BinaryOperator.And => CreateElement(ConvertToBoolean(left) && ConvertToBoolean(right)),
            BinaryOperator.Or => CreateElement(ConvertToBoolean(left) || ConvertToBoolean(right)),
            BinaryOperator.Lesser => CreateElement(ConvertToDecimal(left) < ConvertToDecimal(right)),
            BinaryOperator.Greater => CreateElement(ConvertToDecimal(left) > ConvertToDecimal(right)),
            BinaryOperator.LesserOrEqual => CreateElement(ConvertToDecimal(left) <= ConvertToDecimal(right)),
            BinaryOperator.GreaterOrEqual => CreateElement(ConvertToDecimal(left) >= ConvertToDecimal(right)),

            BinaryOperator.Equal => CreateElement(EvaluateEquality(left, right)),
            BinaryOperator.NotEqual => CreateElement(!EvaluateEquality(left, right)),

            BinaryOperator.Subtract => CreateElement(ConvertToDecimal(left) - ConvertToDecimal(right)),
            BinaryOperator.Multiply => CreateElement(ConvertToDecimal(left) * ConvertToDecimal(right)),
            BinaryOperator.Divide => CreateElement(ConvertToDecimal(left) / ConvertToDecimal(right)),
            BinaryOperator.Modulo => CreateElement(ConvertToDecimal(left) % ConvertToDecimal(right)),
            BinaryOperator.Concat => CreateElement(ConvertToString(left) + ConvertToString(right)),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private Option<JsonElement> EvaluateAddition(Option<JsonElement> left, Option<JsonElement> right)
    {
        if (left.IsNone || left.AsT0 is { ValueKind: JsonValueKind.Null }) return right;
        if (right.IsNone || right.AsT0 is { ValueKind: JsonValueKind.Null }) return CreateNullElement();

        if (left.AsT0 is { ValueKind: JsonValueKind.Number } && right.AsT0 is { ValueKind: JsonValueKind.Number })
        {
            return CreateElement(left.AsT0.GetDecimal() + right.AsT0.GetDecimal());
        }
        
        return CreateElement($"{ConvertToString(left.AsT0)}{ConvertToString(right.AsT0)}");
    }
    
    private bool EvaluateEquality(Option<JsonElement> left, Option<JsonElement> right)
    {
        if (left.IsSome != right.IsSome) return false;
        if (!left.IsSome) return true;

        return EvaluateEquality(left.AsT0, right.AsT0);
    }
    
    private bool EvaluateEquality(JsonElement left, JsonElement right)
    {
        var kind = left.ValueKind;
        if (kind != right.ValueKind) return false;

        switch (kind)
        {
            case JsonValueKind.Object:
                throw new NotImplementedException();
            case JsonValueKind.Array:
                throw new NotImplementedException();
            case JsonValueKind.String:
            {
                var leftValue = ConvertToString(left);
                var rightValue = ConvertToString(right);
                
                return leftValue == rightValue;
            }
            case JsonValueKind.Number:
            {
                var leftValue = ConvertToDecimal(left);
                var rightValue = ConvertToDecimal(right);
                
                return leftValue == rightValue;
            }
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private bool EvaluateBetween(Expression.Between between, Option<JsonElement> context)
    {
        throw new NotImplementedException();
    }

    private bool EvaluateIsNull(Expression.IsNull isNull, Option<JsonElement> context)
    {
        var value = Evaluate(isNull.Expression, context);

        var hasValue = value is { IsSome: true, AsT0: { ValueKind: not JsonValueKind.Null } };

        return hasValue == isNull.Negate;
    }
        
    private bool EvaluatePresence(Expression.Presence presence, Option<JsonElement> context)
    {
        var value = Evaluate(presence.Expression, context);

        var isMissing = value.IsNone;

        return isMissing == presence.Negate;
    }

    private bool EvaluateIn(Expression.In inExpr, Option<JsonElement> context)
    {
        if (inExpr.StringMatches is not null)
        {
            var value = EvaluateToString(inExpr.Expression, context);
            if (value.IsNone) return false;

            return inExpr.StringMatches.Contains(value.AsT0);
        }
        else
        {
            var value = Evaluate(inExpr.Expression, context);

            foreach (var matchExpr in inExpr.Matches)
            {
                var matchValue = Evaluate(matchExpr, context);

                if (EvaluateEquality(value, matchValue))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private bool EvaluateLike(Expression.Like like, Option<JsonElement> context)
    {
        var pattern = EvaluateToString(like.Pattern, context);
        var escape = like.Escape.SelectMany(x => EvaluateToString(x, context));
        var value = EvaluateToString(like.Expression, context);

        if (pattern.IsNone || pattern.AsT0 is null || value.IsNone)
        {
            return false;
        }

        if (escape .IsSome && escape.AsT0?.Length != 1)
        {
            throw new InvalidOperationException($"Escape should be a single character, was '{escape}'");
        }
        var escapeChar = escape.Select(x => x![0]);
        
        return LikeMatcher.IsMatch(pattern.AsT0, escapeChar, value.AsT0);
    }

    internal static bool ConvertToBoolean(JsonElement context)
    {
        if (context.ValueKind == JsonValueKind.True) return true;
        if (context.ValueKind == JsonValueKind.False) return false;
  
        throw new ArgumentException($"Expected a boolean argument but got {context.ValueKind}");
    }

    internal static decimal ConvertToDecimal(JsonElement context)
    {
        if (context.ValueKind == JsonValueKind.Number) return context.GetDecimal();

        throw new ArgumentException($"Expected a decimal argument but got {context.ValueKind}");
    }
    
    internal static string? ConvertToString(JsonElement context)
    {
        if (context.ValueKind == JsonValueKind.String) return context.GetString()!;
        if (context.ValueKind == JsonValueKind.Null) return null;

        throw new ArgumentException($"Expected a string argument but got {context.ValueKind}");
    }
    
    private Option<string?> EvaluateToString(Expression expr, Option<JsonElement> context)
    {
        var result = Evaluate(expr, context);
        
        if (result.IsNone)
        {
            return new None();
        }

        return ConvertToString(result.AsT0);
    }
}