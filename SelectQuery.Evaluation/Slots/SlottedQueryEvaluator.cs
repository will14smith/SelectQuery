using System;
using SelectParser;
using SelectParser.Queries;
using Utf8Json;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQueryEvaluator
    {
        public static SlottedQueryEvaluation Evaluate(SlottedQuery query, ref JsonReader reader)
        {
            var slottedReader = new SlottedQueryReader(query);

            slottedReader.Read(ref reader);
            
            return new SlottedQueryEvaluation(slottedReader.Slots);
        }
        
        public static bool EvaluatePredicate(SlottedQuery query, SlottedQueryEvaluation evaluation)
        {
            if (query.SlottedPredicate.IsNone)
            {
                return true;
            }

            var result = Evaluate<bool>(query.SlottedPredicate.AsT0, evaluation);

            return !result.IsNone && result.AsT0;
        } 
        
        public static void Write(ref JsonWriter writer, SlottedQuery query, SlottedQueryEvaluation evaluation)
        {
            if (query.SlottedSelect is SlottedQueryBuilder.StarSlot star)
            {
                SlottedQueryWriter.Write(ref writer, evaluation.Slots[star.Slotted.Index]);
            }
            else
            {
                var columns = query.SlottedSelect.AsT1.Columns;

                writer.WriteBeginObject();

                var hasWritten = false;
                foreach (var column in columns)
                {
                    var result = EvaluateColumn(column.Expression, evaluation);
                    if (result.IsNone)
                    {
                        continue;
                    }

                    if (hasWritten)
                    {
                        writer.WriteValueSeparator();
                    }
                    hasWritten = true;

                    writer.WritePropertyName(column.Alias.AsT0);
                    SlottedQueryWriter.Write(ref writer, result.AsT0);
                }

                writer.WriteEndObject();
            }
        }

        private static Option<SlottedQueryEvaluation.Slot> EvaluateColumn(Expression expression, SlottedQueryEvaluation evaluation)
        {
            return expression switch {
                SlottedExpression slot => evaluation.Slots[slot.Index] switch
                {
                    null => Option.None,
                    var x => Option.Some(x),
                },
                _ => Evaluate<object>(expression, evaluation).Select(x => (SlottedQueryEvaluation.Slot) new SlottedQueryEvaluation.Slot.ValueSlot(x)),
            };
        }

        private static Option<T> Evaluate<T>(Expression expression, SlottedQueryEvaluation evaluation)
        {
            return expression switch
            {
                SlottedExpression slot => evaluation.Slots[slot.Index] != null ? Option.Some((T)((SlottedQueryEvaluation.Slot.ValueSlot)evaluation.Slots[slot.Index]).Value) : Option.None,
                Expression.BooleanLiteral lit => (T) (object) lit.Value,
                Expression.NumberLiteral lit => (T) (object) lit.Value,
                Expression.StringLiteral lit => (T) (object) lit.Value,
                
                Expression.Binary binary => EvaluateBinary<T>(binary, evaluation),
                Expression.Presence presence => (T) (object) (Evaluate<object>(presence.Expression, evaluation).IsSome ? !presence.Negate : presence.Negate),
                Expression.In @in => (T) (object) (EvaluateIn(@in, evaluation)),
                
                _ => throw new NotImplementedException($"handle complex expressions: {expression.GetType().FullName}")
            };
        }
        
        private static T EvaluateBinary<T>(Expression.Binary binary, SlottedQueryEvaluation obj)
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

        private static object EvaluateAddition(Option<object> left, Option<object> right)
        {
            if (left.Value is decimal leftNum && right.Value is decimal rightNum)
            {
                return leftNum + rightNum;
            }

            if (left.IsNone || left.Value is null) return right?.Value?.ToString();
            if (right.IsNone || right.Value is null) return null;

            return $"{left.Value}{right.Value}";
        }

        private static bool EvaluateEquality(object left, object right)
        {
            return Equals(left, right);
        }
        
        private static bool EvaluateIn(Expression.In @in, SlottedQueryEvaluation evaluation)
        {
            var value = Evaluate<object>(@in.Expression, evaluation);

            foreach (var matchExpr in @in.Matches)
            {
                var matchValue = Evaluate<object>(matchExpr, evaluation);

                if (EvaluateEquality(value, matchValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}