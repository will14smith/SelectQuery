using System;
using System.Collections.Generic;
using SelectQuery.Results;

namespace SelectQuery.Distribution
{
    public interface IWorkerExecutor
    {
        IReadOnlyCollection<Result> Execute(DistributorPlan plan, IReadOnlyList<Uri> sources);
    }
}