using System;
using System.Collections.Generic;
using System.Linq;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

public class QueryValidator
{
    private readonly List<string> _errors = new();

    public IReadOnlyCollection<string> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;

    public void Validate(Query query)
    {
        if (!string.Equals(query.From.Table, "S3Object", StringComparison.OrdinalIgnoreCase))
        {
            AddError("complex table targets are not currently supported");
        }

        if (query.Order.IsSome)
        {
            AddError("ordering is not currently supported");
        }

        if (query.Select is SelectClause.List list && list.Columns.Any(x => IsQualifiedStar(x.Expression)))
        {
            AddError("qualified star projections not currently supported");
        }

        if (IsAggregateQuery(query) && HasNonAggregateColumns(query))
        {
            throw new NotImplementedException("non-aggregate columns are not supported in an aggregate query");
        }
    }
    
    public void ThrowIfErrors()
    {
        switch (_errors.Count)
        {
            case 0: return;
            case 1: throw new QueryValidationError(_errors[0]);
            default: throw new AggregateException(_errors.Select(x => new QueryValidationError(x)));
        }
    }
    
    private static bool IsQualifiedStar(Expression expr) =>
        expr switch
        {
            Expression.Identifier identifier => identifier.Name == "*",
            Expression.Qualified qualified => qualified.Identifiers[qualified.Identifiers.Count - 1].Name == "*",
            _ => false,
        };

    internal static bool IsAggregateQuery(Query query) =>
        query.Select switch
        {
            SelectClause.Star => false,
            SelectClause.List list => list.Columns.Any(IsAggregateColumn),
            _ => throw new ArgumentOutOfRangeException()
        };

    private static bool HasNonAggregateColumns(Query query) =>
        query.Select switch
        {
            SelectClause.Star => true,
            SelectClause.List list => list.Columns.Any(column => !IsAggregateColumn(column)),
            _ => throw new ArgumentOutOfRangeException()
        };

    private static bool IsAggregateColumn(Column column)
    {
        if (column.Expression is Expression.FunctionExpression functionExpression)
        {
            return functionExpression.Function is AggregateFunction;
        }

        return false;
    }

    private void AddError(string message)
    {
        _errors.Add(message);
    }
}