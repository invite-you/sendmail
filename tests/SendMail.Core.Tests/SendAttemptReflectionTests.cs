namespace SendMail.Core.Tests;

public class SendAttemptReflectionTests
{
    [Fact]
    public void TypeExists()
    {
        var type = Type.GetType("SendMail.Core.Sending.SendAttempt, SendMail.Core");

        Assert.NotNull(type);
    }
}

