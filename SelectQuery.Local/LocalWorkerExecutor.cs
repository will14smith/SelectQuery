using System;
using System.Collections.Generic;
using System.Linq;
using SelectQuery.Distribution;
using SelectQuery.Results;
using SelectQuery.Workers;

namespace SelectQuery.Local
{
    public class LocalWorkerExecutor : IWorkerExecutor
    {
        private readonly Worker _worker;

        public LocalWorkerExecutor(Worker worker)
        {
            _worker = worker;
        }

        public IAsyncEnumerable<Result> ExecuteAsync(WorkerPlan plan, IReadOnlyList<Uri> sources)
        {
            return sources.ToAsyncEnumerable().SelectAwait(async source => await _worker.QueryAsync(new WorkerInput(plan, source)).ConfigureAwait(false));
        }
    }
}