using System;
using System.Collections.Generic;
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
                // Ignore non-matching files (they are not part of the batch policy).
                continue;
            }

            var dateText = match.Groups["date"].Value;
            var year = int.Parse(dateText.Substring(0, 4));
            var month = int.Parse(dateText.Substring(4, 2));
            var day = int.Parse(dateText.Substring(6, 2));

            var date = new DateOnly(year, month, day);
            var yearMonth = new YearMonth(year, month);

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
