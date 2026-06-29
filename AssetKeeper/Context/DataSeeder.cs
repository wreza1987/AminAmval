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
    public static async Task<(int imported, int skipped, string? error)> ImportAssetsFromExcelAsync(
    MyDbContext context, IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var headers = sheet.Row(1).CellsUsed().ToDictionary(
                c => c.GetString().Trim(),
                c => c.Address.ColumnNumber);

            int GetCol(string fa, string en) =>
                headers.TryGetValue(fa, out var col) ? col :
                headers.TryGetValue(en, out col) ? col : -1;

            int cCode    = GetCol("کد اموال",       "AssetCode");
            int cOldCode = GetCol("کد اموال قبلی",  "OldAssetCode");
            int cName    = GetCol("نام اموال",       "Name");
            int cSerial  = GetCol("شماره سریال",     "SerialNumber");
            int cDesc    = GetCol("توضیحات",         "Description");
            int cCat     = GetCol("دسته‌بندی",       "CategoryName");
            int cBrand   = GetCol("برند",            "BrandName");
            int cOwner   = GetCol("مالک",            "Owner");
            int cStatus  = GetCol("وضعیت",           "Status");

            if (cCode == -1 || cName == -1 || cCat == -1 || cBrand == -1)
                return (0, 0, "ستون‌های اصلی فایل پیدا نشد. لطفاً فایل نمونه را دانلود و استفاده کنید.\nستون‌های مورد نیاز: کد اموال، نام اموال، دسته‌بندی، برند");

            int imported = 0, skipped = 0;

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var assetCode = row.Cell(cCode).GetString().Trim();
                if (string.IsNullOrEmpty(assetCode)) continue;

                if (await context.Assets.AnyAsync(a => a.AssetCode == assetCode))
                {
                    skipped++;
                    continue;
                }

                var categoryName = row.Cell(cCat).GetString().Trim();
                var brandName    = row.Cell(cBrand).GetString().Trim();

                var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                if (category == null && !string.IsNullOrEmpty(categoryName))
                {
                    category = new Category { Name = categoryName, Description = "" };
                    context.Categories.Add(category);
                    await context.SaveChangesAsync();
                }

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
                    AssetCode    = assetCode,
                    OldAssetCode = cOldCode > 0 ? row.Cell(cOldCode).GetString() : null,
                    Name         = row.Cell(cName).GetString(),
                    SerialNumber = cSerial > 0 ? NormalizeSerial(row.Cell(cSerial).GetString()) : "ندارد",
                    Description  = cDesc > 0 ? row.Cell(cDesc).GetString() : null,
                    CategoryId   = category.Id,
                    BrandId      = brand.Id,
                    Owner        = cOwner > 0 ? MappingHelper.NormalizeOwner(row.Cell(cOwner).GetString()) : AssetOwner.Unknown,
                    Status       = cStatus > 0 ? MappingHelper.NormalizeStatus(row.Cell(cStatus).GetString()) : AssetStatus.InStock,
                    CreatedAt    = DateTime.Now
                });
                imported++;
            }

            await context.SaveChangesAsync();
            return (imported, skipped, null);
        }
        catch (Exception ex)
        {
            return (0, 0, $"خطا در پردازش فایل: {ex.Message}");
        }
    }

    private static string? NormalizeSerial(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial)) return "ندارد";
        var normalized = serial.Trim().ToLower();
        var emptyValues = new[]
        {
            "ندارد", "نامشخص", "نامعلوم", "مخدوش", "تعریف نشده",
            "0", "na", "none", "null", "-", "n/a", "نا", "ناموجود"
        };
        return emptyValues.Contains(normalized) ? "ندارد" : serial.Trim();
    }

    public static async Task<(int imported, int skipped, string? error)> ImportEmployeesFromExcelAsync(
    MyDbContext context, IFormFile file, bool isWarehouse = false)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            // استخراج ایندکس ستون‌ها (فارسی یا انگلیسی)
            var headers = sheet.Row(1).CellsUsed().ToDictionary(
                c => TextHelper.Normalize(c.GetString()),
                c => c.Address.ColumnNumber);

            int GetCol(string fa, string en) =>
                headers.TryGetValue(TextHelper.Normalize(fa), out var col) ? col :
                headers.TryGetValue(TextHelper.Normalize(en), out col) ? col : -1;

            int cCode   = GetCol("کد پرسنلی",     "PersonnelCode");
            int cFirst  = GetCol("نام",            "FirstName");
            int cLast   = GetCol("نام خانوادگی",  "LastName");
            int cNat    = GetCol("کد ملی",         "NationalCode");
            int cDept   = GetCol("دپارتمان/واحد", "Department");
            int cVice   = GetCol("معاونت",         "VicePresidency");
            int cDate   = GetCol("تاریخ شروع",    "StartDate");
            int cAccess = GetCol("سطح دسترسی",    "AccessLevel");

            if (cCode == -1 || cFirst == -1 || cLast == -1 || cNat == -1 || cDept == -1)
                return (0, 0, "ستون‌های اصلی فایل پیدا نشد. لطفاً فایل نمونه را دانلود و استفاده کنید.\nستون‌های مورد نیاز: کد پرسنلی، نام، نام خانوادگی، کد ملی، دپارتمان/واحد");

            int imported = 0, skipped = 0;

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var personnelCode = row.Cell(cCode).GetString().Trim();
                if (string.IsNullOrEmpty(personnelCode)) continue;

                if (await context.Employees.AnyAsync(e => e.PersonnelCode == personnelCode))
                {
                    skipped++;
                    continue;
                }

                context.Employees.Add(new Employee
                {
                    PersonnelCode  = personnelCode,
                    FirstName      = row.Cell(cFirst).GetString(),
                    LastName       = row.Cell(cLast).GetString(),
                    NationalCode   = row.Cell(cNat).GetString(),
                    Department     = row.Cell(cDept).GetString(),
                    VicePresidency = cVice > 0 ? row.Cell(cVice).GetString() : null,
                    StartDate      = cDate > 0 && row.Cell(cDate).TryGetValue(out DateTime dt)
                                        ? dt.Date : DateTime.Now,
                    AccessLevel    = isWarehouse
                                        ? EmployeeAccessLevel.Normal
                                        : (cAccess > 0
                                            ? MappingHelper.NormalizeAccessLevel(row.Cell(cAccess).GetString())
                                            : EmployeeAccessLevel.Normal),
                });
                imported++;
            }

            await context.SaveChangesAsync();
            return (imported, skipped, null);
        }
        catch (Exception ex)
        {
            return (0, 0, $"خطا در پردازش فایل: {ex.Message}");
        }
    }

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