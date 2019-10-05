using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Distribution
{
    public class Distributor
    {
        private readonly Planner _planner = new Planner();

        private readonly ISourceResolver _sourceResolver;
        private readonly IWorkerExecutor _workerExecutor;
        private readonly IResultsFetcher _resultsFetcher;
        private readonly IResultsStorer _resultsStorer;

        public Distributor(ISourceResolver sourceResolver, IWorkerExecutor workerExecutor, IResultsFetcher resultsFetcher, IResultsStorer resultsStorer)
        {
            _sourceResolver = sourceResolver;
            _workerExecutor = workerExecutor;
            _resultsFetcher = resultsFetcher;
            _resultsStorer = resultsStorer;
        }
        
        public async Task<Result> QueryAsync(DistributorInput input)
        {
            var plan = _planner.Plan(input.Query);

            var sources = await _sourceResolver.ResolveAsync(input.Source);
            var workerResultSets = await _workerExecutor.ExecuteAsync(plan, sources);

            // TODO ordering+limit could be more efficient using the result sets directly (it might also be required for performance)
            var workerFetchedResultSets = await Task.WhenAll(workerResultSets.Select(x => _resultsFetcher.FetchAsync(x)));
            var workerResults = workerFetchedResultSets.SelectMany(x => x);

            var orderedResults = ResultProcessor.Order(plan.Order, workerResults);
            var limitedResults = ResultProcessor.Limit(plan.Limit, orderedResults);
            var results = ProjectResults(limitedResults);

            return await _resultsStorer.StoreAsync(results);
        }

        private IReadOnlyList<ResultRow> ProjectResults(IEnumerable<ResultRow> results)
        {
            return results.Select(row =>
            {
                // TODO skip this if the plan didn't have any
                var nonInternalFields = row.Fields.Where(field => !field.Key.StartsWith("__internal__"));
                return new ResultRow(nonInternalFields.ToDictionary());
            }).ToList();
        }
    }
}
