using System.Collections.Generic;
using OneOf;

namespace SelectParser.Queries
{
    public abstract class SelectClause : OneOfBase<SelectClause.Star, SelectClause.List>
    {
        public abstract override string ToString();

        public class Star : SelectClause
        {
            public override string ToString()
            {
                return "SELECT *";
            }
        }

        public class List : SelectClause
        {
            public List(IReadOnlyList<Column> columns)
            {
                Columns = columns;
            }

            public IReadOnlyList<Column> Columns { get; }

            public override string ToString()
            {
                return $"SELECT {string.Join(", ", Columns)}";
            }
        }
    }
}