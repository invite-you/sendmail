namespace SendMail.Core.Tests;

public class BatchOutputWriterReflectionTests
{
    [Fact]
    public void TypeExists()
    {
        var type = Type.GetType("SendMail.Core.Output.BatchOutputWriter, SendMail.Core");

        Assert.NotNull(type);
    }

    [Fact]
    public void HasWrite()
    {
        var type = Type.GetType("SendMail.Core.Output.BatchOutputWriter, SendMail.Core");
        Assert.NotNull(type);

        var method = type!.GetMethod("Write", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
    }
}
