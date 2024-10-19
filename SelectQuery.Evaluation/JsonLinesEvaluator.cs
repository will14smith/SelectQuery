using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class JsonLinesEvaluator
{
    private readonly Query _query;
    private readonly ConcurrentDictionary<Expression, JsonElement> _literalExpressionCache;
    private readonly JsonRecordWriter _recordWriter;

    public JsonLinesEvaluator(Query query)
    {
        var validator = new QueryValidator();
        validator.Validate(query);
        validator.ThrowIfErrors();
            
        _query = query;
        _literalExpressionCache = new ConcurrentDictionary<Expression, JsonElement>();
        _recordWriter = new JsonRecordWriter(query.From, query.Select, _literalExpressionCache);
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
        var state = new AggregateProcessor(_query, _literalExpressionCache);
            
        var recordsProcessed = 0;
        var limit = _query.Limit.Match(x => x.Limit, _ => int.MaxValue);
            
        while (true)
        {
            var readResult = ReadRecord(ref reader);
            if (readResult.IsNone)
            {
                break;
            }

            using var record = readResult.AsT0;
            var rootElement = record.RootElement;
            if (TestPredicate(rootElement, _literalExpressionCache))
            {
                if (recordsProcessed++ >= limit)
                {
                    break;
                }
                    
                state.ProcessRecord(rootElement);
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

        using var record = readResult.AsT0;
        var rootElement = record.RootElement;
        if (TestPredicate(rootElement, _literalExpressionCache))
        {
            recordsProcessed++;
            _recordWriter.Write(writer, rootElement);
            WriteEndRecord(writer, outputBuffer);
        }

        return true;
    }
    
    private Option<JsonDocument> ReadRecord(ref JsonLinesReader lineReader)
    {
        if (!lineReader.Read())
        {
            return new None();
        }

        var reader = lineReader.Current;

        return JsonDocument.ParseValue(ref reader);
    }

    private bool TestPredicate(JsonElement record, ConcurrentDictionary<Expression,JsonElement> literalExpressionCache)
    {
        var wherePassed = _query.Where.Match(where => ExpressionEvaluator.EvaluateOnTable(where.Condition, _query.From, record, literalExpressionCache).Select(ExpressionEvaluator.ConvertToBoolean), _ => true);

        return wherePassed.IsSome && wherePassed.AsT0;
    }
    
    private static void WriteEndRecord(Utf8JsonWriter writer, Stream outputBuffer)
    {
        writer.Flush();
        writer.Reset();
        outputBuffer.WriteByte((byte)'\n');
    }
}