namespace SelectParser.Queries;

public class WhereClause
{
    public WhereClause(Expression condition)
    {
        Condition = condition;
    }

    public Expression Condition { get; }

    public override string ToString()
    {
        return $"WHERE {Condition}";
    }
}