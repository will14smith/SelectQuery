using System;
using System.Collections.Generic;
using System.Linq;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation
{
    public class ExpressionEvaluator
    {
        public Option<T> Evaluate<T>(Expression expression, object obj)
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
                isNull => (T)(object)EvaluateIsNull(isNull, obj),
                presence => (T)(object)EvaluatePresence(presence, obj),
                inExpr => (T)(object)EvaluateIn(inExpr, obj),
                like => (T)(object)EvaluateLike(like, obj)
            );

            // normalize result values, utf8json parses numbers are decimal but we want doubles
            if (value.Value is double dbl) return (T)(object)Convert.ToDecimal(dbl);
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

        private Option<T> EvaluateIdentifier<T>(Expression.Identifier identifier, object obj)
        {
            if (obj is null)
            {
                return default;
            }

            if (obj is IReadOnlyDictionary<string, object> dict)
            {
                // fast: match exact key
                if (dict.TryGetValue(identifier.Name, out var result))
                    return (T) result;

                if (identifier.CaseSensitive)
                {
                    return new None();
                }

                // slow: try match case-insensitive key, ideally the dictionary would be case insensitive but need some custom deserialization logic to support that
                var entry = dict.FirstOrDefault(x => string.Equals(x.Key, identifier.Name, StringComparison.OrdinalIgnoreCase));
                return entry.Key != null ? (Option<T>) (T) entry.Value : new None();
            }

            throw new NotImplementedException($"don't know how to get identifier ({identifier.Name}) value from {obj?.GetType().FullName ?? "null"}");
        }

        private Option<T> EvaluateQualified<T>(Expression.Qualified qualified, object obj)
        {
            var target = EvaluateIdentifier<object>(qualified.Qualification, obj);

            return target.SelectMany(obj => Evaluate<T>(qualified.Expression, obj));
        }

        private Option<T> EvaluateUnary<T>(Expression.Unary unary, object obj)
        {
            return (unary.Operator switch
            {
                UnaryOperator.Not => Evaluate<bool>(unary.Expression, obj).Select(value => (T) (object) !value),
                UnaryOperator.Negate => Evaluate<decimal>(unary.Expression, obj).Select(value => (T) (object) -value),

                _ => throw new ArgumentOutOfRangeException()
            });
        }

        private T EvaluateBinary<T>(Expression.Binary binary, object obj)
        {
            var leftOpt = Evaluate<object>(binary.Left, obj);
            var rightOpt = Evaluate<object>(binary.Right, obj);

            if (binary.Operator == BinaryOperator.Add)
            {
                return (T) EvaluateAddition(leftOpt, rightOpt);
            }

            var left = leftOpt.Value;
            var right = rightOpt.Value;

            // propagate nulls
            if (leftOpt.IsNone || left == null || rightOpt.IsNone || right == null) return default;

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

        private object EvaluateAddition(Option<object> left, Option<object> right)
        {
            if (left.Value is decimal leftNum && right.Value is decimal rightNum)
            {
                return leftNum + rightNum;
            }

            if (left.IsNone || left.Value is null) return right?.Value?.ToString();
            if (right.IsNone || right.Value is null) return null;

            return $"{left.Value}{right.Value}";
        }

        private bool EvaluateEquality(object left, object right)
        {
            return Equals(left, right);
        }

        private bool EvaluateBetween(Expression.Between between, object obj)
        {
            throw new NotImplementedException();
        }

        private bool EvaluateIsNull(Expression.IsNull isNull, object obj)
        {
            var value = Evaluate<object>(isNull.Expression, obj);

            var hasValue = value.IsSome && value.AsT0 is not null;

            return hasValue == isNull.Negate;
        }
        
        private bool EvaluatePresence(Expression.Presence presence, object obj)
        {
            var value = Evaluate<object>(presence.Expression, obj);

            var isMissing = value.IsNone;

            return isMissing == presence.Negate;
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
        public static Option<T> EvaluateOnTable<T>(this ExpressionEvaluator evaluator, Expression expression, FromClause from, object obj)
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