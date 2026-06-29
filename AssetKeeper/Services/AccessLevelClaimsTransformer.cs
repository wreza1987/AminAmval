using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetKeeper.Services;

public class AccessLevelClaimsTransformer : IClaimsTransformation
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MyDbContext _context;

    public AccessLevelClaimsTransformer(UserManager<ApplicationUser> userManager, MyDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var user = await _userManager.GetUserAsync(principal);
        if (user?.EmployeeId == null) return principal;

        var employee = await _context.Employees.FindAsync(user.EmployeeId);
        if (employee == null) return principal;

        // هر بار از دیتابیس می‌خونه → تغییر AccessLevel بلافاصله اعمال میشه
        var identity = (ClaimsIdentity)principal.Identity;
        var existing = identity.FindFirst("AccessLevel");
        if (existing != null) identity.RemoveClaim(existing);
        identity.AddClaim(new Claim("AccessLevel", employee.AccessLevel.ToString()));

        return principal;
    }
}