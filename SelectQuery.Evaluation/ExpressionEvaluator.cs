using System;
using System.Collections.Generic;
using SelectParser.Queries;

namespace SelectQuery.Evaluation
{
    public class ExpressionEvaluator
    {
        public T Evaluate<T>(Expression expression, object obj)
        {
            var value = expression.Match(
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

            // normalize result values, utf8json parses numbers are decimal but we want doubles
            if (value is double dbl) return (T)(object)Convert.ToDecimal(dbl);
            return value;
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
            return EvaluateIdentifier<T>(identifier.Name, obj);
        }

        private T EvaluateIdentifier<T>(string identifier, object obj)
        {
            if (obj is IReadOnlyDictionary<string, object> dict)
            {
                // TODO handle casing?
                return dict.TryGetValue(identifier, out var result) ? (T) result : default;
            }

            throw new NotImplementedException($"don't know how to get identifier ({identifier}) value from {obj?.GetType().FullName ?? "null"}");
        }

        private T EvaluateQualified<T>(Expression.Qualified qualified, object obj)
        {
            var target = EvaluateIdentifier<object>(qualified.Qualification, obj);
            return Evaluate<T>(qualified.Expression, target);
        }

        private T EvaluateUnary<T>(Expression.Unary unary, object obj)
        {
            return (T) (object) (unary.Operator switch
            {
                UnaryOperator.Not => !Evaluate<bool>(unary.Expression, obj),
                UnaryOperator.Negate => -Evaluate<decimal>(unary.Expression, obj),

                _ => throw new ArgumentOutOfRangeException()
            });
        }

        private T EvaluateBinary<T>(Expression.Binary binary, object obj)
        {
            var left = Evaluate<object>(binary.Left, obj);
            var right = Evaluate<object>(binary.Right, obj);

            if (binary.Operator == BinaryOperator.Add)
            {
                return (T) EvaluateAddition(left, right);
            }

            // propagate nulls
            if (left == null || right == null) return default;

            return (T) (object) (binary.Operator switch
            {
                BinaryOperator.And => (bool)left && (bool)right,
                BinaryOperator.Or => (bool)left || (bool)right,
                BinaryOperator.Lesser => (decimal)left < (decimal)right,
                BinaryOperator.Greater => (decimal)left > (decimal)right,
                BinaryOperator.LesserOrEqual => (decimal)left <= (decimal)right,
                BinaryOperator.GreaterOrEqual => (decimal)left >= (decimal)right,

                BinaryOperator.Equal => EvaluateEquality(left, right),
                BinaryOperator.NotEqual => !EvaluateEquality(left, right),

                BinaryOperator.Subtract => (decimal) left - (decimal) right,
                BinaryOperator.Multiply => (decimal) left * (decimal) right,
                BinaryOperator.Divide => (decimal) left / (decimal) right,
                BinaryOperator.Modulo => (decimal) left % (decimal) right,

                _ => throw new ArgumentOutOfRangeException()
            });
        }

        private object EvaluateAddition(object left, object right)
        {
            if (left is decimal leftNum && right is decimal rightNum)
            {
                return leftNum + rightNum;
            }

            if (left is null) return right?.ToString();
            if (right is null) return null;

            return $"{left}{right}";
        }

        private bool EvaluateEquality(object left, object right)
        {
            return Equals(left, right);
        }

        private bool EvaluateBetween(Expression.Between between, object obj)
        {
            throw new NotImplementedException();
        }

        private bool EvaluateIn(Expression.In inExpr, object obj)
        {
            var value = Evaluate<object>(inExpr.Expression, obj);

            foreach (var matchExpr in inExpr.Matches)
            {
                var matchValue = Evaluate<object>(matchExpr, obj);

                if (EvaluateEquality(value, matchValue))
                {
                    return true;
                }
            }

            return false;
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