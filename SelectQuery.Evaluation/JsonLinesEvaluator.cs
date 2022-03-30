﻿using SelectParser.Queries;
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

            while (ProcessRecord(ref reader, ref writer)) { }

            return writer.ToUtf8ByteArray();
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
            if (wherePassed.IsSome && wherePassed.AsT0)
            {
                _recordWriter.Write(ref writer, obj);
                writer.WriteRaw((byte) '\n');
            }

            return true;
        }
    }
}