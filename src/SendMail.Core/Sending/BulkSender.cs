namespace SendMail.Core.Sending;

public static class BulkSender
{
    public static Task<IReadOnlyList<SendAttempt>> SendAsync(
        string smtpSender,
        string subjectTemplate,
        string bodyTemplate,
        IReadOnlyList<string> attachmentPaths,
        IReadOnlyList<SendMail.Core.Models.EmailGroup> groups,
        IReadOnlyDictionary<string, int> roundsByEmail,
        Func<MimeKit.MimeMessage, CancellationToken, Task> sendAsync,
        Func<CancellationToken, Task>? beforeEachSendAsync = null,
        Func<DateTime>? clock = null,
        CancellationToken cancellationToken = default)
    {
        return SendInternalAsync(
            smtpSender,
            subjectTemplate,
            bodyTemplate,
            attachmentPaths,
            groups,
            roundsByEmail,
            sendAsync,
            beforeEachSendAsync,
            clock,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<SendAttempt>> SendInternalAsync(
        string smtpSender,
        string subjectTemplate,
        string bodyTemplate,
        IReadOnlyList<string> attachmentPaths,
        IReadOnlyList<SendMail.Core.Models.EmailGroup> groups,
        IReadOnlyDictionary<string, int> roundsByEmail,
        Func<MimeKit.MimeMessage, CancellationToken, Task> sendAsync,
        Func<CancellationToken, Task>? beforeEachSendAsync,
        Func<DateTime>? clock,
        CancellationToken cancellationToken)
    {
        if (sendAsync is null)
        {
            throw new ArgumentNullException(nameof(sendAsync));
        }

        clock ??= static () => DateTime.Now;

        var sender = smtpSender?.Trim() ?? string.Empty;
        var subjectTpl = subjectTemplate ?? string.Empty;
        var bodyTpl = bodyTemplate ?? string.Empty;
        var attachments = attachmentPaths ?? Array.Empty<string>();

        var attempts = new List<SendAttempt>(capacity: groups?.Count ?? 0);

        if (groups is null || groups.Count == 0)
        {
            return attempts;
        }

        for (var i = 0; i < groups.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                MarkRemainingAsStopped(attempts, groups, roundsByEmail, startIndex: i, clock());
                break;
            }

            if (beforeEachSendAsync is not null)
            {
                try
                {
                    await beforeEachSendAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    MarkRemainingAsStopped(attempts, groups, roundsByEmail, startIndex: i, clock());
                    break;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                MarkRemainingAsStopped(attempts, groups, roundsByEmail, startIndex: i, clock());
                break;
            }

            var group = groups[i];
            var toEmail = (group.Email ?? string.Empty).Trim();
            var round = roundsByEmail is not null && roundsByEmail.TryGetValue(toEmail, out var r) ? r : 1;

            try
            {
                var rendered = SendMail.Core.Template.MailTemplateRenderer.Render(
                    subjectTemplate: subjectTpl,
                    bodyTemplate: bodyTpl,
                    round: round,
                    group: group);

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(string.Empty, sender));
                message.ReplyTo.Add(new MimeKit.MailboxAddress(string.Empty, sender));
                message.To.Add(new MimeKit.MailboxAddress(string.Empty, toEmail));
                message.Subject = rendered.Subject;

                var builder = new MimeKit.BodyBuilder { HtmlBody = rendered.HtmlBody };
                foreach (var attachment in attachments)
                {
                    if (string.IsNullOrWhiteSpace(attachment))
                    {
                        continue;
                    }

                    builder.Attachments.Add(attachment);
                }

                message.Body = builder.ToMessageBody();

                await sendAsync(message, cancellationToken).ConfigureAwait(false);

                attempts.Add(new SendAttempt(
                    Email: toEmail,
                    Round: round,
                    Status: SendAttemptStatus.Sent,
                    ErrorCode: null,
                    ErrorMessage: null,
                    Timestamp: clock()));
            }
            catch (OperationCanceledException)
            {
                attempts.Add(new SendAttempt(
                    Email: toEmail,
                    Round: round,
                    Status: SendAttemptStatus.Failed,
                    ErrorCode: "US001",
                    ErrorMessage: "Stopped by user.",
                    Timestamp: clock()));

                MarkRemainingAsStopped(attempts, groups, roundsByEmail, startIndex: i + 1, clock());
                break;
            }
            catch (Exception ex)
            {
                // Keep going: no retries, but one failure shouldn't block other recipients.
                attempts.Add(new SendAttempt(
                    Email: toEmail,
                    Round: round,
                    Status: SendAttemptStatus.Failed,
                    ErrorCode: "SM001",
                    ErrorMessage: ex.Message,
                    Timestamp: clock()));
            }
        }

        return attempts;
    }

    private static void MarkRemainingAsStopped(
        List<SendAttempt> attempts,
        IReadOnlyList<SendMail.Core.Models.EmailGroup> groups,
        IReadOnlyDictionary<string, int>? roundsByEmail,
        int startIndex,
        DateTime timestamp)
    {
        for (var i = startIndex; i < groups.Count; i++)
        {
            var email = (groups[i].Email ?? string.Empty).Trim();
            var round = roundsByEmail is not null && roundsByEmail.TryGetValue(email, out var r) ? r : 1;

            attempts.Add(new SendAttempt(
                Email: email,
                Round: round,
                Status: SendAttemptStatus.Failed,
                ErrorCode: "US001",
                ErrorMessage: "Stopped by user.",
                Timestamp: timestamp));
        }
    }
}
