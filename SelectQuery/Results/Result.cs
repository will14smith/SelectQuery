using System;
using System.Collections.Generic;
using OneOf;

namespace SelectQuery.Results
{
    [GenerateOneOf]
    public partial class Result : OneOfBase<Result.Direct, Result.Serialized, Result.InDirect>
    {
        public class Direct
        {
            public Direct(IReadOnlyList<ResultRow> rows)
            {
                Rows = rows;
            }

            public IReadOnlyList<ResultRow> Rows { get; }
        }

        public class Serialized
        {
            public Serialized(byte[] data)
            {
                Data = data;
            }

            public byte[] Data { get; }
        }

        public class InDirect
        {
            public InDirect(Uri location)
            {
                Location = location;
            }

            public Uri Location { get; }
        }
    }
}
