using System.Text.Json;
using AssetKeeper.Domain.Enums;

namespace AssetKeeper.Context;

public static class MappingHelper
{
    private static readonly string MappingsPath = Path.Combine("wwwroot", "Data", "Mappings.json");

    private static Dictionary<string, string>? _mappings;

    private static Dictionary<string, string> Mappings
    {
        get
        {
            if (_mappings == null)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), MappingsPath);
                if (File.Exists(fullPath))
                {
                    var json = File.ReadAllText(fullPath);
                    _mappings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                        ?.SelectMany(kv => kv.Value)
                        .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase)
                        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    _mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            return _mappings;
        }
    }

    public static AssetOwner NormalizeOwner(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AssetOwner.Unknown;

        var key = value.Trim();
        if (Mappings.TryGetValue(key, out var mapped) && Enum.TryParse<AssetOwner>(mapped, out var owner))
            return owner;

        return Enum.TryParse<AssetOwner>(key, true, out var direct) ? direct : AssetOwner.Unknown;
    }

    public static AssetStatus NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AssetStatus.InStock;

        var key = value.Trim();
        if (Mappings.TryGetValue(key, out var mapped) && Enum.TryParse<AssetStatus>(mapped, out var status))
            return status;

        return Enum.TryParse<AssetStatus>(key, true, out var direct) ? direct : AssetStatus.InStock;
    }

    public static EmployeeAccessLevel NormalizeAccessLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return EmployeeAccessLevel.Normal;

        var key = value.Trim();
        if (Mappings.TryGetValue(key, out var mapped) && Enum.TryParse<EmployeeAccessLevel>(mapped, out var level))
            return level;

        return Enum.TryParse<EmployeeAccessLevel>(key, true, out var direct) ? direct : EmployeeAccessLevel.Normal;
    }
}
