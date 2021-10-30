using System;
using System.Collections.Generic;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQueryBuilder
    {
        public static SlottedQuery Build(Query query)
        {
            var slotBuilder = new SlottedExpressionsBuilder(query.From);
            var queryBuilder = new SlottedQueryBuilder(slotBuilder);

            var select = queryBuilder.BuildSelect(query.Select);
            var predicate = queryBuilder.BuildPredicate(query.Where);
            
            return new SlottedQuery(select, predicate, slotBuilder.Slots);
        }

        private readonly SlottedExpressionsBuilder _slotBuilder;

        private SlottedQueryBuilder(SlottedExpressionsBuilder slotBuilder)
        {
            _slotBuilder = slotBuilder;
        }
        
        private SelectClause BuildSelect(SelectClause select)
        {
            return select.Match(BuildSelectStar, BuildSelectList);
        }

        private SelectClause BuildSelectStar(SelectClause.Star select)
        {
            var slot = (SlottedExpression) _slotBuilder.Build(new Expression.Identifier("*", false));
            return new StarSlot(slot);
        }

        internal class StarSlot : SelectClause
        {
            public SlottedExpression Slotted { get; }

            public StarSlot(SlottedExpression slotted)
            {
                Slotted = slotted;
            }

            public override string ToString() => $"$Star{Slotted}";
        }

        private SelectClause BuildSelectList(SelectClause.List select)
        {
            var slotColumns = new List<Column>();

            for (var index = 0; index < select.Columns.Count; index++)
            {
                var column = select.Columns[index];

                var expr = _slotBuilder.Build(column.Expression);
                slotColumns.Add(new Column(expr, GetColumnName(index, column)));
            }

            return new SelectClause.List(slotColumns);
        }
        
        private static string GetColumnName(int index, Column column)
        {
            if (column.Alias.IsSome)
            {
                return column.Alias.AsT0;
            }

            var expression = column.Expression;
            return GetColumnName(index, expression);
        }

        private static string GetColumnName(int index, Expression expression)
        {
            if (expression.IsT3)
            {
                // use the identifier name
                return expression.AsT3.Name;
            }

            if (expression.IsT4)
            {
                // use the last identifier when qualified
                return expression.AsT4.LastIdentifier.Name;
            }

            // default is _N for the Nth column (1 indexed)
            return $"_{index + 1}";
        }

        private Option<Expression> BuildPredicate(Option<WhereClause> where)
        {
            if (where.IsNone)
            {
                return new None();
            }
            
            return _slotBuilder.Build(where.AsT0.Condition);
        }
    }
}