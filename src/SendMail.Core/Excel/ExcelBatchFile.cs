namespace SendMail.Core.Excel;

public sealed record ExcelBatchFile(
    string FullPath,
    string FileName,
    DateOnly Date,
    YearMonth Month);
