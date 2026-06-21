using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Assets;

public class EditModel : BasePageModel
{
    public EditModel(MyDbContext context) : base(context) { }

    [BindProperty]
    public Asset Asset { get; set; } = new();

    [BindProperty]
    public string? NewNote { get; set; }

    public SelectList Categories { get; set; } = null!;
    public SelectList Brands { get; set; } = null!;

    public List<AssetHistory> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        TempData.Remove("Success");

        var asset = await _context.Assets
            .Include(a => a.History)
                .ThenInclude(h => h.ChangedByEmployee)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null) return NotFound();

        Asset = asset;
        History = asset.History.ToList();

        await LoadDropdowns();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
    ModelState.Remove("Asset.Category");
    ModelState.Remove("Asset.Brand");

    if (await _context.Assets.AnyAsync(a => a.AssetCode == Asset.AssetCode && a.Id != Asset.Id))
        ModelState.AddModelError("Asset.AssetCode", "این کد اموال قبلاً ثبت شده است.");

    if (!string.IsNullOrWhiteSpace(Asset.SerialNumber))
    {
        if (await _context.Assets.AnyAsync(a => a.SerialNumber == Asset.SerialNumber && a.Id != Asset.Id))
            ModelState.AddModelError("Asset.SerialNumber", "این شماره سریال قبلاً ثبت شده است.");
    }

    if (!ModelState.IsValid)
    {
        await LoadDropdowns();
        return Page();
    }

    var existing = await _context.Assets
        .Include(a => a.History)
        .Include(a => a.Category)
        .Include(a => a.Brand)
        .FirstOrDefaultAsync(a => a.Id == Asset.Id);

    if (existing == null) return NotFound();

    if (existing.AssetCode != Asset.AssetCode)
    {
        existing.OldAssetCode = existing.AssetCode;
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = existing.Id,
            ChangeType = ChangeType.AssetCodeChanged,
            OldValue = existing.AssetCode,
            NewValue = Asset.AssetCode,
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });
    }

    if (existing.Name != Asset.Name)
    {
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = existing.Id,
            ChangeType = ChangeType.NameChanged,
            OldValue = existing.Name,
            NewValue = Asset.Name,
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });
    }

    if (existing.SerialNumber != Asset.SerialNumber)
    {
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = existing.Id,
            ChangeType = ChangeType.SerialNumberChanged,
            OldValue = existing.SerialNumber ?? "-",
            NewValue = Asset.SerialNumber ?? "-",
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });
    }

    if (existing.Owner != Asset.Owner)
    {
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = existing.Id,
            ChangeType = ChangeType.OwnerChanged,
            OldValue = existing.Owner.ToString(),
            NewValue = Asset.Owner.ToString(),
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });
    }

    if (existing.CategoryId != Asset.CategoryId)
    {
        // نام دسته‌بندی را می‌خوانیم نه Id را
        var oldCat = await _context.Categories.FindAsync(existing.CategoryId);
        var newCat = await _context.Categories.FindAsync(Asset.CategoryId);
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = existing.Id,
            ChangeType = ChangeType.CategoryChanged,
            OldValue = oldCat?.Name ?? existing.CategoryId.ToString(),
            NewValue = newCat?.Name ?? Asset.CategoryId.ToString(),
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });
    }

    if (existing.BrandId != Asset.BrandId)
    {
        var oldBrand = await _context.Brands.FindAsync(existing.BrandId);
        var newBrand = await _context.Brands.FindAsync(Asset.BrandId);
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = existing.Id,
            ChangeType = ChangeType.BrandChanged,
            OldValue = oldBrand?.Name ?? existing.BrandId.ToString(),
            NewValue = newBrand?.Name ?? Asset.BrandId.ToString(),
            ChangedByEmployeeId = null,
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
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });
    }

    // اعمال تغییرات — Status دست نمی‌خوره
    existing.AssetCode = Asset.AssetCode;
    existing.Name = Asset.Name;
    existing.SerialNumber = Asset.SerialNumber;
    existing.Owner = Asset.Owner;
    existing.CategoryId = Asset.CategoryId;
    existing.BrandId = Asset.BrandId;
    // existing.Status دست نمی‌خوره

    await _context.SaveChangesAsync();

    TempData["Success"] = "اموال با موفقیت ویرایش شد.";
    return RedirectToPage("Index");
    }

    private async Task LoadDropdowns()
    {
        Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        Brands = new SelectList(await _context.Brands.OrderBy(b => b.Name).ToListAsync(), "Id", "Name");
    }
}