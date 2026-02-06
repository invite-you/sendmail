using SendMail.Core.Validation;

namespace SendMail.Core.Tests;

public class EmailValidatorTests
{
    [Fact]
    public void IsValidRfc5322AddrSpec_RejectsMissingAtSign()
    {
        Assert.False(EmailValidator.IsValidRfc5322AddrSpec("not-an-email"));
    }
}

