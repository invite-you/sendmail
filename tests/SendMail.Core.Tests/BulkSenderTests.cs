using MimeKit;
using SendMail.Core.Models;
using SendMail.Core.Sending;

namespace SendMail.Core.Tests;

public class BulkSenderTests
{
    [Fact]
    public async Task SendAsync_SendsAllGroups_ReportsSent()
    {
        var groups = new[]
        {
            new EmailGroup(
                Email: "a@example.com",
                Rows: new[]
                {
                    new ExcelRow("f.xlsx", 2, "a@example.com", new Dictionary<string, string?>())
                }),
            new EmailGroup(
                Email: "b@example.com",
                Rows: new[]
                {
                    new ExcelRow("f.xlsx", 2, "b@example.com", new Dictionary<string, string?>())
                })
        };

        var rounds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["a@example.com"] = 2,
            ["b@example.com"] = 3
        };

        var sentTo = new List<string>();
        var subjects = new List<string>();

        Task FakeSendAsync(MimeMessage message, CancellationToken ct)
        {
            sentTo.Add(message.To.Mailboxes.Single().Address);
            subjects.Add(message.Subject);
            return Task.CompletedTask;
        }

        var clockNow = new DateTime(2026, 2, 9, 12, 30, 40);

        var attempts = await BulkSender.SendAsync(
            smtpSender: "sender@example.com",
            subjectTemplate: "[{round}] hello",
            bodyTemplate: "<p>{이메일}</p>",
            attachmentPaths: Array.Empty<string>(),
            groups: groups,
            roundsByEmail: rounds,
            sendAsync: FakeSendAsync,
            beforeEachSendAsync: null,
            clock: () => clockNow,
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { "a@example.com", "b@example.com" }, sentTo);
        Assert.Equal(new[] { "[2] hello", "[3] hello" }, subjects);

        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, a => Assert.Equal(SendAttemptStatus.Sent, a.Status));
        Assert.Equal(new[] { "a@example.com", "b@example.com" }, attempts.Select(a => a.Email).ToArray());
        Assert.Equal(new[] { 2, 3 }, attempts.Select(a => a.Round).ToArray());
        Assert.All(attempts, a => Assert.Equal(clockNow, a.Timestamp));
    }

    [Fact]
    public async Task SendAsync_OneSendThrows_RecordsFailureAndContinues()
    {
        var groups = new[]
        {
            new EmailGroup("a@example.com", new[] { new ExcelRow("f.xlsx", 2, "a@example.com", new Dictionary<string, string?>()) }),
            new EmailGroup("b@example.com", new[] { new ExcelRow("f.xlsx", 2, "b@example.com", new Dictionary<string, string?>()) }),
            new EmailGroup("c@example.com", new[] { new ExcelRow("f.xlsx", 2, "c@example.com", new Dictionary<string, string?>()) })
        };

        var rounds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["a@example.com"] = 1,
            ["b@example.com"] = 1,
            ["c@example.com"] = 1
        };

        var callCount = 0;
        Task FakeSendAsync(MimeMessage message, CancellationToken ct)
        {
            callCount++;
            var to = message.To.Mailboxes.Single().Address;
            if (string.Equals(to, "b@example.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("boom");
            }

            return Task.CompletedTask;
        }

        var now = new DateTime(2026, 2, 9, 12, 31, 0);

        var attempts = await BulkSender.SendAsync(
            smtpSender: "sender@example.com",
            subjectTemplate: "[{round}] hello",
            bodyTemplate: "<p>{이메일}</p>",
            attachmentPaths: Array.Empty<string>(),
            groups: groups,
            roundsByEmail: rounds,
            sendAsync: FakeSendAsync,
            beforeEachSendAsync: null,
            clock: () => now,
            cancellationToken: CancellationToken.None);

        Assert.Equal(3, callCount);
        Assert.Equal(3, attempts.Count);

        var a = attempts.Single(x => x.Email == "a@example.com");
        Assert.Equal(SendAttemptStatus.Sent, a.Status);

        var b = attempts.Single(x => x.Email == "b@example.com");
        Assert.Equal(SendAttemptStatus.Failed, b.Status);
        Assert.NotNull(b.ErrorCode);
        Assert.Contains("boom", b.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var c = attempts.Single(x => x.Email == "c@example.com");
        Assert.Equal(SendAttemptStatus.Sent, c.Status);
    }

    [Fact]
    public async Task SendAsync_WhenCanceled_MarksRemainingAsStoppedAndDoesNotSend()
    {
        var groups = new[]
        {
            new EmailGroup("a@example.com", new[] { new ExcelRow("f.xlsx", 2, "a@example.com", new Dictionary<string, string?>()) }),
            new EmailGroup("b@example.com", new[] { new ExcelRow("f.xlsx", 2, "b@example.com", new Dictionary<string, string?>()) }),
            new EmailGroup("c@example.com", new[] { new ExcelRow("f.xlsx", 2, "c@example.com", new Dictionary<string, string?>()) })
        };

        var rounds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["a@example.com"] = 1,
            ["b@example.com"] = 2,
            ["c@example.com"] = 3
        };

        var cts = new CancellationTokenSource();
        var callCount = 0;

        Task FakeSendAsync(MimeMessage message, CancellationToken ct)
        {
            callCount++;
            cts.Cancel(); // cancel after the first send attempt
            return Task.CompletedTask;
        }

        var now = new DateTime(2026, 2, 9, 12, 32, 0);

        var attempts = await BulkSender.SendAsync(
            smtpSender: "sender@example.com",
            subjectTemplate: "[{round}] hello",
            bodyTemplate: "<p>{이메일}</p>",
            attachmentPaths: Array.Empty<string>(),
            groups: groups,
            roundsByEmail: rounds,
            sendAsync: FakeSendAsync,
            beforeEachSendAsync: null,
            clock: () => now,
            cancellationToken: cts.Token);

        Assert.Equal(1, callCount);
        Assert.Equal(3, attempts.Count);

        var a = attempts.Single(x => x.Email == "a@example.com");
        Assert.Equal(SendAttemptStatus.Sent, a.Status);

        var b = attempts.Single(x => x.Email == "b@example.com");
        Assert.Equal(SendAttemptStatus.Failed, b.Status);
        Assert.Equal("US001", b.ErrorCode);

        var c = attempts.Single(x => x.Email == "c@example.com");
        Assert.Equal(SendAttemptStatus.Failed, c.Status);
        Assert.Equal("US001", c.ErrorCode);

        Assert.Equal(new[] { 1, 2, 3 }, attempts.Select(x => x.Round).ToArray());
    }
}

