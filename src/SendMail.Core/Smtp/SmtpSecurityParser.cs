using System;

namespace SendMail.Core.Smtp;

public static class SmtpSecurityParser
{
    public static SmtpSecurityMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SmtpSecurityMode.Auto;
        }

        var trimmed = value.Trim();

        if (string.Equals(trimmed, "None", StringComparison.OrdinalIgnoreCase))
        {
            return SmtpSecurityMode.None;
        }

        if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return SmtpSecurityMode.Auto;
        }

        if (string.Equals(trimmed, "SslOnConnect", StringComparison.OrdinalIgnoreCase))
        {
            return SmtpSecurityMode.SslOnConnect;
        }

        if (string.Equals(trimmed, "StartTls", StringComparison.OrdinalIgnoreCase))
        {
            return SmtpSecurityMode.StartTls;
        }

        if (string.Equals(trimmed, "StartTlsWhenAvailable", StringComparison.OrdinalIgnoreCase))
        {
            return SmtpSecurityMode.StartTlsWhenAvailable;
        }

        return SmtpSecurityMode.Auto;
    }
}
