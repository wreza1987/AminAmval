using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AssetKeeper.Shared;

public static class EnumHelper
{
    public static string GetDisplayName(Enum value)
    {
        if (value == null) return string.Empty;

        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? value.ToString();
    }
}
