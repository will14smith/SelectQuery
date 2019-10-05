using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectQuery.Distribution;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Implementations
{
    internal class LambdaWorkerExecutor : IWorkerExecutor
    {
        public Task<IReadOnlyCollection<Result>> ExecuteAsync(DistributorPlan plan, IReadOnlyList<Uri> sources)
        {
            throw new NotImplementedException();
        }
    }
}