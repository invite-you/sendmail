namespace SendMail.Core.Models;

public sealed record EmailGroup(string Email, IReadOnlyList<ExcelRow> Rows);

