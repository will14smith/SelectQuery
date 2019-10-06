using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.S3;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Distribution;
using SelectQuery.Lambda.Implementations;
using SelectQuery.Lambda.Inputs;
using SelectQuery.Lambda.Outputs;
using SelectQuery.Results;
using Superpower;

namespace SelectQuery.Lambda
{
    public class DistributorEntryPoint
    {
        private readonly Distributor _distributor;

        public DistributorEntryPoint()
        {
            var resolver = CreateResolver();
            var executor = CreateExecutor();
            var storage = CreateStorage();

            _distributor = new Distributor(resolver, executor, storage, storage);
        }

        public DistributorEntryPoint(ISourceResolver resolver, IWorkerExecutor executor, IResultsFetcher resultsFetcher, IResultsStorer resultsStorer)
        {
            _distributor = new Distributor(resolver, executor, resultsFetcher, resultsStorer);
        }

        private static S3SourceResolver CreateResolver()
        {
            var s3 = new AmazonS3Client();

            return new S3SourceResolver(s3);
        }

        private static LambdaWorkerExecutor CreateExecutor()
        {
            var lambda = new AmazonLambdaClient();
            var functionName = Environment.GetEnvironmentVariable("WORKER_FUNCTION_NAME");

            return new LambdaWorkerExecutor(lambda, functionName);
        }
        private static S3ResultStorage CreateStorage()
        {
            var s3 = new AmazonS3Client();
            var resultBucket = Environment.GetEnvironmentVariable("RESULT_BUCKET_NAME");

            return new S3ResultStorage(s3, resultBucket);
        }

        public async Task<Stream> Handler(DistributorPublicInput input)
        {
            var queryInput = ConvertInput(input);

            var result = await _distributor.QueryAsync(queryInput);

            return PublicResult.Serialize(result);
        }

        private static DistributorInput ConvertInput(DistributorPublicInput input)
        {
            var query = ParseQuery(input.Query);
            var source = input.DataSource.Prefix == null
                ? (DataSource)new DataSource.List(input.DataSource.Locations ?? new List<Uri>())
                : new DataSource.Prefix(input.DataSource.Prefix);

            return new DistributorInput(query, source);
        }

        private static Query ParseQuery(string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return Parser.Query.Parse(tokens);
        }
    }
}