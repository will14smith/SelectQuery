using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
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

        public async Task<IReadOnlyList<ResultRow>> ExecuteAsync(Query query, Uri dataLocation)
        {
            if (dataLocation.Scheme != "s3") throw new ArgumentException("DataLocation is not S3", nameof(dataLocation));

            var bucketName = dataLocation.Host;
            var objectKey = dataLocation.AbsolutePath.TrimStart('/');

            using var response = await SelectAsync(query, bucketName, objectKey);
            return ReadResponse(response);
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
                OutputSerialization = new OutputSerialization {JSON = new JSONOutput()},
            };
            var response = await _s3.SelectObjectContentAsync(request);

            return response.Payload;
        }

        private static IReadOnlyList<ResultRow> ReadResponse(ISelectObjectContentEventStream eventStream)
        {
            var results = new List<ResultRow>();

            var buffer = ReadOnlySpan<char>.Empty;

            foreach (var ev in eventStream)
            {
                if (!(ev is RecordsEvent records)) continue;

                using var reader = new StreamReader(records.Payload, Encoding.UTF8);
                buffer = CombineBuffers(buffer, reader.ReadToEnd());

                ParsePartialBuffer(ref buffer, results);
            }

            if (!buffer.IsEmpty)
            {
                throw new InvalidOperationException("Buffer was not empty after all data was received");
            }

            return results;
        }

        private static ReadOnlySpan<char> CombineBuffers(ReadOnlySpan<char> buffer, string newBuffer)
        {
            return buffer.IsEmpty 
                ? newBuffer.AsSpan() 
                : (buffer.ToString() + newBuffer).AsSpan();
        }

        private static void ParsePartialBuffer(ref ReadOnlySpan<char> buffer, ICollection<ResultRow> results)
        {
            while (true)
            {
                var nextOffset = buffer.IndexOf('\n');
                if (nextOffset == -1)
                {
                    break;
                }

                var recordBuffer = new string(buffer.Slice(0, nextOffset));
                var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(recordBuffer);
                results.Add(new ResultRow(record));

                buffer = buffer.Slice(nextOffset + 1);
            }
        }
    }
}
