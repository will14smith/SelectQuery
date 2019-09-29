using OneOf.Types;

namespace SelectParser.Queries
{
    public class FromClause
    {
        public FromClause(string table)
        {
            Table = table;
            Alias = new None();
        }
        public FromClause(string table, string alias)
        {
            Table = table;
            Alias = alias;
        }

        public string Table { get; }
        public Option<string> Alias { get; }
    }
}