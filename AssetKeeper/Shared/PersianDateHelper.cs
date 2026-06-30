using System.Globalization;

namespace AssetKeeper.Shared;

public static class PersianDateHelper
{
    private static readonly PersianCalendar _pc = new();

    // میلادی → شمسی (نمایش)
    public static string ToPersian(this DateTime date, string format = "yyyy/MM/dd")
    {
        int y = _pc.GetYear(date);
        int m = _pc.GetMonth(date);
        int d = _pc.GetDayOfMonth(date);

        return format
            .Replace("yyyy", y.ToString("0000"))
            .Replace("MM", m.ToString("00"))
            .Replace("dd", d.ToString("00"));
    }

    public static string ToPersian(this DateTime? date, string format = "yyyy/MM/dd")
        => date.HasValue ? date.Value.ToPersian(format) : "—";

    public static string ToPersianWithTime(this DateTime date)
        => $"{date.ToPersian()} {date:HH:mm}";

    // شمسی (رشته ورودی کاربر) → میلادی
    public static DateTime? FromPersian(string? persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return null;

        var parts = persianDate.Replace('-', '/').Split('/');
        if (parts.Length != 3) return null;

        if (!int.TryParse(parts[0], out int y) ||
            !int.TryParse(parts[1], out int m) ||
            !int.TryParse(parts[2], out int d))
            return null;

        try
        {
            return _pc.ToDateTime(y, m, d, 0, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }
}