namespace SelectParser.Queries
{
    public class Query
    {
        public Query(SelectClause select, FromClause from, Option<WhereClause> where, Option<OrderClause> order, Option<LimitClause> limit)
        {
            Select = select;
            From = from;
            Where = where;
            Order = order;
            Limit = limit;
        }

        public SelectClause Select { get; }
        public FromClause From { get; }
        public Option<WhereClause> Where { get; }
        public Option<OrderClause> Order { get; }
        public Option<LimitClause> Limit { get; }
    }
}
