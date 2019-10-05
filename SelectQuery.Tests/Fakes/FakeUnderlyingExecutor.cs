using System;
using System.Collections.Generic;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Results;
using SelectQuery.Workers;
using Xunit;

namespace SelectQuery.Tests.Fakes
{
    public class FakeUnderlyingExecutor : IUnderlyingExecutor
    {
        public Option<Query> ExpectedQuery { get; set; } = new None();
        public Option<Uri> ExpectedDataLocation { get; set; } = new None();

        private readonly IReadOnlyList<ResultRow> _results;

        public FakeUnderlyingExecutor(IReadOnlyList<ResultRow> results)
        {
            _results = results;
        }

        public IReadOnlyList<ResultRow> Execute(Query query, Uri dataLocation)
        {
            if (ExpectedQuery.IsSome)
            {
                QueryTestHelpers.AssertEqual(ExpectedQuery.AsT0, query);
            }
            if (ExpectedDataLocation.IsSome)
            {
                Assert.Equal(ExpectedDataLocation.AsT0, dataLocation);
            }

            return _results;
        }
    }
}
