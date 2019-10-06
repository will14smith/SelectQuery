using System;
using System.Collections.Generic;
using System.Linq;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Results;

namespace SelectQuery
{
    internal class ResultProcessor
    {
        public static IAsyncEnumerable<ResultRow> Order(Option<OrderClause> orderOpt, IAsyncEnumerable<ResultRow> results)
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

        public static IAsyncEnumerable<ResultRow> Limit(Option<LimitClause> limitOpt, IAsyncEnumerable<ResultRow> results)
        {
            if (limitOpt.IsNone) return results;
            var limit = limitOpt.AsT0;

            var offset = limit.Offset.Match(x => x, _ => 0);

            return results.Skip(offset).Take(limit.Limit);
        }
    }
}
