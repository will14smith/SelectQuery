using System;
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
            ValidateQuery(query);

            _query = query;
            _recordWriter = new JsonRecordWriter(query.From, query.Select);
        }

        public byte[] Run(byte[] file)
        {

            var reader = new JsonReader(file);
            var writer = new JsonWriter();

            while (ProcessRecord(ref reader, ref writer)) { }

            return writer.ToUtf8ByteArray();
        }

        private static void ValidateQuery(Query query)
        {
            if (!string.Equals(query.From.Table, "S3Object", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException("complex table targets are not currently supported");
            }

            if (query.Order.IsSome)
            {
                throw new NotImplementedException("ordering is not currently supported");
            }

            // TODO validate query semantics?
        }

        private bool ProcessRecord(ref JsonReader reader, ref JsonWriter jsonWriter)
        {
            reader.SkipWhiteSpace();
            if (reader.GetCurrentOffsetUnsafe() >= reader.GetBufferUnsafe().Length)
            {
                return false;
            }

            var obj = Formatter.Deserialize(ref reader, StandardResolver.Default);

            var wherePassed = _query.Where.Match(where => ExpressionEvaluator.Evaluate<bool>(where.Condition, obj), _ => true);
            if (!wherePassed)
            {
                return false;
            }

            _recordWriter.Write(ref jsonWriter, obj);

            return true;
        }
    }
}