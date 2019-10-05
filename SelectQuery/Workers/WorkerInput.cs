using System;

namespace SelectQuery.Workers
{
    public class WorkerInput
    {
        public WorkerInput(WorkerPlan plan, Uri dataLocation)
        {
            Plan = plan;
            DataLocation = dataLocation;
        }

        public WorkerPlan Plan { get; }
        public Uri DataLocation { get; }
    }
}
