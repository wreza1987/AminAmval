using AssetKeeper.Domain.Enums;
using AssetKeeper.Context;

namespace AssetKeeper.Shared;

public static class ImportNormalizer
{
    public static DateTime NormalizeDate(string? value) =>
        DateTime.TryParse(value, out var dt) ? dt.Date : DateTime.Today;

    public static EmployeeAccessLevel NormalizeAccessLevel(string? value) =>
        MappingHelper.NormalizeAccessLevel(value);

    public static AssetStatus NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AssetStatus.InStock;
        var key = value.Trim();
        if (Enum.TryParse<AssetStatus>(key, true, out var direct)) return direct;
        return MappingHelper.NormalizeStatus(value); // fallback به mapping فارسی/انگلیسی
    }

    public static AssetOwner NormalizeOwner(string? value) =>
        MappingHelper.NormalizeOwner(value);

    public static string NormalizeSerial(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial)) return "ندارد";
        var normalized = serial.Trim().ToLower();
        var emptyValues = new[]
        {
            "ندارد", "نامشخص", "نامعلوم", "مخدوش", "تعریف نشده",
            "0", "na", "none", "null", "-", "n/a", "نا", "ناموجود"
        };
        return emptyValues.Contains(normalized) ? "ندارد" : serial.Trim();
    }
}