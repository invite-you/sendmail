using System.Globalization;
using SendMail.Core.Models;
using SendMail.Core.Output;

namespace SendMail.Core.Tests;

public class LogFileAppenderTests
{
    [Fact]
    public void Append_WritesToLogsAppYyyyMmDd()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sendmail-tests", Guid.NewGuid().ToString("N"));

        var entry = new LogEntry(
            Timestamp: new DateTime(2026, 2, 9, 1, 2, 3),
            Level: LogLevel.Warning,
            Message: "hello");

        var path = LogFileAppender.Append(tempDir, entry);

        Assert.True(File.Exists(path));
        Assert.EndsWith(Path.Combine(tempDir, "app-20260209.log"), path, StringComparison.Ordinal);

        var lines = File.ReadAllLines(path);
        Assert.Single(lines);

        var ts = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        Assert.Equal($"{ts} [WARNING] hello", lines[0]);
    }
}

