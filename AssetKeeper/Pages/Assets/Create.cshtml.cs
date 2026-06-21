using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AssetKeeper.Pages.Assets;

public class CreateModel : BasePageModel
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CreateModel(MyDbContext context, IWebHostEnvironment webHostEnvironment) 
        : base(context)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    [BindProperty]
    public Asset Asset { get; set; } = new();

    public SelectList Categories { get; set; } = null!;
    public SelectList Brands { get; set; } = null!;

    public async Task OnGetAsync()
    {
        await LoadDropdowns();
    }

    // مهم: new اضافه شود
    public new async Task<JsonResult> OnGetSearchCategoriesAsync(string term)
        => await base.OnGetSearchCategoriesAsync(term);

    public new async Task<JsonResult> OnGetSearchBrandsAsync(string term)
        => await base.OnGetSearchBrandsAsync(term);
        
    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Asset.Category");
        ModelState.Remove("Asset.Brand");

        // بررسی تکراری بودن
        if (await _context.Assets.AnyAsync(a => a.AssetCode == Asset.AssetCode))
            ModelState.AddModelError("Asset.AssetCode", "این کد اموال قبلاً ثبت شده است.");

        if (!string.IsNullOrWhiteSpace(Asset.SerialNumber))
        {
            if (await _context.Assets.AnyAsync(a => a.SerialNumber == Asset.SerialNumber))
                ModelState.AddModelError("Asset.SerialNumber", "این شماره سریال قبلاً ثبت شده است.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return Page();
        }

        // آپلود تصویر
        if (Asset.ImageFile != null && Asset.ImageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "assets");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Asset.ImageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await Asset.ImageFile.CopyToAsync(fileStream);
            }

            Asset.ImagePath = "/images/assets/" + uniqueFileName;
        }

        Asset.Status = AssetStatus.InStock;
        Asset.CreatedAt = DateTime.Now;

        _context.Assets.Add(Asset);
        await _context.SaveChangesAsync();

        // ثبت تاریخچه اولیه
        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = Asset.Id,
            ChangeType = ChangeType.Other,
            OldValue = "-",
            NewValue = "ثبت اولیه اموال",
            ChangedByEmployeeId = null,
            ChangeDate = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "اموال با موفقیت ثبت شد.";
        return RedirectToPage("Index");
    }

    private async Task LoadDropdowns()
    {
        Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        Brands = new SelectList(await _context.Brands.OrderBy(b => b.Name).ToListAsync(), "Id", "Name");
    }

}