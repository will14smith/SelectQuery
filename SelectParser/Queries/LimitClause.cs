namespace SelectParser.Queries
{
    public class LimitClause
    {
        public LimitClause(int limit)
        {
            Limit = limit;
        }

        public int Limit { get; }
    }
}