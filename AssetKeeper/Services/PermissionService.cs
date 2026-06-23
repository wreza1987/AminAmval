using AssetKeeper.Context;
using AssetKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AssetKeeper.Services;

public class PermissionService
{
    private readonly MyDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionService(MyDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> IsAllowedAsync(string pageKey, EmployeeAccessLevel level)
    {
        // Admin همیشه دسترسی داره
        if (level == EmployeeAccessLevel.Admin) return true;
        // Disable هیچ‌وقت دسترسی نداره
        if (level == EmployeeAccessLevel.Disable) return false;

        var cacheKey = $"perm_{pageKey}_{level}";
        if (_cache.TryGetValue(cacheKey, out bool cached)) return cached;

        var perm = await _context.PagePermissions
            .FirstOrDefaultAsync(p => p.PageKey == pageKey && p.AccessLevel == level);

        var result = perm?.IsAllowed ?? false;
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public void ClearCache()
    {
        // بعد از تغییر توسط ادمین کش رو پاک می‌کنیم
        if (_cache is MemoryCache mc) mc.Clear();
    }
}