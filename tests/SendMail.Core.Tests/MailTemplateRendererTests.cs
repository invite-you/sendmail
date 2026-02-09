using SendMail.Core.Models;
using SendMail.Core.Template;

namespace SendMail.Core.Tests;

public class MailTemplateRendererTests
{
    [Fact]
    public void Render_ReplacesRoundAndRendersRows()
    {
        var group = new EmailGroup(
            Email: "a@example.com",
            Rows: new[]
            {
                new ExcelRow("f.xlsx", 2, "a@example.com", new Dictionary<string, string?>
                {
                    ["컴퓨터 이름"] = "PC1"
                }),
                new ExcelRow("f.xlsx", 3, "a@example.com", new Dictionary<string, string?>
                {
                    ["컴퓨터 이름"] = "PC2"
                })
            });

        var rendered = MailTemplateRenderer.Render(
            subjectTemplate: "[{round}회차] test",
            bodyTemplate: "<h2>{round}회차</h2><p>{이메일}</p>{% for row in rows %}{{ row[\"컴퓨터 이름\"] }}{% endfor %}",
            round: 3,
            group: group);

        Assert.Equal("[3회차] test", rendered.Subject);
        Assert.Contains("3회차", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("a@example.com", rendered.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PC1", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("PC2", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_SubstitutesColumnTokenFromFirstRow()
    {
        var group = new EmailGroup(
            Email: "a@example.com",
            Rows: new[]
            {
                new ExcelRow("f.xlsx", 2, "a@example.com", new Dictionary<string, string?>
                {
                    ["컴퓨터 이름"] = "PC1"
                }),
                new ExcelRow("f.xlsx", 3, "a@example.com", new Dictionary<string, string?>
                {
                    ["컴퓨터 이름"] = "PC2"
                })
            });

        var rendered = MailTemplateRenderer.Render(
            subjectTemplate: "[{round}회차] test",
            bodyTemplate: "<p>{컴퓨터 이름}</p>",
            round: 1,
            group: group);

        Assert.Contains("PC1", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("PC2", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_AllowsJinjaRoundVariable()
    {
        var group = new EmailGroup(
            Email: "a@example.com",
            Rows: new[]
            {
                new ExcelRow("f.xlsx", 2, "a@example.com", new Dictionary<string, string?>())
            });

        var rendered = MailTemplateRenderer.Render(
            subjectTemplate: "[{round}회차] test",
            bodyTemplate: "<p>{{ round }}</p>",
            round: 7,
            group: group);

        Assert.Contains(">7<", rendered.HtmlBody, StringComparison.Ordinal);
    }
}

