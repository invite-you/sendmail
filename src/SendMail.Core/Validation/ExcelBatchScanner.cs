using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SendMail.Core.Excel;

namespace SendMail.Core.Validation;

public static class ExcelBatchScanner
{
    // Pattern: ^(?<date>\d{8}).*\.xlsx$
    private static readonly Regex FileNameRegex = new(
        "^(?<date>\\d{8}).*\\.xlsx$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static IReadOnlyList<string> EnumerateCandidateFiles(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(folderPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Order()
            .ToArray();
    }

    public static (ExcelBatchScanResult? Result, ExcelBatchScanError? Error) ValidateMonthlyContinuity(
        IEnumerable<string> xlsxPaths)
    {
        var parsed = new List<ExcelBatchFile>();

        foreach (var path in xlsxPaths)
        {
            var fileName = Path.GetFileName(path);
            var match = FileNameRegex.Match(fileName);
            if (!match.Success)
            {
                return (null, new ExcelBatchScanError(
                    "EX004",
                    $"Invalid Excel filename (pattern mismatch): {fileName}"));
            }

            var dateText = match.Groups["date"].Value;
            if (!DateOnly.TryParseExact(
                    dateText,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return (null, new ExcelBatchScanError(
                    "EX004",
                    $"Invalid Excel filename (invalid YYYYMMDD): {fileName}"));
            }

            var yearMonth = new YearMonth(date.Year, date.Month);

            parsed.Add(new ExcelBatchFile(path, fileName, date, yearMonth));
        }

        if (parsed.Count == 0)
        {
            // No candidates to open/validate.
            return (null, new ExcelBatchScanError("EX003", "No matching .xlsx files found."));
        }

        var monthGroups = parsed.GroupBy(f => f.Month.Value).ToArray();
        if (monthGroups.Any(g => g.Count() > 1))
        {
            return (null, new ExcelBatchScanError("EX001", "Duplicate YYYYMM detected (one file per month is allowed)."));
        }

        var months = monthGroups.Select(g => YearMonth.FromYyyyMm(g.Key)).OrderBy(m => m.Value).ToArray();
        for (var i = 1; i < months.Length; i++)
        {
            var expected = months[i - 1].AddMonths(1);
            if (months[i] != expected)
            {
                return (null, new ExcelBatchScanError("EX002", "Non-continuous YYYYMM range detected."));
            }
        }

        var orderedFiles = parsed.OrderBy(f => f.Month.Value).ToArray();
        var minMonth = months.First();
        var maxMonth = months.Last();

        return (new ExcelBatchScanResult(orderedFiles, minMonth, maxMonth), null);
    }
}
