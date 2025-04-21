using System;
using System.Collections.Generic;
using System.Linq;

namespace SelectParser.Queries;

public abstract class Expression : IEquatable<Expression>
{
    public abstract bool Equals(Expression? other);
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public class StringLiteral(string value) : Expression, IEquatable<StringLiteral>
    {
        public string Value { get; } = value;

        public override bool Equals(Expression? other) => Equals(other as StringLiteral);
        public bool Equals(StringLiteral? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is StringLiteral other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        // TODO handle escaping
        public override string ToString() => $"'{Value}'";
    }
    public class NumberLiteral(decimal value) : Expression, IEquatable<NumberLiteral>
    {
        public decimal Value { get; } = value;

        public override bool Equals(Expression? other) => Equals(other as NumberLiteral);
        public bool Equals(NumberLiteral? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is NumberLiteral other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"{Value}";
    }
    public class BooleanLiteral(bool value) : Expression, IEquatable<BooleanLiteral>
    {
        public bool Value { get; } = value;

        public override bool Equals(Expression? other) => Equals(other as BooleanLiteral);
        public bool Equals(BooleanLiteral? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is BooleanLiteral other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value ? "true" : "false";
    }

    public class Identifier(string name, bool caseSensitive) : Expression, IEquatable<Identifier>
    {
        public string Name { get; } = name;
        public bool CaseSensitive { get; } = caseSensitive;

        public override bool Equals(Expression? other) => Equals(other as Identifier);
        public bool Equals(Identifier? other) => other is not null && Name == other.Name && CaseSensitive == other.CaseSensitive;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Identifier other && Equals(other);
        public override int GetHashCode() => unchecked((Name.GetHashCode() * 397) ^ CaseSensitive.GetHashCode());

        public static bool operator ==(Identifier left, Identifier right) => Equals(left, right);
        public static bool operator !=(Identifier left, Identifier right) => !Equals(left, right);

        // TODO handle quoting
        public override string ToString() => Name;
    }
    public class Qualified(IReadOnlyList<Identifier> identifiers) : Expression, IEquatable<Qualified>
    {
        public IReadOnlyList<Identifier> Identifiers { get; } = identifiers;

        public override bool Equals(Expression? other) => Equals(other as Qualified);
        public bool Equals(Qualified? other) => other is not null && Identifiers.SequenceEqual(other.Identifiers);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Qualified other && Equals(other);
        public override int GetHashCode() => Identifiers.Aggregate(0, (a, x) => a ^ x.GetHashCode());

        public override string ToString() => string.Join(".", Identifiers);
    }

    public class FunctionExpression(Function function) : Expression, IEquatable<FunctionExpression>
    {
        public Function Function { get; } = function;

        public override bool Equals(Expression? other) => Equals(other as FunctionExpression);
        public bool Equals(FunctionExpression? other) => other is not null && Function.Equals(other.Function);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is FunctionExpression other && Equals(other);
        public override int GetHashCode() => Function.GetHashCode();

        public override string? ToString() => Function.ToString();
    }

    public class Unary(UnaryOperator @operator, Expression expression) : Expression, IEquatable<Unary>
    {
        public UnaryOperator Operator { get; } = @operator;
        public Expression Expression { get; } = expression;

        public override bool Equals(Expression? other) => Equals(other as Unary);
        public bool Equals(Unary? other) => other is not null && Operator == other.Operator && Equals(Expression, other.Expression);
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
    public class Binary(BinaryOperator @operator, Expression left, Expression right) : Expression, IEquatable<Binary>
    {
        public BinaryOperator Operator { get; } = @operator;
        public Expression Left { get; } = left;
        public Expression Right { get; } = right;

        public override bool Equals(Expression? other) => Equals(other as Binary);
        public bool Equals(Binary? other) => other is not null && Operator == other.Operator && Equals(Left, other.Left) && Equals(Right, other.Right);
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
    public class Between(bool negate, Expression expression, Expression lower, Expression upper) : Expression, IEquatable<Between>
    {
        public bool Negate { get; } = negate;
        public Expression Expression { get; } = expression;
        public Expression Lower { get; } = lower;
        public Expression Upper { get; } = upper;

        public override bool Equals(Expression? other) => Equals(other as Between);
        public bool Equals(Between? other) => other is not null && Negate == other.Negate && Equals(Expression, other.Expression) && Equals(Lower, other.Lower) && Equals(Upper, other.Upper);
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

    public class IsNull(Expression expression, bool negate) : Expression, IEquatable<IsNull>
    {
        public Expression Expression { get; } = expression;
        public bool Negate { get; } = negate;

        public override bool Equals(Expression? other) => Equals(other as IsNull);
        public bool Equals(IsNull? other) => other is not null && Equals(Expression, other.Expression) && Negate == other.Negate;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is IsNull other && Equals(other);
        public override int GetHashCode() => unchecked((Expression.GetHashCode() * 397) ^ Negate.GetHashCode());

        public override string ToString() => $"{Expression} IS {(Negate ? "NOT " : "")}NULL";
    }
    public class Presence(Expression expression, bool negate) : Expression, IEquatable<Presence>
    {
        public Expression Expression { get; } = expression;
        public bool Negate { get; } = negate;

        public override bool Equals(Expression? other) => Equals(other as Presence);
        public bool Equals(Presence? other) => other is not null && Equals(Expression, other.Expression) && Negate == other.Negate;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Presence other && Equals(other);
        public override int GetHashCode() => unchecked((Expression.GetHashCode() * 397) ^ Negate.GetHashCode());

        public override string ToString() => $"{Expression} IS {(Negate ? "" : "NOT ")}MISSING";
    }
    public class In : Expression, IEquatable<In>
    {
        public In(Expression expression, IReadOnlyList<Expression> matches)
        {
            Expression = expression;
            Matches = matches;

            if (matches.All(x => x is StringLiteral))
            {
                StringMatches = new HashSet<string>(matches.Select(x => ((StringLiteral)x).Value));
            }
        }

        public Expression Expression { get; }
        public IReadOnlyList<Expression> Matches { get; }
        /// <summary>
        /// if all matches are static strings, this set can be used for faster lookups
        /// </summary>
        public ISet<string>? StringMatches { get; } 
        
        public override bool Equals(Expression? other) => Equals(other as In);
        public bool Equals(In? other) => other is not null && Equals(Expression, other.Expression) && Matches.SequenceEqual(other.Matches);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is In other && Equals(other);
        public override int GetHashCode() => unchecked((Expression.GetHashCode() * 397) ^ Matches.GetHashCode());

        public override string ToString() => $"{Expression} IN ({string.Join(", ", Matches)})";
    }
    public class Like(Expression expression, Expression pattern, Option<Expression> escape) : Expression, IEquatable<Like>
    {
        public Expression Expression { get; } = expression;
        public Expression Pattern { get; } = pattern;
        public Option<Expression> Escape { get; } = escape;

        public override bool Equals(Expression? other) => Equals(other as Like);
        public bool Equals(Like? other) => other is not null && Equals(Expression, other.Expression) && Equals(Pattern, other.Pattern) && Equals(Escape, other.Escape);
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