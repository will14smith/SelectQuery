using System;
using System.Threading.Tasks;
using SelectParser.Queries;
using SelectQuery.Distribution;
using SelectQuery.Results;
using SelectQuery.Tests.Fakes;
using Xunit;
using static SelectQuery.Tests.QueryTestHelpers;
using static SelectQuery.Tests.ResultTestHelpers;

namespace SelectQuery.Tests.Distribution
{
    public class DistributorTests
    {
        private static readonly Query DefaultQuery = ParseQuery("SELECT * FROM table");
        private static readonly DataSource DefaultSource = new DataSource.Prefix(new Uri("http://localhost/data"));

        [Fact]
        public async Task Distributor_ShouldPassSourceToResolver()
        {
            var resolver = new FakeSourceResolver(new Uri[0]) { ExpectedDataSource = DefaultSource };
            var distributor = CreateDistributor(sourceResolver: resolver);

            await distributor.QueryAsync(new DistributorInput(DefaultQuery, DefaultSource));

            // assertion is done by `resolver`
        }

        [Fact]
        public async Task Distributor_ShouldPassSourcesToExecutor()
        {
            var sources = new[] { new Uri("http://localhost/data/1"), };
            var resolver = new FakeSourceResolver(sources);
            var executor = new FakeWorkerExecutor(new Result[0]) { ExpectedSources = sources };
            var distributor = CreateDistributor(sourceResolver: resolver, workerExecutor: executor);

            await distributor.QueryAsync(new DistributorInput(DefaultQuery, DefaultSource));

            // assertion is done by `executor`
        }

        [Fact]
        public async Task Distributor_ShouldOrderResult()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name");
            var expectedResults = new[]
            {
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var workerResults = new[]
            {
                new Result.Direct(new [] { expectedResults[1] }),
                new Result.Direct(new [] { expectedResults[0], expectedResults[2] }),
            };
            var executor = new FakeWorkerExecutor(workerResults);
            var distributor = CreateDistributor(workerExecutor: executor);

            var result = await distributor.QueryAsync(new DistributorInput(query, DefaultSource));

            AssertResultsEqual(expectedResults, result);
        }

        [Fact]
        public async Task Distributor_ShouldLimitResult()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name LIMIT 1");
            var results = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var workerResults = new[]
            {
                new Result.Direct(new [] { results[0] }),
                new Result.Direct(new [] { results[1], results[2] }),
            };
            var expectedResults = new[] {results[1]};
            var executor = new FakeWorkerExecutor(workerResults);
            var distributor = CreateDistributor(workerExecutor: executor);

            var result = await distributor.QueryAsync(new DistributorInput(query, DefaultSource));

            AssertResultsEqual(expectedResults, result);
        }


        [Fact]
        public async Task Distributor_ShouldFilterOutInternalColumns()
        {
            var workerResults = new[]
            {
                new Result.Direct(new []
                {
                    CreateRow(("id", 2), ("__internal__order_0", "a")),
                })
            };
            var expectedResults = new[]
            {
                CreateRow(("id", 2)),
            };
            var executor = new FakeWorkerExecutor(workerResults);
            var distributor = CreateDistributor(workerExecutor: executor);

            var result = await distributor.QueryAsync(new DistributorInput(DefaultQuery, DefaultSource));

            AssertResultsEqual(expectedResults, result);
        }

        private static Distributor CreateDistributor(ISourceResolver sourceResolver = null, IWorkerExecutor workerExecutor = null, IResultsFetcher resultsesFetcher = null, IResultsStorer resultsStorer = null)
        {
            return new Distributor(
                sourceResolver ?? new FakeSourceResolver(new Uri[0]),
                workerExecutor ?? new FakeWorkerExecutor(new Result[0]),
                resultsesFetcher ?? new FakeResultsFetcher(),
                resultsStorer ?? new FakeResultsStorer()
            );
        }
    }
}
