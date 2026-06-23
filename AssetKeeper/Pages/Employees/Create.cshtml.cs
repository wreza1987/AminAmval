using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


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
        TempData.Remove("Success");
        Employee.StartDate = DateTime.Today;
    }

    // public async Task<IActionResult> OnPostAsync()
    // {
    //     TempData.Remove("Success");

    //     // === جمع‌آوری همه خطاهای تکراری قبل از چک ModelState ===
    //     bool hasError = false;

    //     if (await _context.Employees.AnyAsync(e => e.PersonnelCode == Employee.PersonnelCode))
    //     {
    //         ModelState.AddModelError("Employee.PersonnelCode", "این کد پرسنلی قبلاً ثبت شده است.");
    //         hasError = true;
    //     }

    //     if (await _context.Employees.AnyAsync(e => e.NationalCode == Employee.NationalCode))
    //     {
    //         ModelState.AddModelError("Employee.NationalCode", "این کد ملی قبلاً ثبت شده است.");
    //         hasError = true;
    //     }

    //     if (!ModelState.IsValid || hasError)
    //     {
    //         return Page();
    //     }

    //     // ثبت نهایی
    //     Employee.IsActive = true;
    //     _context.Employees.Add(Employee);
    //     await _context.SaveChangesAsync();

    //     TempData["Success"] = "پرسنل با موفقیت ثبت شد.";
    //     return RedirectToPage("Index");
    // }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // چک تکراری
        if (await _context.Employees.AnyAsync(e => e.PersonnelCode == Employee.PersonnelCode))
        {
            ModelState.AddModelError("Employee.PersonnelCode", "این کد پرسنلی قبلاً ثبت شده است.");
            return Page();
        }

        // === ایجاد پرسنل ===
        _context.Employees.Add(Employee);
        await _context.SaveChangesAsync();

        // === ایجاد کاربر Identity ===
        var user = new ApplicationUser
        {
            UserName = Employee.PersonnelCode,
            Email = $"{Employee.PersonnelCode}@internal.com", // دلخواه
            PersonnelCode = Employee.PersonnelCode,
            EmployeeId = Employee.Id
        };

        var result = await _userManager.CreateAsync(user, Employee.NationalCode);

        if (!result.Succeeded)
        {
            // اگر خطا داشت، لاگ کن
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return Page();
        }

        // لینک کردن
        Employee.IdentityUserId = user.Id;
        await _context.SaveChangesAsync();

        TempData["Success"] = "پرسنل و حساب کاربری با موفقیت ایجاد شد. پسورد پیش‌فرض = کد ملی";
        return RedirectToPage("Index");
    }
}