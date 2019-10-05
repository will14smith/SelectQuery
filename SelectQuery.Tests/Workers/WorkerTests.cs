using System;
using System.Collections.Generic;
using System.Linq;
using SelectQuery.Results;
using SelectQuery.Tests.Fakes;
using SelectQuery.Workers;
using Xunit;
using static SelectQuery.Tests.QueryTestHelpers;

namespace SelectQuery.Tests.Workers
{
    public class WorkerTests
    {
        [Fact]
        public void BasicQuery_ShouldReturnUnderlyingResults()
        {
            var query = ParseQuery("SELECT * FROM table");
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("name", "a")),
                CreateRow(("id", 2), ("name", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults);
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = underlyingResults;

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        [Fact]
        public void Query_ShouldPassDataLocationToUnderlyingExecutor()
        {
            var query = ParseQuery("SELECT * FROM table");
            var dataLocation = new Uri("http://localhost/data");
            var underlying = new FakeUnderlyingExecutor(new ResultRow[0]) { ExpectedDataLocation = dataLocation };
            var worker = new Worker(underlying, new FakeResultsStorer());

            worker.Query(new WorkerInput(query, dataLocation));

            // assert is done in `underlying`
        }

        #region ordering
        [Fact]
        public void OrderedQuery_ShouldOrderResults()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Multiple_ShouldOrderResults()
        {
            var query = ParseQuery("SELECT id, name, other FROM table ORDER BY name, other");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Desc_ShouldOrderResults()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name DESC");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void OrderedQuery_Missing_ShouldOrderResults()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        [Fact]
        public void OrderedQuery_ShouldPassTransformedQueryToUnderlyingExecutor()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name");
            var underlying = new FakeUnderlyingExecutor(new ResultRow[0]) { ExpectedQuery = ParseQuery("SELECT id, name FROM table") };
            var worker = new Worker(underlying, new FakeResultsStorer());

            worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            // assert is done in `underlying`
        }
        #endregion

        #region limiting

        [Fact]
        public void LimitedQuery_Limit_ShouldLimitResults()
        {
            var query = ParseQuery("SELECT id, name FROM table LIMIT 1");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitedQuery_LimitHigherThanCount_ShouldLimitResults()
        {
            var query = ParseQuery("SELECT id, name FROM table LIMIT 10");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitedQuery_LimitOffset_ShouldLimitResults()
        {
            var query = ParseQuery("SELECT id, name FROM table LIMIT 1 OFFSET 1");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }
        [Fact]
        public void LimitedQuery_LimitAndOrder_ShouldLimitResultsAfterOrdering()
        {
            var query = ParseQuery("SELECT id, name FROM table ORDER BY name DESC LIMIT 1");
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

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        [Fact]
        public void LimitQuery_ShouldPassTransformedQueryToUnderlyingExecutor()
        {
            var query = ParseQuery("SELECT id, name FROM table LIMIT 1 OFFSET 1");
            var underlying = new FakeUnderlyingExecutor(new ResultRow[0]) { ExpectedQuery = ParseQuery("SELECT id, name FROM table LIMIT 2") };
            var worker = new Worker(underlying, new FakeResultsStorer());

            worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            // assert is done in `underlying`
        }

        #endregion

        #region projecting

        [Fact]
        public void Query_WithInternalColumns_ShouldProjectResults()
        {
            var query = ParseQuery("SELECT id FROM table ORDER BY name");
            var underlyingResults = new[]
            {
                CreateRow(("id", 1), ("__internal__order_0", "b")),
            };
            var underlying = new FakeUnderlyingExecutor(underlyingResults) {   ExpectedQuery = ParseQuery("SELECT id, name as __internal__order_0 FROM table") };
            var worker = new Worker(underlying, new FakeResultsStorer());
            var expectedResults = new[]
            {
                CreateRow(("id", 1))
            };

            var results = worker.Query(new WorkerInput(query, new Uri("http://localhost/data")));

            AssertResultsEqual(expectedResults, results);
        }

        #endregion

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
