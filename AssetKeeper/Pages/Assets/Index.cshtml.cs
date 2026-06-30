﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Shared;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using ClosedXML.Excel;

namespace AssetKeeper.Pages.Assets;
[Authorize]
public class IndexModel : BasePageModel
{
    // public IndexModel(MyDbContext context) : base(context) { }
    private readonly IWebHostEnvironment _webHostEnvironment;
    public IndexModel(MyDbContext context, IWebHostEnvironment webHostEnvironment) : base(context)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public IList<Asset> Assets { get; set; } = new List<Asset>();
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    private const int PageSize = 20;
    public bool HasSearched { get; set; } = true; //false;

    [BindProperty(SupportsGet = true)] public string? FilterAssetCodeFrom { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterAssetCodeTo { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterName { get; set; }
    [BindProperty(SupportsGet = true)] public List<string> FilterCategories { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<string> FilterBrands { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<string> FilterStatuses { get; set; } = new();
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    public List<string> AllCategories { get; set; } = new();
    public List<string> AllBrands { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllCategories = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
        AllBrands = await _context.Brands.OrderBy(b => b.Name).Select(b => b.Name).ToListAsync();

        // // ✅ اگه هیچ فیلتری نداره، چیزی نمایش نده
        // bool anyFilter = !string.IsNullOrWhiteSpace(FilterAssetCodeFrom) ||
        //                 !string.IsNullOrWhiteSpace(FilterAssetCodeTo) ||
        //                 !string.IsNullOrWhiteSpace(FilterName) ||
        //                 FilterCategories.Any() ||
        //                 FilterBrands.Any() ||
        //                 FilterStatuses.Any();

        // if (!anyFilter) return;

        // HasSearched = true;

        var all = await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Brand)
            .Include(a => a.Assignments).ThenInclude(aa => aa.Employee)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var filtered = all.AsEnumerable();

        var NormAssetCodeFrom = TextHelper.Normalize(FilterAssetCodeFrom);
        var NormAssetCodeTo   = TextHelper.Normalize(FilterAssetCodeTo);
        var NormName          = TextHelper.Normalize(FilterName);

        if (!string.IsNullOrWhiteSpace(NormAssetCodeFrom) && string.IsNullOrWhiteSpace(NormAssetCodeTo))
            filtered = filtered.Where(a => TextHelper.Normalize(a.AssetCode).Contains(NormAssetCodeFrom, StringComparison.OrdinalIgnoreCase));
        else
        {
            if (!string.IsNullOrWhiteSpace(NormAssetCodeFrom))
                filtered = filtered.Where(a => string.Compare(TextHelper.Normalize(a.AssetCode), NormAssetCodeFrom, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(NormAssetCodeTo))
                filtered = filtered.Where(a => string.Compare(TextHelper.Normalize(a.AssetCode), NormAssetCodeTo, StringComparison.OrdinalIgnoreCase) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(NormName))
            filtered = filtered.Where(a => TextHelper.Normalize(a.Name).Contains(NormName, StringComparison.OrdinalIgnoreCase));

        // if (FilterCategories.Any())
        //     filtered = filtered.Where(a => FilterCategories.Any(fc =>
        //         TextHelper.Normalize(fc) == TextHelper.Normalize(a.Category?.Name)));

        // if (FilterBrands.Any())
        //     filtered = filtered.Where(a => FilterBrands.Any(fb =>
        //         TextHelper.Normalize(fb) == TextHelper.Normalize(a.Brand?.Name)));

        if (FilterCategories?.Any() == true)
        {
            filtered = filtered.Where(a => 
                !string.IsNullOrEmpty(a.Category?.Name) && 
                FilterCategories.Any(fc => 
                    TextHelper.Normalize(fc) == TextHelper.Normalize(a.Category.Name)));
        }

        if (FilterBrands?.Any() == true)
        {
            filtered = filtered.Where(a => 
                !string.IsNullOrEmpty(a.Brand?.Name) && 
                FilterBrands.Any(fb => 
                    TextHelper.Normalize(fb) == TextHelper.Normalize(a.Brand.Name)));
        }

        if (FilterStatuses.Any())
        {
            var statuses = FilterStatuses
                .Where(s => Enum.TryParse<AssetStatus>(s, out _))
                .Select(s => Enum.Parse<AssetStatus>(s)).ToList();
            if (statuses.Any())
                filtered = filtered.Where(a => statuses.Contains(a.Status));
        }

        var list = filtered.ToList();
        TotalCount = list.Count;
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        PageNumber = Math.Max(1, Math.Min(Page, TotalPages == 0 ? 1 : TotalPages));

        Assets = list.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
    }
    
    public async Task<IActionResult> OnPostImportAsync(IFormFile ExcelFile)
    {
        if (ExcelFile == null || ExcelFile.Length == 0)
        {
            TempData["Error"] = "لطفاً فایل اکسل انتخاب کنید.";
            return RedirectToPage();
        }

        var (imported, skipped, invalid, errorFile, error) =
            await ExcelImportHelper.ImportAssetsFromExcelAsync(_context, ExcelFile);

        if (error != null)
            TempData["Error"] = error;

        TempData["Success"] = $"✅ {imported} رکورد ثبت شد." +
            (invalid > 0 ? $" ⚠️ {invalid} سطر معیوب بود." : "");

        if (errorFile != null)
        {
            var fileName = $"خطاهای_اموال_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var path = Path.Combine(_webHostEnvironment.WebRootPath, "Data", "Errors", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await System.IO.File.WriteAllBytesAsync(path, errorFile);
            TempData["ErrorFileUrl"] = $"/Data/Errors/{fileName}";
        }

        return RedirectToPage();
    }
}