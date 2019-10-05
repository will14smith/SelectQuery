using System.Collections.Generic;

namespace SelectQuery.Results
{
    public interface IResultsStorer
    {
        Result Store(IReadOnlyList<ResultRow> rows);
    }
}
