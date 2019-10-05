using System;
using System.Collections.Generic;

namespace SelectQuery.Distribution
{
    public interface ISourceResolver
    {
        IReadOnlyList<Uri> Resolve(DataSource source);
    }
}