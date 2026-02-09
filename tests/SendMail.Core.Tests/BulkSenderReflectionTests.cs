namespace SendMail.Core.Tests;

public class BulkSenderReflectionTests
{
    [Fact]
    public void TypeExists()
    {
        var type = Type.GetType("SendMail.Core.Sending.BulkSender, SendMail.Core");

        Assert.NotNull(type);
    }

    [Fact]
    public void HasSendAsync()
    {
        var type = Type.GetType("SendMail.Core.Sending.BulkSender, SendMail.Core");
        Assert.NotNull(type);

        var method = type!.GetMethod("SendAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
    }
}
