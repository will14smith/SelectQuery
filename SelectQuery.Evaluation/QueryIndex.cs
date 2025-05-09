using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SelectParser;
using SelectParser.Queries;
using SimdJsonDotNet;
using SimdJsonDotNet.Model;
using Array = System.Array;

namespace SelectQuery.Evaluation;

public class QueryIndex(SelectClause select, Option<Expression> where, string tableAlias, int indexCount, ValueIndex index)
{
    // Used for mapping expressions to an index, which will later be used for evaluating the expression against the JSON document using on 2 passes
    // 1. The first pass will be used to map the expression to an index
    // 2. The second pass will be used to evaluate the expression against the JSON document using the index
    
    // SELECT s3.one, s3.two, s3.three FROM s3
    // would be mapped to
    // SELECT $0, $1, $2 FROM s3
    // and an index of
    // s3.{ one -> $0, two -> $1, three -> $2 }
    
    // SELECT s3.a.b, s.a FROM s3
    // would be mapped to
    // SELECT $0, $1 FROM s3
    // and an index of
    // s3.{ a -> ($1, { b -> $0 }) }
    
    // SELECT * FROM s3 WHERE s3.a.b = 'a'
    // would be mapped to
    // SELECT * FROM s3 WHERE $0 = 'a'
    // and an index of
    // s3.{ a -> { b -> $0 } }
    
    public SelectClause Select { get; } = select;
    public Option<Expression> Where { get; } = where;
    public string TableAlias { get; } = tableAlias;
    private byte[] TableAliasUtf8 { get; } = Encoding.UTF8.GetBytes(tableAlias);

    public int IndexCount { get; } = indexCount;
    public ValueIndex Index { get; } = index;

    public static QueryIndex Create(Query query)
    {
        var paths = new Dictionary<Expression.Qualified, int>();
        
        var select = ReplacePaths(query.Select, paths);
        var where = query.Where.Select(x => ReplacePaths(x.Condition, paths));
        var tableAlias = query.From.Alias.Match(alias => alias, _ => "s3object");
        
        var index = BuildIndex(paths);

        return new QueryIndex(select, where, tableAlias, paths.Count, index);
    }
    
    private static SelectClause ReplacePaths(SelectClause select, Dictionary<Expression.Qualified, int> paths) =>
        select switch
        {
            SelectClause.Star star => star,
            SelectClause.List list => ReplacePaths(list, paths),
            _ => throw new ArgumentOutOfRangeException(nameof(select))
        };
    private static SelectClause.List ReplacePaths(SelectClause.List select, Dictionary<Expression.Qualified, int> paths)
    {
        var columns = new List<Column>(select.Columns.Count);
        
        foreach (var column in select.Columns)
        {
            columns.Add(new Column(ReplacePaths(column.Expression, paths), column.Alias));
        }

        return new SelectClause.List(columns);
    }

    private static Expression ReplacePaths(Expression expr, Dictionary<Expression.Qualified, int> paths) =>
        expr switch
        {
            Expression.StringLiteral => expr,
            Expression.NumberLiteral => expr,
            Expression.BooleanLiteral => expr,

            Expression.Qualified qualified => ReplacePaths(qualified, paths),
            Expression.Binary binary => new Expression.Binary(binary.Operator, ReplacePaths(binary.Left, paths), ReplacePaths(binary.Right, paths)),
            Expression.In inExpr => new Expression.In(ReplacePaths(inExpr.Expression, paths), inExpr.Matches.Select(x => ReplacePaths(x, paths)).ToArray()),

            _ => throw new NotImplementedException($"Expression type not supported: {expr.GetType().FullName}")
        };

    private static Expression ReplacePaths(Expression.Qualified qualified, Dictionary<Expression.Qualified, int> paths)
    {
        if (paths.TryGetValue(qualified, out var index))
        {
            return new IndexReference(index, qualified);
        }
        
        index = paths.Count;
        paths.Add(qualified, index);

        return new IndexReference(index, qualified);
    }

    
    private static ValueIndex BuildIndex(IReadOnlyDictionary<Expression.Qualified, int> paths)
    {
        ValueIndex index = new ValueIndex.Object(new Utf8KeyedDictionary<ValueIndex>());
        
        foreach (var pathEntry in paths)
        {
            index = AddToIndex(index, pathEntry.Key.Identifiers, 0, pathEntry.Value);
        }

        return index;
    }

