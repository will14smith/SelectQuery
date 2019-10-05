using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Workers
{
    public class WorkerPlan
    {
        public WorkerPlan(Query underlyingQuery, Option<OrderClause> order, Option<LimitClause> limit)
        {
            UnderlyingQuery = underlyingQuery;
            Order = order;
            Limit = limit;
        }

        // underlying
        public Query UnderlyingQuery { get; }

        // worker
        public Option<OrderClause> Order { get; }
        public Option<LimitClause> Limit { get; }
    }
}