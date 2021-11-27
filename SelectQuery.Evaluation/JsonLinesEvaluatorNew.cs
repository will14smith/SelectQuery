using System;
using System.Linq;
using SelectParser.Queries;
using SelectQuery.Evaluation.Slots;
using Utf8Json;

namespace SelectQuery.Evaluation
{
    public class JsonLinesEvaluatorNew
    {
        private readonly SlottedQuery _query;

        public JsonLinesEvaluatorNew(Query query)
        {
            ValidateQuery(query);

            _query = SlottedQueryBuilder.Build(query);
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
            if (expr.IsT3) return expr.AsT3.Name == "*";
            if (expr.IsT4) return expr.AsT4.LastIdentifier.Name == "*";

            return false;
        }

        private bool ProcessRecord(ref JsonReader reader, ref JsonWriter writer)
        {
            reader.SkipWhiteSpace();
            if (reader.GetCurrentOffsetUnsafe() >= reader.GetBufferUnsafe().Length)
            {
                return false;
            }
            
            var eval = SlottedQueryEvaluator.Evaluate(_query, ref reader);
            if (!SlottedQueryEvaluator.EvaluatePredicate(_query, eval))
            {
                return true;
            }
            
            SlottedQueryEvaluator.Write(ref writer, _query, eval);
            writer.WriteRaw((byte) '\n');

            return true;
        }
    }
}