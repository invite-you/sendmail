using SendMail.Core.Validation;

namespace SendMail.Core.Tests;

public class RecipientCalculatorTests
{
    [Fact]
    public void FromEmailColumnValues_TrimsSkipsBlankAndDistinctsCaseInsensitive()
    {
        var input = new[]
        {
            " a@example.com ",
            "",
            "   ",
            "A@EXAMPLE.COM",
            "b@example.com"
        };

        var recipients = RecipientCalculator.FromEmailColumnValues(input);

        Assert.Equal(2, recipients.Count);
        Assert.Contains("a@example.com", recipients);
        Assert.Contains("b@example.com", recipients);
    }
}
