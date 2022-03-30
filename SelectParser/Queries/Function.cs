using OneOf;

namespace SelectParser.Queries
{
    [GenerateOneOf]
    public partial class Function : OneOfBase<AggregateFunction>
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
            protected bool Equals(Count other) => base.Equals(other);

            public override bool Equals(object obj) =>
                ReferenceEquals(this, obj) || obj is Count other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return base.GetHashCode() * 397;
                }
            }

            public override string ToString() => $"COUNT(*)";
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
}