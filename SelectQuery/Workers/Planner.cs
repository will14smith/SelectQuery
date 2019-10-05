using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Workers
{
    internal class Planner
    {
        public Plan Plan(Query input)
        {
            return new Plan(input, input, new None(), new None(), new None());
        }
    }

    internal class Plan
    {
        public Plan(Query inputQuery, Query underlyingQuery, Option<OrderClause> order, Option<SelectClause> projection, Option<LimitClause> limit)
        {
            InputQuery = inputQuery;
            UnderlyingQuery = underlyingQuery;
            Order = order;
            Projection = projection;
            Limit = limit;
        }

        // input
        public Query InputQuery { get; }

        // underlying
        public Query UnderlyingQuery { get; }

        // worker
        public Option<OrderClause> Order { get; }
        public Option<SelectClause> Projection { get; }
        public Option<LimitClause> Limit { get; }
    }
}
