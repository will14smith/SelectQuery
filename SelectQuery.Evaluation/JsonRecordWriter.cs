using System;
using System.Collections.Generic;
using SelectParser.Queries;
using Utf8Json;
using Utf8Json.Resolvers;

namespace SelectQuery.Evaluation
{
    public class JsonRecordWriter
    {
        private static readonly IJsonFormatter<object> Formatter = StandardResolver.Default.GetFormatter<object>();
        private static readonly ExpressionEvaluator ExpressionEvaluator = new ExpressionEvaluator();

        private readonly FromClause _from;
        private readonly SelectClause _select;

        public JsonRecordWriter(FromClause from, SelectClause select)
        {
            _from = from;
            _select = select;
        }

        public void Write(ref JsonWriter writer, object obj)
        {
            if (_select.IsT0)
            {
                WriteStar(ref writer, obj);
            }
            else
            {
                writer.WriteBeginObject();
                WriteColumns(ref writer, _select.AsT1.Columns, obj);
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

                var result = ExpressionEvaluatorExtensions.EvaluateOnTable<object>(column.Expression, _from, obj);
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

        private static string GetColumnName(int index, Column column)
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
            if (expression.IsT3)
            {
                // use the identifier name
                return expression.AsT3.Name;
            }

            if (expression.IsT4)
            {
                // use the last identifier when qualified
                return expression.AsT4.LastIdentifier.Name;
            }

            // default is _N for the Nth column (1 indexed)
            return $"_{index + 1}";
        }
    }
}