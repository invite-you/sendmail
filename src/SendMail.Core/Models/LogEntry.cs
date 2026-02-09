namespace SendMail.Core.Models;

public sealed record LogEntry(DateTime Timestamp, LogLevel Level, string Message);

