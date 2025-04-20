using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectParser.Queries;

public abstract class SelectClause : IEquatable<SelectClause>
{
    public abstract bool Equals(SelectClause? other);
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public class Star : SelectClause, IEquatable<Star>
    {
        public override bool Equals(SelectClause? other) => Equals(other as Star);
        public bool Equals(Star? other) => other is not null;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as Star);
        public override int GetHashCode() => 11;

        public static bool operator ==(Star? left, Star? right) => Equals(left, right);
        public static bool operator !=(Star? left, Star? right) => !Equals(left, right);
        
        public override string ToString() => "SELECT *";
    }

    public class List(IReadOnlyList<Column> columns) : SelectClause, IEquatable<List>
    {
        public IReadOnlyList<Column> Columns { get; } = columns;

        public override bool Equals(SelectClause? other) => Equals(other as List);
        public bool Equals(List? other) => other is not null && Columns.SequenceEqual(other.Columns);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as List);
        public override int GetHashCode() => 11;

        public static bool operator ==(List? left, List? right) => Equals(left, right);
        public static bool operator !=(List? left, List? right) => !Equals(left, right);
        
        public override string ToString() => $"SELECT {string.Join(", ", Columns)}";
    }
}