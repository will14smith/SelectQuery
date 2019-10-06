using System;
using System.Collections.Generic;
using System.Linq;
using SelectQuery.Results;

namespace SelectQuery.Tests.Fakes
{
    public class FakeResultsFetcher : IResultsFetcher
    {
        public IAsyncEnumerable<ResultRow> FetchAsync(Result result)
        {
            return result.Match(
                direct => direct.Rows.ToAsyncEnumerable(),
                _ => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException()
            );
        }
    }
}