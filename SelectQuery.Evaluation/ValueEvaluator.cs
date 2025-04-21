using System;
using System.Buffers.Text;
using System.IO;
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

        var obj = document.GetObject(); 
        
        for (var i = 1; i < qualified.Identifiers.Count - 1; i++)
        {
            // TODO handle case sensitivity
            obj = obj[qualified.Identifiers[i].Name].GetObject(); 
        }
        
        var value = obj[qualified.Identifiers[^1].Name];
        return Result.NewValue(value);
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
        var right = Evaluate(document, binary.Right, tableAlias);

        if (binary.Operator == BinaryOperator.Equal)
        {
            return Result.NewLiteral(EvaluateEquality(left, right));
        }
        
        throw new NotImplementedException();
    }

    private static bool EvaluateEquality(Result left, Result right)
    {
        if (left.Type == right.Type)
        {
            throw new NotImplementedException($"compare same types: {left.Type}");
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
                throw new NotImplementedException();
            
            case ResultType.Value: throw new InvalidOperationException("should never get here");
            default: throw new NotSupportedException($"cannot compare json value to {right.Type}");
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal ref struct Result
    {
        [FieldOffset(0)] private ResultType _type;

        [FieldOffset(8)] private string _stringLiteral;

        [FieldOffset(16)] private decimal _numberLiteral;
        [FieldOffset(16)] private bool _booleanLiteral;

        [FieldOffset(32)] private Value _value;
        
        public static Result NewLiteral(string stringValue) => new() { _type = ResultType.StringLiteral, _stringLiteral = stringValue };
        public static Result NewLiteral(decimal numberValue) => new() { _type = ResultType.NumberLiteral, _numberLiteral = numberValue };
        public static Result NewLiteral(bool booleanValue) => new() { _type = ResultType.BooleanLiteral, _booleanLiteral = booleanValue };
        
        public static Result NewValue(Value value) => new() { _type = ResultType.Value, _value = value };

        public ResultType Type => _type;
        public string AsString()
        {
            if (_type != ResultType.StringLiteral)
            {
                throw new InvalidOperationException($"cannot convert {_type} to boolean");
            }

            return _stringLiteral;
        }
        public decimal AsNumber()
        {
            if (_type != ResultType.NumberLiteral)
            {
                throw new InvalidOperationException($"cannot convert {_type} to boolean");
            }

            return _numberLiteral;
        }
        public bool AsBoolean()
        {
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
        
        public void Write(Stream writer)
        {
            switch (_type)
            {
                case ResultType.None: throw new InvalidOperationException();
                
                case ResultType.StringLiteral: WriteString(writer); break;
                case ResultType.NumberLiteral: WriteNumber(writer); break;
                case ResultType.BooleanLiteral: writer.Write(_booleanLiteral ? "true"u8 : "false"u8); break;
                
                case ResultType.Value: WriteValue(writer); break;
                
                default: throw new NotSupportedException($"unexpected result type: {_type}");
            }
        }

        private void WriteString(Stream writer)
        {
            writer.WriteByte((byte)'"');
            // TODO escape if needed
            writer.Write(Encoding.UTF8.GetBytes(_stringLiteral));
            writer.WriteByte((byte)'"');
        }
        
        private void WriteNumber(Stream writer)
        {
            const int maxDecimalLength = 32;
            Span<byte> buffer = stackalloc byte[maxDecimalLength];
            
            if(!Utf8Formatter.TryFormat(_numberLiteral, buffer, out var bytesConsumed) || bytesConsumed == maxDecimalLength)
            {
                throw new NotImplementedException("probably fallback to allocating a string");
            }
            
            writer.Write(buffer[..bytesConsumed]);
        }
        
        private void WriteValue(Stream writer) => writer.Write(_value.GetRawJson());
    }
    
    internal enum ResultType
    {
        None = 0,
        
        StringLiteral,
        NumberLiteral,
        BooleanLiteral,
        
        Value,
    }
}