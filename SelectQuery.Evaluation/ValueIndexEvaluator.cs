using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SelectParser.Queries;
using SimdJsonDotNet.Model;

using Result = SelectQuery.Evaluation.ValueEvaluator.Result;
using ResultType = SelectQuery.Evaluation.ValueEvaluator.ResultType;

namespace SelectQuery.Evaluation;

internal static class ValueIndexEvaluator
{
    public static Result Evaluate(DocumentReference document, DocumentIndex documentIndex, Expression expression)
    {
        return expression switch
        {
            Expression.StringLiteral strLiteral => EvaluateStringLiteral(strLiteral),
            Expression.NumberLiteral numLiteral => EvaluateNumberLiteral(numLiteral),
            Expression.BooleanLiteral boolLiteral => EvaluateBooleanLiteral(boolLiteral),

            IndexReference indexReference => EvaluateIndexReference(document, documentIndex, indexReference),
            Expression.Qualified => throw new NotSupportedException(),
            Expression.Binary binary => EvaluateBinary(document, documentIndex, binary),
            
            Expression.In inExpr => EvaluateIn(document, documentIndex, inExpr),
            
            _ => throw new ArgumentOutOfRangeException(nameof(expression), $"unexpected expression type: {expression.GetType().FullName}"),
        };
    }
    
    private static Result EvaluateStringLiteral(Expression.StringLiteral strLiteral) => Result.NewLiteral(strLiteral.Value);
    private static Result EvaluateNumberLiteral(Expression.NumberLiteral numLiteral) => Result.NewLiteral(numLiteral.Value);
    private static Result EvaluateBooleanLiteral(Expression.BooleanLiteral boolLiteral) => Result.NewLiteral(boolLiteral.Value);
    
    private static Result EvaluateIndexReference(DocumentReference document, DocumentIndex documentIndex, IndexReference indexReference) => 
        documentIndex.TryGetValue(document, indexReference.Index, out var value) ? Result.NewValue(value) : Result.Missing();

    private static Result EvaluateBinary(DocumentReference document, DocumentIndex documentIndex, Expression.Binary binary)
    {
        var left = Evaluate(document, documentIndex, binary.Left);

        // check for short circuit
        if (binary.Operator is BinaryOperator.And or BinaryOperator.Or)
        {
            var leftValue = left.AsBoolean();
            if (binary.Operator == BinaryOperator.And)
            {
                if (!leftValue)
                {
                    return Result.NewLiteral(false);
                }
            }
            else if (leftValue)
            {
                return Result.NewLiteral(true);
            }
        }

        var right = Evaluate(document, documentIndex, binary.Right);

        return binary.Operator switch
        {
            BinaryOperator.Lesser => Result.NewLiteral(left.AsNumber() < right.AsNumber()),
            BinaryOperator.Greater => Result.NewLiteral(left.AsNumber() > right.AsNumber()),
            BinaryOperator.LesserOrEqual => Result.NewLiteral(left.AsNumber() <= right.AsNumber()),
            BinaryOperator.GreaterOrEqual => Result.NewLiteral(left.AsNumber() >= right.AsNumber()),
            
            BinaryOperator.Equal => Result.NewLiteral(EvaluateEquality(left, right)),
            BinaryOperator.NotEqual => Result.NewLiteral(!EvaluateEquality(left, right)),
            
            // we've already checked the left, so just need to look at the right
            BinaryOperator.And or BinaryOperator.Or => Result.NewLiteral(right.AsBoolean()),
            _ => throw new NotImplementedException($"operator not implemented: {binary.Operator}")
        };
    }

    private static bool EvaluateEquality(Result left, Result right)
    {
        if (left.Type == right.Type)
        {
            return EvaluateEqualitySameType(left, right);
        }
        
        if (right.Type == ResultType.Value)
        {
            // move the json value to the left side
            var temp = right;
            right = left;
            left = temp;
        }

        if (left.Type != ResultType.Value)
        {
            // types are different but neither is a json value so don't try to compare
            return false;
        }
        
        var leftValue = left.AsValue();
        
        switch (right.Type)
        {
            case ResultType.None: throw new InvalidOperationException("cannot compare to none");
            
            case ResultType.StringLiteral:
                if (leftValue.Type != JsonType.String)
                {
                    return false;
                }
                
                // TODO could probably avoid the allocation
                return leftValue.GetString() == right.AsString();
                
            case ResultType.NumberLiteral: 
                if (leftValue.Type != JsonType.Number)
                {
                    return false;
                }
                
                return leftValue.GetDecimal() == right.AsNumber();
            
            case ResultType.BooleanLiteral: 
                if (leftValue.Type != JsonType.Boolean)
                {
                    return false;
                }
                
                return leftValue.GetBool() == right.AsBoolean();
            
            case ResultType.Value: throw new InvalidOperationException("should never get here");
            default: throw new NotSupportedException($"cannot compare json value to {right.Type}");
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EvaluateEqualitySameType(Result left, Result right)
    {
        if (left.Type == ResultType.StringLiteral)
        {
            return left.AsString() == right.AsString();
        }
        if (left.Type == ResultType.NumberLiteral)
        {
            return left.AsNumber() == right.AsNumber();
        }
        if (left.Type == ResultType.BooleanLiteral)
        {
            return left.AsBoolean() == right.AsBoolean();
        }
        
        if (left.Type != ResultType.Value)
        {
            throw new NotImplementedException($"comparing {left.Type}");
        }
        
        var leftValue = left.AsValue();
        var rightValue = right.AsValue();
        
        if (leftValue.Type != rightValue.Type)
        {
            return false;
        }
        
        return leftValue.Type switch
        {
            JsonType.String => leftValue.GetString() == rightValue.GetString(),
            JsonType.Number => leftValue.GetDecimal() == rightValue.GetDecimal(),
            JsonType.Boolean => leftValue.GetBool() == rightValue.GetBool(),
            
            _ => throw new NotSupportedException($"cannot compare json values of type {leftValue.Type}"),
        };
    }

    private static Result EvaluateIn(DocumentReference document, DocumentIndex documentIndex, Expression.In expr)
    {
        var value = Evaluate(document, documentIndex, expr.Expression);
        
        if (expr.StringMatches is not null)
        {
            if (!value.IsString())
            {
                return Result.NewLiteral(false);
            }

            var hasMatch = expr.StringMatches.Contains(value.AsString());
            return Result.NewLiteral(hasMatch);
        }

        foreach (var matchExpr in expr.Matches)
        {
            var matchValue = Evaluate(document, documentIndex, matchExpr);
            if (EvaluateEquality(value, matchValue))
            {
                return Result.NewLiteral(true);
            }
        }
            
        return Result.NewLiteral(false);
    }
}