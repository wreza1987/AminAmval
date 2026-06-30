using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Context;
using AssetKeeper.Shared;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Pages.Assets;

[Authorize]
public class StatusModel : BasePageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public StatusModel(MyDbContext context, UserManager<ApplicationUser> userManager) : base(context) 
    {
        userManager = _userManager;
    }

    [BindProperty] public Asset Asset { get; set; } = new();
    [BindProperty] public string? NewNote { get; set; }
    [BindProperty] public bool ReassignToAnother { get; set; } = false;
    [BindProperty] public int? NewEmployeeId { get; set; }
    [BindProperty] public DateTime? NewAssignmentDate { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null) return NotFound();

        Asset = asset;
        NewAssignmentDate = DateTime.Today;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        int? currentEmployeeId = currentUser?.EmployeeId;

        var existing = await _context.Assets
            .Include(a => a.Assignments)
            .FirstOrDefaultAsync(a => a.Id == Asset.Id);

        if (existing == null) return NotFound();

        var oldStatus = existing.Status;
        var newStatus = Asset.Status;

        // عودت خودکار اگر از Assigned خارج شود
        if (oldStatus == AssetStatus.Assigned && newStatus != AssetStatus.Assigned)
        {
            var current = existing.Assignments.FirstOrDefault(a => a.ReturnDate == null);
            if (current != null)
            {
                current.ReturnDate = DateTime.Now;
                var emp = await _context.Employees.FindAsync(current.EmployeeId);
                string empLabel = emp != null
                    ? $"{emp.PersonnelCode} - {emp.FirstName} {emp.LastName}"
                    : "نامشخص";

                _context.AssetHistory.Add(new AssetHistory
                {
                    AssetId = existing.Id,
                    ChangeType = ChangeType.ReturnedFromEmployee,
                    OldValue = empLabel,
                    NewValue = "انبار",
                    ChangedByEmployeeId = currentEmployeeId,
                    ChangeDate = DateTime.Now
                });
            }
        }

        // ثبت تغییر وضعیت فقط اگر نه تحویل و نه عودت باشه
        if (oldStatus != newStatus
            && newStatus != AssetStatus.Assigned
            && !(oldStatus == AssetStatus.Assigned && newStatus != AssetStatus.Assigned))
        {
            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = existing.Id,
                ChangeType = ChangeType.StatusChanged,
                OldValue = EnumHelper.GetDisplayName(oldStatus),
                NewValue = EnumHelper.GetDisplayName(newStatus),
                ChangedByEmployeeId = currentEmployeeId,
                ChangeDate = DateTime.Now
            });
        }

        // تحویل به پرسنل جدید
        if (newStatus == AssetStatus.Assigned && NewEmployeeId.HasValue)
        {
            var currentOpen = existing.Assignments.FirstOrDefault(a => a.ReturnDate == null);
            if (currentOpen != null)
                currentOpen.ReturnDate = DateTime.Now;

            var newEmp = await _context.Employees.FindAsync(NewEmployeeId.Value);
            string newEmpLabel = newEmp != null
                ? $"{newEmp.PersonnelCode} - {newEmp.FirstName} {newEmp.LastName}"
                : NewEmployeeId.Value.ToString();

            _context.AssetAssignments.Add(new AssetAssignment
            {
                AssetId = existing.Id,
                EmployeeId = NewEmployeeId.Value,
                AssignmentDate = NewAssignmentDate ?? DateTime.Now,
                Notes = NewNote ?? "تحویل",
                CreatedAt = DateTime.Now
            });

            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = existing.Id,
                ChangeType = ChangeType.AssignedToEmployee,
                OldValue = "انبار",
                NewValue = newEmpLabel,
                ChangedByEmployeeId = currentEmployeeId,
                ChangeDate = DateTime.Now
            });
        }

        if (!string.IsNullOrWhiteSpace(NewNote))
        {
            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = existing.Id,
                ChangeType = ChangeType.NoteAdded,
                OldValue = "-",
                NewValue = NewNote,
                ChangedByEmployeeId = currentEmployeeId,
                ChangeDate = DateTime.Now
            });
        }

        existing.Status = newStatus;
        await _context.SaveChangesAsync();

        TempData["Success"] = "وضعیت اموال با موفقیت تغییر یافت.";
        return RedirectToPage("Index");
    }
}