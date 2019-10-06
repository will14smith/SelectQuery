using OneOf.Types;

namespace SelectParser.Queries
{
    public class LimitClause
    {
        public LimitClause(int limit) : this(limit, new None()) { }
        public LimitClause(int limit, Option<int> offset)
        {
            Limit = limit;
            Offset = offset;
        }

        public int Limit { get; }
        public Option<int> Offset { get; }

        public override string ToString()
        {
            return Offset.IsNone ? $"LIMIT {Limit}" : $"LIMIT {Limit} OFFSET {Offset.AsT0}";
        }
    }
}