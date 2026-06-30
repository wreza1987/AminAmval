using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using AssetKeeper.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Pages.Assignments;

[Authorize]
public class ReturnModel : BasePageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ReturnModel(MyDbContext context, UserManager<ApplicationUser> userManager) : base(context)
    {
        _userManager = userManager;
    }

    [BindProperty] public AssetAssignment CurrentAssignment { get; set; } = new();
    [BindProperty] public bool ReassignToAnother { get; set; } = false;
    [BindProperty] public string? NewPersonnelCode { get; set; }
    [BindProperty] public int? NewEmployeeId { get; set; }
    [BindProperty] public DateTime? NewAssignmentDate { get; set; }

    public SelectList Employees { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var assignment = await _context.AssetAssignments
            .Include(a => a.Asset)
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null || assignment.ReturnDate != null)
            return NotFound();

        CurrentAssignment = assignment;
        NewAssignmentDate = DateTime.Today;

        await LoadEmployees();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        int? currentEmployeeId = currentUser?.EmployeeId;

        var assignment = await _context.AssetAssignments
            .Include(a => a.Asset)
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == CurrentAssignment.Id);

        if (assignment == null) return NotFound();

        if (CurrentAssignment.ReturnDate < assignment.AssignmentDate)
        {
            ModelState.AddModelError("CurrentAssignment.ReturnDate", "تاریخ عودت نمی‌تواند زودتر از تاریخ تحویل باشد.");
            await LoadEmployees();
            return Page();
        }

        // نام تحویل‌گیرنده قبلی
        string prevEmpLabel = assignment.Employee != null
            ? $"{assignment.Employee.PersonnelCode} - {assignment.Employee.FirstName} {assignment.Employee.LastName}"
            : "نامشخص";

        // ثبت عودت
        assignment.ReturnDate = CurrentAssignment.ReturnDate ?? DateTime.Now;
        assignment.Notes += $" | عودت در {DateTime.Now.ToPersian():yyyy/MM/dd}";
        assignment.Asset.Status = AssetStatus.InStock;

        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = assignment.AssetId,
            ChangeType = ChangeType.ReturnedFromEmployee,
            OldValue = prevEmpLabel,
            NewValue = "انبار",
            ChangedByEmployeeId = currentEmployeeId,
            ChangeDate = DateTime.Now
        });

        // تحویل فوری به شخص دیگر
        if (ReassignToAnother && NewEmployeeId.HasValue)
        {
            var newEmp = await _context.Employees.FindAsync(NewEmployeeId.Value);
            string newEmpLabel = newEmp != null
                ? $"{newEmp.PersonnelCode} - {newEmp.FirstName} {newEmp.LastName}"
                : NewEmployeeId.Value.ToString();

            _context.AssetAssignments.Add(new AssetAssignment
            {
                AssetId = assignment.AssetId,
                EmployeeId = NewEmployeeId.Value,
                AssignmentDate = NewAssignmentDate ?? DateTime.Now,
                Notes = "تحویل پس از عودت",
                ReturnDate = null,
                CreatedAt = DateTime.Now
            });

            assignment.Asset.Status = AssetStatus.Assigned;

            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = assignment.AssetId,
                ChangeType = ChangeType.AssignedToEmployee,
                OldValue = "انبار",
                NewValue = newEmpLabel,
                ChangedByEmployeeId = currentEmployeeId,
                ChangeDate = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "عملیات عودت با موفقیت انجام شد.";
        return RedirectToPage("Index");
    }

    private async Task LoadEmployees()
    {
        Employees = new SelectList(await _context.Employees
            .Where(e => e.AccessLevel != EmployeeAccessLevel.Disable)
            .Select(e => new { e.Id, Display = e.PersonnelCode + " - " + e.FirstName + " " + e.LastName })
            .ToListAsync(), "Id", "Display");
    }
}