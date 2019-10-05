using System.Collections.Generic;

namespace SelectQuery.Results
{
    public interface IResultsFetcher
    {
        IReadOnlyList<ResultRow> Fetch(Result result);
    }
}
