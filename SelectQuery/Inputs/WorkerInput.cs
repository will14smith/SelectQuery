using System;
using SelectParser.Queries;

namespace SelectQuery.Inputs
{
    public class WorkerInput
    {
        public WorkerInput(Query query, Uri dataLocation)
        {
            Query = query;
            DataLocation = dataLocation;
        }

        public Query Query { get; }
        public Uri DataLocation { get; }
    }
}
