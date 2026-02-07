using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SendMail.Core.Models;
using SimpleJinja2DotNet;

namespace SendMail.Core.Template;

public static class MailTemplateRenderer
{
    private static readonly Regex RoundTokenRegex = new(
        "\\{round\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static RenderedTemplate Render(
        string subjectTemplate,
        string bodyTemplate,
        int round,
        EmailGroup group)
    {
        if (group is null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        var subject = ReplaceRoundToken(subjectTemplate ?? string.Empty, round);

        var rows = group.Rows ?? Array.Empty<ExcelRow>();
        var firstRow = rows.Count > 0 ? rows[0] : null;
        var firstFields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (firstRow is not null)
        {
            foreach (var kv in firstRow.Fields)
            {
                firstFields[kv.Key] = kv.Value;
            }
        }

        var processedBody = ReplaceRoundToken(bodyTemplate ?? string.Empty, round);

        var bodyTokens = TokenScanner.ScanSingleBraceTokens(processedBody);
        foreach (var token in bodyTokens)
        {
            if (string.Equals(token, "round", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(token, "이메일", StringComparison.OrdinalIgnoreCase))
            {
                processedBody = ReplaceSingleBraceToken(processedBody, token, group.Email);
                continue;
            }

            if (firstFields.TryGetValue(token, out var value))
            {
                processedBody = ReplaceSingleBraceToken(processedBody, token, value ?? string.Empty);
            }
            else
            {
                // Leave unknown tokens untouched; TemplateValidator should have caught it.
            }
        }

        var rowDicts = rows
            .Select(r =>
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                dict["이메일"] = r.Email;
                foreach (var kv in r.Fields)
                {
                    dict[kv.Key] = kv.Value ?? string.Empty;
                }

                return dict;
            })
            .ToList();

        var globals = new RenderGlobals(round, group.Email, rowDicts);

        var template = SimpleJinja2DotNet.Template.FromString(processedBody);
        var html = template.Render(globals);

        return new RenderedTemplate(subject, html);
    }

    private static string ReplaceRoundToken(string text, int round)
    {
        return RoundTokenRegex.Replace(text, _ => round.ToString());
    }

    private static string ReplaceSingleBraceToken(string text, string token, string replacement)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Ensure we only replace "{token}" (single braces), ignoring case.
        var pattern = $"\\{{{Regex.Escape(token)}\\}}";
        return Regex.Replace(
            text,
            pattern,
            _ => replacement ?? string.Empty,
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private sealed class RenderGlobals
    {
        // Use lower-case property names to match Jinja template variable names.
        public RenderGlobals(int round, string email, IReadOnlyList<IDictionary<string, object?>> rows)
        {
            this.round = round;
            this.email = email;
            this.rows = rows;
        }

        public int round { get; }
        public string email { get; }
        public IReadOnlyList<IDictionary<string, object?>> rows { get; }
    }
}
