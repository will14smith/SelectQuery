using System.Runtime.CompilerServices;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class JsonLinesEvaluator
{
    private readonly Query _query;

    public JsonLinesEvaluator(Query query)
    {
        var validator = new QueryValidator();
        validator.Validate(query);
        validator.ThrowIfErrors();
            
        _query = query;
    }

    public byte[] Run(byte[] file)
    {
        using var writer = JsonRecordWriter.Create(_query);
        using var predicate = PredicateEvaluator.Create(_query);
        
        var stream = SimdJsonDotNet.Parser.ParseMany(file);

        var isAggregateQuery = QueryValidator.IsAggregateQuery(_query);
        var state = new State
        {
            Limit = _query.Limit.Match(x => x.Limit, _ => int.MaxValue),
            Aggregate = isAggregateQuery ? new AggregateProcessor(_query) : null,
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

            if (isAggregateQuery)
            {
                state.Aggregate!.ProcessRecord(record);
            }
            else
            {
                writer.EvaluateSelect(record);
            }

            state.Count++;
        }

        if (isAggregateQuery)
        {
            state.Aggregate!.Write(writer);
        }
        
        return writer.ToArray();
    }
    
    private struct State
    {
        public int Count;
        public int Limit;
        public AggregateProcessor? Aggregate;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLimitReached() => Count >= Limit;
    }
}