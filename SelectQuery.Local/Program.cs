using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Distribution;
using SelectQuery.Lambda.Implementations;
using SelectQuery.Workers;
using Superpower;

namespace SelectQuery.Local
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var source = new CredentialProfileStoreChain();
            var creds = source.TryGetProfile("personal", out var c) ? AWSCredentialsFactory.GetAWSCredentials(c, source) : null;
            var s3 = new AmazonS3Client(creds, RegionEndpoint.EUWest2); 

            var resultsStorage = new S3ResultStorage(s3, "selectquery-dev-resultsbucket-cmlezfu231g");

            var underlyingExecutor = new S3SelectExecutor(s3, new InputSerialization
            {
                JSON = new JSONInput { JsonType = JsonType.Lines },
                CompressionType = CompressionType.Gzip
            });
            var worker = new Worker(underlyingExecutor, resultsStorage);
            
            var sourceResolver = new S3SourceResolver(s3);
            var workerExecutor = new LocalWorkerExecutor(worker);

            var distributor = new Distributor(sourceResolver, workerExecutor, resultsStorage, resultsStorage);
            
            await distributor.QueryAsync(new DistributorInput(
                ParseQuery("SELECT * FROM s3object s LIMIT 100"),
                new DataSource.List(new[]
                {
                    new Uri("s3://selectquery-data/0ECr0RR7ADAmZ7B6.gz"),
                })
            ));
        }


        private static Query ParseQuery(string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return Parser.Query.Parse(tokens);
        }
    }
}
