using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using OneOf.Types;
using SelectParser;

namespace SelectQuery.Evaluation;

internal static class FunctionEvaluator
{
    public static Option<JsonNode?> Evaluate(string? name, IReadOnlyList<Option<JsonNode?>> arguments)
    {
        var normalisedName = name?.ToLowerInvariant();

        return normalisedName switch
        {
            "lower" => Lower(arguments),
            
            _ => throw new ArgumentOutOfRangeException($"Scalar function '{name}' is not supported", nameof(name))
        };
    }

    private static Option<JsonNode?> Lower(IReadOnlyList<Option<JsonNode?>> arguments)
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
        
        return argument.AsT0 switch
        {
            JsonValue value when value.TryGetValue(out string? stringValue) => JsonValue.Create(stringValue.ToLowerInvariant()),
            null => null,
            
            _ => throw new ArgumentException($"Expected a string argument but got {argument.GetType().Name}")
        };
    }
}