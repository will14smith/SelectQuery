using System;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SelectParser.Queries;

[GenerateOneOf]
public partial class Expression : OneOfBase<Expression.StringLiteral, Expression.NumberLiteral, Expression.BooleanLiteral, Expression.Identifier, Expression.Qualified, Expression.FunctionExpression, Expression.Unary, Expression.Binary, Expression.Between, Expression.IsNull, Expression.Presence, Expression.In, Expression.Like>
{
    public override string? ToString() => Value.ToString();

    public class StringLiteral(string value)
    {
        public string Value { get; } = value;

        public bool Equals(StringLiteral? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is StringLiteral other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        // TODO handle escaping
        public override string ToString() => $"'{Value}'";
    }
    public class NumberLiteral(decimal value)
    {
        public decimal Value { get; } = value;

        public bool Equals(NumberLiteral? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is NumberLiteral other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"{Value}";
    }
    public class BooleanLiteral(bool value)
    {
        public bool Value { get; } = value;

        public bool Equals(BooleanLiteral? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is BooleanLiteral other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value ? "true" : "false";
    }

    public class Identifier(string name, bool caseSensitive)
    {
        public string Name { get; } = name;
        public bool CaseSensitive { get; } = caseSensitive;

        protected bool Equals(Identifier other) => Name == other.Name && CaseSensitive == other.CaseSensitive;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Identifier other && Equals(other);
        public override int GetHashCode() => unchecked((Name.GetHashCode() * 397) ^ CaseSensitive.GetHashCode());

        public static bool operator ==(Identifier left, Identifier right) => Equals(left, right);
        public static bool operator !=(Identifier left, Identifier right) => !Equals(left, right);

        // TODO handle quoting
        public override string ToString() => Name;
    }
    public class Qualified(Identifier qualification, Expression expression)
    {
        public Identifier Qualification { get; } = qualification;
        public Expression Expression { get; } = expression;

        protected bool Equals(Qualified other) => Equals(Qualification, other.Qualification) && Equals(Expression, other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Qualified other && Equals(other);
        public override int GetHashCode() => unchecked((Qualification.GetHashCode() * 397) ^ Expression.GetHashCode());

        public override string ToString() => $"{Qualification}.{Expression}";
    }

    public class FunctionExpression(Function function)
    {
        public Function Function { get; } = function;

        protected bool Equals(FunctionExpression other) => Function.Equals(other.Function);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is FunctionExpression other && Equals(other);
        public override int GetHashCode() => Function.GetHashCode();

        public override string? ToString() => Function.ToString();
    }

    public class Unary(UnaryOperator @operator, Expression expression)
    {
        public UnaryOperator Operator { get; } = @operator;
        public Expression Expression { get; } = expression;

        protected bool Equals(Unary other) => Operator == other.Operator && Equals(Expression, other.Expression);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Unary other && Equals(other);
        public override int GetHashCode() => unchecked(((int)Operator * 397) ^ Expression.GetHashCode());

        public override string ToString()
        {
            var op = Operator switch
            {
                UnaryOperator.Not => "!",
                UnaryOperator.Negate => "-",
                    
                _ => throw new ArgumentOutOfRangeException(nameof(Operator))
            };

            // TODO precedence
            return $"({op}{Expression})";
        }
    }
    public class Binary(BinaryOperator @operator, Expression left, Expression right)
    {
        public BinaryOperator Operator { get; } = @operator;
        public Expression Left { get; } = left;
        public Expression Right { get; } = right;

        protected bool Equals(Binary other) => Operator == other.Operator && Equals(Left, other.Left) && Equals(Right, other.Right);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Binary other && Equals(other);
        public override int GetHashCode() => unchecked(((((int)Operator * 397) ^ Left.GetHashCode()) * 397) ^ Right.GetHashCode());

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
                
                BinaryOperator.Concat=> "||",
                    
                _ => throw new ArgumentOutOfRangeException(nameof(Operator))
            };

            // TODO precedence
            return $"({Left} {op} {Right})";
        }
    }
    public class Between(bool negate, Expression expression, Expression lower, Expression upper)
    {
        public bool Negate { get; } = negate;
        public Expression Expression { get; } = expression;
        public Expression Lower { get; } = lower;
        public Expression Upper { get; } = upper;

        protected bool Equals(Between other) => Negate == other.Negate && Equals(Expression, other.Expression) && Equals(Lower, other.Lower) && Equals(Upper, other.Upper);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Between other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Negate.GetHashCode();
                hashCode = (hashCode * 397) ^ Expression.GetHashCode();
                hashCode = (hashCode * 397) ^ Lower.GetHashCode();
                hashCode = (hashCode * 397) ^ Upper.GetHashCode();
                return hashCode;
            }
        }

        // TODO bracketing?
        public override string ToString() => $"{Expression}{(Negate ? " NOT" : "")} BETWEEN {Lower} AND {Upper}";
    }

    public class IsNull(Expression expression, bool negate)
    {
        public Expression Expression { get; } = expression;
        public bool Negate { get; } = negate;

        protected bool Equals(IsNull other) => Equals(Expression, other.Expression) && Negate == other.Negate;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is IsNull other && Equals(other);
        public override int GetHashCode() => unchecked((Expression.GetHashCode() * 397) ^ Negate.GetHashCode());

        public override string ToString() => $"{Expression} IS {(Negate ? "NOT " : "")}NULL";
    }
    public class Presence(Expression expression, bool negate)
    {
        public Expression Expression { get; } = expression;
        public bool Negate { get; } = negate;

        protected bool Equals(Presence other) => Equals(Expression, other.Expression) && Negate == other.Negate;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Presence other && Equals(other);
        public override int GetHashCode() => unchecked((Expression.GetHashCode() * 397) ^ Negate.GetHashCode());

        public override string ToString() => $"{Expression} IS {(Negate ? "" : "NOT ")}MISSING";
    }
    public class In
    {
        public In(Expression expression, IReadOnlyList<Expression> matches)
        {
            Expression = expression;
            Matches = matches;

            if (matches.All(x => x.IsT0))
            {
                StringMatches = new HashSet<string>(matches.Select(x => x.AsT0.Value));
            }
        }

        public Expression Expression { get; }
        public IReadOnlyList<Expression> Matches { get; }
        /// <summary>
        /// if all matches are static strings, this set can be used for faster lookups
        /// </summary>
        public IReadOnlyCollection<string>? StringMatches { get; } 
        
        protected bool Equals(In other) => Equals(Expression, other.Expression) && Matches.SequenceEqual(other.Matches);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is In other && Equals(other);
        public override int GetHashCode() => unchecked((Expression.GetHashCode() * 397) ^ Matches.GetHashCode());

        public override string ToString() => $"{Expression} IN ({string.Join(", ", Matches)})";
    }
    public class Like(Expression expression, Expression pattern, Option<Expression> escape)
    {
        public Expression Expression { get; } = expression;
        public Expression Pattern { get; } = pattern;
        public Option<Expression> Escape { get; } = escape;

        protected bool Equals(Like other) => Equals(Expression, other.Expression) && Equals(Pattern, other.Pattern) && Equals(Escape, other.Escape);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Like other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Expression.GetHashCode();
                hashCode = (hashCode * 397) ^ Pattern.GetHashCode();
                hashCode = (hashCode * 397) ^ Escape.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() => Escape.IsNone ? $"{Expression} LIKE {Pattern}" : $"{Expression} LIKE {Pattern} ESCAPE {Escape.AsT0}";
    }
}