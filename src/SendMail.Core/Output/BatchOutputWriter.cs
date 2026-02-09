using SendMail.Core.Sending;

namespace SendMail.Core.Output;

public static class BatchOutputWriter
{
    public static (string ResultsPath, string FailuresPath) Write(
        string outputDir,
        string batchId,
        IReadOnlyList<SendAttempt> attempts)
    {
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            throw new ArgumentException("Output directory is empty.", nameof(outputDir));
        }

        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("BatchId is empty.", nameof(batchId));
        }

        Directory.CreateDirectory(outputDir);

        var resultsPath = Path.Combine(outputDir, $"results-{batchId}.csv");
        var failuresPath = Path.Combine(outputDir, $"failures-{batchId}.csv");

        var utf8Bom = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        using var resultsWriter = new StreamWriter(resultsPath, append: false, utf8Bom);
        using var failuresWriter = new StreamWriter(failuresPath, append: false, utf8Bom);

        resultsWriter.WriteLine("Email,Round,Status,ErrorCode,ErrorMessage,Timestamp");
        failuresWriter.WriteLine("Email,Round,ErrorCode,ErrorMessage,Timestamp");

        foreach (var attempt in attempts ?? Array.Empty<SendAttempt>())
        {
            var status = attempt.Status == SendAttemptStatus.Sent ? "SENT" : "FAILED";
            var ts = FormatTimestamp(attempt.Timestamp);

            resultsWriter.WriteLine(CsvJoin(
                attempt.Email,
                attempt.Round.ToString(System.Globalization.CultureInfo.InvariantCulture),
                status,
                attempt.ErrorCode ?? string.Empty,
                attempt.ErrorMessage ?? string.Empty,
                ts));

            if (attempt.Status == SendAttemptStatus.Failed)
            {
                failuresWriter.WriteLine(CsvJoin(
                    attempt.Email,
                    attempt.Round.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    attempt.ErrorCode ?? string.Empty,
                    attempt.ErrorMessage ?? string.Empty,
                    ts));
            }
        }

        return (resultsPath, failuresPath);
    }

    private static string FormatTimestamp(DateTime timestamp)
        => timestamp.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

    private static string CsvJoin(params string[] values)
        => string.Join(",", values.Select(CsvEscape));

    private static string CsvEscape(string? value)
    {
        var s = value ?? string.Empty;
        var mustQuote = s.IndexOfAny([',', '"', '\r', '\n']) >= 0;

        if (s.Contains('"'))
        {
            s = s.Replace("\"", "\"\"", StringComparison.Ordinal);
        }

        return mustQuote ? $"\"{s}\"" : s;
    }
}
