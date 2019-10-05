using System;
using System.Collections.Generic;
using SelectQuery.Distribution;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Implementations
{
    internal class LambdaWorkerExecutor : IWorkerExecutor
    {
        public IReadOnlyCollection<Result> Execute(DistributorPlan plan, IReadOnlyList<Uri> sources)
        {
            throw new NotImplementedException();
        }
    }
}