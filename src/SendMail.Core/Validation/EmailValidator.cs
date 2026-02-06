using MimeKit;

namespace SendMail.Core.Validation;

public static class EmailValidator
{
    // We accept only a pure addr-spec (no display name, no angle brackets).
    public static bool IsValidRfc5322AddrSpec(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.Contains('@'))
        {
            return false;
        }

        if (!MailboxAddress.TryParse(trimmed, out var mailbox))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(mailbox.Name))
        {
            return false;
        }

        return string.Equals(mailbox.Address, trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
