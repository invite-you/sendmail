using SendMail.Core.Validation;

namespace SendMail.Core.Tests;

public class ExcelBatchScannerTests
{
    [Fact]
    public void DuplicateMonth_ReturnsEx001()
    {
        var paths = new[]
        {
            "/tmp/20250101-a.xlsx",
            "/tmp/20250115-b.xlsx"
        };

        var (_, error) = ExcelBatchScanner.ValidateMonthlyContinuity(paths);

        Assert.NotNull(error);
        Assert.Equal("EX001", error!.Code);
    }

    [Fact]
    public void NonContinuousMonths_ReturnsEx002()
    {
        var paths = new[]
        {
            "/tmp/20250101.xlsx",
            "/tmp/20250301.xlsx"
        };

        var (_, error) = ExcelBatchScanner.ValidateMonthlyContinuity(paths);

        Assert.NotNull(error);
        Assert.Equal("EX002", error!.Code);
    }

    [Fact]
    public void ContinuousAcrossYear_Succeeds()
    {
        var paths = new[]
        {
            "/tmp/20231201.xlsx",
            "/tmp/20240101.xlsx",
            "/tmp/20240201.xlsx"
        };

        var (result, error) = ExcelBatchScanner.ValidateMonthlyContinuity(paths);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("202312", result!.MinMonth.ToString());
        Assert.Equal("202402", result!.MaxMonth.ToString());
        Assert.Equal(3, result.FileCount);
    }

    [Fact]
    public void NonMatchingFiles_ReturnsEx004()
    {
        var paths = new[]
        {
            "/tmp/readme.xlsx",
            "/tmp/20250101.xlsx"
        };

        var (_, error) = ExcelBatchScanner.ValidateMonthlyContinuity(paths);

        Assert.NotNull(error);
        Assert.Equal("EX004", error!.Code);
    }

    [Fact]
    public void InvalidDate_ReturnsEx004()
    {
        var paths = new[]
        {
            "/tmp/20251301.xlsx"
        };

        var (_, error) = ExcelBatchScanner.ValidateMonthlyContinuity(paths);

        Assert.NotNull(error);
        Assert.Equal("EX004", error!.Code);
    }
}
