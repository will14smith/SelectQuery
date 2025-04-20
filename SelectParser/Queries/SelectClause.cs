using System.Collections.Generic;
using OneOf;

namespace SelectParser.Queries;

[GenerateOneOf]
public partial class SelectClause : OneOfBase<SelectClause.Star, SelectClause.List>
{
    public override string? ToString() => Value.ToString();

    public class Star
    {
        public override string ToString() => "SELECT *";
    }

    public class List(IReadOnlyList<Column> columns)
    {
        public IReadOnlyList<Column> Columns { get; } = columns;

        public override string ToString() => $"SELECT {string.Join(", ", Columns)}";
    }
}