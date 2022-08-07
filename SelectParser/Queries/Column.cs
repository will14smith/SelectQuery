using OneOf.Types;

namespace SelectParser.Queries;

public class Column
{
    public Column(Expression expression) : this(expression, new None()) { }
    public Column(Expression expression, string alias) : this(expression, (Option<string>)alias) { }

    public Column(Expression expression, Option<string> alias)
    {
        Expression = expression;
        Alias = alias;
    }

    public Expression Expression { get; }
    public Option<string> Alias { get; }

    public override string ToString()
    {
        return Alias.IsSome ? $"{Expression} AS {Alias.AsT0}" : $"{Expression}";
    }
}