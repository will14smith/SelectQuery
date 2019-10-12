using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
            var stream = new MemoryStream(serializedData);

            return Deserialize(stream);
        }
        private static IAsyncEnumerable<ResultRow> Deserialize(Stream data)
        {
            var pipe = new Pipe();

            Task.Run(async () =>
            {
                var writer = pipe.Writer;

                while (true)
                {
                    var buffer = writer.GetMemory(4096);

                    var bytes = await data.ReadAsync(buffer).ConfigureAwait(false);
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
            FastSerializableKeys<string> keys = null;

            while (true)
            {
                var result = await reader.ReadAsync().ConfigureAwait(false);

                var buffer = result.Buffer;
                SequencePosition? position;

                do
                {
                    position = buffer.PositionOf((byte)'\n');
                    if (position == null) continue;

                    if (keys == null)
                    {
                        keys = DeserializeKeys(buffer.Slice(0, position.Value));
                    }
                    else
                    {
                        yield return DeserializeRow(keys, buffer.Slice(0, position.Value));
                    }

                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

                } while (position != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted) break;
            }

            await reader.CompleteAsync().ConfigureAwait(false);
        }

        private static FastSerializableKeys<string> DeserializeKeys(ReadOnlySequence<byte> slice)
        {
            return FastSerializableKeys<string>.Deserialize(slice);
        }

        private static ResultRow DeserializeRow(FastSerializableKeys<string> keys, ReadOnlySequence<byte> line)
        {
            var fields = FastSerializableDictionary<string, object>.Deserialize(keys, line);
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
            using var streamWrapper = new StreamWrapper(ms);

            var first = true;
            await foreach (var row in rows)
            {
                var fields = row.Fields;
                if (fields is FastSerializableDictionary<string, object> fsd)
                {
                    if (first)
                    {
                        fsd.FastKeys.Serialize(streamWrapper);
                        ms.WriteByte((byte)'\n');
                        first = false;
                    }

                    fsd.Serialize(streamWrapper);
                }
                else
                {
                    await JsonSerializer.SerializeAsync(ms, fields).ConfigureAwait(false);
                }

                ms.WriteByte((byte)'\n');
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

    public class StreamWrapper : IBufferWriter<byte>, IDisposable
    {
        private readonly Stream _stream;
        private readonly Queue<(int, byte[])> _buffers = new Queue<(int, byte[])>();

        public StreamWrapper(Stream stream)
        {
            _stream = stream;
        }

        public void Advance(int count)
        {
            while (count > 0)
            {
                var (bufferLength, buffer) = _buffers.Dequeue();
                var bufferCount = bufferLength <= count ? bufferLength : count;

                _stream.Write(buffer.AsSpan(0, bufferCount));
                ArrayPool<byte>.Shared.Return(buffer);

                count -= bufferCount;
            }
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(sizeHint);

            _buffers.Enqueue((sizeHint, buffer));
            return buffer.AsMemory();
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(sizeHint);

            _buffers.Enqueue((sizeHint, buffer));
            return buffer.AsSpan();
        }

        public void Dispose()
        {
            foreach(var (_, buffer) in _buffers)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}