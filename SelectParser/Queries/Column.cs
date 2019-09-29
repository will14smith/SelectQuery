using OneOf.Types;

namespace SelectParser.Queries
{
    public class Column
    {
        public Column(Expression expression)
        {
            Expression = expression;
            Alias = new None();
        }
        public Column(Expression expression, string alias)
        {
            Expression = expression;
            Alias = alias;
        }

        public Expression Expression { get; }
        public Option<string> Alias { get; }
    }
}