using SendMail.Core.Round;

namespace SendMail.Core.Tests;

public class RoundCalculatorTests
{
    [Fact]
    public void CalculateLatestRounds_AllMonthsPresent_Increments()
    {
        var months = new[]
        {
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a@example.com" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a@example.com" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a@example.com" }
        };

        var rounds = RoundCalculator.CalculateLatestRounds(months);

        Assert.Equal(3, rounds["a@example.com"]);
    }

    [Fact]
    public void CalculateLatestRounds_BreakInContinuity_ResetsTo1()
    {
        var months = new[]
        {
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "b@example.com" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "b@example.com" }
        };

        var rounds = RoundCalculator.CalculateLatestRounds(months);

        Assert.Equal(1, rounds["b@example.com"]);
    }
}

