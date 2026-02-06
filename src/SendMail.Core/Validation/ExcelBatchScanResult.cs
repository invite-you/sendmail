using SendMail.Core.Excel;

namespace SendMail.Core.Validation;

public sealed record ExcelBatchScanResult(
    IReadOnlyList<ExcelBatchFile> Files,
    YearMonth MinMonth,
    YearMonth MaxMonth)
{
    public int FileCount => Files.Count;
}
