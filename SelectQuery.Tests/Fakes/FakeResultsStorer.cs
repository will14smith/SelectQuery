using System.Collections.Generic;
using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Tests.Fakes
{
    public class FakeResultsStorer : IResultsStorer
    {
        public Task<Result> StoreAsync(IReadOnlyList<ResultRow> rows)
        {
            return Task.FromResult<Result>(new Result.Direct(rows));
        }
    }
}
