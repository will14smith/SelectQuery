using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SelectParser;
using SelectParser.Queries;
using SimdJsonDotNet.Model;

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
                if(!TestPredicate(record))
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
    
    // private void ProcessAggregate(ref JsonReader reader, ref JsonWriter writer)
    // {
    //     var state = new AggregateProcessor(_query);
    //         
    //     var recordsProcessed = 0;
    //     var limit = _query.Limit.Match(x => x.Limit, _ => int.MaxValue);
    //         
    //     while (true)
    //     {
    //         var readResult = ReadRecord(ref reader);
    //         if (readResult.IsNone)
    //         {
    //             break;
    //         }
    //
    //         var record = readResult.AsT0;
    //         if (TestPredicate(record))
    //         {
    //             if (recordsProcessed++ >= limit)
    //             {
    //                 break;
    //             }
    //                 
    //             state.ProcessRecord(record);
    //         }
    //     }
    //
    //     state.Write(ref writer);
    //     writer.WriteRaw((byte) '\n');
    // }
        
    // private bool ProcessRecord(ref JsonReader reader, ref JsonWriter writer, ref int recordsProcessed)
    // {
    //     var readResult = ReadRecord(ref reader);
    //     if (readResult.IsNone)
    //     {
    //         return false;
    //     }
    //
    //     var record = readResult.AsT0;
    //     if (TestPredicate(record))
    //     {
    //         recordsProcessed++;
    //         _recordWriter.Write(ref writer, record);
    //         writer.WriteRaw((byte) '\n');
    //     }
    //
    //     return true;
    // }

    // private Option<object> ReadRecord(ref JsonReader reader)
    // {
    //     reader.SkipWhiteSpace();
    //     if (reader.GetCurrentOffsetUnsafe() >= reader.GetBufferUnsafe().Length)
    //     {
    //         return new None();
    //     }
    //
    //     return Formatter.Deserialize(ref reader, StandardResolver.Default);
    // }

    private bool TestPredicate(in DocumentReference document)
    {
        if (_query.Where.IsNone)
        {
            return true;
        }
        
        throw new NotImplementedException();
        // var wherePassed = _query.Where.Match(where => ExpressionEvaluator.EvaluateOnTable<bool>(where.Condition, _query.From, record), _ => true);
        //
        // return wherePassed.IsSome && wherePassed.AsT0;
    }
}