using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SelectParser.Queries;
using SimdJsonDotNet.Memory;
using SimdJsonDotNet.Model;

namespace SelectQuery.Evaluation;

internal abstract class JsonRecordWriter : IDisposable
{
    private readonly MemoryStream _stream = new();
    
    public static JsonRecordWriter Create(Query query) =>
        query.Select switch
        {
            SelectClause.Star => new StarWriter(),
            SelectClause.List list => new ListWriter(query, list),
            _ => throw new ArgumentOutOfRangeException(nameof(query))
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRow() => _stream.WriteByte((byte) '{');
    public abstract void WriteColumn(int index, ValueEvaluator.Result value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndRow() => Write("}\n"u8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value) => _stream.WriteByte(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> buffer)
    {
#if NETSTANDARD
        var length = buffer.Length;
        var tempBuffer = ArrayPool<byte>.Shared.Rent(length);
        
        try
        {
            buffer.CopyTo(tempBuffer);

            _stream.Write(tempBuffer, 0, length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
#else 
        _stream.Write(buffer);
#endif

    }
    
    public abstract void EvaluateSelect(DocumentReference record);

    public virtual void Dispose()
    {
        _stream.Dispose();
    }

    public byte[] ToArray() => _stream.ToArray();

    private class StarWriter : JsonRecordWriter
    {
        public override void WriteColumn(int index, ValueEvaluator.Result value) => throw new NotSupportedException();
        public override void EvaluateSelect(DocumentReference record)
        {
            var rawJson = record.GetRawJson();
            Write(rawJson);
            
            // since the raw json potentially includes trailing whitespace, we'll only add it if needed
            if (rawJson[rawJson.Length - 1] != '\n')
            {
                _stream.WriteByte((byte)'\n');
            }
        }
    }

    private class ListWriter : JsonRecordWriter
    {
        private readonly string _tableAlias;
        
        private readonly List<ColumnMetadata> _list;
        private readonly PooledList<byte> _metadataBuffer;

        public ListWriter(Query query, SelectClause.List list)
        {
            _tableAlias = query.From.Alias.Match(alias => alias, _ => "s3object");
            (_metadataBuffer, _list) = BuildBuffer(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteColumn(int index, ValueEvaluator.Result value)
        {
            if (value.Type is ValueEvaluator.ResultType.None)
            {
                return;
            }

            _list[index].WriteStart(_metadataBuffer, this);
            value.Write(this);
        }

        public override void EvaluateSelect(DocumentReference record)
        {
            BeginRow();

            for (var index = 0; index < _list.Count; index++)
            {
                // TODO pre-calculate any constant operations
                var value = ValueEvaluator.Evaluate(record, _list[index].Expression, _tableAlias);
                WriteColumn(index, value);
            }

            EndRow();
        }

        private static (PooledList<byte>, List<ColumnMetadata>) BuildBuffer(SelectClause.List list)
        {
            var buffer = new PooledList<byte>();
            var metadata = new List<ColumnMetadata>();

            for (var index = 0; index < list.Columns.Count; index++)
            {
                var column = list.Columns[index];
                var name = GetColumnName(index, column);

                var start = buffer.Length;
                
                if (index > 0)
                {
                    buffer.AddRange(",\""u8);
                }
                else
                {
                    buffer.Add((byte)'"');
                }

                // TODO escape if needed
                buffer.AddRange(Encoding.UTF8.GetBytes(name));
                buffer.AddRange("\":"u8);
                
                var metadataEntry = new ColumnMetadata(column.Expression, start, buffer.Length - start);
                metadata.Add(metadataEntry);
            }
            
            return (buffer, metadata);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _metadataBuffer.Dispose();
        }
        
        private readonly struct ColumnMetadata(Expression expression, int startIndex, int startLength)
        {
            public Expression Expression { get; } = expression;

            public void WriteStart(PooledList<byte> buffer, ListWriter writer) => writer.Write(buffer.AsSpan(startIndex, startLength));
        }
    }
    
    internal static string GetColumnName(int index, Column column) => 
        column.Alias.IsSome ? column.Alias.Value! : GetColumnName(index, column.Expression);

    private static string GetColumnName(int index, Expression expression) =>
        expression switch
        {
            Expression.Identifier identifier => identifier.Name,
            Expression.Qualified qualified => qualified.Identifiers[qualified.Identifiers.Count - 1].Name,
            // default is _N for the Nth column (1 indexed)
            _ => $"_{index + 1}"
        };
}