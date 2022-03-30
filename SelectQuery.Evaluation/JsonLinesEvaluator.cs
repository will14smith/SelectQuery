using OneOf.Types;
using SelectParser;
using SelectParser.Queries;
using Utf8Json;
using Utf8Json.Resolvers;

namespace SelectQuery.Evaluation
{
    public class JsonLinesEvaluator
    {
        private static readonly IJsonFormatter<object> Formatter = StandardResolver.Default.GetFormatter<object>();
        private static readonly ExpressionEvaluator ExpressionEvaluator = new ExpressionEvaluator();

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
            var reader = new JsonReader(file);
            var writer = new JsonWriter();

            if (QueryValidator.IsAggregateQuery(_query))
            {
                ProcessAggregate(ref reader, ref writer);
            }
            else
            {
                while (ProcessRecord(ref reader, ref writer)) { }
            }

            return writer.ToUtf8ByteArray();
        }
        
        private void ProcessAggregate(ref JsonReader reader, ref JsonWriter writer)
        {
            var state = new AggregateProcessor(_query);

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
                    state.ProcessRecord(record);
                }
            }

            state.Write(ref writer);
            writer.WriteRaw((byte) '\n');
        }
        
        private bool ProcessRecord(ref JsonReader reader, ref JsonWriter writer)
        {
            var readResult = ReadRecord(ref reader);
            if (readResult.IsNone)
            {
                return false;
            }

            var record = readResult.AsT0;
            if (TestPredicate(record))
            {
                _recordWriter.Write(ref writer, record);
                writer.WriteRaw((byte) '\n');
            }

            return true;
        }

        private Option<object> ReadRecord(ref JsonReader reader)
        {
            reader.SkipWhiteSpace();
            if (reader.GetCurrentOffsetUnsafe() >= reader.GetBufferUnsafe().Length)
            {
                return new None();
            }

            return Formatter.Deserialize(ref reader, StandardResolver.Default);
        }

        private bool TestPredicate(object record)
        {
            var wherePassed = _query.Where.Match(where => ExpressionEvaluator.EvaluateOnTable<bool>(where.Condition, _query.From, record), _ => true);

            return wherePassed.IsSome && wherePassed.AsT0;
        }
    }
}