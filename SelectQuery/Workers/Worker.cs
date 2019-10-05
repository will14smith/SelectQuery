using System;
using System.Collections.Generic;
using System.Linq;
using SelectParser;
using SelectParser.Queries;
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

        public Result Query(WorkerInput input)
        {
            var plan = input.Plan;

            var underlyingResults = _underlying.Execute(plan.UnderlyingQuery, input.DataLocation);
            var orderedResults = OrderResults(plan.Order, underlyingResults);
            var limitedResults = LimitResults(plan.Limit, orderedResults);
            var results = ProjectResults(limitedResults);

            return _resultsStorer.Store(results);
        }
        
        private static IEnumerable<ResultRow> OrderResults(Option<OrderClause> orderOpt, IEnumerable<ResultRow> results)
        {
            if (orderOpt.IsNone || orderOpt.AsT0.Columns.Count == 0) return results;
            var columns = orderOpt.AsT0.Columns;

            var firstColumn = columns[0];
            var orderedResults = firstColumn.Direction == OrderDirection.Ascending 
                ? results.OrderBy(x => OrderSelector(firstColumn.Expression, x)) 
                : results.OrderByDescending(x => OrderSelector(firstColumn.Expression, x));

            for (var i = 1; i < columns.Count; i++)
            {
                var (expression, direction) = columns[i];

                orderedResults = direction == OrderDirection.Ascending 
                    ? orderedResults.ThenBy(x => OrderSelector(expression, x)) 
                    : orderedResults.ThenByDescending(x => OrderSelector(expression, x));
            }

            return orderedResults;

            static object OrderSelector(Expression expression, ResultRow row)
            {
                if (!(expression is Expression.Identifier identifier))
                {
                    throw new NotSupportedException("Worker can't execute expressions, this should have been projected for the underlying query to handle");
                }

                return row.Fields.TryGetValue(identifier.Name, out var value) ? value : null;
            }
        }
        
        private static IEnumerable<ResultRow> LimitResults(Option<LimitClause> limitOpt, IEnumerable<ResultRow> results)
        {
            if (limitOpt.IsNone) return results;
            var limit = limitOpt.AsT0;

            var offset = limit.Offset.Match(x => x, _ => 0);

            return results.Skip(offset).Take(limit.Limit);
        }

        private static IReadOnlyList<ResultRow> ProjectResults(IEnumerable<ResultRow> results)
        {
            // TODO do we need any worker side projection?
            return results.ToList();
        }
    }
}
