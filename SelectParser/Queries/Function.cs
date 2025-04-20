using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectParser.Queries;

public abstract class Function : IEquatable<Function>
{
    public abstract bool Equals(Function? other);
}

public abstract class AggregateFunction : Function, IEquatable<AggregateFunction>
{
    public override bool Equals(Function? other) => Equals(other as AggregateFunction);
    public abstract bool Equals(AggregateFunction? other);
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public class Average(Expression expression) : AggregateFunction, IEquatable<Average>
    {
        public Expression Expression { get; } = expression;

        public override bool Equals(AggregateFunction? other) => Equals(other as Average);
        public bool Equals(Average? other) => other is not null && Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Average other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"AVG({Expression})";
    }

    public class Count(Option<Expression> expression) : AggregateFunction, IEquatable<Count>
    {
        public Option<Expression> Expression { get; } = expression;

        public override bool Equals(AggregateFunction? other) => Equals(other as Count);
        public bool Equals(Count? other) => other is not null && Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Count other && Equals(other);

        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"COUNT({(Expression.IsSome ? Expression : "*")})";
    }

    public class Max(Expression expression) : AggregateFunction, IEquatable<Max>
    {
        public Expression Expression { get; } = expression;

        public override bool Equals(AggregateFunction? other) => Equals(other as Max);
        public bool Equals(Max? other) => other is not null && Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Max other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"MAX({Expression})";
    }

    public class Min(Expression expression) : AggregateFunction, IEquatable<Min>
    {
        public Expression Expression { get; } = expression;

        public override bool Equals(AggregateFunction? other) => Equals(other as Min);
        public bool Equals(Min? other) => other is not null && Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Min other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"MIN({Expression})";
    }

    public class Sum(Expression expression) : AggregateFunction, IEquatable<Sum>
    {
        public Expression Expression { get; } = expression;

        public override bool Equals(AggregateFunction? other) => Equals(other as Sum);
        public bool Equals(Sum? other) => other is not null && Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Sum other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"SUM({Expression})";
    }
}

public class ScalarFunction(Expression.Identifier identifier, IReadOnlyList<Expression> arguments) : Function, IEquatable<ScalarFunction>
{
    public Expression.Identifier Identifier { get; } = identifier;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;

    public override bool Equals(Function? other) => Equals(other as ScalarFunction);
    public bool Equals(ScalarFunction? other) => other is not null && Identifier.Equals(other.Identifier) && Arguments.SequenceEqual(other.Arguments);
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is ScalarFunction other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hc = (Identifier.GetHashCode() * 397) ^ Arguments.Count.GetHashCode();
            
            foreach (var expression in Arguments)
            {
                hc = (hc * 397) ^ expression.GetHashCode();
            }

            return hc;
        }
    }

    public override string ToString() => $"{Identifier.Name}({string.Join(", ", Arguments)})";
}