using SendMail.Core.Validation;

namespace SendMail.Core.Tests;

public class RecipientCalculatorTests
{
    [Fact]
    public void FromEmailColumnValues_UsesOnlyRfc5322ValidEmails()
    {
        var input = new[]
        {
            " a@example.com ",
            "not-an-email",
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
