using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SelectParser.Queries;

[GenerateOneOf]
public partial class Function : OneOfBase<AggregateFunction, ScalarFunction>
{
    public override string ToString() => Value.ToString();
}

[GenerateOneOf]
public partial class AggregateFunction : OneOfBase<AggregateFunction.Average, AggregateFunction.Count,
    AggregateFunction.Max, AggregateFunction.Min, AggregateFunction.Sum>
{
    public override string ToString() => Value.ToString();

    public class Average
    {
        public Average(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        protected bool Equals(Average other) => base.Equals(other) && Expression.Equals(other.Expression);

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || obj is Average other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Expression.GetHashCode();
            }
        }

        public override string ToString() => $"AVG({Expression})";
    }

    public class Count
    {
        public Count(Option<Expression> expression)
        {
            Expression = expression;
        }

        public Option<Expression> Expression { get; }

        protected bool Equals(Count other) => base.Equals(other) && Expression.Equals(other.Expression);

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || obj is Count other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Expression.GetHashCode();
            }
        }

        public override string ToString() => $"COUNT({(Expression.IsSome ? Expression : "*")})";
    }

    public class Max
    {
        public Max(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        protected bool Equals(Max other) => base.Equals(other) && Expression.Equals(other.Expression);
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Max other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Expression.GetHashCode();
            }
        }

        public override string ToString() => $"MAX({Expression})";
    }

    public class Min
    {
        public Min(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        protected bool Equals(Min other) => base.Equals(other) && Expression.Equals(other.Expression);
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Min other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Expression.GetHashCode();
            }
        }

        public override string ToString() => $"MIN({Expression})";
    }

    public class Sum
    {
        public Sum(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }

        protected bool Equals(Sum other) => base.Equals(other) && Expression.Equals(other.Expression);
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Sum other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Expression.GetHashCode();
            }
        }

        public override string ToString() => $"SUM({Expression})";
    }
}

public class ScalarFunction
{
    public ScalarFunction(Expression.Identifier identifier, IReadOnlyList<Expression> arguments)
    {
        Identifier = identifier;
        Arguments = arguments;
    }

    public Expression.Identifier Identifier { get; }
    public IReadOnlyList<Expression> Arguments { get; }

    protected bool Equals(ScalarFunction other)
    {
        return Identifier.Equals(other.Identifier) && Arguments.SequenceEqual(other.Arguments);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ScalarFunction other && Equals(other);
    }

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

    public override string ToString()
    {
        return $"{Identifier.Name}({string.Join(", ", Arguments)})";
    }
}