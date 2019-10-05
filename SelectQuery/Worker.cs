using System;
using System.Collections.Generic;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Inputs;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery
{
    public class Worker
    {
        private readonly Planner _planner = new Planner();

        private readonly IUnderlyingExecutor _underlying;
        private readonly IResultsStorer _resultsStorer;

        public Worker(IUnderlyingExecutor underlying, IResultsStorer resultsStorer)
        {
            _underlying = underlying;
            _resultsStorer = resultsStorer;
        }

        public Result Query(WorkerInput input)
        {
            var plan = _planner.Plan(input.Query);

            var underlyingResults = _underlying.Execute(plan.UnderlyingQuery);
            var orderedResults = OrderResults(plan.Order, underlyingResults);
            var limitedResults = LimitResults(plan.Limit, orderedResults);
            var results = ProjectResults(limitedResults);

            return _resultsStorer.Store(results);
        }
        
        private IReadOnlyList<ResultRow> OrderResults(Option<OrderClause> order, IReadOnlyList<ResultRow> results)
        {
            throw new NotImplementedException();
        }

        private IReadOnlyList<ResultRow> LimitResults(Option<LimitClause> limit, IReadOnlyList<ResultRow> results)
        {
            throw new NotImplementedException();
        }

        private IReadOnlyList<ResultRow> ProjectResults(IReadOnlyList<ResultRow> results)
        {
            throw new NotImplementedException();
        }
    }
}
