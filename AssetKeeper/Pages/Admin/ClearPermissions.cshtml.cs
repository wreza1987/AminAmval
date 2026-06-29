using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Admin;

public class ClearPermissionsModel : PageModel
{
    private readonly MyDbContext _context;
    public ClearPermissionsModel(MyDbContext context) => _context = context;
    public string Message { get; set; } = "";

    public async Task OnGetAsync()
    {
        // پاک کردن قبلی‌ها
        var all = await _context.PagePermissions.ToListAsync();
        _context.PagePermissions.RemoveRange(all);
        await _context.SaveChangesAsync();

        // Seed جدید
        var pages = new[]
        {
            ("Index",               "داشبورد"),
            ("Assets/Index",        "لیست اموال"),
            ("Assets/Details",      "جزئیات اموال"),
            ("Assets/Create",       "ثبت اموال"),
            ("Assets/Edit",         "ویرایش اموال"),
            ("Assets/Status",       "تغییر وضعیت"),
            ("Employees/Index",     "لیست پرسنل"),
            ("Employees/Create",    "ثبت پرسنل"),
            ("Employees/Edit",      "ویرایش پرسنل"),
            ("Employees/Details",   "جزئیات پرسنل"),
            ("Assignments/Index",   "تخصیص‌ها"),
            ("Assignments/Create",  "تخصیص جدید"),
            ("Assignments/Return",  "عودت اموال"),
            ("Requests/Index",      "درخواست‌های کاربران"),
            ("Categories/Index",    "دسته‌بندی‌ها"),
            ("Brands/Index",        "برندها"),
            ("Logs/AssetLogs",    "لاگ اموال"),
            ("Logs/EmployeeLogs", "لاگ پرسنل"),
        };

        foreach (var (key, title) in pages)
        {
            _context.PagePermissions.Add(new PagePermission
            {
                PageKey = key,
                PageTitle = title,
                AccessLevel = EmployeeAccessLevel.WarehouseKeeper,
                IsAllowed = true
            });
            _context.PagePermissions.Add(new PagePermission
            {
                PageKey = key,
                PageTitle = title,
                AccessLevel = EmployeeAccessLevel.Normal,
                IsAllowed = false
            });
        }

        await _context.SaveChangesAsync();
        var count = await _context.PagePermissions.CountAsync();
        Message = $"✅ {count} رکورد PagePermission ثبت شد.";
    }
}