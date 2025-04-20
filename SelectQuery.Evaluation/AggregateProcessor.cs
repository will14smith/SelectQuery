using System;
using System.Collections.Generic;
using SelectParser;
using SelectParser.Queries;
using Utf8Json;

namespace SelectQuery.Evaluation;

internal class AggregateProcessor(Query query)
{
    private static readonly ExpressionEvaluator ExpressionEvaluator = new();

    private readonly IReadOnlyList<ColumnState> _columns = ColumnState.CreateForQuery(query);

    public void ProcessRecord(object record)
    {
        foreach (var column in _columns)
        {
            column.ProcessRecord(record);
        }
    }

    public void Write(ref JsonWriter writer)
    {
        writer.WriteBeginObject();
     
        var hasWritten = false;
        for (var index = 0; index < _columns.Count; index++)
        {
            if (hasWritten)
            {
                writer.WriteValueSeparator();
            }
            hasWritten = true;
            
            var columnState = _columns[index];
            
            writer.WritePropertyName(JsonRecordWriter.GetColumnName(index, columnState.Column));
            columnState.WriteResult(ref writer);
        }

        writer.WriteEndObject();
    }
    
    private abstract class ColumnState(Column column, Query query)
    {
        public Column Column { get; } = column;

        public static IReadOnlyList<ColumnState> CreateForQuery(Query query)
        {
            var states = new List<ColumnState>();

            var columns = ((SelectClause.List)query.Select).Columns;
            foreach (var column in columns)
            {
                states.Add(CreateForColumn(query, column));
            }
                        
            return states;
        }

        private static ColumnState CreateForColumn(Query query, Column column)
        {
            if (column.Expression is not Expression.FunctionExpression { Function: AggregateFunction aggregateFunction })
            {
                throw new NotImplementedException();
            }

            return aggregateFunction switch
            {
                AggregateFunction.Average average => new AverageColumnState(column, query, average),
                AggregateFunction.Count count => new CountColumnState(column, query, count),
                AggregateFunction.Max max => new MaxColumnState(column, query, max),
                AggregateFunction.Min min => new MinColumnState(column, query, min),
                AggregateFunction.Sum sum => new SumColumnState(column, query, sum),
                _ => throw new ArgumentOutOfRangeException(nameof(aggregateFunction))
            };
        }
        
        public abstract void ProcessRecord(object record);
        public abstract void WriteResult(ref JsonWriter writer);

        protected Option<T> Evaluate<T>(Expression expression, object record) => ExpressionEvaluator.EvaluateOnTable<T>(expression, query.From, record);

        private class AverageColumnState(Column column, Query query, AggregateFunction.Average average) : ColumnState(column, query)
        {
            private int _count;
            private decimal _acc;

            public override void ProcessRecord(object record)
            {
                var value = Evaluate<object>(average.Expression, record);
                if (value.IsNone)
                {
                    return;
                }
                
                _count++;
                _acc += (decimal) value.AsT0;
            }

            public override void WriteResult(ref JsonWriter writer)
            {
                if (_count > 0)
                {
                    var result = _acc / _count;

                    writer.WriteDouble((double)result);
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }
        
        private class CountColumnState(Column column, Query query, AggregateFunction.Count count) : ColumnState(column, query)
        {
            private int _acc;

            public override void ProcessRecord(object record)
            {
                if (!count.Expression.IsNone)
                {
                    var value = Evaluate<object>(count.Expression.AsT0, record);
                    if (value.IsNone)
                    {
                        return;
                    }
                }

                _acc++;
            }

            public override void WriteResult(ref JsonWriter writer)
            {
                writer.WriteInt32(_acc);
            }
        }
        
        private class MaxColumnState(Column column, Query query, AggregateFunction.Max max) : ColumnState(column, query)
        {
            private decimal? _best;

            public override void ProcessRecord(object record)
            {
                var result = Evaluate<object>(max.Expression, record);
                if (result.IsNone)
                {
                    return;
                }
                
                var value = (decimal) result.AsT0;
                if (_best == null || value > _best)
                {
                    _best = value;
                }
            }

            public override void WriteResult(ref JsonWriter writer)
            {
                if (_best != null)
                {
                    writer.WriteDouble((double)_best);
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }
        
        private class MinColumnState(Column column, Query query, AggregateFunction.Min min) : ColumnState(column, query)
        {
            private decimal? _best;

            public override void ProcessRecord(object record)
            {
                var result = Evaluate<object>(min.Expression, record);
                if (result.IsNone)
                {
                    return;
                }
                
                var value = (decimal) result.AsT0;
                if (_best == null || value < _best)
                {
                    _best = value;
                }
            }

            public override void WriteResult(ref JsonWriter writer)
            {
                if (_best != null)
                {
                    writer.WriteDouble((double)_best);
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }
        
        private class SumColumnState(Column column, Query query, AggregateFunction.Sum sum) : ColumnState(column, query)
        {
            private decimal? _acc;

            public override void ProcessRecord(object record)
            {
                var value = Evaluate<object>(sum.Expression, record);
                if (value.IsNone)
                {
                    return;
                }
                
                _acc = (_acc ?? 0) + (decimal) value.AsT0;
            }

            public override void WriteResult(ref JsonWriter writer)
            {
                if (_acc != null)
                {
                    writer.WriteDouble((double)_acc);
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }
    }
}