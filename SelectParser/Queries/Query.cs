using System.Text;

namespace SelectParser.Queries;

public class Query(
    SelectClause select,
    FromClause from,
    Option<WhereClause> where,
    Option<OrderClause> order,
    Option<LimitClause> limit)
{
    public SelectClause Select { get; } = select;
    public FromClause From { get; } = from;
    public Option<WhereClause> Where { get; } = where;
    public Option<OrderClause> Order { get; } = order;
    public Option<LimitClause> Limit { get; } = limit;

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(Select);
        sb.Append(' ');
        sb.Append(From);

        if (Where.IsSome)
        {
            sb.Append(' ');
            sb.Append(Where.AsT0);
        }
        if (Order.IsSome)
        {
            sb.Append(' ');
            sb.Append(Order.AsT0);
        }
        if (Limit.IsSome)
        {
            sb.Append(' ');
            sb.Append(Limit.AsT0);
        }

        return sb.ToString();
    }
}