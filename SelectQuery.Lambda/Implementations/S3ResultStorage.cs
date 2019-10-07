using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Implementations
{
    public class S3ResultStorage : IResultsFetcher, IResultsStorer
    {
        private const int KB = 1024;
        private const int MB = 1024 * KB;
        private const int LambdaPayloadThreshold = 5 * MB;

        private readonly IAmazonS3 _s3;
        private readonly string _resultsBucket;

        public S3ResultStorage(IAmazonS3 s3, string resultsBucket)
        {
            _s3 = s3;
            _resultsBucket = resultsBucket;
        }

        public IAsyncEnumerable<ResultRow> FetchAsync(Result result)
        {
            return result.Match(
                direct => direct.Rows.ToAsyncEnumerable(),
                serialized => Deserialize(serialized.Data),
                indirect => FetchAsync(indirect.Location)
            );
        }

        private static IAsyncEnumerable<ResultRow> Deserialize(byte[] serializedData)
        {
            using var stream = new MemoryStream(serializedData);

            return Deserialize(stream);
        }
        private static IAsyncEnumerable<ResultRow> Deserialize(Stream data)
        {
            var pipe = new Pipe();

            Task.Run(async () =>
            {
                var writer = pipe.Writer;

                using var gzip = new GZipStream(data, CompressionMode.Decompress);

                while (true)
                {
                    var buffer = writer.GetMemory(4096);

                    var bytes = await gzip.ReadAsync(buffer).ConfigureAwait(false);
                    if (bytes == 0) break;

                    writer.Advance(bytes);

                    var flush = await writer.FlushAsync().ConfigureAwait(false);
                    if (flush.IsCompleted) break;
                }

                await writer.CompleteAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
            
            return Reader(pipe.Reader);
        }

        private static async IAsyncEnumerable<ResultRow> Reader(PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);
                
                var buffer = result.Buffer;
                SequencePosition? position;

                do
                {
                    position = buffer.PositionOf((byte)'\n');
                    if (position == null) continue;

                    yield return DeserializeRow(buffer.Slice(0, position.Value));

                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

                } while (position != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted) break;
            }

            await reader.CompleteAsync().ConfigureAwait(false);
        }

        private static ResultRow DeserializeRow(ReadOnlySequence<byte> line)
        {
            var reader = new Utf8JsonReader(line);
            var fields = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader);
            return new ResultRow(fields);
        }

        private async IAsyncEnumerable<ResultRow> FetchAsync(Uri indirectLocation)
        {
            if (indirectLocation.Scheme != "s3") throw new ArgumentException("IndirectLocation is not S3", nameof(indirectLocation));

            var bucketName = indirectLocation.Host;
            var objectKey = indirectLocation.AbsolutePath.TrimStart('/');

            using var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            }).ConfigureAwait(false);
            using var responseStream = response.ResponseStream;

            await foreach (var row in Deserialize(responseStream))
            {
                yield return row;
            }
        }

        public async Task<Result> StoreAsync(IAsyncEnumerable<ResultRow> rows)
        {
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                await foreach (var row in rows)
                {
                    gzip.Write(JsonSerializer.SerializeToUtf8Bytes(row.Fields));
                    gzip.WriteByte((byte)'\n');
                }
            }

            if (ms.Length < LambdaPayloadThreshold)
            {
                return new Result.Serialized(ms.ToArray());
            }

            return await StoreAsync(ms, CreateResultLocation()).ConfigureAwait(false);
        }

        private async Task<Result> StoreAsync(Stream data, Uri dataLocation)
        {
            if (dataLocation.Scheme != "s3") throw new ArgumentException("DataLocation is not S3", nameof(dataLocation));

            var bucketName = dataLocation.Host;
            var objectKey = dataLocation.AbsolutePath.TrimStart('/');

            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,

                InputStream = data
            }).ConfigureAwait(false);

            return new Result.InDirect(dataLocation);
        }

        private Uri CreateResultLocation()
        {
            return new Uri($"s3://{_resultsBucket}/{Guid.NewGuid()}");
        }
    }
}