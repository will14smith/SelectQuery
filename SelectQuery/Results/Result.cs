using System;
using System.Collections.Generic;
using OneOf;

namespace SelectQuery.Results
{
    public abstract class Result : OneOfBase<Result.Direct, Result.InDirect>
    {
        public class Direct : Result
        {
            public Direct(IReadOnlyList<ResultRow> rows)
            {
                Rows = rows;
            }

            public IReadOnlyList<ResultRow> Rows { get; }
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
