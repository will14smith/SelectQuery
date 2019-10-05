using SelectParser;
using SelectParser.Queries;
using SelectQuery.Workers;

namespace SelectQuery.Distribution
{
    public class DistributorPlan
    {
        public DistributorPlan(Query inputQuery, Query underlyingQuery, Option<OrderClause> order, Option<LimitClause> limit)
        {
            InputQuery = inputQuery;
            UnderlyingQuery = underlyingQuery;
            Order = order;
            Limit = limit;
        }

        // input
        public Query InputQuery { get; }

        // worker
        public WorkerPlan WorkerPlan => new WorkerPlan(UnderlyingQuery, Order, Limit);
        public Query UnderlyingQuery { get; }

        // collector
        public Option<OrderClause> Order { get; }
        public Option<LimitClause> Limit { get; }
    }
}
