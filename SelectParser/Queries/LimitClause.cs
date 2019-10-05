namespace SelectParser.Queries
{
    public class LimitClause
    {
        public LimitClause(int limit, Option<int> offset)
        {
            Limit = limit;
            Offset = offset;
        }

        public int Limit { get; }
        public Option<int> Offset { get; }
    }
}