using System.Collections.Generic;
using SelectQuery.Results;

namespace SelectQuery.Tests.Fakes
{
    public class FakeResultsStorer : IResultsStorer
    {
        public Result Store(IReadOnlyList<ResultRow> rows)
        {
            return new Result.Direct(rows);
        }
    }
}
