using System.Collections.Generic;
using OneOf;

namespace SelectParser.Queries
{
    [GenerateOneOf]
    public partial class SelectClause : OneOfBase<SelectClause.Star, SelectClause.List>
    {
        public class Star
        {
            public override string ToString()
            {
                return "SELECT *";
            }
        }

        public class List
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