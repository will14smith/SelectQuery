using System.Collections.Generic;

namespace SelectQuery.Results
{
    public class ResultRow
    {
        public ResultRow(IReadOnlyDictionary<string, object> fields)
        {
            Fields = fields;
        }

        public IReadOnlyDictionary<string, object> Fields { get; }
    }
}