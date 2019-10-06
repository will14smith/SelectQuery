using System.Collections.Generic;

namespace SelectQuery.Results
{
    public interface IResultsFetcher
    {
        IAsyncEnumerable<ResultRow> FetchAsync(Result result);
    }
}
