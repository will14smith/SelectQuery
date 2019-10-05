using System;
using System.Collections.Generic;
using SelectQuery.Results;

namespace SelectQuery.Tests.Fakes
{
    public class FakeResultsFetcher : IResultsFetcher
    {
        public IReadOnlyList<ResultRow> Fetch(Result result)
        {
            return result.Match(
                direct => direct.Rows,
                _ => throw new InvalidOperationException()
            );
        }
    }
}