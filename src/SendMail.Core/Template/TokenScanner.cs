using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SendMail.Core.Template;

public static class TokenScanner
{
    private static readonly Regex Jinja2RowKeyRegex = new(
        "row\\s*\\[\\s*(?:\"(?<key>[^\"]+)\"|'(?<key>[^']+)')\\s*\\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> ScanSingleBraceTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        // Tokens are of form "{token}" (single braces).
        // We intentionally ignore Jinja2 constructs like "{{ ... }}", "{% ... %}", "{# ... #}".
        // We also ignore CSS/JSON-like blocks commonly containing ":" or ";" to avoid false positives.
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '{')
            {
                continue;
            }

            if (i > 0 && text[i - 1] == '{')
            {
                // "{{"
                continue;
            }

            if (i + 1 >= text.Length)
            {
                continue;
            }

            var next = text[i + 1];
            if (next is '{' or '%' or '#')
            {
                continue;
            }

            var end = text.IndexOf('}', i + 1);
            if (end < 0)
            {
                continue;
            }

            var token = text.Substring(i + 1, end - i - 1).Trim();
            i = end;

            if (token.Length == 0)
            {
                continue;
            }

            if (token.StartsWith('%') || token.StartsWith('#'))
            {
                continue;
            }

            if (token.IndexOfAny([':', ';', '\r', '\n', '\t']) >= 0)
            {
                continue;
            }

            if (seen.Add(token))
            {
                results.Add(token);
            }
        }

        return results;
    }

    public static IReadOnlyList<string> ScanJinja2RowColumnKeys(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in Jinja2RowKeyRegex.Matches(text))
        {
            var key = match.Groups["key"].Value.Trim();
            if (key.Length == 0)
            {
                continue;
            }

            if (seen.Add(key))
            {
                results.Add(key);
            }
        }

        return results;
    }
}
