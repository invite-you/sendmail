namespace SendMail.Core.Models;

public sealed record ExcelRow(
    string SourceFile,
    int RowNumber,
    string Email,
    IReadOnlyDictionary<string, string?> Fields);

