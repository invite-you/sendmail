using SendMail.Core.Smtp;

namespace SendMail.Core.Tests;

public class SmtpSecurityParserTests
{
    [Theory]
    [InlineData("None", SmtpSecurityMode.None)]
    [InlineData("Auto", SmtpSecurityMode.Auto)]
    [InlineData("SslOnConnect", SmtpSecurityMode.SslOnConnect)]
    [InlineData("StartTls", SmtpSecurityMode.StartTls)]
    [InlineData("StartTlsWhenAvailable", SmtpSecurityMode.StartTlsWhenAvailable)]
    [InlineData("starttls", SmtpSecurityMode.StartTls)]
    public void Parse_KnownValues_ReturnsMode(string input, SmtpSecurityMode expected)
    {
        var actual = SmtpSecurityParser.Parse(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_UnknownValue_DefaultsToAuto()
    {
        var actual = SmtpSecurityParser.Parse("???");
        Assert.Equal(SmtpSecurityMode.Auto, actual);
    }
}

