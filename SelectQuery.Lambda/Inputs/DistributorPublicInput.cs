using System;
using System.Collections.Generic;

namespace SelectQuery.Lambda.Inputs
{
    public class DistributorPublicInput
    {
        public string Query { get; set; }
        public DistributorPublicDataSource DataSource { get; set; }
    }

    public class DistributorPublicDataSource
    {
        public List<Uri> Locations { get; set; }
        public Uri Prefix { get; set; }
    }
}
