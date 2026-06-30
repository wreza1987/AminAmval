using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using AssetKeeper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Admin;

[Authorize(Policy = "AdminOnly")]
public class AccessLevelsModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly PermissionService _permissionService;

    public AccessLevelsModel(MyDbContext context, PermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    public List<Employee> Employees { get; set; } = new();
    public List<PagePermission> Permissions { get; set; } = new();
    public List<string> PageKeys { get; set; } = new();

    [BindProperty] public int EmployeeId { get; set; }
    [BindProperty] public EmployeeAccessLevel NewLevel { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // تغییر سطح دسترسی کاربر
    public async Task<IActionResult> OnPostChangeAccessAsync()
    {
        var employee = await _context.Employees.FindAsync(EmployeeId);
        if (employee == null) return NotFound();

        employee.AccessLevel = NewLevel;
        await _context.SaveChangesAsync();
        _permissionService.ClearCache();

        TempData["Success"] = "سطح دسترسی تغییر کرد.";
        return RedirectToPage();
    }

    // تغییر دسترسی صفحه
    // public async Task<IActionResult> OnPostTogglePermissionAsync(
    //     string pageKey, EmployeeAccessLevel level, bool isAllowed)
    // {
    //     var perm = await _context.PagePermissions
    //         .FirstOrDefaultAsync(p => p.PageKey == pageKey && p.AccessLevel == level);

    //     if (perm != null)
    //     {
    //         perm.IsAllowed = isAllowed;
    //         await _context.SaveChangesAsync();
    //         _permissionService.ClearCache();
    //     }

    //     return new JsonResult(new { success = true });
    // }

public async Task<IActionResult> OnPostTogglePermissionAsync(
    string pageKey, 
    EmployeeAccessLevel level, 
    bool isAllowed)
{
    Console.WriteLine($"=== Toggle Called: Page={pageKey}, Level={level}, Allowed={isAllowed} ==="); // لاگ

    var perm = await _context.PagePermissions
        .FirstOrDefaultAsync(p => p.PageKey == pageKey && p.AccessLevel == level);

    if (perm != null)
    {
        perm.IsAllowed = isAllowed;
        await _context.SaveChangesAsync();
        _permissionService.ClearCache();
        Console.WriteLine("تغییرات ذخیره شد.");
    }
    else
    {
        Console.WriteLine("رکورد Permission پیدا نشد!");
    }

    return new JsonResult(new { success = true });
}



    private async Task LoadAsync()
    {
        Employees = await _context.Employees.OrderBy(e => e.PersonnelCode).ToListAsync();
        Permissions = await _context.PagePermissions.ToListAsync();
        PageKeys = Permissions.Select(p => p.PageKey).Distinct().OrderBy(k => k).ToList();
    }
}