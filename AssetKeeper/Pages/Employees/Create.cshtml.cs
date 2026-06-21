using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Employees;

public class CreateModel : PageModel
{
    private readonly MyDbContext _context;

    public CreateModel(MyDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Employee Employee { get; set; } = new();

    public void OnGet()
    {
        TempData.Remove("Success");
        Employee.StartDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TempData.Remove("Success");

        // === جمع‌آوری همه خطاهای تکراری قبل از چک ModelState ===
        bool hasError = false;

        if (await _context.Employees.AnyAsync(e => e.PersonnelCode == Employee.PersonnelCode))
        {
            ModelState.AddModelError("Employee.PersonnelCode", "این کد پرسنلی قبلاً ثبت شده است.");
            hasError = true;
        }

        if (await _context.Employees.AnyAsync(e => e.NationalCode == Employee.NationalCode))
        {
            ModelState.AddModelError("Employee.NationalCode", "این کد ملی قبلاً ثبت شده است.");
            hasError = true;
        }

        if (!ModelState.IsValid || hasError)
        {
            return Page();
        }

        // ثبت نهایی
        Employee.IsActive = true;
        _context.Employees.Add(Employee);
        await _context.SaveChangesAsync();

        TempData["Success"] = "پرسنل با موفقیت ثبت شد.";
        return RedirectToPage("Index");
    }
}