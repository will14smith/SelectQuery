using System.IO;
using System.Text.Json;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class JsonLinesEvaluator
{
    private static readonly ExpressionEvaluator ExpressionEvaluator = new();

    private readonly Query _query;
    private readonly JsonRecordWriter _recordWriter;

    public JsonLinesEvaluator(Query query)
    {
        var validator = new QueryValidator();
        validator.Validate(query);
        validator.ThrowIfErrors();
            
        _query = query;
        _recordWriter = new JsonRecordWriter(query.From, query.Select);
    }

    public byte[] Run(byte[] file)
    {
        var outputBuffer = new MemoryStream();
        
        var reader = new JsonLinesReader(file);
        var writer = new Utf8JsonWriter(outputBuffer);

        if (QueryValidator.IsAggregateQuery(_query))
        {
            ProcessAggregate(ref reader, writer, outputBuffer);
        }
        else
        {
            var recordsProcessed = 0;
            var limit = _query.Limit.Match(x => x.Limit, _ => int.MaxValue);

            while (ProcessRecord(ref reader, writer, outputBuffer, ref recordsProcessed))
            {
                if (recordsProcessed >= limit)
                {
                    break;
                }
            }
        }

        return outputBuffer.ToArray();
    }
        
    private void ProcessAggregate(ref JsonLinesReader reader, Utf8JsonWriter writer, Stream outputBuffer)
    {
        var state = new AggregateProcessor(_query);
            
        var recordsProcessed = 0;
        var limit = _query.Limit.Match(x => x.Limit, _ => int.MaxValue);
            
        while (true)
        {
            var readResult = ReadRecord(ref reader);
            if (readResult.IsNone)
            {
                break;
            }

            var record = readResult.AsT0;
            if (TestPredicate(record))
            {
                if (recordsProcessed++ >= limit)
                {
                    break;
                }
                    
                state.ProcessRecord(record);
            }
        }

        state.Write(writer);
        WriteEndRecord(writer, outputBuffer);
    }
        
    private bool ProcessRecord(ref JsonLinesReader reader, Utf8JsonWriter writer, Stream outputBuffer, ref int recordsProcessed)
    {
        var readResult = ReadRecord(ref reader);
        if (readResult.IsNone)
        {
            return false;
        }

        var record = readResult.AsT0;
        if (TestPredicate(record))
        {
            recordsProcessed++;
            _recordWriter.Write(writer, record);
            WriteEndRecord(writer, outputBuffer);
        }

        return true;
    }
    
    private Option<object> ReadRecord(ref JsonLinesReader lineReader)
    {
        if (!lineReader.Read())
        {
            return new None();
        }

        var reader = lineReader.Current;
        return JsonSerializer.Deserialize<object>(ref reader);
    }

    private bool TestPredicate(object record)
    {
        var wherePassed = _query.Where.Match(where => ExpressionEvaluator.EvaluateOnTable<bool>(where.Condition, _query.From, record), _ => true);

        return wherePassed.IsSome && wherePassed.AsT0;
    }
    
    private static void WriteEndRecord(Utf8JsonWriter writer, Stream outputBuffer)
    {
        writer.Flush();
        writer.Reset();
        outputBuffer.WriteByte((byte)'\n');
    }
}