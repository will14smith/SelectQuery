using OneOf.Types;

namespace SelectParser.Queries
{
    public class FromClause
    {
        public FromClause(string table) : this(table, new None()) { }
        public FromClause(string table, string alias) : this(table, (Option<string>)alias) { }

        public FromClause(string table, Option<string> alias)
        {
            Table = table;
            Alias = alias;
        }

        public string Table { get; }
        public Option<string> Alias { get; }
    }
}