using System.Threading.Tasks;
using SelectQuery.Lambda.Implementations;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery.Lambda
{
    public class WorkerEntryPoint
    {
        private readonly Worker _worker;

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
            return new S3SelectExecutor();
        }
        private static S3ResultStorage CreateStorage()
        {
            return new S3ResultStorage();
        }

        public Task<Result> Handler(WorkerInput input)
        {
            var result = _worker.Query(input);

            return Task.FromResult(result);
        }
    }
}
