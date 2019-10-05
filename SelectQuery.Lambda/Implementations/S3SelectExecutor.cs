using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectParser.Queries;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3SelectExecutor : IUnderlyingExecutor
    {
        public Task<IReadOnlyList<ResultRow>> ExecuteAsync(Query query, Uri dataLocation)
        {
            throw new NotImplementedException();
        }
    }
}
