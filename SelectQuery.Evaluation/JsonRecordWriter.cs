using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class JsonRecordWriter(FromClause from, SelectClause select, ConcurrentDictionary<Expression, JsonElement> literalExpressionCache)
{
    public void Write(Utf8JsonWriter writer, JsonElement obj)
    {
        switch (select)
        {
            case SelectClause.Star:
                WriteStar(writer, obj);
                break;

            case SelectClause.List list:
                writer.WriteStartObject();
                WriteColumns(writer, list.Columns, obj);
                writer.WriteEndObject();
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(select));
        }
    }

    private static void WriteStar(Utf8JsonWriter writer, JsonElement obj)
    {
        obj.WriteTo(writer);
    }

    private void WriteColumns(Utf8JsonWriter writer, IReadOnlyList<Column> columns, JsonElement obj)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];

            var result = ExpressionEvaluator.EvaluateOnTable(column.Expression, from, obj, literalExpressionCache);
            if (!result.IsSome)
            {
                continue;
            }
            
            writer.WritePropertyName(GetColumnName(index, column));
            result.AsT0.WriteTo(writer);
        }
    }

    internal static string GetColumnName(int index, Column column)
    {
        if (column.Alias.IsSome)
        {
            return column.Alias.AsT0;
        }

        var expression = column.Expression;
        return GetColumnName(index, expression);
    }

    private static string GetColumnName(int index, Expression expression)
    {
        return expression switch
        {
            Expression.Identifier identifier => identifier.Name,
            Expression.Qualified qualified => qualified.Identifiers[qualified.Identifiers.Count - 1].Name,
            _ => $"_{index + 1}"
        };
    }
}