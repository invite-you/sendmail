using System;
using System.Collections.Generic;

namespace SendMail.Core.Validation;

public static class RecipientCalculator
{
    public static IReadOnlyList<string> FromEmailColumnValues(IEnumerable<string?> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recipients = new List<string>();

        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                recipients.Add(trimmed);
            }
        }

        return recipients;
    }
}

