using System;
using System.Collections.Generic;
using SelectParser.Queries;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3SelectExecutor : IUnderlyingExecutor
    {
        public IReadOnlyList<ResultRow> Execute(Query query, Uri dataLocation)
        {
            throw new NotImplementedException();
        }
    }
}
