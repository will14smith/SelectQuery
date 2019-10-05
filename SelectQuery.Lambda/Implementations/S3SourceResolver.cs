using System;
using System.Collections.Generic;
using SelectQuery.Distribution;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3SourceResolver : ISourceResolver
    {
        public IReadOnlyList<Uri> Resolve(DataSource source)
        {
            throw new NotImplementedException();
        }
    }
}
