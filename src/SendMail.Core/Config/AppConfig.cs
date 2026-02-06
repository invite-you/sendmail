namespace SendMail.Core.Config;

public sealed class AppConfig
{
    public AppSection App { get; set; } = new();
    public ExcelSection Excel { get; set; } = new();
    public SmtpSection Smtp { get; set; } = new();
    public MailSection Mail { get; set; } = new();
}

public sealed class AppSection
{
    public string OutputDir { get; set; } = "output";
    public string LogDir { get; set; } = "logs";
    public long MaxAttachmentBytes { get; set; } = 10 * 1024 * 1024;
    public string ExcelRegex { get; set; } = "^(?<date>\\d{8}).*\\.xlsx$";
}

public sealed class ExcelSection
{
    public string Password { get; set; } = string.Empty;
    public string EmailColumn { get; set; } = "이메일";
}

public sealed class SmtpSection
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Security { get; set; } = "StartTls";
    public string Sender { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}

public sealed class MailSection
{
    public string Subject { get; set; } = "[{round}회차] 고위험자 안내";
    public string BodyPath { get; set; } = "config/body.html";
    public List<string> Attachments { get; set; } = new();
    public string DefaultTestRecipient { get; set; } = "test@example.com";
}

