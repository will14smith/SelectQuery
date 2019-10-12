using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using SelectParser.Queries;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery.Lambda.Implementations
{
    public class S3SelectExecutor : IUnderlyingExecutor
    {
        private readonly IAmazonS3 _s3;
        private readonly InputSerialization _inputSerialization;

        public S3SelectExecutor(IAmazonS3 s3, InputSerialization inputSerialization)
        {
            _s3 = s3;
            _inputSerialization = inputSerialization;
        }

        public async IAsyncEnumerable<ResultRow> ExecuteAsync(Query query, Uri dataLocation)
        {
            if (dataLocation.Scheme != "s3") throw new ArgumentException("DataLocation is not S3", nameof(dataLocation));

            var bucketName = dataLocation.Host;
            var objectKey = dataLocation.AbsolutePath.TrimStart('/');

            using var response = await SelectAsync(query, bucketName, objectKey).ConfigureAwait(false);
            await foreach (var row in ReadResponse(response))
            {
                yield return row;
            }
        }

        private async Task<ISelectObjectContentEventStream> SelectAsync(Query query, string bucketName, string objectKey)
        {
            var request = new SelectObjectContentRequest
            {
                Bucket = bucketName,
                Key = objectKey,

                Expression = query.ToString(),
                ExpressionType = ExpressionType.SQL,

                InputSerialization = _inputSerialization,
                OutputSerialization = new OutputSerialization { JSON = new JSONOutput() },
            };
            var response = await _s3.SelectObjectContentAsync(request).ConfigureAwait(false);

            return response.Payload;
        }

        private static IAsyncEnumerable<ResultRow> ReadResponse(ISelectObjectContentEventStream eventStream)
        {
            var pipe = new Pipe();

            Task.Run(async () =>
            {
                var writer = pipe.Writer;

                foreach (var ev in eventStream)
                {
                    if (!(ev is RecordsEvent records)) continue;

                    while (true)
                    {
                        var buffer = writer.GetMemory(4096);

                        var bytesRead = await records.Payload.ReadAsync(buffer).ConfigureAwait(false);
                        if (bytesRead == 0) break;

                        writer.Advance(bytesRead);

                        var flush = await writer.FlushAsync().ConfigureAwait(false);
                        if (flush.IsCompleted) break;
                    }
                }

                await writer.CompleteAsync().ConfigureAwait(false);
            });

            return Reader(pipe.Reader);
        }

        public static async IAsyncEnumerable<ResultRow> Reader(PipeReader reader)
        {
            var keys = new FastSerializableKeys<string>();

            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);

                var buffer = result.Buffer;
                SequencePosition? position;

                do
                {
                    position = buffer.PositionOf((byte)'\n');
                    if (position == null) continue;

                    var row = DeserializeRow(keys, buffer.Slice(0, position.Value));
                    yield return row;

                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

                } while (position != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted) break;
            }

            await reader.CompleteAsync().ConfigureAwait(false);
        }

        private static ResultRow DeserializeRow(FastSerializableKeys<string> keys, ReadOnlySequence<byte> line)
        {
            var fields = new FastSerializableDictionary<string, object>(keys);
            var reader = new Utf8JsonReader(line);

            var foundStart = false;
            while (reader.Read())
            {
                if (!foundStart)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        throw new FormatException();
                    }
                    foundStart = true;
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (reader.BytesConsumed != line.Length)
                    {
                        throw new FormatException();
                    }
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var key = reader.GetString();
                if (!reader.Read())
                {
                    throw new FormatException();
                }

                fields[key] = reader.FastReadObject<object>();
            }

            return new ResultRow(fields);
        }
    }
}
