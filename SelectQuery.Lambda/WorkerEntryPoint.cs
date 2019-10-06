using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Lambda.Implementations;
using SelectQuery.Lambda.Inputs;
using SelectQuery.Lambda.Outputs;
using SelectQuery.Results;
using SelectQuery.Workers;
using Superpower;

namespace SelectQuery.Lambda
{
    public class WorkerEntryPoint
    {
        private readonly Worker _worker;

        static WorkerEntryPoint()
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        public WorkerEntryPoint()
        {
            var executor = CreateExecutor();
            var storage = CreateStorage();

            _worker = new Worker(executor, storage);
        }

        public WorkerEntryPoint(IUnderlyingExecutor executor, IResultsStorer resultsStorer)
        {
            _worker = new Worker(executor, resultsStorer);
        }

        private static S3SelectExecutor CreateExecutor()
        {
            var s3 = new AmazonS3Client();
            var inputSerialization = new InputSerialization
            {
                JSON = new JSONInput { JsonType = JsonType.Lines },
                CompressionType = CompressionType.Gzip
            };

            return new S3SelectExecutor(s3, inputSerialization);
        }
        private static S3ResultStorage CreateStorage()
        {
            var s3 = new AmazonS3Client();
            var resultBucket = Environment.GetEnvironmentVariable("RESULT_BUCKET_NAME");

            return new S3ResultStorage(s3, resultBucket);
        }

        public async Task<Stream> Handler(WorkerPublicInput input)
        {
            var queryInput = ConvertInput(input);

            var result = await _worker.QueryAsync(queryInput);

            return PublicResult.Serialize(result);
        }

        private static WorkerInput ConvertInput(WorkerPublicInput input)
        {
            var underlyingQuery = Parse(Parser.Query, input.UnderlyingQuery);
            var order = string.IsNullOrEmpty(input.Order)
                ? (Option<OrderClause>)new None()
                : Parse(Parser.OrderByClause, input.Order);
            var limit = string.IsNullOrEmpty(input.Limit)
                ? (Option<LimitClause>)new None()
                : Parse(Parser.LimitClause, input.Limit);

            var plan = new WorkerPlan(underlyingQuery, order, limit);

            return new WorkerInput(plan, input.DataLocation);
        }
        
        private static T Parse<T>(TokenListParser<SelectToken, T> parser, string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return parser.Parse(tokens);
        }
    }
}
