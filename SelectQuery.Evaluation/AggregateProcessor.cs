using System;
using System.Collections.Generic;
using SelectParser.Queries;
using SimdJsonDotNet.Model;

namespace SelectQuery.Evaluation;

internal class AggregateProcessor(Query query)
{
    private readonly IReadOnlyList<ColumnState> _columns = ColumnState.CreateForQuery(query);
    private readonly string _tableAlias = query.From.Alias.Match(alias => alias, _ => "s3object");
    
    public void ProcessRecord(DocumentReference record)
    {
        foreach (var column in _columns)
        {
            column.ProcessRecord(record, _tableAlias);
        }
    }

    public void Write(JsonRecordWriter writer)
    {
        writer.BeginRow();

        for (var index = 0; index < _columns.Count; index++)
        {
            var column = _columns[index];
            writer.WriteColumn(index, column.GetResult());
        }

        writer.EndRow();
    }

    private abstract class ColumnState
    {
        public static IReadOnlyList<ColumnState> CreateForQuery(Query query)
        {
            var states = new List<ColumnState>();

            var columns = ((SelectClause.List)query.Select).Columns;
            foreach (var column in columns)
            {
                states.Add(CreateForColumn(column));
            }
                        
            return states;
        }

        private static ColumnState CreateForColumn(Column column)
        {
            if (column.Expression is not Expression.FunctionExpression { Function: AggregateFunction aggregateFunction })
            {
                throw new NotImplementedException();
            }

            return aggregateFunction switch
            {
                AggregateFunction.Average average => new AverageColumnState(average),
                AggregateFunction.Count count => new CountColumnState(count),
                AggregateFunction.Max max => new MaxColumnState(max),
                AggregateFunction.Min min => new MinColumnState(min),
                AggregateFunction.Sum sum => new SumColumnState(sum),
                _ => throw new ArgumentOutOfRangeException(nameof(aggregateFunction))
            };
        }
        
        public abstract void ProcessRecord(DocumentReference record, string tableAlias);
        public abstract ValueEvaluator.Result GetResult();
        
        private class AverageColumnState(AggregateFunction.Average average) : ColumnState
        {
            private int _count;
            private decimal _acc;

            public override void ProcessRecord(DocumentReference record, string tableAlias)
            {
                var value = ValueEvaluator.Evaluate(record, average.Expression, tableAlias);
                if (value.Type == ValueEvaluator.ResultType.None)
                {
                    return;
                }
                
                _count++;
                _acc += value.AsNumber();
            }

            public override ValueEvaluator.Result GetResult() => 
                _count == 0 
                    ? ValueEvaluator.Result.Null() 
                    : ValueEvaluator.Result.NewLiteral(_acc / _count);
        }
        
        private class CountColumnState(AggregateFunction.Count count) : ColumnState
        {
            private int _acc;

            public override void ProcessRecord(DocumentReference record, string tableAlias)
            {
                if (!count.Expression.IsNone)
                {
                    var value = ValueEvaluator.Evaluate(record, count.Expression.Value!, tableAlias);
                    if (value.Type == ValueEvaluator.ResultType.None)
                    {
                        return;
                    }
                }

                _acc++;
            }

            public override ValueEvaluator.Result GetResult() => ValueEvaluator.Result.NewLiteral(_acc);
        }
        
        private class MaxColumnState(AggregateFunction.Max max) : ColumnState
        {
            private decimal? _best;

            public override void ProcessRecord(DocumentReference record, string tableAlias)
            {
                var result = ValueEvaluator.Evaluate(record, max.Expression, tableAlias);
                if (result.Type == ValueEvaluator.ResultType.None)
                {
                    return;
                }
                
                var value = result.AsNumber();
                if (_best == null || value > _best)
                {
                    _best = value;
                }
            }

            public override ValueEvaluator.Result GetResult() => 
                _best != null
                    ? ValueEvaluator.Result.NewLiteral(_best.Value) 
                    : ValueEvaluator.Result.Null();
        }
        
        private class MinColumnState(AggregateFunction.Min min) : ColumnState
        {
            private decimal? _best;

            public override void ProcessRecord(DocumentReference record, string tableAlias)
            {
                var result = ValueEvaluator.Evaluate(record, min.Expression, tableAlias);
                if (result.Type == ValueEvaluator.ResultType.None)
                {
                    return;
                }
                
                var value = result.AsNumber();
                if (_best == null || value < _best)
                {
                    _best = value;
                }
            }

            public override ValueEvaluator.Result GetResult() =>
                _best != null
                    ? ValueEvaluator.Result.NewLiteral(_best.Value) 
                    : ValueEvaluator.Result.Null();
        }
        
        private class SumColumnState(AggregateFunction.Sum sum) : ColumnState
        {
            private decimal? _acc;

            public override void ProcessRecord(DocumentReference record, string tableAlias)
            {
                var value = ValueEvaluator.Evaluate(record, sum.Expression, tableAlias);
                if (value.Type == ValueEvaluator.ResultType.None)
                {
                    return;
                }
                
                _acc = (_acc ?? 0) + value.AsNumber();
            }

            public override ValueEvaluator.Result GetResult() =>
                _acc != null
                    ? ValueEvaluator.Result.NewLiteral(_acc.Value)
                    : ValueEvaluator.Result.Null();

        }
    }
}