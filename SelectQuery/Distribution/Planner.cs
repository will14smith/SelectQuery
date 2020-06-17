using System.Collections.Generic;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Distribution
{
    internal class Planner
    {
        public DistributorPlan Plan(Query input)
        {
            SelectClause underlyingSelect = null;
            Option<LimitClause> underlyingLimit = new None();

            Option<OrderClause> order = new None();
            Option<LimitClause> limit = new None();

            if (input.Order.IsSome)
            {
                var builder = new PlanOrderBuilder(input, input.Order.AsT0);

                underlyingSelect = builder.UnderlyingSelect;
                order = builder.Order;
            }

            if (input.Limit.IsSome)
            {
                var limitValue = input.Limit.AsT0;

                limit = limitValue;

                // we can't sort on the underlying query so we need to load the whole result set
                if (input.Order.IsNone)
                {
                    underlyingLimit = new LimitClause(limitValue.Limit);
                }
            }

            if (underlyingSelect == null)
            {
                underlyingSelect = input.Select;
            }

            var underlying = new Query(underlyingSelect, input.From, input.Where, new None(), underlyingLimit);

            return new DistributorPlan(input, underlying, order, limit);
        }

        private class PlanOrderBuilder
        {
            private readonly Query _input;
            private readonly OrderClause _order;

            private readonly List<Column> _underlyingSelect;
            private readonly List<(Expression Expression, OrderDirection Direction)> _orderColumns;

            private int _counter;

            public SelectClause UnderlyingSelect => new SelectClause.List(_underlyingSelect);
            public OrderClause Order => new OrderClause(_orderColumns);

            public PlanOrderBuilder(Query input, OrderClause order)
            {
                _input = input;
                _order = order;

                _underlyingSelect = new List<Column>();
                _orderColumns = new List<(Expression Expression, OrderDirection Direction)>();

                _input.Select.Switch(
                    BuildStar,
                    BuildList
                );
            }

            private void BuildStar(SelectClause.Star star)
            {
                var table = _input.From.Alias.Match(x => x, _ => _input.From.Table);
                _underlyingSelect.Add(new Column(new Expression.Qualified(new Expression.Identifier(table, false), new Expression.Identifier("*", false))));

                foreach (var (orderExpression, orderDirection) in _order.Columns)
                {
                    AddInternalColumn(orderExpression, orderDirection);
                }
            }

            private void BuildList(SelectClause.List list)
            {
                _underlyingSelect.AddRange(list.Columns);

                foreach (var (orderExpression, orderDirection) in _order.Columns)
                {
                    if (!TryUsingExistingColumn(list, orderExpression, orderDirection))
                    {
                        AddInternalColumn(orderExpression, orderDirection);
                    }
                }
            }

            private bool TryUsingExistingColumn(SelectClause.List list, Expression orderExpression, OrderDirection orderDirection)
            {
                for (var i = 0; i < list.Columns.Count; i++)
                {
                    var selectColumn = list.Columns[i];
                    if (!selectColumn.Expression.Equals(orderExpression)) continue;

                    var columnName = GetProjectedColumnName(selectColumn, i);
                    var expr = new Expression.Identifier(columnName, false);

                    _orderColumns.Add((expr, orderDirection));
                    return true;
                }

                return false;
            }

            private void AddInternalColumn(Expression orderExpression, OrderDirection orderDirection)
            {
                var alias = $"__internal__order_{_counter++}";
                _orderColumns.Add((new Expression.Identifier(alias, false), orderDirection));
                _underlyingSelect.Add(new Column(orderExpression, alias));
            }

            private static string GetProjectedColumnName(Column selectColumn, int i)
            {
                return selectColumn.Alias.Match(x => x, _ => TryGetColumnName(selectColumn.Expression, out var name) ? name : $"_{i + 1}");
            }

            private static bool TryGetColumnName(Expression expression, out string name)
            {
                while (true)
                {
                    switch (expression)
                    {
                        case Expression.Identifier identifier:
                            name = identifier.Name;
                            return true;
                        case Expression.Qualified qualified:
                            expression = qualified.Expression;
                            continue;
                        default:
                            name = default;
                            return false;
                    }
                }
            }
        }
    }
}
