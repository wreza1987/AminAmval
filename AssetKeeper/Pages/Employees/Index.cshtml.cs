using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;


namespace AssetKeeper.Pages.Employees;

// public class IndexModel : BasePageModel
// {
//     public IndexModel(MyDbContext context) : base(context) { }

//     public IList<Employee> Employees { get; set; } = new List<Employee>();

//     public async Task OnGetAsync()
//     {
//         Employees = await _context.Employees
//             .OrderBy(e => e.PersonnelCode)
//             .ToListAsync();
//     }

//     public async Task<IActionResult> OnPostImportAsync(IFormFile ExcelFile)
//     {
//         if (ExcelFile == null || ExcelFile.Length == 0)
//         {
//             TempData["Error"] = "لطفاً فایل اکسل انتخاب کنید.";
//             return RedirectToPage();
//         }

//         try
//         {
//             await DataSeeder.ImportEmployeesFromExcelAsync(_context, ExcelFile);
//             TempData["Success"] = "پرسنل با موفقیت از فایل اکسل وارد شدند.";
//         }
//         catch (Exception ex)
//         {
//             TempData["Error"] = $"خطا در وارد کردن فایل: {ex.Message}";
//         }

//         return RedirectToPage();
//     }



//     // Import دستی اموال
//     public static async Task ImportAssetsFromExcelAsync(MyDbContext context, IFormFile file)
//     {
//         using var stream = file.OpenReadStream();
//         using var workbook = new XLWorkbook(stream);
//         var sheet = workbook.Worksheet(1);

//         foreach (var row in sheet.RowsUsed().Skip(1))
//         {
//             var assetCode = row.Cell(1).GetString().Trim();
//             if (string.IsNullOrEmpty(assetCode)) continue;

//             if (await context.Assets.AnyAsync(a => a.AssetCode == assetCode)) continue;

//             var categoryName = row.Cell(6).GetString().Trim();
//             var brandName = row.Cell(7).GetString().Trim();

//             var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
//             var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == brandName);

//             if (category == null || brand == null) continue;

//             context.Assets.Add(new Asset
//             {
//                 AssetCode = assetCode,
//                 OldAssetCode = row.Cell(2).GetString(),
//                 Name = row.Cell(3).GetString(),
//                 SerialNumber = row.Cell(4).GetString(),
//                 Description = row.Cell(5).GetString(),
//                 CategoryId = category.Id,
//                 BrandId = brand.Id,
//                 Owner = Enum.TryParse<AssetOwner>(row.Cell(8).GetString(), out var o) ? o : AssetOwner.Unknown,
//                 Status = Enum.TryParse<AssetStatus>(row.Cell(9).GetString(), out var s) ? s : AssetStatus.InStock,
//                 CreatedAt = DateTime.Now
//             });
//         }
//         await context.SaveChangesAsync();
//     }

//     // Import دستی پرسنل
//     public static async Task ImportEmployeesFromExcelAsync(MyDbContext context, IFormFile file)
//     {
//         using var stream = file.OpenReadStream();
//         using var workbook = new XLWorkbook(stream);
//         var sheet = workbook.Worksheet(1);

//         foreach (var row in sheet.RowsUsed().Skip(1))
//         {
//             var personnelCode = row.Cell(1).GetString().Trim();
//             if (string.IsNullOrEmpty(personnelCode)) continue;

//             if (await context.Employees.AnyAsync(e => e.PersonnelCode == personnelCode)) continue;

//             context.Employees.Add(new Employee
//             {
//                 PersonnelCode = personnelCode,
//                 FirstName = row.Cell(2).GetString(),
//                 LastName = row.Cell(3).GetString(),
//                 NationalCode = row.Cell(4).GetString(),
//                 Department = row.Cell(5).GetString(),
//                 VicePresidency = row.Cell(6).GetString(),
//                 StartDate = row.Cell(7).TryGetValue(out DateTime dt) ? dt : DateTime.Now,
//                 AccessLevel = EmployeeAccessLevel.Normal,   // همیشه Normal
//                 IsActive = true
//             });
//         }
//         await context.SaveChangesAsync();
//     }
// }


[Authorize]
public class IndexModel : PageModel
{
    private readonly MyDbContext _context;

    public IndexModel(MyDbContext context)
    {
        _context = context;
    }

    public IList<Employee> Employees { get; set; } = new List<Employee>();

    public async Task OnGetAsync()
    {
        // پاک کردن تمام پیام‌های قبلی TempData
        TempData.Remove("Success");
        TempData.Remove("Error");

        Employees = await _context.Employees
            .OrderBy(e => e.PersonnelCode)
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
            await DataSeeder.ImportEmployeesFromExcelAsync(_context, ExcelFile);
            TempData["Success"] = "پرسنل با موفقیت از فایل اکسل وارد شدند.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطا در وارد کردن فایل: {ex.Message}";
        }

        return RedirectToPage();
    }



    // Import دستی اموال
    public static async Task ImportAssetsFromExcelAsync(MyDbContext context, IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var assetCode = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(assetCode)) continue;

            if (await context.Assets.AnyAsync(a => a.AssetCode == assetCode)) continue;

            var categoryName = row.Cell(6).GetString().Trim();
            var brandName = row.Cell(7).GetString().Trim();

            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == brandName);

            if (category == null || brand == null) continue;

            context.Assets.Add(new Asset
            {
                AssetCode = assetCode,
                OldAssetCode = row.Cell(2).GetString(),
                Name = row.Cell(3).GetString(),
                SerialNumber = row.Cell(4).GetString(),
                Description = row.Cell(5).GetString(),
                CategoryId = category.Id,
                BrandId = brand.Id,
                Owner = Enum.TryParse<AssetOwner>(row.Cell(8).GetString(), out var o) ? o : AssetOwner.Unknown,
                Status = Enum.TryParse<AssetStatus>(row.Cell(9).GetString(), out var s) ? s : AssetStatus.InStock,
                CreatedAt = DateTime.Now
            });
        }
        await context.SaveChangesAsync();
    }

    // Import دستی پرسنل
    public static async Task ImportEmployeesFromExcelAsync(MyDbContext context, IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var personnelCode = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(personnelCode)) continue;

            if (await context.Employees.AnyAsync(e => e.PersonnelCode == personnelCode)) continue;

            context.Employees.Add(new Employee
            {
                PersonnelCode = personnelCode,
                FirstName = row.Cell(2).GetString(),
                LastName = row.Cell(3).GetString(),
                NationalCode = row.Cell(4).GetString(),
                Department = row.Cell(5).GetString(),
                VicePresidency = row.Cell(6).GetString(),
                StartDate = row.Cell(7).TryGetValue(out DateTime dt) ? dt : DateTime.Now,
                AccessLevel = EmployeeAccessLevel.Normal,   // همیشه Normal
                IsActive = true
            });
        }
        await context.SaveChangesAsync();
    }
}