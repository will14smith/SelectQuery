using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            return new S3SourceResolver();
        }

        private static LambdaWorkerExecutor CreateExecutor()
        {
            return new LambdaWorkerExecutor();
        }
        private static S3ResultStorage CreateStorage()
        {
            return new S3ResultStorage();
        }

        public async Task<PublicResult> Handler(DistributorPublicInput input)
        {
            var queryInput = ConvertInput(input);

            var result =await  _distributor.QueryAsync(queryInput);

            return ConvertOutput(result);
        }

        private static DistributorInput ConvertInput(DistributorPublicInput input)
        {
            var query = ParseQuery(input.Query);
            var source = input.DataSource.Prefix == null 
                ? (DataSource) new DataSource.List(input.DataSource.Locations ?? new List<Uri>()) 
                : new DataSource.Prefix(input.DataSource.Prefix);

            return new DistributorInput(query, source);
        }

        private static PublicResult ConvertOutput(Result result)
        {
            return result.Match(
                direct => new PublicResult { Rows = direct.Rows.Select(x => x.Fields).ToList() },
                indirect => new PublicResult { Location = indirect.Location }
            );
        }

        private static Query ParseQuery(string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return Parser.Query.Parse(tokens);
        }
    }
}