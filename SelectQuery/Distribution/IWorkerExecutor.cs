using System;
using System.Collections.Generic;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery.Distribution
{
    public interface IWorkerExecutor
    {
        IAsyncEnumerable<Result> ExecuteAsync(WorkerPlan plan, IReadOnlyList<Uri> sources);
    }
}