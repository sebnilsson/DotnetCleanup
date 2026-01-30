using System.Text;
using System.Text.RegularExpressions;

namespace DotnetCleanup;

internal sealed class GlobMatcher
{
    private readonly Regex _regex;

    public GlobMatcher(string pattern, bool ignoreCase)
    {
        var normalized = NormalizePathForMatch(pattern);
        var regexPattern = ConvertToRegex(normalized);

        _regex = new Regex(
            regexPattern,
            ignoreCase
                ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                : RegexOptions.CultureInvariant);
    }

    public bool IsMatch(string relativePath)
    {
        var normalized = NormalizePathForMatch(relativePath);
        return _regex.IsMatch(normalized);
    }

    private static string NormalizePathForMatch(string path) => path.Replace('\\', '/');

    private static string ConvertToRegex(string pattern)
    {
        var builder = new StringBuilder();
        builder.Append('^');

        for (var i = 0; i < pattern.Length; i++)
        {
            var current = pattern[i];
            if (current == '*')
            {
                var isDoubleStar = i + 1 < pattern.Length && pattern[i + 1] == '*';
                if (isDoubleStar)
                {
                    var hasSlash = i + 2 < pattern.Length && pattern[i + 2] == '/';
                    if (hasSlash)
                    {
                        builder.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        builder.Append(".*");
                        i++;
                    }

                    continue;
                }

                builder.Append("[^/]*");
                continue;
            }

            if (current == '?')
            {
                builder.Append("[^/]");
                continue;
            }

            if (current is '.' or '+' or '(' or ')' or '$' or '^' or '{' or '}' or '[' or ']' or '|' or '\\')
            {
                builder.Append('\\');
            }

            builder.Append(current);
        }

        builder.Append('$');
        return builder.ToString();
    }
}
