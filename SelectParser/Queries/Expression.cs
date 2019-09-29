using System.Collections.Generic;
using OneOf;

namespace SelectParser.Queries
{
    public abstract class Expression : OneOfBase<Expression.StringLiteral,Expression.NumberLiteral,Expression.BooleanLiteral, Expression.Identifier, Expression.Qualified, Expression.Unary, Expression.Binary, Expression.Between, Expression.In, Expression.Like>
    {
        public class StringLiteral : Expression
        {
            public StringLiteral(string value) => Value = value;
            public string Value { get; }
        }
        public class NumberLiteral : Expression
        {
            public NumberLiteral(decimal value) => Value = value;
            public decimal Value { get; }
        }
        public class BooleanLiteral : Expression
        {
            public BooleanLiteral(bool value) => Value = value;
            public bool Value { get; }
        }

        public class Identifier : Expression
        {
            public Identifier(string name) => Name = name;
            public string Name { get; }
        }
        public class Qualified : Expression
        {
            public Qualified(string qualification, Expression expression)
            {
                Qualification = qualification;
                Expression = expression;
            }

            public string Qualification { get; }
            public Expression Expression { get; }
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
        }
        public class In : Expression
        {
            public In(Expression expression, IReadOnlyCollection<Expression> matches)
            {
                Expression = expression;
                Matches = matches;
            }

            public Expression Expression { get; }
            public IReadOnlyCollection<Expression> Matches { get; }
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
        }
    }
}