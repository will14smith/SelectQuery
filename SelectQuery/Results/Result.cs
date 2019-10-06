using System;
using System.Collections.Generic;
using OneOf;

namespace SelectQuery.Results
{
    public abstract class Result : OneOfBase<Result.Direct, Result.Serialized, Result.InDirect>
    {
        public class Direct : Result
        {
            public Direct(IReadOnlyList<ResultRow> rows)
            {
                Rows = rows;
            }

            public IReadOnlyList<ResultRow> Rows { get; }
        }

        public class Serialized : Result
        {
            public Serialized(byte[] data)
            {
                Data = data;
            }

            public byte[] Data { get; }
        }

        public class InDirect : Result
        {
            public InDirect(Uri location)
            {
                Location = location;
            }

            public Uri Location { get; }
        }
    }
}
