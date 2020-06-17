using System;
using System.Linq;
using OneOf.Types;
using SelectParser.Queries;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SelectParser
{
    public class Parser
    {
        private static readonly TokenListParser<SelectToken, string> Identifier =
            Token.EqualTo(SelectToken.Identifier)
                .Select(ParseIdentifier);

        private static readonly TokenListParser<SelectToken, decimal> Number =
            Token.EqualTo(SelectToken.NumberLiteral)
                .Select(x => decimal.Parse(x.ToStringValue()));

        private static readonly TokenListParser<SelectToken, string> String =
            Token.EqualTo(SelectToken.StringLiteral)
                .Select(ParseString);

        private static readonly TokenListParser<SelectToken, bool> Boolean =
            Token.EqualTo(SelectToken.BooleanLiteral)
                .Select(x => bool.Parse(x.ToStringValue()));


        private static readonly TokenListParser<SelectToken, string> Alias =
            Token.Sequence(SelectToken.As, SelectToken.Identifier).Select(x => ParseIdentifier(x[1]))
                .Or(Identifier);

        #region expression 

        private static readonly TokenListParser<SelectToken, Expression> QualifiedIdentifier =
            Identifier
                .Or(Token.EqualTo(SelectToken.Star).Select(x => "*"))
                .Select(x => new Expression.Identifier(x))
                .AtLeastOnceDelimitedBy(Token.EqualTo(SelectToken.Dot))
                .Select(BuildQualified);
        private static Expression BuildQualified(Expression.Identifier[] identifiers)
        {
            if (identifiers.Length == 0) return null;
            if (identifiers.Length == 1) return identifiers[0];

            Expression result = identifiers[identifiers.Length - 1];
            for (var i = identifiers.Length - 2; i >= 0; i--)
            {
                result = new Expression.Qualified(identifiers[i].Name, result);
            }
            return result;
        }

        public static readonly TokenListParser<SelectToken, Expression> Term =
            QualifiedIdentifier
                .Or(Number.Select(x => (Expression)new Expression.NumberLiteral(x)))
                .Or(String.Select(x => (Expression)new Expression.StringLiteral(x)))
                .Or(Boolean.Select(x => (Expression)new Expression.BooleanLiteral(x)));

        public static readonly TokenListParser<SelectToken, Expression> Unary =
            (
                from op in Token.EqualTo(SelectToken.Negate)
                from term in Term
                select (Expression)new Expression.Unary(UnaryOperator.Negate, term)
            )
            .Or(Term);

        public static readonly TokenListParser<SelectToken, Expression> Multiplicative =
            Parse.Chain(
                Token.EqualTo(SelectToken.Star).Or(Token.EqualTo(SelectToken.Divide)).Or(Token.EqualTo(SelectToken.Modulo)),
                Unary,
                CreateMultiplicative
            );
        private static Expression CreateMultiplicative(Token<SelectToken> opToken, Expression left, Expression right)
        {
            var operation = opToken.Kind switch
            {
                SelectToken.Star => BinaryOperator.Multiply,
                SelectToken.Divide => BinaryOperator.Divide,
                SelectToken.Modulo => BinaryOperator.Modulo,
            };

            return new Expression.Binary(operation, left, right);
        }

        public static readonly TokenListParser<SelectToken, Expression> Additive =
            Parse.Chain(
                Token.EqualTo(SelectToken.Add).Or(Token.EqualTo(SelectToken.Negate)),
                Multiplicative,
                CreateAdditive
            );
        private static Expression CreateAdditive(Token<SelectToken> opToken, Expression left, Expression right)
        {
            var operation = opToken.Kind switch
            {
                SelectToken.Add => BinaryOperator.Add,
                SelectToken.Negate => BinaryOperator.Subtract,
            };

            return new Expression.Binary(operation, left, right);
        }

        public static readonly TokenListParser<SelectToken, Expression> Membership =
        (
            from expression in Additive
            from @in in Token.EqualTo(SelectToken.In)
            from begin in Token.EqualTo(SelectToken.LeftBracket)
            from values in Parse.Ref(() => Expression).ManyDelimitedBy(
                Token.EqualTo(SelectToken.Comma),
                Token.EqualTo(SelectToken.RightBracket))
            select (Expression)new Expression.In(expression, values)
        ).Try().Or(Additive);

        public static readonly TokenListParser<SelectToken, Expression> Presence =
        (
            from expression in Membership
            from @is in Token.EqualTo(SelectToken.Is)
            from not in Token.EqualTo(SelectToken.Not).Optional()
            from missing in Token.EqualTo(SelectToken.Missing)
            select (Expression)new Expression.Presence(expression, !not.HasValue)
        ).Try().Or(Membership);

        public static readonly TokenListParser<SelectToken, Expression> Containment =
        (
            from expression in Presence
            from not in Token.EqualTo(SelectToken.Not).Optional()
            from between in Token.EqualTo(SelectToken.Between)
            from lower in Presence
            from and in Token.EqualTo(SelectToken.And)
            from upper in Parse.Ref(() => Expression)
            select (Expression)new Expression.Between(not.HasValue, expression, lower, upper)
        ).Try().Or(Presence);

        public static readonly TokenListParser<SelectToken, Expression> Pattern =
        (
            from expression in Containment
            from between in Token.EqualTo(SelectToken.Like)
            from pattern in Parse.Ref(() => Expression)
            from escape in Token.EqualTo(SelectToken.Escape)
            from escapeExpression in Parse.Ref(() => Expression)
            select (Expression)new Expression.Like(expression, pattern, escapeExpression)
        ).Try().Or(
            from expression in Containment
            from between in Token.EqualTo(SelectToken.Like)
            from pattern in Parse.Ref(() => Expression)
            select (Expression)new Expression.Like(expression, pattern, new None())
        ).Try().Or(Containment);

        public static readonly TokenListParser<SelectToken, Expression> Comparative =
        (
            from left in Pattern
            from operation in Token.EqualTo(SelectToken.Lesser).Or(Token.EqualTo(SelectToken.Greater)).Or(Token.EqualTo(SelectToken.LesserOrEqual)).Or(Token.EqualTo(SelectToken.GreaterOrEqual))
            from right in Pattern
            select (Expression)new Expression.Binary(GetComparativeOperation(operation), left, right)
        ).Try().Or(Pattern);
        private static BinaryOperator GetComparativeOperation(Token<SelectToken> operation) => operation.Kind switch
        {
            SelectToken.Lesser => BinaryOperator.Lesser,
            SelectToken.Greater => BinaryOperator.Greater,
            SelectToken.LesserOrEqual => BinaryOperator.LesserOrEqual,
            SelectToken.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
        };

        public static readonly TokenListParser<SelectToken, Expression> Equality =
            Parse.ChainRight(
                Token.EqualTo(SelectToken.Equal).Or(Token.EqualTo(SelectToken.NotEqual)),
                Comparative,
                CreateEquality
            );
        private static Expression CreateEquality(Token<SelectToken> opToken, Expression left, Expression right)
        {
            var operation = opToken.Kind switch
            {
                SelectToken.Equal => BinaryOperator.Equal,
                SelectToken.NotEqual => BinaryOperator.NotEqual,
            };

            return new Expression.Binary(operation, left, right);
        }

        public static readonly TokenListParser<SelectToken, Expression> BooleanUnary =
        (
            from operation in Token.EqualTo(SelectToken.Not)
            from expression in Equality
            select (Expression)new Expression.Unary(UnaryOperator.Not, expression)
        ).Or(Equality);

        public static readonly TokenListParser<SelectToken, Expression> BooleanAnd =
            Parse.ChainRight(
                Token.EqualTo(SelectToken.And),
                BooleanUnary,
                (_, left, right) => new Expression.Binary(BinaryOperator.And, left, right)
            );

        public static readonly TokenListParser<SelectToken, Expression> BooleanOr =
            Parse.ChainRight(
                Token.EqualTo(SelectToken.Or),
                BooleanAnd,
                (_, left, right) => new Expression.Binary(BinaryOperator.Or, left, right)
            );

        public static readonly TokenListParser<SelectToken, Expression> Expression = BooleanOr;

        // TODO handle brackets

        #endregion

        #region select
        private static readonly TokenListParser<SelectToken, Column> Column =
            from expr in Expression
            from alias in Alias.Select(x => (Option<string>)x).OptionalOrDefault(new None())
            select new Column(expr, alias);
        private static readonly TokenListParser<SelectToken, SelectClause> ColumnsStar =
            Token.EqualTo(SelectToken.Star).Select(x => (SelectClause)new SelectClause.Star());
        private static readonly TokenListParser<SelectToken, SelectClause> ColumnsList =
            Parse.Chain(Token.EqualTo(SelectToken.Comma), Column.Select(x => new SelectClause.List(new[] { x })), CombineColumnLists)
                .Select(x => (SelectClause)x);

        private static SelectClause.List CombineColumnLists(Token<SelectToken> op, SelectClause.List left, SelectClause.List right)
        {
            var columns = left.Columns.Concat(right.Columns).ToList();
            return new SelectClause.List(columns);
        }

        public static readonly TokenListParser<SelectToken, SelectClause> SelectClause =
            from @select in Token.EqualTo(SelectToken.Select)
            from columns in ColumnsStar.Or(ColumnsList)
            select columns;

        #endregion

        #region from
        public static readonly TokenListParser<SelectToken, FromClause> FromClause =
            from @from in Token.EqualTo(SelectToken.From)
            from tableName in Identifier
            from alias in Alias.Select(x => (Option<string>)x).OptionalOrDefault(new None())
            select new FromClause(tableName, alias);
        #endregion

        #region where
        public static readonly TokenListParser<SelectToken, WhereClause> WhereClause =
            from @where in Token.EqualTo(SelectToken.Where)
            from expression in Expression
            select new WhereClause(expression);
        #endregion

        #region order by

        private static readonly TokenListParser<SelectToken, (Expression Expression, OrderDirection Direction)> OrderByColumn =
            from column in Expression
            from direction in Token.EqualTo(SelectToken.Asc).Select(_ => OrderDirection.Ascending)
                .Or(Token.EqualTo(SelectToken.Desc).Select(_ => OrderDirection.Descending)).OptionalOrDefault(OrderDirection.Ascending)
            select (column, direction);

        public static readonly TokenListParser<SelectToken, OrderClause> OrderByClause =
            from orderBy in Token.Sequence(SelectToken.Order, SelectToken.By)
            from columns in OrderByColumn.ManyDelimitedBy(Token.EqualTo(SelectToken.Comma))
            select new OrderClause(columns);

        #endregion

        #region limit
        public static readonly TokenListParser<SelectToken, LimitClause> LimitClause =
            from limit in Token.EqualTo(SelectToken.Limit)
            from number in Number.Select(x => (int)x)
            select new LimitClause(number);
        #endregion

        public static readonly TokenListParser<SelectToken, Query> Query =
            from @select in SelectClause
            from @from in FromClause
            from @where in WhereClause.Select(x => (Option<WhereClause>)x).OptionalOrDefault(new None())
            from order in OrderByClause.Select(x => (Option<OrderClause>)x).OptionalOrDefault(new None())
            from limit in LimitClause.Select(x => (Option<LimitClause>)x).OptionalOrDefault(new None())
            select new Query(@select, @from, @where, order, limit);

        private static string ParseIdentifier(Token<SelectToken> token)
        {
            var rawValue = token.ToStringValue();

            return rawValue[0] == '\"'
                ? rawValue.Substring(1, rawValue.Length - 2)
                : rawValue.ToLower();
        }

        private static string ParseString(Token<SelectToken> token)
        {
            var rawValue = token.ToStringValue();
            // TODO handle escapes
            return rawValue.Substring(1, rawValue.Length - 2);
        }
    }
}
