using System;
using System.Collections.Generic;
using SelectParser.Queries;
using SelectQuery.Results;

namespace SelectQuery.Workers
{
    public interface IUnderlyingExecutor
    {
        IAsyncEnumerable<ResultRow> ExecuteAsync(Query query, Uri dataLocation);
    }
}
