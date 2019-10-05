using System.Threading.Tasks;
using SelectQuery.Distribution;
using SelectQuery.Lambda.Implementations;
using SelectQuery.Results;

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

        public Task<Result> Handler(DistributorInput input)
        {
            var result = _distributor.Query(input);

            return Task.FromResult(result);
        }
    }
}