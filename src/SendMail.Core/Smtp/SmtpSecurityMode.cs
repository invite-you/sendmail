namespace SendMail.Core.Smtp;

public enum SmtpSecurityMode
{
    Auto = 0,
    None = 1,
    SslOnConnect = 2,
    StartTls = 3,
    StartTlsWhenAvailable = 4
}

