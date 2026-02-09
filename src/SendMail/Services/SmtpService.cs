using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendMail.Core.Smtp;

namespace SendMail.Services;

public sealed class SmtpService
{
    public async Task VerifyConnectAndAuthAsync(
        string host,
        int port,
        SmtpSecurityMode security,
        string username,
        string password,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("SMTP host is empty.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "SMTP port must be 1..65535.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("SMTP username(sender) is empty.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("SMTP password is empty.", nameof(password));
        }

        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be > 0.");
        }

        using var client = new SmtpClient();
        client.Timeout = checked(timeoutSeconds * 1000);

        var options = MapSecurity(security);
        await client.ConnectAsync(host, port, options, cancellationToken).ConfigureAwait(false);
        await client.AuthenticateAsync(username, password, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync(
        string host,
        int port,
        SmtpSecurityMode security,
        string username,
        string password,
        int timeoutSeconds,
        MimeMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("SMTP host is empty.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "SMTP port must be 1..65535.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("SMTP username(sender) is empty.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("SMTP password is empty.", nameof(password));
        }

        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be > 0.");
        }

        using var client = new SmtpClient();
        client.Timeout = checked(timeoutSeconds * 1000);

        var options = MapSecurity(security);
        await client.ConnectAsync(host, port, options, cancellationToken).ConfigureAwait(false);
        await client.AuthenticateAsync(username, password, cancellationToken).ConfigureAwait(false);
        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }

    private static SecureSocketOptions MapSecurity(SmtpSecurityMode mode) =>
        mode switch
        {
            SmtpSecurityMode.None => SecureSocketOptions.None,
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.StartTlsWhenAvailable => SecureSocketOptions.StartTlsWhenAvailable,
            _ => SecureSocketOptions.Auto
        };
}
