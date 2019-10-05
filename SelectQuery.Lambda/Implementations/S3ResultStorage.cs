using System;
using System.Collections.Generic;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3ResultStorage : IResultsFetcher, IResultsStorer
    {
        public IReadOnlyList<ResultRow> Fetch(Result result)
        {
            throw new NotImplementedException();
        }

        public Result Store(IReadOnlyList<ResultRow> rows)
        {
            throw new NotImplementedException();
        }
    }
}