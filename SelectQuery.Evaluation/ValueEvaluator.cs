using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SelectParser.Queries;
using SimdJsonDotNet.Model;

namespace SelectQuery.Evaluation;

internal static class ValueEvaluator
{
    public static Result Evaluate(DocumentReference document, Expression expression, string tableAlias)
    {
        return expression switch
        {
            Expression.StringLiteral strLiteral => EvaluateStringLiteral(strLiteral),
            Expression.NumberLiteral numLiteral => EvaluateNumberLiteral(numLiteral),
            Expression.BooleanLiteral boolLiteral => EvaluateBooleanLiteral(boolLiteral),

            Expression.Qualified qualified => EvaluateQualified(document, qualified, tableAlias),
            Expression.Binary binary => EvaluateBinary(document, binary, tableAlias),
            
            Expression.In inExpr => EvaluateIn(document, inExpr, tableAlias),
            
            _ => throw new ArgumentOutOfRangeException(nameof(expression), $"unexpected expression type: {expression.GetType().FullName}"),
        };
    }
    
    private static Result EvaluateStringLiteral(Expression.StringLiteral strLiteral) => Result.NewLiteral(strLiteral.Value);
    private static Result EvaluateNumberLiteral(Expression.NumberLiteral numLiteral) => Result.NewLiteral(numLiteral.Value);
    private static Result EvaluateBooleanLiteral(Expression.BooleanLiteral boolLiteral) => Result.NewLiteral(boolLiteral.Value);
    
    private static Result EvaluateQualified(DocumentReference document, Expression.Qualified qualified, string tableAlias)
    {
        if (!AreIdentifiersEqual(qualified.Identifiers[0], tableAlias))
        {
            throw new InvalidOperationException("attempting to lookup data from a different table");
        }

        var obj = document.StartOrResumeObject(); 
        
        for (var i = 1; i < qualified.Identifiers.Count - 1; i++)
        {
            // TODO handle case sensitivity
            if(!obj.TryFindFieldUnordered(qualified.Identifiers[i].Name, out var next))
            {
                return Result.Missing();
            }
            
            obj = next.GetObject(); 
        }
        
        return obj.TryFindFieldUnordered(qualified.Identifiers[qualified.Identifiers.Count - 1].Name, out var value) ? Result.NewValue(value) : Result.Missing();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AreIdentifiersEqual(Expression.Identifier identifier, string match)
    {
        if (identifier.CaseSensitive)
        {
            return identifier.Name == match;
        }

        return string.Equals(identifier.Name, match, StringComparison.OrdinalIgnoreCase);
    }
    
    private static Result EvaluateBinary(DocumentReference document, Expression.Binary binary, string tableAlias)
    {
        var left = Evaluate(document, binary.Left, tableAlias);

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

        var right = Evaluate(document, binary.Right, tableAlias);

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

    private static Result EvaluateIn(DocumentReference document, Expression.In expr, string tableAlias)
    {
        var value = Evaluate(document, expr.Expression, tableAlias);
        
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
            var matchValue = Evaluate(document, matchExpr, tableAlias);
            if (EvaluateEquality(value, matchValue))
            {
                return Result.NewLiteral(true);
            }
        }
            
        return Result.NewLiteral(false);
    }

    [StructLayout(LayoutKind.Explicit)]
    internal ref struct Result
    {
        [FieldOffset(0)] private ResultType _type;

        [FieldOffset(8)] private string _stringLiteral;

        [FieldOffset(16)] private decimal _numberLiteral;
        [FieldOffset(16)] private bool _booleanLiteral;

        [FieldOffset(32)] private Value _value;
        
        public static Result Missing() => new() { _type = ResultType.None };
        public static Result Null() => new() { _type = ResultType.Null };
        
        public static Result NewLiteral(string stringValue) => new() { _type = ResultType.StringLiteral, _stringLiteral = stringValue };
        public static Result NewLiteral(decimal numberValue) => new() { _type = ResultType.NumberLiteral, _numberLiteral = numberValue };
        public static Result NewLiteral(bool booleanValue) => new() { _type = ResultType.BooleanLiteral, _booleanLiteral = booleanValue };
        
        public static Result NewValue(Value value) => new() { _type = ResultType.Value, _value = value };

        public ResultType Type => _type;
        public bool IsString() => _type == ResultType.Value ? _value.Type == JsonType.String : _type == ResultType.StringLiteral;
        public string AsString()
        {
            if (_type == ResultType.Value)
            {
                var valueType = _value.Type;
                if (valueType != JsonType.String)
                {
                    throw new InvalidOperationException($"cannot convert {_type} ({valueType}) to string");
                }

                return _value.GetString();
            }
            
            if (_type != ResultType.StringLiteral)
            {
                throw new InvalidOperationException($"cannot convert {_type} to string");
            }

            return _stringLiteral;
        }
        public decimal AsNumber()
        {
            if (_type == ResultType.Value)
            {
                var valueType = _value.Type;
                if (valueType != JsonType.Number)
                {
                    throw new InvalidOperationException($"cannot convert {_type} ({valueType}) to number");
                }

                return _value.GetDecimal();
            }
            
            if (_type != ResultType.NumberLiteral)
            {
                throw new InvalidOperationException($"cannot convert {_type} to number");
            }

            return _numberLiteral;
        }
        public bool AsBoolean()
        {
            if (_type == ResultType.Value)
            {
                var valueType = _value.Type;
                if (valueType != JsonType.Boolean)
                {
                    throw new InvalidOperationException($"cannot convert {_type} ({valueType}) to boolean");
                }

                return _value.GetBool();
            }
            
            if (_type != ResultType.BooleanLiteral)
            {
                throw new InvalidOperationException($"cannot convert {_type} to boolean");
            }

            return _booleanLiteral;
        }
        public Value AsValue()
        {
            if (_type != ResultType.Value)
            {
                throw new InvalidOperationException($"cannot convert {_type} to json value");
            }

            return _value;
        }
        
        public void Write(JsonRecordWriter writer)
        {
            switch (_type)
            {
                case ResultType.None: throw new InvalidOperationException();
                case ResultType.Null: writer.Write("null"u8); break;

                case ResultType.StringLiteral: WriteString(writer); break;
                case ResultType.NumberLiteral: WriteNumber(writer); break;
                case ResultType.BooleanLiteral: writer.Write(_booleanLiteral ? "true"u8 : "false"u8); break;
                
                case ResultType.Value: WriteValue(writer); break;
                
                default: throw new NotSupportedException($"unexpected result type: {_type}");
            }
        }

        private void WriteString(JsonRecordWriter writer)
        {
            writer.Write((byte)'"');
            // TODO escape if needed
            writer.Write(Encoding.UTF8.GetBytes(_stringLiteral));
            writer.Write((byte)'"');
        }
        
        private void WriteNumber(JsonRecordWriter writer)
        {
            const int maxDecimalLength = 32;
            Span<byte> buffer = stackalloc byte[maxDecimalLength];
            
            if(!Utf8Formatter.TryFormat(_numberLiteral, buffer, out var bytesConsumed) || bytesConsumed == maxDecimalLength)
            {
                throw new NotImplementedException("probably fallback to allocating a string");
            }

#if NETSTANDARD
            writer.Write(buffer.Slice(0, bytesConsumed));
#else
            writer.Write(buffer[..bytesConsumed]);
#endif
        }
        
        private void WriteValue(JsonRecordWriter writer) => writer.Write(_value.GetRawJson());
        
    }
    
    internal enum ResultType
    {
        None = 0,
        Null,
        
        StringLiteral,
        NumberLiteral,
        BooleanLiteral,
        
        Value,
    }
}