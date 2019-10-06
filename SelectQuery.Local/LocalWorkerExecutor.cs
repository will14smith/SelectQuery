using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task<IReadOnlyCollection<Result>> ExecuteAsync(DistributorPlan plan, IReadOnlyList<Uri> sources)
        {
            var tasks = new Task<Result>[sources.Count];

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];

                tasks[i] = _worker.QueryAsync(new WorkerInput(plan.WorkerPlan, source));
            }

            return await Task.WhenAll(tasks);
        }
    }
}