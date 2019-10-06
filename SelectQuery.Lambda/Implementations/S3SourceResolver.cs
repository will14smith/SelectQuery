using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using SelectQuery.Distribution;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3SourceResolver : ISourceResolver
    {
        private readonly IAmazonS3 _s3;

        public S3SourceResolver(IAmazonS3 s3)
        {
            _s3 = s3;
        }

        public Task<IReadOnlyList<Uri>> ResolveAsync(DataSource source)
        {
            return source.Match(
                list => Task.FromResult(list.Locations),
                prefix => GetObjectsByPrefixAsync(prefix.BaseLocation)
            );
        }

        private async Task<IReadOnlyList<Uri>> GetObjectsByPrefixAsync(Uri prefixUri)
        {
            if (prefixUri.Scheme != "s3") throw new ArgumentException("Prefix URI was not for S3");

            var bucketName = prefixUri.Host;
            var prefix = prefixUri.AbsolutePath.TrimStart('/');

            var objects = new List<Uri>();

            string continuationToken;
            do
            {
                var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    ContinuationToken = null
                });

                objects.AddRange(response.S3Objects.Select(obj => new Uri($"s3://{bucketName}/{obj.Key}")));

                continuationToken = response.NextContinuationToken;
            } while (!string.IsNullOrEmpty(continuationToken));

            return objects;
        }
    }
}
