using System.Collections.Generic;

namespace SelectParser.Queries
{
    public class OrderClause
    {
        public OrderClause(IReadOnlyList<(Expression Expression, OrderDirection Direction)> columns)
        {
            Columns = columns;
        }

        public IReadOnlyList<(Expression Expression, OrderDirection Direction)> Columns { get; }
    }
}