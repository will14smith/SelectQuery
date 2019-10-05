using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Tests.Fakes
{
    public class FakeResultsFetcher : IResultsFetcher
    {
        public Task<IReadOnlyList<ResultRow>> FetchAsync(Result result)
        {
            return Task.FromResult(result.Match(
                direct => direct.Rows,
                _ => throw new InvalidOperationException()
            ));
        }
    }
}