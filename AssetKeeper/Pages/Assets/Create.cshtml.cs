using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Pages.Assets;

[Authorize]
public class CreateModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(MyDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _userManager = userManager; 
    }

    [BindProperty]
    public Asset Asset { get; set; } = new();

    // برای نگه داشتن مقادیر جستجو بعد از خطا
    [BindProperty]
    public string? SelectedCategoryName { get; set; }
    [BindProperty]
    public string? SelectedBrandName { get; set; }

    public async Task<JsonResult> OnGetSearchCategoriesAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new JsonResult(new List<object>());
        term = term.Trim().ToLower();
        var data = await _context.Categories
            .Where(c => c.Name.ToLower().Contains(term))
            .Select(c => new { id = c.Id, text = c.Name })
            .Take(15).ToListAsync();
        return new JsonResult(data);
    }

    public async Task<JsonResult> OnGetSearchBrandsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new JsonResult(new List<object>());
        term = term.Trim().ToLower();
        var data = await _context.Brands
            .Where(b => b.Name.ToLower().Contains(term))
            .Select(b => new { id = b.Id, text = b.Name })
            .Take(15).ToListAsync();
        return new JsonResult(data);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        int? currentEmployeeId = currentUser?.EmployeeId;

        ModelState.Remove("Asset.Category");
        ModelState.Remove("Asset.Brand");
        ModelState.Remove("Asset.ImageFile");

        Asset.SerialNumber = NormalizeSerial(Asset.SerialNumber);

        // ✅ چک CategoryId و BrandId
        if (Asset.CategoryId == 0)
            ModelState.AddModelError("Asset.CategoryId", "دسته‌بندی الزامی است.");

        if (Asset.BrandId == 0)
            ModelState.AddModelError("Asset.BrandId", "برند الزامی است.");

        if (await _context.Assets.AnyAsync(a => a.AssetCode == Asset.AssetCode))
            ModelState.AddModelError("Asset.AssetCode", "این کد اموال قبلاً ثبت شده است.");

        if (!string.IsNullOrEmpty(Asset.SerialNumber) && Asset.SerialNumber != "ندارد")
        {
            if (await _context.Assets.AnyAsync(a => a.SerialNumber == Asset.SerialNumber))
                ModelState.AddModelError("Asset.SerialNumber", "این شماره سریال قبلاً ثبت شده است.");
        }

        // ✅ لود نام‌های انتخاب شده برای نمایش مجدد
        if (Asset.CategoryId > 0)
            SelectedCategoryName = (await _context.Categories.FindAsync(Asset.CategoryId))?.Name;
        if (Asset.BrandId > 0)
            SelectedBrandName = (await _context.Brands.FindAsync(Asset.BrandId))?.Name;

        if (!ModelState.IsValid)
            return Page();

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
            ChangeDate = DateTime.Now,
            ChangedByEmployeeId = currentEmployeeId 
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "اموال با موفقیت ثبت شد.";
        return RedirectToPage("Index");
    }

    private static string? NormalizeSerial(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial)) return "ندارد";
        var normalized = serial.Trim().ToLower();
        var emptyValues = new[] { "ندارد", "نامشخص", "نامعلوم", "مخدوش", "تعریف نشده",
            "0", "na", "none", "null", "-", "n/a", "نا", "ناموجود" };
        return emptyValues.Contains(normalized) ? "ندارد" : serial.Trim();
    }
}