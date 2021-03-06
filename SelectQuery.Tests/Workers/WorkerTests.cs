﻿using System;
using System.Linq;
using System.Threading.Tasks;
using OneOf.Types;
using SelectParser.Queries;
using SelectQuery.Results;
using SelectQuery.Tests.Fakes;
using SelectQuery.Workers;
using Xunit;
using static SelectQuery.Tests.QueryTestHelpers;
using static SelectQuery.Tests.ResultTestHelpers;

namespace SelectQuery.Tests.Workers
{
    public class WorkerTests
    {
        private static readonly Query Query = ParseQuery("SELECT * FROM table");

        [Fact]
        public async Task BasicQuery_ShouldReturnUnderlyingResults()
        {
            var plan = new WorkerPlan(Query, new None(), new None());
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "a")),
                CreateRow(("id", 2), ("name", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = underlyingResults;

            var results = await worker.QueryAsync(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        [Fact]
        public async Task Query_ShouldPassQueryAndDataLocationToUnderlyingExecutor()
        {
            var plan = new WorkerPlan(Query, new None(), new None());
            var dataLocation = new Uri("http://localhost/data");
            var underlying = new FakeUnderlyingExecutor(new ResultRow[0]) { ExpectedQuery = Query, ExpectedDataLocation = dataLocation };
            var worker = new Worker(underlying, new FakeResultsStorer());

            await worker.QueryAsync(new WorkerInput(plan, dataLocation));

            // assert is done in `underlying`
        }

        [Fact]
        public async Task OrderedQuery_ShouldOrderResults()
        {
            var plan = new WorkerPlan(Query, Order(("name", OrderDirection.Ascending)), new None());
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[1],
                underlyingResults[0],
                underlyingResults[2],
            };

            var results = await worker.QueryAsync(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        [Fact]
        public async Task LimitedQuery_Limit_ShouldLimitResults()
        {
            var plan = new WorkerPlan(Query, new None(), new LimitClause(1));
            var underlyingResults = new[] { CreateRow(("id", 1)), CreateRow(("id", 2)) };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[] { underlyingResults[0] };

            var results = await worker.QueryAsync(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public async Task LimitedQuery_LimitAndOrder_ShouldLimitResultsAfterOrdering()
        {
            var plan = new WorkerPlan(Query, Order(("name", OrderDirection.Descending)), new LimitClause(1));
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "a")),
                CreateRow(("id", 2), ("name", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[] { underlyingResults[1] };

            var results = await worker.QueryAsync(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        private static OrderClause Order(params (string Key, OrderDirection Direction)[] columns)
        {
            return new OrderClause(columns.Select(x => ((Expression)new Expression.Identifier(x.Key, false), x.Direction)).ToList());
        }
    }
}
