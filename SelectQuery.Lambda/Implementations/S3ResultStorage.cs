using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
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

        public Task<IReadOnlyList<ResultRow>> FetchAsync(Result result)
        {
            return result.Match(
                direct => Task.FromResult(direct.Rows),
                serialized => Task.FromResult(Deserialize(serialized.Data)),
                indirect => FetchAsync(indirect.Location)
            );
        }

        private static IReadOnlyList<ResultRow> Deserialize(byte[] serializedData)
        {
            using var stream = new MemoryStream(serializedData);

            return Deserialize(stream);
        }
        private static IReadOnlyList<ResultRow> Deserialize(Stream data)
        {
            using var gzip = new GZipStream(data, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);

            var results = new List<ResultRow>();

            while (true)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) break;

                var fields = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);

                results.Add(new ResultRow(fields));
            }

            return results;
        }

        private async Task<IReadOnlyList<ResultRow>> FetchAsync(Uri indirectLocation)
        {
            if (indirectLocation.Scheme != "s3") throw new ArgumentException("IndirectLocation is not S3", nameof(indirectLocation));

            var bucketName = indirectLocation.Host;
            var objectKey = indirectLocation.AbsolutePath.TrimStart('/');

            using var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            });
            using var responseStream = response.ResponseStream;
            
            return Deserialize(responseStream);
        }

        public async Task<Result> StoreAsync(IReadOnlyList<ResultRow> rows)
        {
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                using var writer = new StreamWriter(gzip, Encoding.UTF8);
                foreach (var row in rows)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(row.Fields));
                }
            }

            if (ms.Length < LambdaPayloadThreshold)
            {
                return new Result.Serialized(ms.ToArray());
            }

            return await StoreAsync(ms, CreateResultLocation());
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
            });

            return new Result.InDirect(dataLocation);
        }

        private Uri CreateResultLocation()
        {
            return new Uri($"s3://{_resultsBucket}/{Guid.NewGuid()}");
        }
    }
}