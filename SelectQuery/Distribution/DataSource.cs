using System;
using System.Collections.Generic;
using OneOf;

namespace SelectQuery.Distribution
{
    [GenerateOneOf]
    public partial class DataSource : OneOfBase<DataSource.List, DataSource.Prefix>
    {
        public class List
        {
            public List(IReadOnlyList<Uri> locations)
            {
                Locations = locations;
            }

            public IReadOnlyList<Uri> Locations { get; }
        }

        public class Prefix
        {
            public Prefix(Uri baseLocation)
            {
                BaseLocation = baseLocation;
            }

            public Uri BaseLocation { get; }
        }
    }
}