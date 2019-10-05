using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectQuery.Results;

namespace SelectQuery.Distribution
{
    public interface IWorkerExecutor
    {
        Task<IReadOnlyCollection<Result>> ExecuteAsync(DistributorPlan plan, IReadOnlyList<Uri> sources);
    }
}