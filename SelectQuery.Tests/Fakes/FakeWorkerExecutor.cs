using System;
using System.Collections.Generic;
using System.Linq;
using OneOf.Types;
using SelectParser;
using SelectQuery.Distribution;
using SelectQuery.Results;
using SelectQuery.Workers;
using Xunit;

namespace SelectQuery.Tests.Fakes
{
    public class FakeWorkerExecutor : IWorkerExecutor
    {
        public Option<WorkerPlan> ExpectedPlan { get; set; } = new None();
        public Option<IReadOnlyList<Uri>> ExpectedSources { get; set; } = new None();

        private readonly IReadOnlyCollection<Result> _results;

        public FakeWorkerExecutor(IReadOnlyCollection<Result> results)
        {
            _results = results;
        }

        public IAsyncEnumerable<Result> ExecuteAsync(WorkerPlan plan, IReadOnlyList<Uri> sources)
        {
            if (ExpectedPlan.IsSome)
            {
                Assert.Equal(ExpectedPlan.AsT0, plan);
            }
            if (ExpectedSources.IsSome)
            {
                Assert.Equal(ExpectedSources.AsT0, sources);
            }

            return _results.ToAsyncEnumerable();
        }
    }
}