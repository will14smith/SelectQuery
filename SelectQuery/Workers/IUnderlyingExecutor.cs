using System.Collections.Generic;
using SelectParser.Queries;
using SelectQuery.Results;

namespace SelectQuery.Workers
{
    public interface IUnderlyingExecutor
    {
        IReadOnlyList<ResultRow> Execute(Query query);
    }
}
