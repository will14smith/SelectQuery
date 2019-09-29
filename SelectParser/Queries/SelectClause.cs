using System.Collections.Generic;
using OneOf;

namespace SelectParser.Queries
{
    public abstract class SelectClause : OneOfBase<SelectClause.Star, SelectClause.List>
    {
        public class Star : SelectClause { }

        public class List : SelectClause
        {
            public List(IReadOnlyCollection<Column> columns)
            {
                Columns = columns;
            }

            public IReadOnlyCollection<Column> Columns { get; }
        }
    }
}