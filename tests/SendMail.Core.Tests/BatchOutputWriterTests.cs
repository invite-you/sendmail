using System.Globalization;
using SendMail.Core.Output;
using SendMail.Core.Sending;

namespace SendMail.Core.Tests;

public class BatchOutputWriterTests
{
    [Fact]
    public void Write_WritesResultsAndFailuresCsv()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sendmail-tests", Guid.NewGuid().ToString("N"));

        var ts = new DateTime(2026, 2, 9, 12, 30, 40);
        var attempts = new[]
        {
            new SendAttempt(
                Email: "a@example.com",
                Round: 1,
                Status: SendAttemptStatus.Sent,
                ErrorCode: null,
                ErrorMessage: null,
                Timestamp: ts),
            new SendAttempt(
                Email: "b@example.com",
                Round: 2,
                Status: SendAttemptStatus.Failed,
                ErrorCode: "SM001",
                ErrorMessage: "bad, \"oops\"",
                Timestamp: ts)
        };

        var (resultsPath, failuresPath) = BatchOutputWriter.Write(
            outputDir: tempDir,
            batchId: "20260209-123040",
            attempts: attempts);

        Assert.True(File.Exists(resultsPath));
        Assert.True(File.Exists(failuresPath));

        var resultsLines = File.ReadAllLines(resultsPath);
        Assert.Equal("Email,Round,Status,ErrorCode,ErrorMessage,Timestamp", resultsLines[0]);

        var tsText = ts.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        Assert.Equal($"a@example.com,1,SENT,,,{tsText}", resultsLines[1]);
        Assert.Equal($"b@example.com,2,FAILED,SM001,\"bad, \"\"oops\"\"\",{tsText}", resultsLines[2]);

        var failureLines = File.ReadAllLines(failuresPath);
        Assert.Equal("Email,Round,ErrorCode,ErrorMessage,Timestamp", failureLines[0]);
        Assert.Single(failureLines.Skip(1));
        Assert.Equal($"b@example.com,2,SM001,\"bad, \"\"oops\"\"\",{tsText}", failureLines[1]);
    }
}
