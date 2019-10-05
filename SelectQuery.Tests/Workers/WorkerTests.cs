using System;
using System.Collections.Generic;
using System.Linq;
using OneOf.Types;
using SelectParser.Queries;
using SelectQuery.Results;
using SelectQuery.Tests.Fakes;
using SelectQuery.Workers;
using Xunit;
using static SelectQuery.Tests.QueryTestHelpers;

namespace SelectQuery.Tests.Workers
{
    public class WorkerTests
    {
        private static readonly Query Query = ParseQuery("SELECT * FROM table");

        [Fact]
        public void BasicQuery_ShouldReturnUnderlyingResults()
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

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        [Fact]
        public void Query_ShouldPassQueryAndDataLocationToUnderlyingExecutor()
        {
            var plan = new WorkerPlan(Query, new None(), new None());
            var dataLocation = new Uri("http://localhost/data");
            var underlying = new FakeUnderlyingExecutor(new ResultRow[0]) { ExpectedQuery = Query, ExpectedDataLocation = dataLocation };
            var worker = new Worker(underlying, new FakeResultsStorer());

            worker.Query(new WorkerInput(plan, dataLocation));

            // assert is done in `underlying`
        }

        #region ordering
        [Fact]
        public void OrderedQuery_ShouldOrderResults()
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

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Multiple_ShouldOrderResults()
        {
            var plan = new WorkerPlan(Query, Order(("name", OrderDirection.Ascending), ("other", OrderDirection.Ascending)), new None());
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "a"), ("other", 2)),
                CreateRow(("id", 2), ("name", "a"), ("other", 1)),
                CreateRow(("id", 3), ("name", "b"), ("other", 1)),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[1],
                underlyingResults[0],
                underlyingResults[2],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Desc_ShouldOrderResults()
        {
            var plan = new WorkerPlan(Query, Order(("name", OrderDirection.Descending)), new None());
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
                underlyingResults[2],
                underlyingResults[0],
                underlyingResults[1],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Missing_ShouldOrderResults()
        {
            var plan = new WorkerPlan(Query, Order(("name", OrderDirection.Ascending)), new None());
            var underlyingResults = new[]
            {
                CreateRow(("id", 1)),
                CreateRow(("id", 2), ("name", "a")),
                CreateRow(("id", 3), ("name", "c")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[0],
                underlyingResults[1],
                underlyingResults[2],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        #endregion

        #region limiting
        [Fact]
        public void LimitedQuery_Limit_ShouldLimitResults()
        {
            var plan = new WorkerPlan(Query, new None(), new LimitClause(1));
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[0],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitedQuery_LimitHigherThanCount_ShouldLimitResults()
        {
            var plan = new WorkerPlan(Query, new None(), new LimitClause(10));
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[0],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitedQuery_LimitOffset_ShouldLimitResults()
        {
            var plan = new WorkerPlan(Query, new None(), new LimitClause(1, 1));
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "b")),
                CreateRow(("id", 2), ("name", "a")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[1],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitedQuery_LimitAndOrder_ShouldLimitResultsAfterOrdering()
        {
            var plan = new WorkerPlan(Query, Order(("name", OrderDirection.Descending)), new LimitClause(1));
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "a")),
                CreateRow(("id", 2), ("name", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                underlyingResults[1],
            };

            var results = worker.Query(new WorkerInput(plan, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        
        #endregion

        private static OrderClause Order(params (string Key, OrderDirection Direction)[] columns)
        {
            return new OrderClause(columns.Select(x => ((Expression)new Expression.Identifier(x.Key), x.Direction)).ToList());
        }
        private static ResultRow CreateRow(params (string Key, object Value)[] fields)
        {
            return new ResultRow(fields.ToDictionary(x => x.Key, x => x.Value));
        }

        private static void AssertResultsEqual(IReadOnlyList<ResultRow> expectedRows, Result actualResults)
        {
            Assert.True(actualResults.IsT0, "Result wasn't a direct result");
            var actualRows = actualResults.AsT0.Rows;

            Assert.Equal(expectedRows.Count, actualRows.Count);

            for (var i = 0; i < expectedRows.Count; i++)
            {
                var expected = expectedRows[i];
                var actual = actualRows[i];

                Assert.Equal(expected.Fields, actual.Fields);
            }
        }
    }
}
