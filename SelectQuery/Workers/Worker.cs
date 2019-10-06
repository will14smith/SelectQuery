using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Workers
{
    public class Worker
    {
        private readonly IUnderlyingExecutor _underlying;
        private readonly IResultsStorer _resultsStorer;

        public Worker(IUnderlyingExecutor underlying, IResultsStorer resultsStorer)
        {
            _underlying = underlying;
            _resultsStorer = resultsStorer;
        }

        public async Task<Result> QueryAsync(WorkerInput input)
        {
            var plan = input.Plan;

            var underlyingResults = _underlying.ExecuteAsync(plan.UnderlyingQuery, input.DataLocation);
            var orderedResults = ResultProcessor.Order(plan.Order, underlyingResults);
            var limitedResults = ResultProcessor.Limit(plan.Limit, orderedResults);
            // TODO do we need any worker side projection?
            var results = limitedResults;

            return await _resultsStorer.StoreAsync(results);
        }
    }
}
