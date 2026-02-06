using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace SendMail.Services;

public sealed class ExcelInteropReader
{
    public sealed record CellString(int RowNumber, string Value);

    public IReadOnlyList<string> ReadColumnValues(string workbookPath, string password, string columnHeader)
    {
        if (string.IsNullOrWhiteSpace(workbookPath))
        {
            throw new ArgumentException("Workbook path is empty.", nameof(workbookPath));
        }

        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("Workbook file not found.", workbookPath);
        }

        if (string.IsNullOrWhiteSpace(columnHeader))
        {
            throw new ArgumentException("Column header is empty.", nameof(columnHeader));
        }

        Excel.Application? app = null;
        Excel.Workbooks? workbooks = null;
        Excel.Workbook? workbook = null;
        Excel.Sheets? sheets = null;
        Excel.Worksheet? worksheet = null;
        Excel.Range? usedRange = null;

        try
        {
            app = new Excel.Application
            {
                Visible = false,
                DisplayAlerts = false
            };

            workbooks = app.Workbooks;

            try
            {
                workbook = workbooks.Open(
                    Filename: workbookPath,
                    ReadOnly: true,
                    Password: string.IsNullOrWhiteSpace(password) ? Type.Missing : password);
            }
            catch (COMException ex)
            {
                throw new ExcelInteropException("EX003", $"Failed to open workbook: {Path.GetFileName(workbookPath)}", ex);
            }

            sheets = workbook.Worksheets;
            if (sheets.Count != 1)
            {
                throw new ExcelInteropException("EX010", $"Workbook must have exactly one sheet: {Path.GetFileName(workbookPath)}");
            }

            worksheet = (Excel.Worksheet)sheets[1];
            usedRange = worksheet.UsedRange;

            var value = usedRange.Value2;
            if (value is null)
            {
                return Array.Empty<string>();
            }

            var values = To2dArray(value);
            var rowLBound = values.GetLowerBound(0);
            var rowUBound = values.GetUpperBound(0);
            var colLBound = values.GetLowerBound(1);
            var colUBound = values.GetUpperBound(1);

            var headerRow = rowLBound;
            var targetCol = FindHeaderColumn(values, headerRow, colLBound, colUBound, columnHeader);
            if (targetCol < colLBound)
            {
                throw new InvalidOperationException($"Email column not found: '{columnHeader}' ({Path.GetFileName(workbookPath)})");
            }

            var results = new List<string>();
            for (var r = headerRow + 1; r <= rowUBound; r++)
            {
                var raw = values.GetValue(r, targetCol);
                var text = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                results.Add(text);
            }

            return results;
        }
        finally
        {
            try
            {
                workbook?.Close(SaveChanges: false);
            }
            catch
            {
                // Best-effort cleanup.
            }

            try
            {
                app?.Quit();
            }
            catch
            {
                // Best-effort cleanup.
            }

            ReleaseComObject(usedRange);
            ReleaseComObject(worksheet);
            ReleaseComObject(sheets);
            ReleaseComObject(workbook);
            ReleaseComObject(workbooks);
            ReleaseComObject(app);

            // Ensure RCWs are collected.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public IReadOnlyList<CellString> ReadColumnCellStrings(string workbookPath, string password, string columnHeader)
    {
        if (string.IsNullOrWhiteSpace(workbookPath))
        {
            throw new ArgumentException("Workbook path is empty.", nameof(workbookPath));
        }

        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("Workbook file not found.", workbookPath);
        }

        if (string.IsNullOrWhiteSpace(columnHeader))
        {
            throw new ArgumentException("Column header is empty.", nameof(columnHeader));
        }

        Excel.Application? app = null;
        Excel.Workbooks? workbooks = null;
        Excel.Workbook? workbook = null;
        Excel.Sheets? sheets = null;
        Excel.Worksheet? worksheet = null;
        Excel.Range? usedRange = null;

        try
        {
            app = new Excel.Application
            {
                Visible = false,
                DisplayAlerts = false
            };

            workbooks = app.Workbooks;

            try
            {
                workbook = workbooks.Open(
                    Filename: workbookPath,
                    ReadOnly: true,
                    Password: string.IsNullOrWhiteSpace(password) ? Type.Missing : password);
            }
            catch (COMException ex)
            {
                throw new ExcelInteropException("EX003", $"Failed to open workbook: {Path.GetFileName(workbookPath)}", ex);
            }

            sheets = workbook.Worksheets;
            if (sheets.Count != 1)
            {
                throw new ExcelInteropException("EX010", $"Workbook must have exactly one sheet: {Path.GetFileName(workbookPath)}");
            }

            worksheet = (Excel.Worksheet)sheets[1];
            usedRange = worksheet.UsedRange;

            var value = usedRange.Value2;
            if (value is null)
            {
                return Array.Empty<CellString>();
            }

            var values = To2dArray(value);
            var rowLBound = values.GetLowerBound(0);
            var rowUBound = values.GetUpperBound(0);
            var colLBound = values.GetLowerBound(1);
            var colUBound = values.GetUpperBound(1);

            var rangeFirstRow = usedRange.Row;
            var headerRow = rowLBound;
            var targetCol = FindHeaderColumn(values, headerRow, colLBound, colUBound, columnHeader);
            if (targetCol < colLBound)
            {
                throw new InvalidOperationException($"Email column not found: '{columnHeader}' ({Path.GetFileName(workbookPath)})");
            }

            var results = new List<CellString>();
            for (var r = headerRow + 1; r <= rowUBound; r++)
            {
                var raw = values.GetValue(r, targetCol);
                var text = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var excelRow = rangeFirstRow + (r - rowLBound);
                results.Add(new CellString(excelRow, text));
            }

            return results;
        }
        finally
        {
            try
            {
                workbook?.Close(SaveChanges: false);
            }
            catch
            {
                // Best-effort cleanup.
            }

            try
            {
                app?.Quit();
            }
            catch
            {
                // Best-effort cleanup.
            }

            ReleaseComObject(usedRange);
            ReleaseComObject(worksheet);
            ReleaseComObject(sheets);
            ReleaseComObject(workbook);
            ReleaseComObject(workbooks);
            ReleaseComObject(app);

            // Ensure RCWs are collected.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static int FindHeaderColumn(
        Array values,
        int headerRow,
        int colLBound,
        int colUBound,
        string columnHeader)
    {
        for (var c = colLBound; c <= colUBound; c++)
        {
            var header = Convert.ToString(values.GetValue(headerRow, c), CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            if (string.Equals(header, columnHeader, StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }

        return -1;
    }

    private static Array To2dArray(object value)
    {
        if (value is Array array && array.Rank == 2)
        {
            return array;
        }

        // Single-cell UsedRange can return a scalar.
        var single = Array.CreateInstance(typeof(object), new[] { 1, 1 }, new[] { 1, 1 });
        single.SetValue(value, 1, 1);
        return single;
    }

    private static void ReleaseComObject(object? obj)
    {
        if (obj is null)
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(obj);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
