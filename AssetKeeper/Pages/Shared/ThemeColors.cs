namespace AssetKeeper.Shared;

public static class ThemeColors
{
    // ==================== رنگ‌های پیش‌فرض Bootstrap ====================
    public static class Buttons
    {
        public const string Primary = "btn btn-primary";
        public const string Success = "btn btn-success";
        public const string Warning = "btn btn-warning";
        public const string Danger = "btn btn-danger";
        public const string Info = "btn btn-info";
        public const string Secondary = "btn btn-secondary";
        public const string Dark = "btn btn-dark";
    }

    // ==================== تولید رنگ دلخواه با RGB ====================
    public static class Custom
    {
        /// ThemeColors.Custom.Bg(255, 100, 50)
        public static string Bg(int r, int g, int b, double alpha = 1.0)
            => $"background-color: rgba({r}, {g}, {b}, {alpha});";

        public static string Text(int r, int g, int b)
            => $"color: rgb({r}, {g}, {b});";

        public static string Border(int r, int g, int b)
            => $"border-color: rgb({r}, {g}, {b});";

        public static string Button(int r, int g, int b, string textColor = "white", int fontWeight = 500)
            //=> $"btn btn-custom" +
            //$" style='background-color: rgb({r},{g},{b}); color: {textColor}; border-color: rgb({r},{g},{b});'";
            =>  $"btn" +
                $" style='background-color: rgb({r},{g},{b});" +
                $" color: {textColor};" +
                $" border-color: rgb({r},{g},{b});" +
                $" font-weight: {fontWeight};'";
    }

    // ==================== رنگ‌های ثابت کاربردی ====================
    public static class Status
    {
        public const string InStock = "bg-success text-white";
        public const string Assigned = "bg-primary text-white";
        public const string UnderRepair = "bg-warning text-dark";
        public const string Scrapped = "bg-danger text-white";
        public const string Lost = "bg-dark text-white";
        public const string Disabled = "bg-secondary text-white";
    }
}