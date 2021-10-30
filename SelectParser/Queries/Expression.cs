using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SelectParser.Queries
{
    public abstract class Expression : OneOfBase<Expression.StringLiteral, Expression.NumberLiteral, Expression.BooleanLiteral, Expression.Identifier, Expression.Qualified, Expression.Unary, Expression.Binary, Expression.Between, Expression.Presence, Expression.In, Expression.Like>
    {
        public abstract override string ToString();

        public class StringLiteral : Expression
        {
            public StringLiteral(string value) => Value = value;
            public new string Value { get; }

            public bool Equals(StringLiteral other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is StringLiteral other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (base.GetHashCode() * 397) ^ (Value?.GetHashCode() ?? 0);
                }
            }

            public override string ToString()
            {
                // TODO handle escaping
                return $"'{Value}'";
            }
        }
        public class NumberLiteral : Expression
        {
            public NumberLiteral(decimal value) => Value = value;
            public new decimal Value { get; }

            public bool Equals(NumberLiteral other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is NumberLiteral other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public override string ToString()
            {
                return $"{Value}";
            }
        }
        public class BooleanLiteral : Expression
        {
            public BooleanLiteral(bool value) => Value = value;
            public new bool Value { get; }

            public bool Equals(BooleanLiteral other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is BooleanLiteral other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public override string ToString()
            {
                return Value ? "true" : "false";
            }
        }

        public class Identifier : Expression
        {
            public Identifier(string name, bool caseSensitive)
            {
                Name = name;
                CaseSensitive = caseSensitive;
            }

            public string Name { get; }
            public bool CaseSensitive { get; }

            protected bool Equals(Identifier other)
            {
                return Name == other.Name && CaseSensitive == other.CaseSensitive;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Identifier other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = base.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ CaseSensitive.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(Identifier left, Identifier right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Identifier left, Identifier right)
            {
                return !Equals(left, right);
            }

            public override string ToString()
            {
                // TODO handle quoting
                return Name;
            }
        }
        public class Qualified : Expression
        {
            public Qualified(params Identifier[] identifiers)
            {
                Identifiers = identifiers;
            }
            
            public Qualified(IReadOnlyList<Identifier> identifiers)
            {
                Identifiers = identifiers;
            }

            public IReadOnlyList<Identifier> Identifiers { get; }
            public Identifier LastIdentifier => Identifiers[Identifiers.Count - 1];

            protected bool Equals(Qualified other)
            {
                return Identifiers.SequenceEqual(other.Identifiers);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Qualified other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return Identifiers.Aggregate(0, (acc, x) => x.GetHashCode() ^ (acc * 397));
                }
            }

            public override string ToString()
            {
                return string.Join(".", Identifiers);
            }
        }

        public class Unary : Expression
        {
            public Unary(UnaryOperator @operator, Expression expression)
            {
                Operator = @operator;
                Expression = expression;
            }

            public UnaryOperator Operator { get; }
            public Expression Expression { get; }

            protected bool Equals(Unary other)
            {
                return Operator == other.Operator && Equals(Expression, other.Expression);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Unary other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Operator * 397) ^ (Expression?.GetHashCode() ?? 0);
                }
            }

            public override string ToString()
            {
                var op = Operator switch
                {
                    UnaryOperator.Not => "!",
                    UnaryOperator.Negate => "-"
                };

                // TODO precedence
                return $"({op}{Expression})";
            }
        }
        public class Binary : Expression
        {
            public Binary(BinaryOperator @operator, Expression left, Expression right)
            {
                Operator = @operator;
                Left = left;
                Right = right;
            }

            public BinaryOperator Operator { get; }
            public Expression Left { get; }
            public Expression Right { get; }

            protected bool Equals(Binary other)
            {
                return Operator == other.Operator && Equals(Left, other.Left) && Equals(Right, other.Right);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Binary other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((((int)Operator * 397) ^ (Left?.GetHashCode() ?? 0)) * 397) ^ (Right?.GetHashCode() ?? 0);
                }
            }

            public override string ToString()
            {
                var op = Operator switch
                {
                    BinaryOperator.And => "AND",
                    BinaryOperator.Or => "OR",

                    BinaryOperator.Lesser => "<",
                    BinaryOperator.Greater => ">",
                    BinaryOperator.LesserOrEqual => "<=",
                    BinaryOperator.GreaterOrEqual => ">=",
                    BinaryOperator.Equal => "==",
                    BinaryOperator.NotEqual => "<>",

                    BinaryOperator.Add => "+",
                    BinaryOperator.Subtract => "-",
                    BinaryOperator.Multiply => "*",
                    BinaryOperator.Divide => "/",
                    BinaryOperator.Modulo => "%",
                };

                // TODO precedence
                return $"({Left} {op} {Right})";
            }
        }
        public class Between : Expression
        {
            public Between(bool negate, Expression expression, Expression lower, Expression upper)
            {
                Negate = negate;
                Expression = expression;
                Lower = lower;
                Upper = upper;
            }

            public bool Negate { get; }
            public Expression Expression { get; }
            public Expression Lower { get; }
            public Expression Upper { get; }

            protected bool Equals(Between other)
            {
                return Negate == other.Negate && Equals(Expression, other.Expression) && Equals(Lower, other.Lower) && Equals(Upper, other.Upper);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Between other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Negate.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Expression != null ? Expression.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Lower != null ? Lower.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Upper != null ? Upper.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public override string ToString()
            {
                // TODO bracketing?
                return $"{Expression}{(Negate ? " NOT" : "")} BETWEEN {Lower} AND {Upper}";
            }
        }

        public class Presence : Expression
        {
            public Presence(Expression expression, bool negate)
            {
                Expression = expression;
                Negate = negate;
            }

            public Expression Expression { get; }
            public bool Negate { get; }

            protected bool Equals(Presence other)
            {
                return base.Equals(other) && Equals(Expression, other.Expression) && Negate == other.Negate;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Presence other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = base.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Expression?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ Negate.GetHashCode();
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"{Expression} IS {(Negate ? "" : "NOT ")}MISSING";
            }
        }
        public class In : Expression
        {
            public In(Expression expression, IReadOnlyList<Expression> matches)
            {
                Expression = expression;
                Matches = matches;
            }

            public Expression Expression { get; }
            public IReadOnlyList<Expression> Matches { get; }

            protected bool Equals(In other)
            {
                return base.Equals(other) && Equals(Expression, other.Expression) && Matches.SequenceEqual(other.Matches);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is In other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = base.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Expression?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ (Matches?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"{Expression} IN ({string.Join(", ", Matches)})";
            }
        }
        public class Like : Expression
        {
            public Like(Expression expression, Expression pattern, Option<Expression> escape)
            {
                Expression = expression;
                Pattern = pattern;
                Escape = escape;
            }

            public Expression Expression { get; }
            public Expression Pattern { get; }
            public Option<Expression> Escape { get; }

            protected bool Equals(Like other)
            {
                return Equals(Expression, other.Expression) && Equals(Pattern, other.Pattern) && Equals(Escape, other.Escape);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Like other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Expression?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ (Pattern?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ (Escape?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return Escape.IsNone ? $"{Expression} LIKE {Pattern}" : $"{Expression} LIKE {Pattern} ESCAPE {Escape.AsT0}";
            }
        }
    }
}