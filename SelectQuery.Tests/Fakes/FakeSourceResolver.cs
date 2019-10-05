using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneOf.Types;
using SelectParser;
using SelectQuery.Distribution;
using Xunit;

namespace SelectQuery.Tests.Fakes
{
    public class FakeSourceResolver : ISourceResolver
    {
        public Option<DataSource> ExpectedDataSource { get; set; } = new None();

        private readonly IReadOnlyList<Uri> _sources;

        public FakeSourceResolver(IReadOnlyList<Uri> sources)
        {
            _sources = sources;
        }

        public Task<IReadOnlyList<Uri>> ResolveAsync(DataSource source)
        {
            if (ExpectedDataSource.IsSome)
            {
                Assert.Equal(ExpectedDataSource.AsT0, source);
            }

            return Task.FromResult(_sources);
        }
    }
}
