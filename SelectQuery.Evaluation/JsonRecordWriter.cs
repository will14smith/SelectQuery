using System.Collections.Generic;
using SelectParser.Queries;
using Utf8Json;
using Utf8Json.Resolvers;

namespace SelectQuery.Evaluation;

public class JsonRecordWriter(FromClause from, SelectClause select)
{
    private static readonly IJsonFormatter<object> Formatter = StandardResolver.Default.GetFormatter<object>();
    private static readonly ExpressionEvaluator ExpressionEvaluator = new();

    public void Write(ref JsonWriter writer, object obj)
    {
        if (select is not SelectClause.List list)
        {
            WriteStar(ref writer, obj);
        }
        else
        {
            writer.WriteBeginObject();
            WriteColumns(ref writer, list.Columns, obj);
            writer.WriteEndObject();
        }
    }

    private static void WriteStar(ref JsonWriter writer, object obj)
    {
        Formatter.Serialize(ref writer, obj, StandardResolver.Default);
    }

    private void WriteColumns(ref JsonWriter writer, IReadOnlyList<Column> columns, object obj)
    {
        var hasWritten = false;
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];

            var result = ExpressionEvaluator.EvaluateOnTable<object>(column.Expression, from, obj);
            if (!result.IsSome)
            {
                continue;
            }

            if (hasWritten)
            {
                writer.WriteValueSeparator();
            }
            hasWritten = true;

            writer.WritePropertyName(GetColumnName(index, column));
            Formatter.Serialize(ref writer, result.AsT0, StandardResolver.Default);
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
            if (expression is Expression.Identifier identifier)
            {
                // use the identifier name
                return identifier.Name;
            }

            if (expression is not Expression.Qualified qualified)
            {
                // default is _N for the Nth column (1 indexed)
                return $"_{index + 1}";
            }

            // recurse down qualified expressions
            expression = qualified.Expression;
        }
    }
}