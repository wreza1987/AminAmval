using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using AssetKeeper.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Pages.Assignments;

[Authorize]
public class CreateModel : BasePageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(MyDbContext context, UserManager<ApplicationUser> userManager) : base(context) 
    {
        _userManager = userManager;    
    }

    [BindProperty]
    public AssetAssignmentCreateDto AssignmentDto { get; set; } = new();

    public SelectList AvailableAssets { get; set; } = null!;


    public async Task OnGetAsync()
{
    // TempData.Remove("Success");
    if (AssignmentDto.AssignmentDate == default)
        AssignmentDto.AssignmentDate = DateTime.Today;

    await LoadAvailableAssets();
}

private async Task LoadAvailableAssets()
{
    AvailableAssets = new SelectList(await _context.Assets
        .Where(a => a.Status != AssetStatus.Assigned)
        .OrderBy(a => a.AssetCode)
        .Select(a => new { a.Id, Display = a.AssetCode + " - " + a.Name })
        .ToListAsync(), "Id", "Display");
}

public async Task<IActionResult> OnPostAsync()
{
    var currentUser = await _userManager.GetUserAsync(User);
    int? currentEmployeeId = currentUser?.EmployeeId;

    if (!ModelState.IsValid)
    {
        await LoadAvailableAssets();   // فقط لیست را لود کن، مدل را ریست نکن
        return Page();
    }

    // چک تداخل زمانی قوی و دقیق
    var hasConflict = await _context.AssetAssignments
        .AnyAsync(a => a.AssetId == AssignmentDto.AssetId &&
            a.ReturnDate == null &&
            a.AssignmentDate <= AssignmentDto.AssignmentDate);

    if (hasConflict)
    {
        ModelState.AddModelError("", "این کالا در بازه زمانی تحویل داده شده و عودت نشده است.");
        await LoadAvailableAssets();
        return Page();
    }

    // چک تداخل زمانی قوی
    var overlapping = await _context.AssetAssignments
        .AnyAsync(a => a.AssetId == AssignmentDto.AssetId &&
            ((AssignmentDto.AssignmentDate >= a.AssignmentDate &&
              (a.ReturnDate == null || AssignmentDto.AssignmentDate <= a.ReturnDate)) ||
             (a.AssignmentDate <= AssignmentDto.AssignmentDate &&
              (AssignmentDto.AssignmentDate <= a.ReturnDate || a.ReturnDate == null))));

    if (overlapping)
    {
        ModelState.AddModelError("", "این کالا در بازه زمانی انتخاب شده قبلاً به شخص دیگری تحویل داده شده است.");
        await LoadAvailableAssets();   // ← اینجا مهم است
        return Page();
    }

    var assignment = new AssetAssignment
    {
        AssetId = AssignmentDto.AssetId,
        EmployeeId = AssignmentDto.EmployeeId,
        AssignmentDate = AssignmentDto.AssignmentDate,
        Notes = AssignmentDto.Notes,
        CreatedAt = DateTime.Now
    };
    _context.AssetAssignments.Add(assignment);

    // ثبت تحویل در تاریخچه
    var assignedEmp = await _context.Employees.FindAsync(AssignmentDto.EmployeeId);
    string empLabel = assignedEmp != null
        ? $"{assignedEmp.PersonnelCode} - {assignedEmp.FirstName} {assignedEmp.LastName}"
        : AssignmentDto.EmployeeId.ToString();

    _context.AssetHistory.Add(new AssetHistory
    {
        AssetId = AssignmentDto.AssetId,
        ChangeType = ChangeType.AssignedToEmployee,
        OldValue = "انبار",
        NewValue = empLabel,
        ChangedByEmployeeId = currentEmployeeId,
        ChangeDate = DateTime.Now
    });

    var asset = await _context.Assets.FindAsync(AssignmentDto.AssetId);
    if (asset != null) asset.Status = AssetStatus.Assigned;

    await _context.SaveChangesAsync();

    TempData["Success"] = "اموال با موفقیت تخصیص داده شد.";
    return RedirectToPage("Index");
}
}