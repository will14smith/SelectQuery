using System;
using System.Collections.Generic;
using SelectParser;
using SelectParser.Queries;
using Utf8Json;

namespace SelectQuery.Evaluation;

internal class AggregateProcessor
{
    private static readonly ExpressionEvaluator ExpressionEvaluator = new();

    private readonly IReadOnlyList<ColumnState> _columns;

    public AggregateProcessor(Query query)
    {
        _columns = ColumnState.CreateForQuery(query);
    }

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
    
    private abstract class ColumnState
    {
        private readonly Query _query;
        public Column Column { get; }

        protected ColumnState(Column column, Query query)
        {
            _query = query;
            Column = column;
        }

        public static IReadOnlyList<ColumnState> CreateForQuery(Query query)
        {
            var states = new List<ColumnState>();

            var columns = query.Select.AsT1.Columns;
            foreach (var column in columns)
            {
                states.Add(CreateForColumn(query, column));
            }
                        
            return states;
        }

        private static ColumnState CreateForColumn(Query query, Column column)
        {
            if (!(column.Expression.Value is Expression.FunctionExpression functionExpression))
            {
                throw new NotImplementedException();
            }

            if (!(functionExpression.Function.Value is AggregateFunction aggregateFunction))
            {
                throw new NotImplementedException();
            }

            return aggregateFunction.Match<ColumnState>(
                x => new AverageColumnState(column, query, x),                
                x => new CountColumnState(column, query, x),                
                x => new MaxColumnState(column, query, x),                
                x => new MinColumnState(column, query, x),                
                x => new SumColumnState(column, query, x)                
            );
        }
        
        public abstract void ProcessRecord(object record);
        public abstract void WriteResult(ref JsonWriter writer);

        protected Option<T> Evaluate<T>(Expression expression, object record) => ExpressionEvaluator.EvaluateOnTable<T>(expression, _query.From, record);

        private class AverageColumnState : ColumnState
        {
            private readonly AggregateFunction.Average _average;
            private int _count;
            private decimal _acc;
            
            public AverageColumnState(Column column, Query query, AggregateFunction.Average average) : base(column, query) => _average = average;

            public override void ProcessRecord(object record)
            {
                var value = Evaluate<object>(_average.Expression, record);
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
        
        private class CountColumnState : ColumnState
        {
            private readonly AggregateFunction.Count _count;
            private int _acc;
            
            public CountColumnState(Column column, Query query, AggregateFunction.Count count) : base(column, query) => _count = count;
            
            public override void ProcessRecord(object record)
            {
                if (!_count.Expression.IsNone)
                {
                    var value = Evaluate<object>(_count.Expression.AsT0, record);
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
        
        private class MaxColumnState : ColumnState
        {
            private readonly AggregateFunction.Max _max;
            private decimal? _best;
            
            public MaxColumnState(Column column, Query query, AggregateFunction.Max max) : base(column, query) => _max = max;
            
            public override void ProcessRecord(object record)
            {
                var result = Evaluate<object>(_max.Expression, record);
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
        
        private class MinColumnState : ColumnState
        {
            private readonly AggregateFunction.Min _min;
            private decimal? _best;

            public MinColumnState(Column column, Query query, AggregateFunction.Min min) : base(column, query) => _min = min;
            
            public override void ProcessRecord(object record)
            {
                var result = Evaluate<object>(_min.Expression, record);
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
        
        private class SumColumnState : ColumnState
        {
            private readonly AggregateFunction.Sum _sum;
            private decimal? _acc;
            
            public SumColumnState(Column column, Query query, AggregateFunction.Sum sum) : base(column, query) => _sum = sum;
            
            public override void ProcessRecord(object record)
            {
                var value = Evaluate<object>(_sum.Expression, record);
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