using System;

namespace SelectQuery.Lambda.Inputs
{
    public class WorkerPublicInput
    {
        public string UnderlyingQuery { get; set; }
        public string Order { get; set; }
        public string Limit { get; set; }

        public Uri DataLocation { get; set; }

    }
}
