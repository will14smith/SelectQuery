namespace SelectParser.Queries;

public class LimitClause
{
    public LimitClause(int limit)
    {
        Limit = limit;
    }

    public int Limit { get; }
    // TODO add key/sort based offsetting


    public override string ToString()
    {
        return $"LIMIT {Limit}";
    }
}