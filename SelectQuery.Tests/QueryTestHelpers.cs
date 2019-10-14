using SelectParser;
using SelectParser.Queries;
using Superpower;
using Xunit;

namespace SelectQuery.Tests
{
    internal static class QueryTestHelpers
    {
        public static Query ParseQuery(string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return Parser.Query.Parse(tokens);
        }

        #region Query Assertions
        public static void AssertEqual(Query expected, Query actual)
        {
            AssertEqual(expected.Select, actual.Select);
            AssertEqual(expected.From, actual.From);
            AssertEqual(expected.Where, actual.Where);
            AssertEqual(expected.Order, actual.Order);
            AssertEqual(expected.Limit, actual.Limit);
        }

        public static void AssertEqual(Option<SelectClause> expected, Option<SelectClause> actual)
        {
            Assert.Equal(expected.IsSome, actual.IsSome);
            if (expected.IsSome)
            {
                AssertEqual(expected.AsT0, actual.AsT0);
            }
        }
        public static void AssertEqual(SelectClause expected, SelectClause actual)
        {
            expected.Switch(
                expectedStar =>
                {
                    Assert.True(actual.IsT0, $"Expected '*' but got '{actual}' instead");
                },
                expectedList =>
                {
                    Assert.True(actual.IsT1, $"Expected '{expected}' but got '{actual}' instead");
                    var actualList = actual.AsT1;

                    Assert.Equal(expectedList.Columns.Count, actualList.Columns.Count);

                    for (var i = 0; i < expectedList.Columns.Count; i++)
                    {
                        var expectedColumn = expectedList.Columns[i];
                        var actualColumn = actualList.Columns[i];

                        Assert.Equal(expectedColumn.Expression, actualColumn.Expression);
                        Assert.Equal(expectedColumn.Alias, actualColumn.Alias);
                    }
                }
            );
        }

        public static void AssertEqual(FromClause expected, FromClause actual)
        {
            Assert.Equal(expected.Table, actual.Table);
            Assert.Equal(expected.Alias, actual.Alias);
        }

        public static void AssertEqual(Option<WhereClause> expected, Option<WhereClause> actual)
        {
            Assert.Equal(expected.IsSome, actual.IsSome);
            if (expected.IsSome)
            {
                AssertEqual(expected.AsT0, actual.AsT0);
            }
        }
        public static void AssertEqual(WhereClause expected, WhereClause actual)
        {
            Assert.Equal(expected.Condition, actual.Condition);
        }

        public static void AssertEqual(string expected, Option<OrderClause> actual)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(expected);
            var expectedClause = Parser.OrderByClause.Parse(tokens);

            AssertEqual(expectedClause, actual);
        }
        public static void AssertEqual(Option<OrderClause> expected, Option<OrderClause> actual)
        {
            Assert.Equal(expected.IsSome, actual.IsSome);
            if (expected.IsSome)
            {
                AssertEqual(expected.AsT0, actual.AsT0);
            }
        }
        public static void AssertEqual(OrderClause expected, OrderClause actual)
        {
            Assert.Equal(expected.Columns.Count, actual.Columns.Count);

            for (var i = 0; i < expected.Columns.Count; i++)
            {
                var (expectedExpression, expectedDirection) = expected.Columns[i];
                var (actualExpression, actualDirection) = actual.Columns[i];

                Assert.Equal(expectedExpression, actualExpression);
                Assert.Equal(expectedDirection, actualDirection);
            }
        }

        public static void AssertEqual(string expected, Option<LimitClause> actual)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(expected);
            var expectedClause = Parser.LimitClause.Parse(tokens);

            AssertEqual(expectedClause, actual);
        }
        public static void AssertEqual(Option<LimitClause> expected, Option<LimitClause> actual)
        {
            Assert.Equal(expected.IsSome, actual.IsSome);
            if (expected.IsSome)
            {
                AssertEqual(expected.AsT0, actual.AsT0);
            }
        }
        public static void AssertEqual(LimitClause expected, LimitClause actual)
        {
            Assert.Equal(expected.Limit, actual.Limit);
        }
        #endregion
    }
}