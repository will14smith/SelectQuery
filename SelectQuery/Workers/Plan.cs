using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Workers
{
    internal class Plan
    {
        public Plan(Query inputQuery, Query underlyingQuery, Option<OrderClause> order, Option<LimitClause> limit)
        {
            InputQuery = inputQuery;
            UnderlyingQuery = underlyingQuery;
            Order = order;
            Limit = limit;
        }

        // input
        public Query InputQuery { get; }

        // underlying
        public Query UnderlyingQuery { get; }

        // worker
        public Option<OrderClause> Order { get; }
        public Option<LimitClause> Limit { get; }
    }
}