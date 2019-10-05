using System;
using System.Collections.Generic;
using OneOf;

namespace SelectQuery.Distribution
{
    public abstract class DataSource : OneOfBase<DataSource.List, DataSource.Prefix>
    {
        public class List
        {
            public List(IReadOnlyCollection<Uri> locations)
            {
                Locations = locations;
            }

            public IReadOnlyCollection<Uri> Locations { get; }
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