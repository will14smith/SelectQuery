using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SelectParser;

public ref struct NewTokenizer(ReadOnlySpan<char> input, int offset = 0)
{
    private static Token Eof => new(TokenType.Eof, ReadOnlySpan<char>.Empty);

    private readonly ReadOnlySpan<char> _input = input;
    private int _offset = offset;
    
    public static Token Read(ref NewTokenizer lexer)
    {
        var result = SkipWhiteSpace(ref lexer);
        if (result == Result.Eof) return Eof;

        var next = lexer._input[lexer._offset];
        
        // identifier
        if (char.IsLetter(next) || next == '_')
        {
            var start = lexer._offset++;

            while (true)
            {
                if (lexer._offset >= lexer._input.Length)
                {
                    // eof, so this must be the end of the identifier
                    break;
                }

                next = lexer._input[lexer._offset];
                // check the next char is an identifier
                if(!char.IsLetterOrDigit(next) && next != '_')
                {
                    // it wasn't, so we've reached the end of the identifier
                    break;
                }

                // it was, so skip it
                lexer._offset++;
            }
            
            var end = lexer._offset;

            var text = lexer._input.Slice(start, end - start);

            if (IsIdentifier(text, "SELECT")) { return new Token(TokenType.Select, text); }
            if (IsIdentifier(text, "AS")) { return new Token(TokenType.As, text); }
            if (IsIdentifier(text, "FROM")) { return new Token(TokenType.From, text); }
            if (IsIdentifier(text, "WHERE")) { return new Token(TokenType.Where, text); }
            if (IsIdentifier(text, "ORDER")) { return new Token(TokenType.Order, text); }
            if (IsIdentifier(text, "BY")) { return new Token(TokenType.By, text); }
            if (IsIdentifier(text, "LIMIT")) { return new Token(TokenType.Limit, text); }
            if (IsIdentifier(text, "OFFSET")) { return new Token(TokenType.Offset, text); }
            if (IsIdentifier(text, "ASC")) { return new Token(TokenType.Asc, text); }
            if (IsIdentifier(text, "DESC")) { return new Token(TokenType.Desc, text); }
            if (IsIdentifier(text, "IS")) { return new Token(TokenType.Is, text); }
            if (IsIdentifier(text, "MISSING")) { return new Token(TokenType.Missing, text); }
            if (IsIdentifier(text, "NULL")) { return new Token(TokenType.Null, text); }
            if (IsIdentifier(text, "AVG")) { return new Token(TokenType.Avg, text); }
            if (IsIdentifier(text, "COUNT")) { return new Token(TokenType.Count, text); }
            if (IsIdentifier(text, "MAX")) { return new Token(TokenType.Max, text); }
            if (IsIdentifier(text, "MIN")) { return new Token(TokenType.Min, text); }
            if (IsIdentifier(text, "SUM")) { return new Token(TokenType.Sum, text); }
            if (IsIdentifier(text, "TRUE")) { return new Token(TokenType.BooleanLiteral, text); }
            if (IsIdentifier(text, "FALSE")) { return new Token(TokenType.BooleanLiteral, text); }
            if (IsIdentifier(text, "NOT")) { return new Token(TokenType.Not, text); }
            if (IsIdentifier(text, "AND")) { return new Token(TokenType.And, text); }
            if (IsIdentifier(text, "OR")) { return new Token(TokenType.Or, text); }
            if (IsIdentifier(text, "BETWEEN")) { return new Token(TokenType.Between, text); }
            if (IsIdentifier(text, "IN")) { return new Token(TokenType.In, text); }
            if (IsIdentifier(text, "LIKE")) { return new Token(TokenType.Like, text); }
            if (IsIdentifier(text, "ESCAPE")) { return new Token(TokenType.Escape, text); }
            
            return new Token(TokenType.Identifier, text);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsIdentifier(ReadOnlySpan<char> text, string token) => text.Equals(token.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
        
        if (next == '"')
        {
            var start = lexer._offset++;

            while (true)
            {
                if (lexer._offset >= lexer._input.Length)
                {
                    // eof, this is an error
                    return new Token(TokenType.Error, "Unexpected EOF in QuotedIdentifier".AsSpan());
                }

                next = lexer._input[lexer._offset];
                if(next == '"')
                {
                    // reached the end of the quoted section
                    break;
                }

                lexer._offset++;
            }
            
            // make sure we actually consume the end quote
            var end = ++lexer._offset;

            var text = lexer._input.Slice(start, end - start);

            return new Token(TokenType.Identifier, text);
        }

        if (next == '\'')
        {
            var start = lexer._offset++;

            while (true)
            {
                if (lexer._offset >= lexer._input.Length)
                {
                    // eof, this is an error
                    return new Token(TokenType.Error, "Unexpected EOF in StringLiteral".AsSpan());
                }

                next = lexer._input[lexer._offset];
                if(next == '\'')
                {
                    // check the next char isn't also a quote
                    if (lexer._offset + 1 >= lexer._input.Length || lexer._input[lexer._offset + 1] != '\'')
                    {
                        // reached the end of the quoted section
                        break;
                    }

                    // consume the escape
                    lexer._offset++;
                }

                lexer._offset++;
            }
            
            // make sure we actually consume the end quote
            var end = ++lexer._offset;

            var text = lexer._input.Slice(start, end - start);

            return new Token(TokenType.StringLiteral, text);
        }

        if (char.IsDigit(next))
        {
            var start = lexer._offset++;
            
            while (true)
            {
                if (lexer._offset >= lexer._input.Length)
                {
                    // eof is OK in a number
                    break;
                }

                next = lexer._input[lexer._offset];
                if (!char.IsDigit(next))
                {
                    // wasn't a digit so lets move on
                    break;
                }

                lexer._offset++;
            }

            if (lexer._offset < lexer._input.Length && lexer._input[lexer._offset] == '.')
            {
                // we found a decimal, lets continue
                lexer._offset++;
                
                if (lexer._offset >= lexer._input.Length)
                {
                    // can't end a number of a decimal point
                    return new Token(TokenType.Error, "Unexpected EOF in NumberLiteral".AsSpan());
                }

                    
                while (true)
                {
                    if (lexer._offset >= lexer._input.Length)
                    {
                        break;
                    }

                    next = lexer._input[lexer._offset];
                    if (!char.IsDigit(next))
                    {
                        break;
                    }

                    lexer._offset++;
                }

            }
            
            var end = lexer._offset;

            var text = lexer._input.Slice(start, end - start);

            return new Token(TokenType.NumberLiteral, text);

        }

        if (next == '&')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(TokenType.Error, "Unexpected EOF in AND operator".AsSpan());
            }
            
            next = lexer._input[lexer._offset];

            if (next != '&')
            {
                return new Token(TokenType.Error, "Unrecognised char in AND operator".AsSpan());
            }

            lexer._offset++;

            return new Token(TokenType.And, lexer._input.Slice(lexer._offset - 2, 2));
        }
        if (next == '|')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(TokenType.Error, "Unexpected EOF in OR operator".AsSpan());
            }
            
            next = lexer._input[lexer._offset];

            if (next != '|')
            {
                return new Token(TokenType.Error, "Unrecognised char in OR operator".AsSpan());
            }

            lexer._offset++;

            return new Token(TokenType.Or, lexer._input.Slice(lexer._offset - 2, 2));
        }     
        if (next == '<')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(TokenType.Lesser, lexer._input.Slice(lexer._offset - 1, 1));
            }
            
            next = lexer._input[lexer._offset];

            if (next == '=')
            {
                lexer._offset++;

                return new Token(TokenType.LesserOrEqual, lexer._input.Slice(lexer._offset - 2, 2));
            }
            
            if (next == '>')
            {
                lexer._offset++;

                return new Token(TokenType.NotEqual, lexer._input.Slice(lexer._offset - 2, 2));
            }

            return new Token(TokenType.Lesser, lexer._input.Slice(lexer._offset - 1, 1));
        }    
        if (next == '>')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(TokenType.Greater, lexer._input.Slice(lexer._offset - 1, 1));
            }
            
            next = lexer._input[lexer._offset];

            if (next != '=')
            {
                return new Token(TokenType.Greater, lexer._input.Slice(lexer._offset - 1, 1));
            }

            lexer._offset++;

            return new Token(TokenType.GreaterOrEqual, lexer._input.Slice(lexer._offset - 2, 2));
        }   
        if (next == '=')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(TokenType.Equal, lexer._input.Slice(lexer._offset - 1, 1));
            }
            
            next = lexer._input[lexer._offset];

            if (next != '=')
            {
                return new Token(TokenType.Equal, lexer._input.Slice(lexer._offset - 1, 1));
            }

            lexer._offset++;

            return new Token(TokenType.Equal, lexer._input.Slice(lexer._offset - 2, 2));
        }
        if (next == '!')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(TokenType.Not, lexer._input.Slice(lexer._offset - 1, 1));
            }
            
            next = lexer._input[lexer._offset];

            if (next != '=')
            {
                return new Token(TokenType.Not, lexer._input.Slice(lexer._offset - 1, 1));
            }

            lexer._offset++;

            return new Token(TokenType.NotEqual, lexer._input.Slice(lexer._offset - 2, 2));
        }
        
        if (next == '.') { return new Token(TokenType.Dot, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == ',') { return new Token(TokenType.Comma, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == '(') { return new Token(TokenType.LeftBracket, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == ')') { return new Token(TokenType.RightBracket, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == '*') { return new Token(TokenType.Star, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == '-') { return new Token(TokenType.Negate, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == '+') { return new Token(TokenType.Add, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == '/') { return new Token(TokenType.Divide, lexer._input.Slice(lexer._offset++, 1)); }
        if (next == '%') { return new Token(TokenType.Modulo, lexer._input.Slice(lexer._offset++, 1)); }
        
        return new Token(TokenType.Error, "Unrecognised char".AsSpan());
    }
    
    private static Result SkipWhiteSpace(ref NewTokenizer lexer)
    {
        while (lexer._offset < lexer._input.Length)
        {
            if (!char.IsWhiteSpace(lexer._input[lexer._offset]))
            {
                return Result.Ok;
            }

            lexer._offset++;
        }

        return Result.Eof;
    }

    private enum Result
    {
        Ok,
        Eof,
    }
    
    public readonly ref struct Token(TokenType type, ReadOnlySpan<char> span)
    {
        public readonly TokenType Type = type;
        public readonly ReadOnlySpan<char> Span = span;

        public string ToStringValue()
        {
            return new string(Span.ToArray());
        }
    }

    public enum TokenType
    {
        Error = -2,
        Eof = -1,
        Unknown = 0,
        
        Identifier = 1,
        
        // literals
        StringLiteral,
        NumberLiteral,
        BooleanLiteral,
        // keywords
        Select,
        As,
        From,
        Where,
        Order,
        By,
        Limit,
        Offset,
        Asc,
        Desc,
        Is,
        Missing,
        Null,
        // functions
        Avg,
        Count,
        Max,
        Min,
        Sum,
        // operators
        Dot,
        Comma,
        LeftBracket,
        RightBracket,
        Star,

        Not,
        Negate,

        And,
        Or,

        Lesser,
        Greater,
        LesserOrEqual,
        GreaterOrEqual,
        Equal,
        NotEqual,

        Add,
        Divide,
        Modulo,

        Between,
        In,
        Like,
        Escape,
    }
}