using System;
using System.Collections.Generic;
using System.Text.Json;
using OneOf.Types;
using SelectParser;

namespace SelectQuery.Evaluation;

internal static class FunctionEvaluator
{
    public static Option<JsonElement> Evaluate(ExpressionEvaluator evaluator, string? name, IReadOnlyList<Option<JsonElement>> arguments)
    {
        var normalisedName = name?.ToLowerInvariant();

        return normalisedName switch
        {
            "lower" => Lower(evaluator, arguments),
            
            _ => throw new ArgumentOutOfRangeException($"Scalar function '{name}' is not supported", nameof(name))
        };
    }

    private static Option<JsonElement> Lower(ExpressionEvaluator evaluator, IReadOnlyList<Option<JsonElement>> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Expected a single argument but got {arguments.Count}");
        }

        var argument = arguments[0];
        if (argument.IsNone)
        {
            return new None();
        }

        var stringValue = ExpressionEvaluator.ConvertToString(argument.Value);
        
        return evaluator.CreateElement(stringValue?.ToLowerInvariant());
    }
}