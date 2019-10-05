using System.Collections.Generic;

namespace SelectQuery.Results
{
    public interface IResultFetcher
    {
        IReadOnlyList<ResultRow> Fetch(Result result);
    }
}
