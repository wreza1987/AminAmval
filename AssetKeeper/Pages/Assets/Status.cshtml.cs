// file: AssetKeeper\AssetKeeper\Pages\Assets\Status.cshtml.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Assets;

public class StatusModel : BasePageModel
{
    public StatusModel(MyDbContext context) : base(context) { }

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
        var existing = await _context.Assets
            .Include(a => a.Assignments)
            .FirstOrDefaultAsync(a => a.Id == Asset.Id);

        if (existing == null) return NotFound();

        var oldStatus = existing.Status;
        var newStatus = Asset.Status;

        // === عودت خودکار اگر از Assigned خارج شود ===
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
                    ChangedByEmployeeId = null,
                    ChangeDate = DateTime.Now
                });
            }
        }

        // === ثبت تغییر وضعیت (اگر واقعاً تغییر کرده) ===
        if (oldStatus != newStatus)
        {
            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = existing.Id,
                ChangeType = ChangeType.StatusChanged,
                OldValue = oldStatus.ToString(),
                NewValue = newStatus.ToString(),
                ChangedByEmployeeId = null,
                ChangeDate = DateTime.Now
            });
        }

        // === تحویل به پرسنل جدید ===
        if (newStatus == AssetStatus.Assigned && NewEmployeeId.HasValue)
        {
            // بستن تخصیص باز قبلی اگر وجود داشت
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
                Notes = NewNote ?? "تحویل پس از تغییر وضعیت",
                CreatedAt = DateTime.Now
            });

            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = existing.Id,
                ChangeType = ChangeType.AssignedToEmployee,
                OldValue = "انبار",
                NewValue = newEmpLabel,
                ChangedByEmployeeId = null,
                ChangeDate = DateTime.Now
            });
        }

        // === یادداشت ===
        if (!string.IsNullOrWhiteSpace(NewNote))
        {
            _context.AssetHistory.Add(new AssetHistory
            {
                AssetId = existing.Id,
                ChangeType = ChangeType.NoteAdded,
                OldValue = "-",
                NewValue = NewNote,
                ChangedByEmployeeId = null,
                ChangeDate = DateTime.Now
            });
        }

        existing.Status = newStatus;
        await _context.SaveChangesAsync();

        TempData["Success"] = "وضعیت اموال با موفقیت تغییر یافت.";
        return RedirectToPage("Index");
    }
}