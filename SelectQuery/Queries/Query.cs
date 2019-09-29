namespace SelectQuery.Queries
{
    public class Query
    {
        public Query(SelectClause select, FromClause from, Option<WhereClause> where, Option<LimitClause> limit)
        {
            Select = select;
            From = from;
            Where = where;
            Limit = limit;
        }

        public SelectClause Select { get; }
        public FromClause From { get; }
        public Option<WhereClause> Where { get; }
        public Option<LimitClause> Limit { get; }
    }
}
