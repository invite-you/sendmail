namespace SendMail.Core.Output;

public static class LogFileAppender
{
    public static string Append(string logDir, SendMail.Core.Models.LogEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(logDir))
        {
            throw new ArgumentException("Log directory is empty.", nameof(logDir));
        }

        Directory.CreateDirectory(logDir);

        var fileName = $"app-{entry.Timestamp:yyyyMMdd}.log";
        var path = Path.Combine(logDir, fileName);

        var ts = entry.Timestamp.ToString(
            "yyyy-MM-dd HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture);

        var level = entry.Level.ToString().ToUpperInvariant();
        var line = $"{ts} [{level}] {entry.Message}";

        var utf8Bom = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        File.AppendAllText(path, line + Environment.NewLine, utf8Bom);

        return path;
    }
}
