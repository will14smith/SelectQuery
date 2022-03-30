using System;
using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SelectParser
{
    public class SelectTokenizer : Tokenizer<SelectToken>
    {
        private static readonly IReadOnlyDictionary<string, SelectToken> Keywords = new Dictionary<string, SelectToken>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "SELECT", SelectToken.Select },
            { "AS", SelectToken.As },
            { "FROM", SelectToken.From },
            { "WHERE", SelectToken.Where },
            { "ORDER", SelectToken.Order },
            { "BY", SelectToken.By },
            { "LIMIT", SelectToken.Limit },
            { "OFFSET", SelectToken.Offset },
            { "ASC", SelectToken.Asc },
            { "DESC", SelectToken.Desc },
            { "IS", SelectToken.Is },
            { "MISSING", SelectToken.Missing },
            { "NULL", SelectToken.Null },

            { "AVG", SelectToken.Avg },
            { "COUNT", SelectToken.Count },
            { "MAX", SelectToken.Max },
            { "MIN", SelectToken.Min },
            { "SUM", SelectToken.Sum },

            { "TRUE", SelectToken.BooleanLiteral },
            { "FALSE", SelectToken.BooleanLiteral },

            { "NOT", SelectToken.Not },
            { "AND", SelectToken.And },
            { "OR", SelectToken.Or },
            { "BETWEEN", SelectToken.Between },
            { "IN", SelectToken.In },
            { "LIKE", SelectToken.Like },
            { "ESCAPE", SelectToken.Escape },
        };

        private static readonly SelectToken[] SimpleOperator = new SelectToken[128];

        private static readonly TextParser<string> QuotedIdentifier =
            Character.EqualTo('"')
                .IgnoreThen(Character.ExceptIn('"', '\r', '\n').Many())
                .Then(s => Character.EqualTo('"').Value(new string(s)));

        private static readonly TextParser<string> StringLiteral =
            Character.EqualTo('\'')
                .IgnoreThen(Span.EqualTo("''").Value('\'').Try().Or(Character.ExceptIn('\'', '\r', '\n')).Many())
                .Then(s => Character.EqualTo('\'').Value(new string(s)));

        private static readonly TextParser<TextSpan> NumberLiteral =
            Numerics.Integer
                .Then(n => Character.EqualTo('.').IgnoreThen(Numerics.Integer).OptionalOrDefault()
                    .Select(f => f == TextSpan.None ? n : new TextSpan(n.Source, n.Position, n.Length + f.Length + 1)));

        private static readonly TextParser<SelectToken> And = Span.EqualTo("&&").Value(SelectToken.And);
        private static readonly TextParser<SelectToken> Or = Span.EqualTo("||").Value(SelectToken.Or);
        private static readonly TextParser<SelectToken> LesserOrEqual = Span.EqualTo("<=").Value(SelectToken.LesserOrEqual);
        private static readonly TextParser<SelectToken> GreaterOrEqual = Span.EqualTo(">=").Value(SelectToken.GreaterOrEqual);
        private static readonly TextParser<SelectToken> Equal = Span.EqualTo("==").Value(SelectToken.Equal);
        private static readonly TextParser<SelectToken> NotEqual = Span.EqualTo("!=").Or(Span.EqualTo("<>")).Value(SelectToken.NotEqual);

        private static readonly TextParser<SelectToken> CompoundOperator =
            And.Or(Or.Or(GreaterOrEqual.Or(Equal.Or(LesserOrEqual.Try().Or(NotEqual)))));

        static SelectTokenizer()
        {
            SimpleOperator['.'] = SelectToken.Dot;
            SimpleOperator[','] = SelectToken.Comma;
            SimpleOperator['('] = SelectToken.LeftBracket;
            SimpleOperator[')'] = SelectToken.RightBracket;
            SimpleOperator['*'] = SelectToken.Star;

            SimpleOperator['!'] = SelectToken.Not;
            SimpleOperator['-'] = SelectToken.Negate;

            SimpleOperator['<'] = SelectToken.Lesser;
            SimpleOperator['>'] = SelectToken.Greater;
            SimpleOperator['='] = SelectToken.Equal;

            SimpleOperator['+'] = SelectToken.Add;
            SimpleOperator['/'] = SelectToken.Divide;
            SimpleOperator['%'] = SelectToken.Modulo;
        }

        protected override IEnumerable<Result<SelectToken>> Tokenize(TextSpan input)
        {
            var next = SkipWhiteSpace(input);

            while (next.HasValue)
            {
                if (char.IsLetter(next.Value) || next.Value == '_')
                {
                    var start = next.Location;
                    do
                    {
                        next = next.Remainder.ConsumeChar();
                    } while (next.HasValue && (char.IsLetterOrDigit(next.Value) || next.Value == '_'));
                    var end = next.Location;

                    var text = start.Until(end).ToStringValue();
                    if (Keywords.TryGetValue(text, out var token))
                    {
                        yield return Result.Value(token, start, end);
                    }
                    else
                    {
                        yield return Result.Value(SelectToken.Identifier, start, end);
                    }
                }
                else if (next.Value == '"')
                {
                    var result = QuotedIdentifier(next.Location);
                    if (!result.HasValue)
                    {
                        yield return Result.CastEmpty<string, SelectToken>(result);
                    }
                    else
                    {
                        next = result.Remainder.ConsumeChar();
                    }

                    yield return Result.Value(SelectToken.Identifier, result.Location, result.Remainder);
                }
                else if (next.Value == '\'')
                {
                    var result = StringLiteral(next.Location);
                    if (!result.HasValue)
                    {
                        yield return Result.CastEmpty<string, SelectToken>(result);
                    }
                    else
                    {
                        yield return Result.Value(SelectToken.StringLiteral, result.Location, result.Remainder);
                    }

                    next = result.Remainder.ConsumeChar();
                }
                else if (char.IsDigit(next.Value))
                {
                    var result = NumberLiteral(next.Location);
                    if (!result.HasValue)
                    {
                        yield return Result.CastEmpty<TextSpan, SelectToken>(result);
                    }
                    else
                    {
                        yield return Result.Value(SelectToken.NumberLiteral, result.Location, result.Remainder);
                    }

                    next = result.Remainder.ConsumeChar();
                }
                else
                {
                    var compound = CompoundOperator(next.Location);
                    if (compound.HasValue)
                    {
                        yield return Result.Value(compound.Value, compound.Location, compound.Remainder);
                        next = compound.Remainder.ConsumeChar();
                    }
                    else if (next.Value < SimpleOperator.Length && SimpleOperator[next.Value] != SelectToken.None)
                    {
                        yield return Result.Value(SimpleOperator[next.Value], next.Location, next.Remainder);
                        next = next.Remainder.ConsumeChar();
                    }
                    else
                    {
                        yield return Result.Empty<SelectToken>(next.Location, $"unrecognised `{next.Value}`");
                        next = next.Remainder.ConsumeChar();
                    }
                }

                next = SkipWhiteSpace(next.Location);
            }
        }
    }
}
