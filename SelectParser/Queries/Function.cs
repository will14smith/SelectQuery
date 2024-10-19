using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectParser.Queries;

public abstract class Function;

public abstract class AggregateFunction : Function
{
    public class Average(Expression expression) : AggregateFunction, IEquatable<Average>
    {
        public Expression Expression { get; } = expression;

        public bool Equals(Average? other) => Expression.Equals(other?.Expression);

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Average other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"AVG({Expression})";
    }

    public class Count(Option<Expression> expression) : AggregateFunction, IEquatable<Count>
    {
        public Option<Expression> Expression { get; } = expression;

        public bool Equals(Count? other) => Expression.Equals(other?.Expression);

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Count other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"COUNT({(Expression.IsSome ? Expression : "*")})";
    }

    public class Max(Expression expression) : AggregateFunction, IEquatable<Max>
    {
        public Expression Expression { get; } = expression;

        public bool Equals(Max? other) => Expression.Equals(other?.Expression);
        
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Max other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"MAX({Expression})";
    }

    public class Min(Expression expression) : AggregateFunction, IEquatable<Min>
    {
        public Expression Expression { get; } = expression;

        public bool Equals(Min? other) => Expression.Equals(other?.Expression);
        
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Min other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"MIN({Expression})";
    }

    public class Sum(Expression expression) : AggregateFunction, IEquatable<Sum>
    {
        public Expression Expression { get; } = expression;

        public bool Equals(Sum? other) => Expression.Equals(other?.Expression);
        
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Sum other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"SUM({Expression})";
    }
}

public class ScalarFunction(Expression.Identifier identifier, IReadOnlyList<Expression> arguments) : Function, IEquatable<ScalarFunction>
{
    public Expression.Identifier Identifier { get; } = identifier;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;

    public bool Equals(ScalarFunction? other) => Identifier.Equals(other?.Identifier) && Arguments.SequenceEqual(other.Arguments);

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