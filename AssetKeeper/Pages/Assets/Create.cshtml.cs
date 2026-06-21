using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AssetKeeper.Pages.Assets;

public class CreateModel : PageModel   // ← موقتاً از BasePageModel ارث نبرد
{
    private readonly MyDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CreateModel(MyDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _webHostEnvironment = webHostEnvironment;
    }

    [BindProperty]
    public Asset Asset { get; set; } = new();

    // === جستجوهای مستقیم (بدون BasePageModel) ===
    public async Task<JsonResult> OnGetSearchCategoriesAsync(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new JsonResult(new List<object>());

            term = term.Trim().ToLower();

            var data = await _context.Categories
                .Where(c => c.Name.ToLower().Contains(term))
                .Select(c => new { id = c.Id, text = c.Name })
                .Take(15)
                .ToListAsync();

            return new JsonResult(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR SearchCategories] {ex}");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<JsonResult> OnGetSearchBrandsAsync(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new JsonResult(new List<object>());

            term = term.Trim().ToLower();

            var data = await _context.Brands
                .Where(b => b.Name.ToLower().Contains(term))
                .Select(b => new { id = b.Id, text = b.Name })
                .Take(15)
                .ToListAsync();

            return new JsonResult(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR SearchBrands] {ex}");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ... کد OnPost قبلی شما (همان قبلی) ...
        ModelState.Remove("Asset.Category");
        ModelState.Remove("Asset.Brand");

        if (!ModelState.IsValid)
            return Page();

        // آپلود تصویر + ذخیره + تاریخچه (کد قبلی)
        if (Asset.ImageFile != null && Asset.ImageFile.Length > 0)
        {
            var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "images", "assets");
            Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + "_" + Asset.ImageFile.FileName;
            var path = Path.Combine(uploads, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await Asset.ImageFile.CopyToAsync(stream);

            Asset.ImagePath = "/images/assets/" + fileName;
        }

        Asset.Status = AssetStatus.InStock;
        _context.Assets.Add(Asset);
        await _context.SaveChangesAsync();

        _context.AssetHistory.Add(new AssetHistory
        {
            AssetId = Asset.Id,
            ChangeType = ChangeType.Other,
            OldValue = "-",
            NewValue = "ثبت اولیه",
            ChangeDate = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "اموال ثبت شد.";
        return RedirectToPage("Index");
    }
}