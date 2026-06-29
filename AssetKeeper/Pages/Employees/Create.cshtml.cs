using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Employees;

[Authorize]
public class CreateModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(MyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public Employee Employee { get; set; } = new();

    public void OnGet()
    {
        // TempData.Remove("Success");
        Employee.StartDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (await _context.Employees.AnyAsync(e => e.PersonnelCode == Employee.PersonnelCode))
        {
            ModelState.AddModelError("Employee.PersonnelCode", "این کد پرسنلی قبلاً ثبت شده است.");
            return Page();
        }

        // انباردار فقط می‌تواند Normal ثبت کند
        var currentLevel = User.FindFirst("AccessLevel")?.Value;
        if (currentLevel != "Admin")
            Employee.AccessLevel = EmployeeAccessLevel.Normal;

        _context.Employees.Add(Employee);
        await _context.SaveChangesAsync();

        // ایجاد کاربر Identity — پسورد پیش‌فرض = کد ملی
        var user = new ApplicationUser
        {
            UserName = Employee.PersonnelCode,
            Email = $"{Employee.PersonnelCode}@internal.com",
            PersonnelCode = Employee.PersonnelCode,
            EmployeeId = Employee.Id
        };

        var result = await _userManager.CreateAsync(user, Employee.NationalCode);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return Page();
        }

        Employee.IdentityUserId = user.Id;
        await _context.SaveChangesAsync();

        TempData["Success"] = "پرسنل و حساب کاربری ثبت شد. رمز پیش‌فرض = کد ملی";
        return RedirectToPage("Index");
    }
}