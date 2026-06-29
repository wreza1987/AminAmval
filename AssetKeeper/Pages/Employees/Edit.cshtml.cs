using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Employees;

[Authorize]
public class EditModel : BasePageModel
{
    public EditModel(MyDbContext context) : base(context) { }

    [BindProperty]
    public Employee Employee { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // TempData.Remove("Success");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();

        Employee = employee;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // TempData.Remove("Success");

        if (!ModelState.IsValid)
            return Page();

        var existing = await _context.Employees.FindAsync(Employee.Id);
        if (existing == null) return NotFound();

        // ثبت تاریخچه تغییرات
        string changes = $"تغییرات در تاریخ {DateTime.Now:yyyy/MM/dd HH:mm}:\n";

        if (existing.Department != Employee.Department)
            changes += $"- واحد از '{existing.Department}' به '{Employee.Department}' تغییر کرد.\n";

        if (existing.VicePresidency != Employee.VicePresidency)
            changes += $"- معاونت از '{existing.VicePresidency}' به '{Employee.VicePresidency}' تغییر کرد.\n";

        // if (existing.IsActive != Employee.IsActive)
        //     changes += $"- وضعیت از {(existing.IsActive ? "فعال" : "غیرفعال")} به {(Employee.IsActive ? "فعال" : "غیرفعال")} تغییر کرد.\n";

        if (!string.IsNullOrEmpty(changes))
        {
            existing.Description = changes + (existing.Description ?? "");
        }

        // اعمال تغییرات
        existing.FirstName = Employee.FirstName;
        existing.LastName = Employee.LastName;
        existing.NationalCode = Employee.NationalCode;
        existing.Department = Employee.Department;
        existing.VicePresidency = Employee.VicePresidency;
        existing.StartDate = Employee.StartDate;
        // existing.IsActive = Employee.IsActive;

        var currentUserAccessLevel = User.FindFirst("AccessLevel")?.Value;
        if (currentUserAccessLevel == "Admin")
            existing.AccessLevel = Employee.AccessLevel;

        // existing.AccessLevel = Employee.IsActive ? Employee.AccessLevel : EmployeeAccessLevel.Disable;

        await _context.SaveChangesAsync();

        TempData["Success"] = "اطلاعات پرسنل با موفقیت ویرایش شد.";
        return RedirectToPage("Index");
    }
}