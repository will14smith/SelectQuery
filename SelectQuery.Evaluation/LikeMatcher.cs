using System;
using System.Text;
using System.Text.RegularExpressions;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation;

internal class LikeMatcher
{
    public static bool IsMatch(string pattern, Option<char> escape, string value) => ToRegex(pattern, escape).IsMatch(value);

    private static Regex ToRegex(string pattern, Option<char> escape)
    {
        var sb = new StringBuilder();

        sb.Append('^');
            
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '_')
            {
                sb.Append('.');
            } 
            else if (c == '%')
            {
                sb.Append(".*");
            }
            else if (escape.IsSome && escape.AsT0 == c)
            {
                if (i + 1 >= pattern.Length)
                {
                    throw new InvalidOperationException($"Escape character was at end of pattern: {pattern}");
                }

                sb.Append(pattern[i + 1]);
                i += 1;
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }

        sb.Append('$');
            
        return new Regex(sb.ToString());
    }
}