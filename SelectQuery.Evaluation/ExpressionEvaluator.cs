using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class ExpressionEvaluator(IReadOnlyDictionary<string, JsonNode?> tables)
{
    public static Option<JsonNode?> EvaluateOnTable(Expression expression, FromClause from, JsonNode? obj)
    {
        var tableName = from.Alias.Match(alias => alias, _ => "s3object");
        
        var tables = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase)
        {
            { tableName, obj }
        };

        var evaluator = new ExpressionEvaluator(tables);
        
        return evaluator.Evaluate(expression, new None());
    }
    
    public Option<JsonNode?> Evaluate(Expression expression, Option<JsonNode?> context)
    {
        var value = expression.Match<Option<JsonNode?>>(
            strLiteral => JsonValue.Create(strLiteral.Value),
            numLiteral => JsonValue.Create(numLiteral.Value),
            boolLiteral => JsonValue.Create(boolLiteral.Value),
            identifier => EvaluateIdentifier(identifier, context),
            qualified => EvaluateQualified(qualified, context),
            function => EvaluateFunction(function.Function, context),
            unary => EvaluateUnary(unary, context),
            binary => EvaluateBinary(binary, context),
            between => JsonValue.Create(EvaluateBetween(between, context)),
            isNull => JsonValue.Create(EvaluateIsNull(isNull, context)),
            presence => JsonValue.Create(EvaluatePresence(presence, context)),
            inExpr => JsonValue.Create(EvaluateIn(inExpr, context)),
            like => JsonValue.Create(EvaluateLike(like, context))
        );

        return value;
    }

    private Option<JsonNode?> EvaluateIdentifier(Expression.Identifier identifier, Option<JsonNode?> context)
    {
        if (context.IsNone)
        {
            if (identifier.CaseSensitive)
            {
                throw new NotImplementedException();
            }

            return tables.TryGetValue(identifier.Name, out var table) ? table : default;
        }

        if (context.AsT0 is JsonObject contextObject)
        {
            // fast: match exact key
            if (contextObject.TryGetPropertyValue(identifier.Name, out var result))
            {
                return result;
            }
            
            if (identifier.CaseSensitive)
            {
                return new None();
            }
            
            // slow: try match case-insensitive key
            var entry = contextObject.FirstOrDefault(x => string.Equals(x.Key, identifier.Name, StringComparison.OrdinalIgnoreCase));
            return entry.Key != default ? entry.Value : new None();
        }
        
        throw new NotImplementedException($"don't know how to get identifier ({identifier.Name}) value from {context.AsT0?.GetType().FullName ?? "null"}");
    }

    private Option<JsonNode?> EvaluateQualified(Expression.Qualified qualified, Option<JsonNode?> context)
    {
        var target = EvaluateIdentifier(qualified.Qualification, context);

        if (target.IsNone)
        {
            return target;
        }

        return Evaluate(qualified.Expression, target);

    }
    
    private Option<JsonNode?> EvaluateFunction(Function function, Option<JsonNode?> context)
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

    private Option<JsonNode?> EvaluateUnary(Expression.Unary unary, Option<JsonNode?> context)
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
                return JsonValue.Create(!boolValue);
            }
            case UnaryOperator.Negate:
            {
                var boolValue = ConvertToDecimal(value.AsT0);
                return JsonValue.Create(-boolValue);
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Option<JsonNode?> EvaluateBinary(Expression.Binary binary, Option<JsonNode?> context)
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
        if (left == null || right == null) return new None();

        
        return binary.Operator switch
        {
            BinaryOperator.And => JsonValue.Create(ConvertToBoolean(left) && ConvertToBoolean(right)),
            BinaryOperator.Or => JsonValue.Create(ConvertToBoolean(left) || ConvertToBoolean(right)),
            BinaryOperator.Lesser => JsonValue.Create(ConvertToDecimal(left) < ConvertToDecimal(right)),
            BinaryOperator.Greater => JsonValue.Create(ConvertToDecimal(left) > ConvertToDecimal(right)),
            BinaryOperator.LesserOrEqual => JsonValue.Create(ConvertToDecimal(left) <= ConvertToDecimal(right)),
            BinaryOperator.GreaterOrEqual => JsonValue.Create(ConvertToDecimal(left) >= ConvertToDecimal(right)),

            BinaryOperator.Equal => JsonValue.Create(EvaluateEquality(left, right)),
            BinaryOperator.NotEqual => JsonValue.Create(!EvaluateEquality(left, right)),

            BinaryOperator.Subtract => JsonValue.Create(ConvertToDecimal(left) - ConvertToDecimal(right)),
            BinaryOperator.Multiply => JsonValue.Create(ConvertToDecimal(left) * ConvertToDecimal(right)),
            BinaryOperator.Divide => JsonValue.Create(ConvertToDecimal(left) / ConvertToDecimal(right)),
            BinaryOperator.Modulo => JsonValue.Create(ConvertToDecimal(left) % ConvertToDecimal(right)),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private Option<JsonNode?> EvaluateAddition(Option<JsonNode?> left, Option<JsonNode?> right)
    {
        if (left.IsNone || left.AsT0 is null) return right;
        if (right.IsNone || right.AsT0 is null) return (JsonNode?)null;

        if (left.AsT0 is JsonValue leftValue && leftValue.TryGetValue(out decimal leftNumber) && right.AsT0 is JsonValue rightValue && rightValue.TryGetValue(out decimal rightNumber))
        {
            return JsonValue.Create(leftNumber + rightNumber);
        }
        
        return JsonValue.Create($"{ConvertToString(left.AsT0)}{ConvertToString(right.AsT0)}");
    }
    
    private bool EvaluateEquality(Option<JsonNode?> left, Option<JsonNode?> right)
    {
        if (left.IsSome != right.IsSome) return false;
        if (!left.IsSome) return true;

        return EvaluateEquality(left.AsT0, right.AsT0);
    }
    
    private bool EvaluateEquality(JsonNode? left, JsonNode? right)
    {
        if (left is null) return right is null;
        if (right is null) return false;

        var kind = left.GetValueKind();
        if (kind != right.GetValueKind()) return false;

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
    
    private bool EvaluateBetween(Expression.Between between, Option<JsonNode?> context)
    {
        throw new NotImplementedException();
    }

    private bool EvaluateIsNull(Expression.IsNull isNull, Option<JsonNode?> context)
    {
        var value = Evaluate(isNull.Expression, context);

        var hasValue = value is { IsSome: true, AsT0: not null };

        return hasValue == isNull.Negate;
    }
        
    private bool EvaluatePresence(Expression.Presence presence, Option<JsonNode?> context)
    {
        var value = Evaluate(presence.Expression, context);

        var isMissing = value.IsNone;

        return isMissing == presence.Negate;
    }

    private bool EvaluateIn(Expression.In inExpr, Option<JsonNode?> context)
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

    private bool EvaluateLike(Expression.Like like, Option<JsonNode?> context)
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

    internal static bool ConvertToBoolean(JsonNode? context)
    {
        if (context is not JsonValue jsonValue)
        {
            throw new ArgumentException($"Expected a boolean argument but got {context?.GetType().Name ?? "null"}");
        }
        
        if (jsonValue.TryGetValue(out bool value))
        {
            return value;
        }

        throw new ArgumentException($"Expected a boolean argument but got {jsonValue.GetValueKind()}");
    }

    internal static decimal ConvertToDecimal(JsonNode? context)
    {
        if (context is not JsonValue jsonValue)
        {
            throw new ArgumentException($"Expected a decimal argument but got {context?.GetType().Name ?? "null"}");
        }
        
        if (jsonValue.TryGetValue(out decimal value))
        {
            return value;
        }

        throw new ArgumentException($"Expected a decimal argument but got {jsonValue.GetValueKind()}");
    }
    
    internal static string ConvertToString(JsonNode? context)
    {
        if (context is not JsonValue jsonValue)
        {
            throw new ArgumentException($"Expected a decimal argument but got {context?.GetType().Name ?? "null"}");
        }
        
        if (jsonValue.TryGetValue(out string? value))
        {
            return value;
        }

        throw new ArgumentException($"Expected a decimal argument but got {jsonValue.GetValueKind()}");
    }
    
    private Option<string?> EvaluateToString(Expression expr, Option<JsonNode?> context)
    {
        var result = Evaluate(expr, context);
        
        if (result.IsNone)
        {
            return new None();
        }

        return ConvertToString(result.AsT0);
    }
}