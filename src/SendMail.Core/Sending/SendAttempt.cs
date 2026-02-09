namespace SendMail.Core.Sending;

public sealed record SendAttempt(
    string Email,
    int Round,
    SendAttemptStatus Status,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime Timestamp);

