using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelectQuery.Results
{
    public interface IResultsFetcher
    {
        Task<IReadOnlyList<ResultRow>> FetchAsync(Result result);
    }
}
