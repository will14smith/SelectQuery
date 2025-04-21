using System;
using System.IO;
using System.Text;
using SelectParser.Queries;
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

    public abstract void EvaluateSelect(DocumentReference record);

    public virtual void Dispose()
    {
        _stream.Dispose();
    }

    public byte[] ToArray() => _stream.ToArray();

    private class StarWriter : JsonRecordWriter
    {
        public override void EvaluateSelect(DocumentReference record)
        {
            var rawJson = record.GetRawJson();
            _stream.Write(rawJson);
            
            // since the raw json potentially includes trailing whitespace, we'll only add it if needed
            if (rawJson[^1] != '\n')
            {
                _stream.WriteByte((byte)'\n');
            }
        }
    }

    private class ListWriter(Query query, SelectClause.List list) : JsonRecordWriter
    {
        public override void EvaluateSelect(DocumentReference record)
        {
            _stream.WriteByte((byte) '{');

            for (var index = 0; index < list.Columns.Count; index++)
            {
                var column = list.Columns[index];
                
                // TODO pre-calculate all this
                if (index > 0)
                {
                    _stream.Write(",\""u8);
                }
                else
                {
                    _stream.WriteByte((byte)'"');
                }
                var name = GetColumnName(index, column);
                // TODO escape if needed
                _stream.Write(Encoding.UTF8.GetBytes(name));
                _stream.Write("\":"u8);
                
                // TODO pre-calculate any constant operations
                var value = ValueEvaluator.Evaluate(record, column.Expression);
                value.Write(_stream);
            }

            _stream.Write("}\n"u8);
        }
    }
    
    internal static string GetColumnName(int index, Column column) => 
        column.Alias.IsSome ? column.Alias.Value : GetColumnName(index, column.Expression);

    private static string GetColumnName(int index, Expression expression) =>
        expression switch
        {
            Expression.Identifier identifier => identifier.Name,
            Expression.Qualified qualified => qualified.Identifiers[^1].Name,
            // default is _N for the Nth column (1 indexed)
            _ => $"_{index + 1}"
        };
}