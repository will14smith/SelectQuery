using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectParser.Queries;
using SelectQuery.Results;

namespace SelectQuery.Workers
{
    public interface IUnderlyingExecutor
    {
        Task<IReadOnlyList<ResultRow>> ExecuteAsync(Query query, Uri dataLocation);
    }
}
