using SendMail.Core.Template;

namespace SendMail.Core.Tests;

public class TemplateValidatorTests
{
    [Fact]
    public void Validate_FailsWhenSubjectMissingRoundToken()
    {
        var error = TemplateValidator.Validate(
            subject: "hello",
            bodyTemplate: "<p>{이메일}</p>",
            availableColumns: new[] { "이메일" },
            attachments: Array.Empty<AttachmentInfo>(),
            maxAttachmentBytes: 10);

        Assert.NotNull(error);
        Assert.Equal("TM001", error!.Code);
    }

    [Fact]
    public void Validate_FailsWhenBodyUsesUnknownToken()
    {
        var error = TemplateValidator.Validate(
            subject: "[{round}] hi",
            bodyTemplate: "<p>{unknown}</p>",
            availableColumns: new[] { "이메일" },
            attachments: Array.Empty<AttachmentInfo>(),
            maxAttachmentBytes: 10);

        Assert.NotNull(error);
        Assert.Equal("TM002", error!.Code);
        Assert.Contains("unknown", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FailsWhenJinja2RowUsesUnknownColumn()
    {
        var error = TemplateValidator.Validate(
            subject: "[{round}] hi",
            bodyTemplate: "<p>{{ row[\"does-not-exist\"] }}</p>",
            availableColumns: new[] { "이메일" },
            attachments: Array.Empty<AttachmentInfo>(),
            maxAttachmentBytes: 10);

        Assert.NotNull(error);
        Assert.Equal("TM002", error!.Code);
        Assert.Contains("does-not-exist", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_AllowsCaseInsensitiveTokenMatching()
    {
        var error = TemplateValidator.Validate(
            subject: "[{Round}] hi",
            bodyTemplate: "<p>{이메일} / {NIC1_IP}</p>",
            availableColumns: new[] { "이메일", "nic1_ip" },
            attachments: new[] { new AttachmentInfo("a.txt", LengthBytes: 9, Exists: true) },
            maxAttachmentBytes: 10);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_IgnoresCssBraces()
    {
        var error = TemplateValidator.Validate(
            subject: "[{round}] hi",
            bodyTemplate: "<style>body { color: red; }</style><p>{이메일}</p>",
            availableColumns: new[] { "이메일" },
            attachments: Array.Empty<AttachmentInfo>(),
            maxAttachmentBytes: 10);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_FailsWhenAttachmentMissing()
    {
        var error = TemplateValidator.Validate(
            subject: "[{round}] hi",
            bodyTemplate: "<p>{이메일}</p>",
            availableColumns: new[] { "이메일" },
            attachments: new[] { new AttachmentInfo("missing.pdf", LengthBytes: 0, Exists: false) },
            maxAttachmentBytes: 10);

        Assert.NotNull(error);
        Assert.Equal("TM003", error!.Code);
    }

    [Fact]
    public void Validate_FailsWhenAttachmentTooLarge()
    {
        var error = TemplateValidator.Validate(
            subject: "[{round}] hi",
            bodyTemplate: "<p>{이메일}</p>",
            availableColumns: new[] { "이메일" },
            attachments: new[] { new AttachmentInfo("big.zip", LengthBytes: 11, Exists: true) },
            maxAttachmentBytes: 10);

        Assert.NotNull(error);
        Assert.Equal("TM004", error!.Code);
    }
}
