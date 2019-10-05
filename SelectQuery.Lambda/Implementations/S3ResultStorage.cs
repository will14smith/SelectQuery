using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3ResultStorage : IResultsFetcher, IResultsStorer
    {
        public Task<IReadOnlyList<ResultRow>> FetchAsync(Result result)
        {
            throw new NotImplementedException();
        }

        public Task<Result> StoreAsync(IReadOnlyList<ResultRow> rows)
        {
            throw new NotImplementedException();
        }
    }
}