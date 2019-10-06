using System.Text;

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

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Select);
            sb.Append(" ");
            sb.Append(From);

            if (Where.IsSome)
            {
                sb.Append(" ");
                sb.Append(Where.AsT0);
            }
            if (Order.IsSome)
            {
                sb.Append(" ");
                sb.Append(Order.AsT0);
            }
            if (Limit.IsSome)
            {
                sb.Append(" ");
                sb.Append(Limit.AsT0);
            }

            return sb.ToString();
        }
    }
}
