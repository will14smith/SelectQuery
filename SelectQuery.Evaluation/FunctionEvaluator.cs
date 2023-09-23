using System;
using System.Collections.Generic;
using System.Text.Json;
using OneOf.Types;
using SelectParser;

namespace SelectQuery.Evaluation;

internal class FunctionEvaluator
{
    public static Option<T> Evaluate<T>(string name, IReadOnlyList<Option<object>> arguments)
    {
        var normalisedName = name?.ToLowerInvariant();

        switch (normalisedName)
        {
            case "lower": return Lower<T>(arguments);
            
            default: throw new ArgumentOutOfRangeException($"Scalar function '{name}' is not supported", nameof(name));
        }
    }

    private static Option<T> Lower<T>(IReadOnlyList<Option<object>> arguments)
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

        var stringValue = argument.Value switch
        {
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            string str => str,
            null => null,
            _ => throw new ArgumentException($"Expected a string argument but got {argument.Value.GetType().Name}")
        };
        
        return (T)(object)stringValue?.ToLowerInvariant();
    }
}