using System;
using System.Collections.Generic;

namespace SelectQuery.Lambda.Outputs
{
    public class PublicResult
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Rows { get; set; }
        public Uri Location { get; set; }
    }
}
