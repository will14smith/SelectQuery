using System.Collections.Generic;
using System.Linq;

namespace SelectParser.Queries;

public class OrderClause
{
    public OrderClause(IReadOnlyList<(Expression Expression, OrderDirection Direction)> columns)
    {
        Columns = columns;
    }

    public IReadOnlyList<(Expression Expression, OrderDirection Direction)> Columns { get; }

    public override string ToString()
    {
        var columns = Columns.Select(x => $"{x.Expression}{(x.Direction == OrderDirection.Descending ? " DESC" : "")}");
        return $"ORDER BY {string.Join(", ", columns)}";
    }
}