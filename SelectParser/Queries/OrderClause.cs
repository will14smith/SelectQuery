using System.Collections.Generic;

namespace SelectParser.Queries
{
    public class OrderClause
    {
        public OrderClause(IReadOnlyList<(Expression Express, OrderDirection Direction)> columns)
        {
            Columns = columns;
        }

        public IReadOnlyList<(Expression Express, OrderDirection Direction)> Columns { get; }
    }
}