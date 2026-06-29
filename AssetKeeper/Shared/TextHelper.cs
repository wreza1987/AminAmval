namespace AssetKeeper.Shared;

public static class TextHelper
{
    // نیم‌فاصله را با فاصله یکی درنظر می‌گیره
    public static string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace('\u200C', ' ').Trim(); // \u200C = نیم‌فاصله
    }
}