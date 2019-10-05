using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SelectQuery.Distribution;

namespace SelectQuery.Lambda.Implementations
{
    internal class S3SourceResolver : ISourceResolver
    {
        public Task<IReadOnlyList<Uri>> ResolveAsync(DataSource source)
        {
            throw new NotImplementedException();
        }
    }
}
