using System;
using System.Collections.Generic;

namespace SendMail.Core.Round;

public static class RoundCalculator
{
    public static IReadOnlyDictionary<string, int> CalculateLatestRounds(IReadOnlyList<IReadOnlySet<string>> emailsByMonth)
    {
        if (emailsByMonth is null)
        {
            throw new ArgumentNullException(nameof(emailsByMonth));
        }

        if (emailsByMonth.Count == 0)
        {
            throw new ArgumentException("At least one month is required.", nameof(emailsByMonth));
        }

        var previous = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var monthEmails in emailsByMonth)
        {
            if (monthEmails is null)
            {
                throw new ArgumentException("Month email set is null.", nameof(emailsByMonth));
            }

            var current = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var email in monthEmails)
            {
                if (previous.TryGetValue(email, out var prevRound))
                {
                    current[email] = prevRound + 1;
                }
                else
                {
                    current[email] = 1;
                }
            }

            previous = current;
        }

        return previous;
    }
}
