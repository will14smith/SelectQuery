using System.Collections.Generic;
using System.Linq;
using SelectQuery.Results;
using Xunit;

namespace SelectQuery.Tests
{
    internal static class ResultTestHelpers
    {
        public static ResultRow CreateRow(params (string Key, object Value)[] fields)
        {
            return new ResultRow(fields.ToDictionary(x => x.Key, x => x.Value));
        }

        public static void AssertResultsEqual(IReadOnlyList<ResultRow> expectedRows, Result actualResults)
        {
            Assert.True(actualResults.IsT0, "Result wasn't a direct result");
            var actualRows = actualResults.AsT0.Rows;

            AssertResultsEqual(expectedRows, actualRows);
        }

        public static void AssertResultsEqual(IReadOnlyList<ResultRow> expectedRows, IEnumerable<ResultRow> actualRows)
        {
            AssertResultsEqual(expectedRows, actualRows.ToList());
        }
        public static void AssertResultsEqual(IReadOnlyList<ResultRow> expectedRows, IReadOnlyList<ResultRow> actualRows)
        {
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
