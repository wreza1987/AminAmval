using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;


namespace AssetKeeper.Pages.Assets;

[Authorize]
public class IndexModel : BasePageModel
{
    public IndexModel(MyDbContext context) : base(context) { }

    public IList<Asset> Assets { get; set; } = new List<Asset>();

    public async Task OnGetAsync()
    {
        // TempData.Remove("Success");

        Assets = await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Brand)
            .Include(a => a.Assignments)
                .ThenInclude(aa => aa.Employee)
            .OrderBy(a => a.AssetCode)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile ExcelFile)
    {
        if (ExcelFile == null || ExcelFile.Length == 0)
        {
            TempData["Error"] = "لطفاً فایل اکسل انتخاب کنید.";
            return RedirectToPage();
        }

        try
        {
            await DataSeeder.ImportAssetsFromExcelAsync(_context, ExcelFile);
            TempData["Success"] = "اموال با موفقیت از فایل اکسل وارد شدند.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطا در وارد کردن فایل: {ex.Message}";
        }

        return RedirectToPage();
    }
}