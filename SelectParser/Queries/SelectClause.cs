using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectParser.Queries;

public abstract class SelectClause
{
    public class Star : SelectClause, IEquatable<Star>
    {
        public override string ToString() => "SELECT *";

        public bool Equals(Star? other) => other is not null;

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as Star);
        public override int GetHashCode() => 11;

        public static bool operator ==(Star? left, Star? right) => Equals(left, right);
        public static bool operator !=(Star? left, Star? right) => !Equals(left, right);
    }

    public class List(IReadOnlyList<Column> columns) : SelectClause, IEquatable<List>
    {
        public IReadOnlyList<Column> Columns { get; } = columns;

        public override string ToString() => $"SELECT {string.Join(", ", Columns)}";
        
        public bool Equals(List? other) => other is not null && Columns.SequenceEqual(other.Columns);

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as List);
        public override int GetHashCode() => 11;

        public static bool operator ==(List? left, List? right) => Equals(left, right);
        public static bool operator !=(List? left, List? right) => !Equals(left, right);

    }
}