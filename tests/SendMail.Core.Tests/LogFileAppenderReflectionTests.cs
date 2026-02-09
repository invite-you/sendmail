namespace SendMail.Core.Tests;

public class LogFileAppenderReflectionTests
{
    [Fact]
    public void TypeExists()
    {
        var type = Type.GetType("SendMail.Core.Output.LogFileAppender, SendMail.Core");

        Assert.NotNull(type);
    }

    [Fact]
    public void HasAppend()
    {
        var type = Type.GetType("SendMail.Core.Output.LogFileAppender, SendMail.Core");
        Assert.NotNull(type);

        var method = type!.GetMethod("Append", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
    }
}
