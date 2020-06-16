using System;
using System.Collections.Generic;
using SelectParser.Queries;

namespace SelectQuery.Evaluation
{
    public class ExpressionEvaluator
    {
        public T Evaluate<T>(Expression expression, object obj)
        {
            return expression.Match(
                strLiteral => (T)(object)EvaluateStringLiteral(strLiteral),
                numLiteral => (T)(object)EvaluateNumberLiteral(numLiteral),
                boolLiteral => (T)(object)EvaluateBooleanLiteral(boolLiteral),
                identifier => EvaluateIdentifier<T>(identifier, obj),
                qualified => EvaluateQualified<T>(qualified, obj),
                unary => EvaluateUnary<T>(unary, obj),
                binary => EvaluateBinary<T>(binary, obj),
                between => (T)(object)EvaluateBetween(between, obj),
                inExpr => (T)(object)EvaluateIn(inExpr, obj),
                like => (T)(object)EvaluateLike(like, obj)
            );
        }

        private string EvaluateStringLiteral(Expression.StringLiteral strLiteral)
        {
            return strLiteral.Value;
        }

        private decimal EvaluateNumberLiteral(Expression.NumberLiteral numLiteral)
        {
            return numLiteral.Value;
        }

        private bool EvaluateBooleanLiteral(Expression.BooleanLiteral boolLiteral)
        {
            return boolLiteral.Value;
        }

        private T EvaluateIdentifier<T>(Expression.Identifier identifier, object obj)
        {
            throw new NotImplementedException();
        }

        private T EvaluateQualified<T>(Expression.Qualified qualified, object obj)
        {
            throw new NotImplementedException();
        }

        private T EvaluateUnary<T>(Expression.Unary unary, object obj)
        {
            throw new NotImplementedException();
        }

        private T EvaluateBinary<T>(Expression.Binary binary, object obj)
        {
            throw new NotImplementedException();
        }

        private bool EvaluateBetween(Expression.Between between, object obj)
        {
            throw new NotImplementedException();
        }

        private bool EvaluateIn(Expression.In inExpr, object obj)
        {
            throw new NotImplementedException();
        }

        private bool EvaluateLike(Expression.Like like, object obj)
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