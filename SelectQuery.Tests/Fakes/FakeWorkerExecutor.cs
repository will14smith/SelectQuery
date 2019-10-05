using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneOf.Types;
using SelectParser;
using SelectQuery.Distribution;
using SelectQuery.Results;
using Xunit;

namespace SelectQuery.Tests.Fakes
{
    public class FakeWorkerExecutor : IWorkerExecutor
    {
        public Option<DistributorPlan> ExpectedPlan { get; set; } = new None();
        public Option<IReadOnlyList<Uri>> ExpectedSources { get; set; } = new None();

        private readonly IReadOnlyCollection<Result> _results;

        public FakeWorkerExecutor(IReadOnlyCollection<Result> results)
        {
            _results = results;
        }

        public Task<IReadOnlyCollection<Result>> ExecuteAsync(DistributorPlan plan, IReadOnlyList<Uri> sources)
        {
            if (ExpectedPlan.IsSome)
            {
                Assert.Equal(ExpectedPlan.AsT0, plan);
            }
            if (ExpectedSources.IsSome)
            {
                Assert.Equal(ExpectedSources.AsT0, sources);
            }

            return Task.FromResult(_results);
        }
    }
}