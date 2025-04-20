using System;
using System.Runtime.CompilerServices;

namespace SelectParser;

public ref struct SelectTokenizer(ReadOnlySpan<char> input, int offset = 0)
{
    private static Token Eof => new(SelectToken.Eof, ReadOnlySpan<char>.Empty);

    private readonly ReadOnlySpan<char> _input = input;
    private int _offset = offset;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> Slice(int start, int end)
    {
#if NETSTANDARD
        return _input.Slice(start, end - start);
#else
        return _input[start..end];
#endif
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> SlicePrevious(int count)
    {
#if NETSTANDARD
        return _input.Slice(_offset - count, count);
#else
        return _input[(_offset - count).._offset];
#endif
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> SliceCurrentWithIncrement()
    {
#if NETSTANDARD
        return _input.Slice(_offset++, 1);
#else
        return _input[_offset..++_offset];
#endif
    }
    
    public static Token Read(ref SelectTokenizer lexer)
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

            var text = lexer.Slice(start, end);

            if (IsIdentifier(text, "SELECT")) { return new Token(SelectToken.Select, text); }
            if (IsIdentifier(text, "AS")) { return new Token(SelectToken.As, text); }
            if (IsIdentifier(text, "FROM")) { return new Token(SelectToken.From, text); }
            if (IsIdentifier(text, "WHERE")) { return new Token(SelectToken.Where, text); }
            if (IsIdentifier(text, "ORDER")) { return new Token(SelectToken.Order, text); }
            if (IsIdentifier(text, "BY")) { return new Token(SelectToken.By, text); }
            if (IsIdentifier(text, "LIMIT")) { return new Token(SelectToken.Limit, text); }
            if (IsIdentifier(text, "OFFSET")) { return new Token(SelectToken.Offset, text); }
            if (IsIdentifier(text, "ASC")) { return new Token(SelectToken.Asc, text); }
            if (IsIdentifier(text, "DESC")) { return new Token(SelectToken.Desc, text); }
            if (IsIdentifier(text, "IS")) { return new Token(SelectToken.Is, text); }
            if (IsIdentifier(text, "MISSING")) { return new Token(SelectToken.Missing, text); }
            if (IsIdentifier(text, "NULL")) { return new Token(SelectToken.Null, text); }
            if (IsIdentifier(text, "AVG")) { return new Token(SelectToken.Avg, text); }
            if (IsIdentifier(text, "COUNT")) { return new Token(SelectToken.Count, text); }
            if (IsIdentifier(text, "MAX")) { return new Token(SelectToken.Max, text); }
            if (IsIdentifier(text, "MIN")) { return new Token(SelectToken.Min, text); }
            if (IsIdentifier(text, "SUM")) { return new Token(SelectToken.Sum, text); }
            if (IsIdentifier(text, "TRUE")) { return new Token(SelectToken.BooleanLiteral, text); }
            if (IsIdentifier(text, "FALSE")) { return new Token(SelectToken.BooleanLiteral, text); }
            if (IsIdentifier(text, "NOT")) { return new Token(SelectToken.Not, text); }
            if (IsIdentifier(text, "AND")) { return new Token(SelectToken.And, text); }
            if (IsIdentifier(text, "OR")) { return new Token(SelectToken.Or, text); }
            if (IsIdentifier(text, "BETWEEN")) { return new Token(SelectToken.Between, text); }
            if (IsIdentifier(text, "IN")) { return new Token(SelectToken.In, text); }
            if (IsIdentifier(text, "LIKE")) { return new Token(SelectToken.Like, text); }
            if (IsIdentifier(text, "ESCAPE")) { return new Token(SelectToken.Escape, text); }
            
            return new Token(SelectToken.Identifier, text);

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
                    return new Token(SelectToken.Error, "Unexpected EOF in QuotedIdentifier".AsSpan());
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
            
            return new Token(SelectToken.Identifier, lexer.Slice(start, end));
        }

        if (next == '\'')
        {
            var start = lexer._offset++;

            while (true)
            {
                if (lexer._offset >= lexer._input.Length)
                {
                    // eof, this is an error
                    return new Token(SelectToken.Error, "Unexpected EOF in StringLiteral".AsSpan());
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

            var text = lexer.Slice(start, end);

            return new Token(SelectToken.StringLiteral, text);
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
                    return new Token(SelectToken.Error, "Unexpected EOF in NumberLiteral".AsSpan());
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

            var text = lexer.Slice(start, end);

            return new Token(SelectToken.NumberLiteral, text);

        }

        if (next == '&')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(SelectToken.Error, "Unexpected EOF in AND operator".AsSpan());
            }
            
            next = lexer._input[lexer._offset];

            if (next != '&')
            {
                return new Token(SelectToken.Error, "Unrecognised char in AND operator".AsSpan());
            }

            lexer._offset++;

            return new Token(SelectToken.And, lexer.SlicePrevious(2));
        }
        if (next == '|')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(SelectToken.Error, "Unexpected EOF in Concat operator".AsSpan());
            }
            
            next = lexer._input[lexer._offset];

            if (next != '|')
            {
                return new Token(SelectToken.Error, "Unrecognised char in Concat operator".AsSpan());
            }

            lexer._offset++;

            return new Token(SelectToken.Concat, lexer.SlicePrevious(2));
        }
        if (next == '<')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(SelectToken.Lesser, lexer.SlicePrevious(1));
            }
            
            next = lexer._input[lexer._offset];

            if (next == '=')
            {
                lexer._offset++;

                return new Token(SelectToken.LesserOrEqual, lexer.SlicePrevious(2));
            }
            
            if (next == '>')
            {
                lexer._offset++;

                return new Token(SelectToken.NotEqual, lexer.SlicePrevious(2));
            }

            return new Token(SelectToken.Lesser, lexer.SlicePrevious(1));
        }    
        if (next == '>')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(SelectToken.Greater, lexer.SlicePrevious(1));
            }
            
            next = lexer._input[lexer._offset];

            if (next != '=')
            {
                return new Token(SelectToken.Greater, lexer.SlicePrevious(1));
            }

            lexer._offset++;

            return new Token(SelectToken.GreaterOrEqual, lexer.SlicePrevious(2));
        }   
        if (next == '=')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(SelectToken.Equal, lexer.SlicePrevious(1));
            }
            
            next = lexer._input[lexer._offset];

            if (next != '=')
            {
                return new Token(SelectToken.Equal, lexer.SlicePrevious(1));
            }

            lexer._offset++;

            return new Token(SelectToken.Equal, lexer.SlicePrevious(2));
        }
        if (next == '!')
        {
            lexer._offset++;
            
            if (lexer._offset >= lexer._input.Length)
            {
                return new Token(SelectToken.Not, lexer.SlicePrevious(1));
            }
            
            next = lexer._input[lexer._offset];

            if (next != '=')
            {
                return new Token(SelectToken.Not, lexer.SlicePrevious(1));
            }

            lexer._offset++;

            return new Token(SelectToken.NotEqual, lexer.SlicePrevious(2));
        }
        
        if (next == '.') { return new Token(SelectToken.Dot, lexer.SliceCurrentWithIncrement()); }
        if (next == ',') { return new Token(SelectToken.Comma, lexer.SliceCurrentWithIncrement()); }
        if (next == '(') { return new Token(SelectToken.LeftBracket, lexer.SliceCurrentWithIncrement()); }
        if (next == ')') { return new Token(SelectToken.RightBracket, lexer.SliceCurrentWithIncrement()); }
        if (next == '*') { return new Token(SelectToken.Star, lexer.SliceCurrentWithIncrement()); }
        if (next == '-') { return new Token(SelectToken.Negate, lexer.SliceCurrentWithIncrement()); }
        if (next == '+') { return new Token(SelectToken.Add, lexer.SliceCurrentWithIncrement()); }
        if (next == '/') { return new Token(SelectToken.Divide, lexer.SliceCurrentWithIncrement()); }
        if (next == '%') { return new Token(SelectToken.Modulo, lexer.SliceCurrentWithIncrement()); }
        
        return new Token(SelectToken.Error, "Unrecognised char".AsSpan());
    }
    
    private static Result SkipWhiteSpace(ref SelectTokenizer lexer)
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
    
    public readonly ref struct Token(SelectToken type, ReadOnlySpan<char> span)
    {
        public readonly SelectToken Type = type;
        public readonly ReadOnlySpan<char> Span = span;

        public string ToStringValue()
        {
            return new string(Span.ToArray());
        }
    }
}