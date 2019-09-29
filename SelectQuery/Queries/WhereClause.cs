namespace SelectQuery.Queries
{
    public class WhereClause
    {
        public WhereClause(Expression condition)
        {
            Condition = condition;
        }

        public Expression Condition { get; }
    }
}