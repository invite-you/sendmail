namespace SendMail.Core.Excel;

public readonly record struct YearMonth(int Year, int Month)
{
    public int Value => (Year * 100) + Month;

    public static YearMonth FromYyyyMm(int yyyyMm)
    {
        var year = yyyyMm / 100;
        var month = yyyyMm % 100;
        return new YearMonth(year, month);
    }

    public YearMonth AddMonths(int months)
    {
        if (months == 0)
        {
            return this;
        }

        var totalMonths = (Year * 12) + (Month - 1) + months;
        var newYear = totalMonths / 12;
        var newMonth = (totalMonths % 12) + 1;
        return new YearMonth(newYear, newMonth);
    }

    public override string ToString() => $"{Year:D4}{Month:D2}";
}
