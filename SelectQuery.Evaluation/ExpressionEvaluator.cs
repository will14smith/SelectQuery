using System;
using System.Collections.Generic;
using SelectParser.Queries;

namespace SelectQuery.Evaluation
{
    public class ExpressionEvaluator
    {
        public T Evaluate<T>(Expression expression, object obj)
        {
            throw new NotImplementedException();
        }
    }

    public static class ExpressionEvaluatorExtensions
    {
        public static T EvaluateOnTable<T>(this ExpressionEvaluator evaluator, Expression expression, FromClause from, object obj)
        {
            var tableName = from.Alias.Match(alias => alias, _ => "s3object");
            var input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { tableName, obj }
            };

            return evaluator.Evaluate<T>(expression, input);
        }
    }
}