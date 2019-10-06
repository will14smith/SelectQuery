using System;
using System.Collections.Generic;
using SelectQuery.Results;

namespace SelectQuery.Distribution
{
    public interface IWorkerExecutor
    {
        IAsyncEnumerable<Result> ExecuteAsync(DistributorPlan plan, IReadOnlyList<Uri> sources);
    }
}