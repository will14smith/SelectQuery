using System.Collections.Generic;
using System.Text.Json;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class JsonRecordWriter
{
    private static readonly ExpressionEvaluator ExpressionEvaluator = new();

    private readonly FromClause _from;
    private readonly SelectClause _select;

    public JsonRecordWriter(FromClause from, SelectClause select)
    {
        _from = from;
        _select = select;
    }

    public void Write(Utf8JsonWriter writer, object obj)
    {
        if (_select.IsT0)
        {
            WriteStar(writer, obj);
        }
        else
        {
            writer.WriteStartObject();
            WriteColumns(writer, _select.AsT1.Columns, obj);
            writer.WriteEndObject();
        }
    }

    private static void WriteStar(Utf8JsonWriter writer, object obj)
    {
        JsonSerializer.Serialize(writer, obj);
    }

    private void WriteColumns(Utf8JsonWriter writer, IReadOnlyList<Column> columns, object obj)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];

            var result = ExpressionEvaluator.EvaluateOnTable<object>(column.Expression, _from, obj);
            if (!result.IsSome)
            {
                continue;
            }
            
            writer.WritePropertyName(GetColumnName(index, column));
            JsonSerializer.Serialize(writer, result.AsT0);
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
        while (true)
        {
            if (expression.IsT3)
            {
                // use the identifier name
                return expression.AsT3.Name;
            }

            if (!expression.IsT4)
            {
                // default is _N for the Nth column (1 indexed)
                return $"_{index + 1}";
            }

            // recurse down qualified expressions
            expression = expression.AsT4.Expression;
        }
    }
}