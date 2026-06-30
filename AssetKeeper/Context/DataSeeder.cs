using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using AssetKeeper.Shared;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Context;

public static class DataSeeder
{
    private static readonly string SeedFolder = Path.Combine("wwwroot", "Data", "Seed");

    public static async Task SeedAsync(MyDbContext context)
    {
        EnsureSeedFolderAndTemplates();

        if (await context.Categories.AnyAsync()) return;

        // DataSeeder.SeedAsync — اضافه کن:
        if (!await context.PagePermissions.AnyAsync())
            await SeedPagePermissions(context);
            
        await SeedCategories(context);
        await SeedBrands(context);
        await SeedEmployees(context);
        await SeedAssets(context);

        await context.SaveChangesAsync();
    }

    private static void EnsureSeedFolderAndTemplates()
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), SeedFolder);
        Directory.CreateDirectory(fullPath);

        CreateTemplateIfNotExists("Categories.xlsx", new[] { "Name", "Description" });
        CreateTemplateIfNotExists("Brands.xlsx", new[] { "Name", "Description" });
        CreateTemplateIfNotExists("Employees.xlsx", new[] { "PersonnelCode", "FirstName", "LastName", "NationalCode", "Department", "VicePresidency", "StartDate", "AccessLevel" });
        CreateTemplateIfNotExists("Assets.xlsx", new[] { "AssetCode", "OldAssetCode", "Name", "SerialNumber", "Description", "CategoryName", "BrandName", "Owner", "Status" });
        CreateTemplateIfNotExists("پرسنل.xlsx", new[]
            { "کد پرسنلی", "نام", "نام خانوادگی", "کد ملی", "دپارتمان/واحد", "معاونت", "تاریخ شروع", "سطح دسترسی" });

        CreateTemplateIfNotExists("اموال.xlsx", new[]
            { "کد اموال", "کد اموال قبلی", "نام اموال", "شماره سریال", "توضیحات", "دسته‌بندی", "برند", "مالک", "وضعیت" });
    }

    private static void CreateTemplateIfNotExists(string fileName, string[] headers)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), SeedFolder, fileName);
        if (File.Exists(filePath)) return;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Data");

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        workbook.SaveAs(filePath);
    }

    // ====================== Seed Initial Data ======================
    private static async Task SeedCategories(MyDbContext context)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), SeedFolder, "Categories.xlsx");
        using var wb = new XLWorkbook(filePath);
        var sheet = wb.Worksheet(1);

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) continue;

            if (!await context.Categories.AnyAsync(c => c.Name == name))
            {
                context.Categories.Add(new Category 
                { 
                    Name = name, 
                    Description = row.Cell(2).GetString() 
                });
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedBrands(MyDbContext context)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), SeedFolder, "Brands.xlsx");
        using var wb = new XLWorkbook(filePath);
        var sheet = wb.Worksheet(1);

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) continue;

            if (!await context.Brands.AnyAsync(b => b.Name == name))
            {
                context.Brands.Add(new Brand 
                { 
                    Name = name, 
                    Description = row.Cell(2).GetString() 
                });
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedEmployees(MyDbContext context)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), SeedFolder, "Employees.xlsx");
        using var wb = new XLWorkbook(filePath);
        var sheet = wb.Worksheet(1);

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var personnelCode = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(personnelCode)) continue;

            if (!await context.Employees.AnyAsync(e => e.PersonnelCode == personnelCode))
            {
                context.Employees.Add(new Employee
                {
                    PersonnelCode = personnelCode,
                    FirstName = row.Cell(2).GetString(),
                    LastName = row.Cell(3).GetString(),
                    NationalCode = row.Cell(4).GetString(),
                    Department = row.Cell(5).GetString(),
                    VicePresidency = row.Cell(6).GetString(),
                    StartDate = row.Cell(7).TryGetValue(out DateTime dt) ? dt.Date : DateTime.Now,
                    AccessLevel = MappingHelper.NormalizeAccessLevel(row.Cell(8).GetString()),
                    // IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();
    }

        private static async Task SeedAssets(MyDbContext context)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), SeedFolder, "Assets.xlsx");
        using var wb = new XLWorkbook(filePath);
        var sheet = wb.Worksheet(1);

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var assetCode = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(assetCode)) continue;

            if (await context.Assets.AnyAsync(a => a.AssetCode == assetCode)) continue;

            var categoryName = row.Cell(6).GetString().Trim();
            var brandName = row.Cell(7).GetString().Trim();

            // === ایجاد خودکار دسته‌بندی اگر وجود نداشت ===
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null && !string.IsNullOrEmpty(categoryName))
            {
                category = new Category { Name = categoryName, Description = "" };
                context.Categories.Add(category);
                await context.SaveChangesAsync();
            }

            // === ایجاد خودکار برند اگر وجود نداشت ===
            var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == brandName);
            if (brand == null && !string.IsNullOrEmpty(brandName))
            {
                brand = new Brand { Name = brandName, Description = "" };
                context.Brands.Add(brand);
                await context.SaveChangesAsync();
            }

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
                Owner = MappingHelper.NormalizeOwner(row.Cell(8).GetString()),
                Status = MappingHelper.NormalizeStatus(row.Cell(9).GetString()),
                CreatedAt = DateTime.Now
            });
        }
    }

    // ====================== Import from Excel ======================
    // public static async Task<(int imported, int skipped, int invalid, byte[]? errorFile, string? error)>
    //     ImportAssetsFromExcelAsync(MyDbContext context, IFormFile file)
    // {
    //     try
    //     {
    //         using var stream = file.OpenReadStream();
    //         using var workbook = new XLWorkbook(stream);
    //         var sheet = workbook.Worksheet(1);

    //         var headers = sheet.Row(1).CellsUsed().ToDictionary(
    //             c => TextHelper.Normalize(c.GetString()),
    //             c => c.Address.ColumnNumber);

    //         int GetCol(string fa, string en) =>
    //             headers.TryGetValue(TextHelper.Normalize(fa), out var col) ? col :
    //             headers.TryGetValue(TextHelper.Normalize(en), out col) ? col : -1;

    //         int cCode    = GetCol("کد اموال",       "AssetCode");
    //         int cOldCode = GetCol("کد اموال قبلی",  "OldAssetCode");
    //         int cName    = GetCol("نام اموال",       "Name");
    //         int cSerial  = GetCol("شماره سریال",     "SerialNumber");
    //         int cDesc    = GetCol("توضیحات",         "Description");
    //         int cCat     = GetCol("دسته‌بندی",       "CategoryName");
    //         int cBrand   = GetCol("برند",            "BrandName");
    //         int cOwner   = GetCol("مالک",            "Owner");
    //         int cStatus  = GetCol("وضعیت",           "Status");

    //         if (cCode == -1 || cName == -1 || cCat == -1 || cBrand == -1)
    //             return (0, 0, 0, null, "ستون‌های اصلی فایل پیدا نشد. لطفاً فایل نمونه را دانلود و استفاده کنید.\nستون‌های مورد نیاز: کد اموال، نام اموال، دسته‌بندی، برند");

    //         int imported = 0, skipped = 0, invalid = 0;
    //         var invalidRows = new List<(IXLRow row, string reason)>();
    //         var seenCodes = new HashSet<string>();

    //         foreach (var row in sheet.RowsUsed().Skip(1))
    //         {
    //             var assetCode = row.Cell(cCode).GetString().Trim();
    //             var name      = row.Cell(cName).GetString().Trim();
    //             var catName   = row.Cell(cCat).GetString().Trim();
    //             var brandName = row.Cell(cBrand).GetString().Trim();

    //             if (string.IsNullOrEmpty(assetCode)) continue;

    //             var reasons = new List<string>();

    //             if (string.IsNullOrWhiteSpace(name)) reasons.Add("نام اموال خالی است");
    //             if (string.IsNullOrWhiteSpace(catName)) reasons.Add("دسته‌بندی خالی است");
    //             if (string.IsNullOrWhiteSpace(brandName)) reasons.Add("برند خالی است");

    //             if (!seenCodes.Add(assetCode))
    //                 reasons.Add("کد اموال در همین فایل تکراری است");

    //             if (await context.Assets.AnyAsync(a => a.AssetCode == assetCode))
    //                 reasons.Add("کد اموال قبلاً در سیستم ثبت شده");

    //             if (reasons.Any())
    //             {
    //                 invalidRows.Add((row, string.Join(" | ", reasons)));
    //                 invalid++;
    //                 continue;
    //             }

    //             var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == catName);
    //             if (category == null)
    //             {
    //                 category = new Category { Name = catName, Description = "" };
    //                 context.Categories.Add(category);
    //                 await context.SaveChangesAsync();
    //             }

    //             var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == brandName);
    //             if (brand == null)
    //             {
    //                 brand = new Brand { Name = brandName, Description = "" };
    //                 context.Brands.Add(brand);
    //                 await context.SaveChangesAsync();
    //             }

    //             context.Assets.Add(new Asset
    //             {
    //                 AssetCode    = assetCode,
    //                 OldAssetCode = cOldCode > 0 ? row.Cell(cOldCode).GetString() : null,
    //                 Name         = name,
    //                 SerialNumber = cSerial > 0 ? ImportNormalizer.NormalizeSerial(row.Cell(cSerial).GetString()) : "ندارد",
    //                 Description  = cDesc > 0 ? row.Cell(cDesc).GetString() : null,
    //                 CategoryId   = category.Id,
    //                 BrandId      = brand.Id,
    //                 Owner        = cOwner > 0 ? ImportNormalizer.NormalizeOwner(row.Cell(cOwner).GetString()) : AssetOwner.Unknown,
    //                 Status       = cStatus > 0 ? ImportNormalizer.NormalizeStatus(row.Cell(cStatus).GetString()) : AssetStatus.InStock,
    //                 CreatedAt    = DateTime.Now
    //             });
    //             imported++;
    //         }

    //         await context.SaveChangesAsync();

    //         byte[]? errorFile = null;
    //         if (invalidRows.Any())
    //             errorFile = BuildErrorWorkbook(sheet, invalidRows, headers.Count);

    //         return (imported, skipped, invalid, errorFile, null);
    //     }
    //     catch (Exception ex)
    //     {
    //         return (0, 0, 0, null, $"خطا در پردازش فایل: {ex.Message}");
    //     }
    // }

    // private static string? NormalizeSerial(string? serial)
    // {
    //     if (string.IsNullOrWhiteSpace(serial)) return "ندارد";
    //     var normalized = serial.Trim().ToLower();
    //     var emptyValues = new[]
    //     {
    //         "ندارد", "نامشخص", "نامعلوم", "مخدوش", "تعریف نشده",
    //         "0", "na", "none", "null", "-", "n/a", "نا", "ناموجود"
    //     };
    //     return emptyValues.Contains(normalized) ? "ندارد" : serial.Trim();
    // }

    // public static async Task<(int imported, int skipped, int invalid, byte[]? errorFile, string? error)>
    //     ImportEmployeesFromExcelAsync(MyDbContext context, IFormFile file, bool isWarehouse = false)
    // {
    //     try
    //     {
    //         using var stream = file.OpenReadStream();
    //         using var workbook = new XLWorkbook(stream);
    //         var sheet = workbook.Worksheet(1);

    //         var headers = sheet.Row(1).CellsUsed().ToDictionary(
    //             c => TextHelper.Normalize(c.GetString()),
    //             c => c.Address.ColumnNumber);

    //         int GetCol(string fa, string en) =>
    //             headers.TryGetValue(TextHelper.Normalize(fa), out var col) ? col :
    //             headers.TryGetValue(TextHelper.Normalize(en), out col) ? col : -1;

    //         int cCode   = GetCol("کد پرسنلی",     "PersonnelCode");
    //         int cFirst  = GetCol("نام",            "FirstName");
    //         int cLast   = GetCol("نام خانوادگی",  "LastName");
    //         int cNat    = GetCol("کد ملی",         "NationalCode");
    //         int cDept   = GetCol("دپارتمان/واحد", "Department");
    //         int cVice   = GetCol("معاونت",         "VicePresidency");
    //         int cDate   = GetCol("تاریخ شروع",    "StartDate");
    //         int cAccess = GetCol("سطح دسترسی",    "AccessLevel");

    //         if (cCode == -1 || cFirst == -1 || cLast == -1 || cNat == -1 || cDept == -1)
    //             return (0, 0, 0, null, "ستون‌های اصلی فایل پیدا نشد. لطفاً فایل نمونه را دانلود و استفاده کنید.\nستون‌های مورد نیاز: کد پرسنلی، نام، نام خانوادگی، کد ملی، دپارتمان/واحد");

    //         int imported = 0, skipped = 0, invalid = 0;
    //         var invalidRows = new List<(IXLRow row, string reason)>();
    //         var seenCodes = new HashSet<string>();
    //         var seenNationalCodes = new HashSet<string>();

    //         foreach (var row in sheet.RowsUsed().Skip(1))
    //         {
    //             var personnelCode = row.Cell(cCode).GetString().Trim();
    //             var firstName     = row.Cell(cFirst).GetString().Trim();
    //             var lastName      = row.Cell(cLast).GetString().Trim();
    //             var nationalCode  = row.Cell(cNat).GetString().Trim();
    //             var department    = row.Cell(cDept).GetString().Trim();

    //             if (string.IsNullOrEmpty(personnelCode)) continue; // ردیف کاملاً خالی، رد شو

    //             var reasons = new List<string>();

    //             // فیلدهای اجباری
    //             if (string.IsNullOrWhiteSpace(firstName)) reasons.Add("نام خالی است");
    //             if (string.IsNullOrWhiteSpace(lastName)) reasons.Add("نام خانوادگی خالی است");
    //             if (string.IsNullOrWhiteSpace(nationalCode)) reasons.Add("کد ملی خالی است");
    //             if (string.IsNullOrWhiteSpace(department)) reasons.Add("دپارتمان/واحد خالی است");

    //             // تکراری در همین فایل
    //             if (!seenCodes.Add(personnelCode))
    //                 reasons.Add("کد پرسنلی در همین فایل تکراری است");
    //             if (!string.IsNullOrWhiteSpace(nationalCode) && !seenNationalCodes.Add(nationalCode))
    //                 reasons.Add("کد ملی در همین فایل تکراری است");

    //             // تکراری در دیتابیس
    //             if (await context.Employees.AnyAsync(e => e.PersonnelCode == personnelCode))
    //                 reasons.Add("کد پرسنلی قبلاً در سیستم ثبت شده");
    //             if (!string.IsNullOrWhiteSpace(nationalCode) &&
    //                 await context.Employees.AnyAsync(e => e.NationalCode == nationalCode))
    //                 reasons.Add("کد ملی قبلاً در سیستم ثبت شده");

    //             if (reasons.Any())
    //             {
    //                 invalidRows.Add((row, string.Join(" | ", reasons)));
    //                 invalid++;
    //                 continue;
    //             }

    //             // ✅ نرمال‌سازی موارد غیر بحرانی طبق قانون مشترک
    //             context.Employees.Add(new Employee
    //             {
    //                 PersonnelCode  = personnelCode,
    //                 FirstName      = firstName,
    //                 LastName       = lastName,
    //                 NationalCode   = nationalCode,
    //                 Department     = department,
    //                 VicePresidency = cVice > 0 ? row.Cell(cVice).GetString() : null,
    //                 StartDate      = cDate > 0 ? ImportNormalizer.NormalizeDate(row.Cell(cDate).GetString()) : DateTime.Today,
    //                 AccessLevel    = isWarehouse
    //                                     ? EmployeeAccessLevel.Normal
    //                                     : (cAccess > 0
    //                                         ? ImportNormalizer.NormalizeAccessLevel(row.Cell(cAccess).GetString())
    //                                         : EmployeeAccessLevel.Normal),
    //             });
    //             imported++;
    //         }

    //         await context.SaveChangesAsync();

    //         byte[]? errorFile = null;
    //         if (invalidRows.Any())
    //             errorFile = BuildErrorWorkbook(sheet, invalidRows, headers.Count);

    //         return (imported, skipped, invalid, errorFile, null);
    //     }
    //     catch (Exception ex)
    //     {
    //         return (0, 0, 0, null, $"خطا در پردازش فایل: {ex.Message}");
    //     }
    // }

    // private static byte[] BuildErrorWorkbook(IXLWorksheet originalSheet,
    //     List<(IXLRow row, string reason)> invalidRows, int colCount)
    // {
    //     using var wb = new XLWorkbook();
    //     var ws = wb.Worksheets.Add("ردیف‌های معیوب");

    //     // کپی سرستون اصلی + ستون دلیل خطا
    //     for (int c = 1; c <= colCount; c++)
    //         ws.Cell(1, c).Value = originalSheet.Cell(1, c).GetString();
    //     ws.Cell(1, colCount + 1).Value = "دلیل خطا";
    //     ws.Row(1).Style.Font.Bold = true;
    //     ws.Cell(1, colCount + 1).Style.Font.FontColor = XLColor.Red;

    //     int destRow = 2;
    //     foreach (var (row, reason) in invalidRows)
    //     {
    //         for (int c = 1; c <= colCount; c++)
    //             ws.Cell(destRow, c).Value = row.Cell(c).GetString();
    //         ws.Cell(destRow, colCount + 1).Value = reason;
    //         ws.Cell(destRow, colCount + 1).Style.Font.FontColor = XLColor.Red;
    //         destRow++;
    //     }

    //     ws.Columns().AdjustToContents();

    //     using var stream = new MemoryStream();
    //     wb.SaveAs(stream);
    //     return stream.ToArray();
    // }

    private static async Task SeedPagePermissions(MyDbContext context)
    {
        var pages = new[]
        {
            ("Index",                "داشبورد"),
            ("Assets/Index",         "لیست اموال"),
            ("Assets/Details",       "جزئیات اموال"),
            ("Assets/Create",        "ثبت اموال"),
            ("Assets/Edit",          "ویرایش اموال"),
            ("Assets/Status",        "تغییر وضعیت"),
            ("Employees/Index",      "لیست پرسنل"),
            ("Employees/Create",     "ثبت پرسنل"),
            ("Employees/Edit",       "ویرایش پرسنل"),
            ("Assignments/Index",    "تخصیص‌های جاری"),
            ("Assignments/Create",   "تخصیص جدید"),
            ("Assignments/Return",   "عودت اموال"),
            ("Requests/Index",       "درخواست‌های کاربران"),
            ("Categories/Index",     "دسته‌بندی‌ها"),
            ("Brands/Index",         "برندها"),
        };

        foreach (var (key, title) in pages)
        {
            // انباردار به همه به جز Admin دسترسی دارد
            context.PagePermissions.Add(new PagePermission
            {
                PageKey = key,
                PageTitle = title,
                AccessLevel = EmployeeAccessLevel.WarehouseKeeper,
                IsAllowed = true
            });
            // کاربر عادی فقط به Account دسترسی دارد (که در Middleware exempt شده)
            context.PagePermissions.Add(new PagePermission
            {
                PageKey = key,
                PageTitle = title,
                AccessLevel = EmployeeAccessLevel.Normal,
                IsAllowed = false
            });
        }
        await context.SaveChangesAsync();
    }


    public static async Task SeedPagePermissionsAsync(MyDbContext context)
    {
        if (await context.PagePermissions.AnyAsync()) return;

        var pages = new[]
        {
            ("Assets/Index",        "لیست اموال"),
            ("Assets/Create",       "ثبت اموال جدید"),
            ("Assets/Edit",         "ویرایش اموال"),
            ("Assets/Details",      "جزئیات اموال"),
            ("Assets/Status",       "وضعیت اموال"),
            ("Categories/Index",    "لیست دسته‌بندی‌ها"),
            ("Categories/Create",   "ثبت دسته‌بندی"),
            ("Categories/Edit",     "ویرایش دسته‌بندی"),
            ("Brands/Index",        "لیست برندها"),
            ("Brands/Create",       "ثبت برند"),
            ("Brands/Edit",         "ویرایش برند"),
            ("Employees/Index",     "لیست پرسنل"),
            ("Employees/Create",    "ثبت پرسنل"),
            ("Employees/Edit",      "ویرایش پرسنل"),
            ("Employees/Details",   "جزئیات پرسنل"),
            ("Assignments/Index",   "لیست تخصیص‌ها"),
            ("Assignments/Create",  "تخصیص اموال"),
            ("Assignments/Return",  "بازگشت اموال"),
            ("Requests/Index",      "درخواست‌های کاربران"),
            ("Index",               "داشبورد"),
        };

        // Normal — فقط حساب من (هیچ‌کدام از صفحات بالا)
        // WarehouseKeeper — همه به جز Admin/AccessLevels
        // Admin — همه چیز (در کد جداگانه هندل میشه)

        foreach (var (key, title) in pages)
        {
            // کاربر عادی — هیچ‌کدام
            context.PagePermissions.Add(new PagePermission
            {
                PageKey = key,
                PageTitle = title,
                AccessLevel = EmployeeAccessLevel.Normal,
                IsAllowed = false
            });

            // انباردار — همه به جز Employees/Edit
            bool warehouseAllowed = key != "Employees/Edit";
            context.PagePermissions.Add(new PagePermission
            {
                PageKey = key,
                PageTitle = title,
                AccessLevel = EmployeeAccessLevel.WarehouseKeeper,
                IsAllowed = warehouseAllowed
            });
        }

        await context.SaveChangesAsync();
    }
}