    private static ValueIndex AddToIndex(ValueIndex? index, IReadOnlyList<Expression.Identifier> path, int pathOffset, int pathIndex)
    {
        if (path.Count == pathOffset)
        {
            return new ValueIndex.Tagged(pathIndex, index);
        }

        switch (index)
        {
            case ValueIndex.Object objIndex:
            {
                var properties = new Utf8KeyedDictionary<ValueIndex>(objIndex.Properties);

                // TODO handle casing
                var name = path[pathOffset].Name;
                if (properties.TryGetValue(name, out var existingSubIndex))
                {
                    properties[name] = AddToIndex(existingSubIndex, path, pathOffset + 1, pathIndex);
                }
                else
                {
                    properties[name] = AddToIndex(null, path, pathOffset + 1, pathIndex);
                }

                return new ValueIndex.Object(properties);
            }
            
            case ValueIndex.Tagged tagged:
                return new ValueIndex.Tagged(tagged.Index, AddToIndex(tagged.Inner, path, pathOffset, pathIndex));
            
            case null:
                return new ValueIndex.Object(new Utf8KeyedDictionary<ValueIndex>
                {
                    { path[pathOffset].Name, AddToIndex(null, path, pathOffset + 1, pathIndex) }
                });
            
            default:
                throw new NotImplementedException();
        }
    }

    public DocumentIndex Process(DocumentReference record)
    {
        var capture = ArrayPool<(int Depth, StructurePositionOffset StartPosition)?>.Shared.Rent(IndexCount);
        Array.Clear(capture, 0, capture.Length);
        var remaining = IndexCount;

        if (record.Type != JsonType.Object)
        {
            throw new NotImplementedException();
        }
        var value = record.GetValue();

        if (Index is not ValueIndex.Object rootIndex)
        {
            throw new NotImplementedException();
        }
        
        // TODO get table alias
        if (rootIndex.Properties.TryGetValue(TableAliasUtf8, out var tableIndex))
        {
            Process(capture, ref remaining, tableIndex!, value);
        }

        return new DocumentIndex(capture);
    }
    
    private void Process((int Depth, StructurePositionOffset StartPosition)?[] captured, ref int remaining, ValueIndex index, Value value)
    {
        switch (index)
        {
            case ValueIndex.Object objectIndex:
                var obj = value.GetObject();
                foreach (var field in obj)
                {
                    if (remaining == 0)
                    {
                        return;
                    }
                    
                    // TODO can we avoid the string allocation?
                    var key = field.Key.Buffer;
                    var keyLength = key.IndexOf((byte) '"');
                    
                    if (objectIndex.Properties.TryGetValue(key.Slice(0, keyLength), out var subIndex))
                    {
                        Process(captured, ref remaining, subIndex!, field.Value);
                    }
                }
                break;
            
            case ValueIndex.Tagged tagged:
                captured[tagged.Index] = (value.Depth, value.StartPosition);
                remaining--;

                if (remaining == 0)
                {
                    return;
                }
                
                if (tagged.Inner != null)
                {
                    Process(captured, ref remaining, tagged.Inner, value);
                }
                break;
            
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}

public readonly struct DocumentIndex((int Depth, StructurePositionOffset StartPosition)?[] index) : IDisposable
{
    private readonly (int Depth, StructurePositionOffset StartPosition)?[] _index = index;

    public bool TryGetValue(DocumentReference document, int index, out Value value)
    {
        var position = _index[index];
        if (position == null)
        {
            value = default;
            return false;
        }

        value = new Value(new ValueIterator(document, position.Value.Depth, position.Value.StartPosition));
        return true;
    }
    
    public void Dispose()
    {
        ArrayPool<(int Depth, StructurePositionOffset StartPosition)?>.Shared.Return(_index);
    }
} 

public abstract class ValueIndex
{
    public class Tagged(int index, ValueIndex? inner) : ValueIndex
    {
        public int Index { get; } = index;
        public ValueIndex? Inner { get; } = inner;
    }

    public class Object(Utf8KeyedDictionary<ValueIndex> properties) : ValueIndex
    {
        public Utf8KeyedDictionary<ValueIndex> Properties { get; } = properties;
    }   
}

public class IndexReference(int index, Expression original) : Expression, IEquatable<IndexReference>
{
    public int Index { get; } = index;
    public Expression Original { get; } = original;

    public override bool Equals(Expression? other) => Equals(other as IndexReference);
    public bool Equals(IndexReference? other) => other is not null && Index == other.Index;
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is IndexReference other && Equals(other);
    public override int GetHashCode() => Index.GetHashCode();
    
    public override string ToString() => $"${Index}";
}