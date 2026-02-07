using System;
using System.Collections.Generic;
using System.Linq;

namespace SendMail.Core.Template;

public static class TemplateValidator
{
    public static TemplateValidationError? Validate(
        string subject,
        string bodyTemplate,
        IReadOnlyCollection<string> availableColumns,
        IReadOnlyList<AttachmentInfo> attachments,
        long maxAttachmentBytes)
    {
        var subjectTokens = TokenScanner.ScanSingleBraceTokens(subject ?? string.Empty);
        if (!subjectTokens.Any(t => string.Equals(t, "round", StringComparison.OrdinalIgnoreCase)))
        {
            return new TemplateValidationError("TM001", "Subject must contain {round}.");
        }

        var columnSet = new HashSet<string>(
            (availableColumns ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var bodyColumnRefs = new HashSet<string>(
            TokenScanner.ScanSingleBraceTokens(bodyTemplate ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        bodyColumnRefs.UnionWith(TokenScanner.ScanJinja2RowColumnKeys(bodyTemplate ?? string.Empty));

        foreach (var token in bodyColumnRefs)
        {
            if (string.Equals(token, "round", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!columnSet.Contains(token))
            {
                return new TemplateValidationError("TM002", $"Unknown token: {token}");
            }
        }

        foreach (var attachment in attachments ?? Array.Empty<AttachmentInfo>())
        {
            if (!attachment.Exists)
            {
                return new TemplateValidationError("TM003", $"Attachment not found: {attachment.Path}");
            }

            if (attachment.LengthBytes > maxAttachmentBytes)
            {
                return new TemplateValidationError(
                    "TM004",
                    $"Attachment too large: {attachment.Path} ({attachment.LengthBytes} bytes)");
            }
        }

        return null;
    }
}
