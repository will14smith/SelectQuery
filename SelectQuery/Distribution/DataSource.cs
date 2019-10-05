using System;
using System.Collections.Generic;
using OneOf;

namespace SelectQuery.Distribution
{
    public abstract class DataSource : OneOfBase<DataSource.List, DataSource.Prefix>
    {
        public class List : DataSource
        {
            public List(IReadOnlyList<Uri> locations)
            {
                Locations = locations;
            }

            public IReadOnlyList<Uri> Locations { get; }
        }

        public class Prefix : DataSource
        {
            public Prefix(Uri baseLocation)
            {
                BaseLocation = baseLocation;
            }

            public Uri BaseLocation { get; }
        }
    }
}