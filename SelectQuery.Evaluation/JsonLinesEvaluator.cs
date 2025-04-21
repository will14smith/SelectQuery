using System;
using System.Runtime.CompilerServices;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class JsonLinesEvaluator
{
    private readonly Query _query;
    // private readonly JsonRecordWriter _recordWriter;

    public JsonLinesEvaluator(Query query)
    {
        var validator = new QueryValidator();
        validator.Validate(query);
        validator.ThrowIfErrors();
            
        _query = query;
        // _recordWriter = new JsonRecordWriter(query.From, query.Select);
    }

    public byte[] Run(byte[] file)
    {
        using var writer = JsonRecordWriter.Create(_query);
        using var predicate = PredicateEvaluator.Create(_query);
        
        var stream = SimdJsonDotNet.Parser.ParseMany(file);
        
        if (QueryValidator.IsAggregateQuery(_query))
        {
            // ProcessAggregate(ref reader, ref writer);
            throw new NotImplementedException();
        }
        else
        {
            var state = new State
            {
                Limit = _query.Limit.Match(x => x.Limit, _ => int.MaxValue)
            };

            using var reader = stream.GetEnumerator();

            while (!state.IsLimitReached())
            {
                if (!reader.MoveNext())
                {
                    // no more input records to process
                    break;
                }
                
                var record = reader.Current;
                if(!predicate.Test(record))
                {
                    continue;
                }
                
                writer.EvaluateSelect(record);
                state.Count++;
            }
        }

        return writer.ToArray();
    }
    
    private struct State
    {
        public int Count;
        public int Limit;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLimitReached() => Count >= Limit;
    }
}