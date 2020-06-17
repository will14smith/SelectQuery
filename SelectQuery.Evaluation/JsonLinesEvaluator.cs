using System;
using System.Linq;
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

            if (query.Select.IsT1 && query.Select.AsT1.Columns.Any(x => IsQualifiedStar(x.Expression)))
            {
                throw new NotImplementedException("qualified star projections not currently supported");
            }

            // TODO validate query semantics?
        }

        private static bool IsQualifiedStar(Expression expr)
        {
            while (true)
            {
                if (expr.IsT3) return expr.AsT3.Name == "*";
                if (!expr.IsT4) return false;
                expr = expr.AsT4.Expression;
            }
        }

        private bool ProcessRecord(ref JsonReader reader, ref JsonWriter writer)
        {
            reader.SkipWhiteSpace();
            if (reader.GetCurrentOffsetUnsafe() >= reader.GetBufferUnsafe().Length)
            {
                return false;
            }

            var obj = Formatter.Deserialize(ref reader, StandardResolver.Default);

            var wherePassed = _query.Where.Match(where => ExpressionEvaluator.EvaluateOnTable<bool>(where.Condition, _query.From, obj), _ => true);
            if (wherePassed)
            {
                _recordWriter.Write(ref writer, obj);
                writer.WriteRaw((byte) '\n');
            }

            return true;
        }
    }
}