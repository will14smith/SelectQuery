using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Tests.Fakes
{
    public class FakeResultsStorer : IResultsStorer
    {
        public async Task<Result> StoreAsync(IAsyncEnumerable<ResultRow> rows)
        {
            return new Result.Direct(await rows.ToListAsync());
        }
    }
}
