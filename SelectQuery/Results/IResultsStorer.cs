using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelectQuery.Results
{
    public interface IResultsStorer
    {
        Task<Result> StoreAsync(IReadOnlyList<ResultRow> rows);
    }
}
