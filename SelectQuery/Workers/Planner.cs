using System;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Workers
{
    internal class Planner
    {
        public Plan Plan(Query input)
        {
            SelectClause underlyingSelect = null;
            Option<LimitClause> underlyingLimit = new None();

            Option<OrderClause> order = new None();
            Option<SelectClause> select = new None();
            Option<LimitClause> limit = new None();

            if (input.Order.IsSome)
            {
                throw new NotImplementedException();
            }

            if (input.Limit.IsSome)
            {
                var limitValue = input.Limit.AsT0;
                var offsetValue = limitValue.Offset.Match(x => x, _ => 0);

                limit = limitValue;
                underlyingLimit = new LimitClause(limitValue.Limit + offsetValue, new None());
            }

            if (underlyingSelect == null)
            {
                underlyingSelect = input.Select;
            }

            var underlying = new Query(underlyingSelect, input.From, input.Where, new None(), underlyingLimit);
            
            return new Plan(input, underlying, order, select, limit);
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
