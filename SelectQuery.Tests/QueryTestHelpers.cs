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

                        AssertEqual(expectedColumn.Expression, actualColumn.Expression);
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
            AssertEqual(expected.Condition, actual.Condition);
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

                AssertEqual(expectedExpression, actualExpression);
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
            Assert.Equal(expected.Offset, actual.Offset);
        }
        #endregion

        #region Expression Assertions
        public static void AssertEqual(Expression expected, Expression actual)
        {
            expected.Switch(
                expectedString =>
                {
                    Assert.True(actual.IsT0, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT0.Value, actual.AsT0.Value);
                }, 
                expectedNumber =>
                {
                    Assert.True(actual.IsT1, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT1.Value, actual.AsT1.Value);
                }, 
                expectedBoolean =>
                {
                    Assert.True(actual.IsT2, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT2.Value, actual.AsT2.Value);
                },

                expectedIdentifier =>
                {
                    Assert.True(actual.IsT3, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT3.Name, actual.AsT3.Name);
                },
                expectedQualified =>
                {
                    Assert.True(actual.IsT4, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT4.Qualification, actual.AsT4.Qualification);
                    AssertEqual(expected.AsT4.Expression, actual.AsT4.Expression);
                },

                expectedUnary =>
                {
                    Assert.True(actual.IsT5, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT5.Operator, actual.AsT5.Operator);
                    AssertEqual(expected.AsT5.Expression, actual.AsT5.Expression);
                }, 
                expectedBinary =>
                {
                    Assert.True(actual.IsT6, $"Expected '{expected}' but got '{actual}' instead");
                    AssertEqual(expected.AsT6.Left, actual.AsT6.Left);
                    Assert.Equal(expected.AsT6.Operator, actual.AsT6.Operator);
                    AssertEqual(expected.AsT6.Right, actual.AsT6.Right);
                },
                expectedBetween =>
                {
                    Assert.True(actual.IsT7, $"Expected '{expected}' but got '{actual}' instead");
                    Assert.Equal(expected.AsT7.Negate, actual.AsT7.Negate);
                    AssertEqual(expected.AsT7.Expression, actual.AsT7.Expression);
                    AssertEqual(expected.AsT7.Lower, actual.AsT7.Lower);
                    AssertEqual(expected.AsT7.Upper, actual.AsT7.Upper);
                },
                expectedIn =>
                {
                    Assert.True(actual.IsT8, $"Expected '{expected}' but got '{actual}' instead");
                    AssertEqual(expected.AsT8.Expression, actual.AsT8.Expression);

                    Assert.Equal(expected.AsT8.Matches.Count, actual.AsT8.Matches.Count);

                    for (var i = 0; i < expected.AsT8.Matches.Count; i++)
                    {
                        var expectedExpression = expected.AsT8.Matches[i];
                        var actualExpression = actual.AsT8.Matches[i];

                        AssertEqual(expectedExpression, actualExpression);
                    }
                },
                expectedLike =>
                {
                    Assert.True(actual.IsT9, $"Expected '{expected}' but got '{actual}' instead");
                    AssertEqual(expected.AsT9.Expression, actual.AsT9.Expression);
                    AssertEqual(expected.AsT9.Pattern, actual.AsT9.Pattern);

                    Assert.Equal(expected.AsT9.Escape.IsSome, actual.AsT9.Escape.IsSome);
                    if (expected.AsT9.Escape.IsSome)
                    {
                        AssertEqual(expected.AsT9.Escape.AsT0, actual.AsT9.Escape.AsT0);
                    }
                }
            );
        }

        public static void AssertIdentifierEqual(string expected, Expression expression)
        {
            var identifier = Assert.IsType<Expression.Identifier>(expression);
            Assert.Equal(expected, identifier.Name);
        }
        #endregion
    }
}