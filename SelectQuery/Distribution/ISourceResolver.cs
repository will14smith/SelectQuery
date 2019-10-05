using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelectQuery.Distribution
{
    public interface ISourceResolver
    {
        Task<IReadOnlyList<Uri>> ResolveAsync(DataSource source);
    }
}