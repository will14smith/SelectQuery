using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SelectParser.Queries;

[GenerateOneOf]
public partial class Function : OneOfBase<AggregateFunction, ScalarFunction>
{
    public override string? ToString() => Value.ToString();
}

[GenerateOneOf]
public partial class AggregateFunction : OneOfBase<AggregateFunction.Average, AggregateFunction.Count,
    AggregateFunction.Max, AggregateFunction.Min, AggregateFunction.Sum>
{
    public override string? ToString() => Value.ToString();

    public class Average(Expression expression)
    {
        public Expression Expression { get; } = expression;

        protected bool Equals(Average other) => Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Average other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"AVG({Expression})";
    }

    public class Count(Option<Expression> expression)
    {
        public Option<Expression> Expression { get; } = expression;

        protected bool Equals(Count other) => Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Count other && Equals(other);

        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"COUNT({(Expression.IsSome ? Expression : "*")})";
    }

    public class Max(Expression expression)
    {
        public Expression Expression { get; } = expression;

        protected bool Equals(Max other) => Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Max other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"MAX({Expression})";
    }

    public class Min(Expression expression)
    {
        public Expression Expression { get; } = expression;

        protected bool Equals(Min other) => Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Min other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"MIN({Expression})";
    }

    public class Sum(Expression expression)
    {
        public Expression Expression { get; } = expression;

        protected bool Equals(Sum other) => Expression.Equals(other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Sum other && Equals(other);
        public override int GetHashCode() => Expression.GetHashCode();

        public override string ToString() => $"SUM({Expression})";
    }
}

public class ScalarFunction(Expression.Identifier identifier, IReadOnlyList<Expression> arguments)
{
    public Expression.Identifier Identifier { get; } = identifier;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;

    protected bool Equals(ScalarFunction other) => Identifier.Equals(other.Identifier) && Arguments.SequenceEqual(other.Arguments);
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