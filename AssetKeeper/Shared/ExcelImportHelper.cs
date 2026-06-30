using ClosedXML.Excel;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using AssetKeeper.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Shared;

public static class ExcelImportHelper
{
    // ===================== پرسنل =====================
    public static async Task<(int imported, int skipped, int invalid, byte[]? errorFile, string? error)>
        ImportEmployeesFromExcelAsync(MyDbContext context, IFormFile file, bool isWarehouse)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

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
                return (0, 0, 0, null,
                    "ستون‌های اصلی فایل پیدا نشد. لطفاً فایل نمونه را دانلود و استفاده کنید.\n" +
                    "ستون‌های مورد نیاز: کد پرسنلی، نام، نام خانوادگی، کد ملی، دپارتمان/واحد");

            int imported = 0, invalid = 0;
            var invalidRows = new List<(IXLRow row, string reason)>();
            var seenCodes = new HashSet<string>();
            var seenNationalCodes = new HashSet<string>();

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var personnelCode = row.Cell(cCode).GetString().Trim();
                var firstName     = row.Cell(cFirst).GetString().Trim();
                var lastName      = row.Cell(cLast).GetString().Trim();
                var nationalCode  = row.Cell(cNat).GetString().Trim();
                var department    = row.Cell(cDept).GetString().Trim();

                // ردیف کاملاً خالی -> نادیده گرفته شود (نه معیوب)
                if (string.IsNullOrEmpty(personnelCode) && string.IsNullOrEmpty(firstName)
                    && string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(nationalCode)
                    && string.IsNullOrEmpty(department))
                    continue;

                // فقط فیلدهای اجباری خالی + تکراری → نامعتبر
                var reasons = new List<string>();
                if (string.IsNullOrWhiteSpace(personnelCode)) reasons.Add("کد پرسنلی خالی است");
                if (string.IsNullOrWhiteSpace(firstName)) reasons.Add("نام خالی است");
                if (string.IsNullOrWhiteSpace(lastName)) reasons.Add("نام خانوادگی خالی است");
                if (string.IsNullOrWhiteSpace(nationalCode)) reasons.Add("کد ملی خالی است");
                if (string.IsNullOrWhiteSpace(department)) reasons.Add("دپارتمان/واحد خالی است");

                if (!string.IsNullOrWhiteSpace(personnelCode) && !seenCodes.Add(personnelCode))
                    reasons.Add("کد پرسنلی در همین فایل تکراری است");
                if (!string.IsNullOrWhiteSpace(nationalCode) && !seenNationalCodes.Add(nationalCode))
                    reasons.Add("کد ملی در همین فایل تکراری است");

                if (!string.IsNullOrWhiteSpace(personnelCode) &&
                    await context.Employees.AnyAsync(e => e.PersonnelCode == personnelCode))
                    reasons.Add("کد پرسنلی قبلاً در سیستم ثبت شده");
                if (!string.IsNullOrWhiteSpace(nationalCode) &&
                    await context.Employees.AnyAsync(e => e.NationalCode == nationalCode))
                    reasons.Add("کد ملی قبلاً در سیستم ثبت شده");

                if (reasons.Any())
                {
                    invalidRows.Add((row, string.Join(" | ", reasons)));
                    invalid++;
                    continue;
                }

                // فیلدهای غیر-اجباری: نرمال‌سازی به جای رد کردن
                context.Employees.Add(new Employee
                {
                    PersonnelCode  = personnelCode,
                    FirstName      = firstName,
                    LastName       = lastName,
                    NationalCode   = nationalCode,
                    Department     = department,
                    VicePresidency = cVice > 0 ? row.Cell(cVice).GetString() : null,
                    StartDate      = cDate > 0
                                        ? ImportNormalizer.NormalizeDate(row.Cell(cDate).GetString())
                                        : DateTime.Today,
                    AccessLevel    = isWarehouse
                                        ? EmployeeAccessLevel.Normal
                                        : (cAccess > 0
                                            ? ImportNormalizer.NormalizeAccessLevel(row.Cell(cAccess).GetString())
                                            : EmployeeAccessLevel.Normal),
                });
                imported++;
            }

            await context.SaveChangesAsync();

            byte[]? errorFile = invalidRows.Any()
                ? BuildErrorWorkbook(sheet, invalidRows, headers.Count)
                : null;
            string? errorMsg = invalidRows.Any()
                ? $"{invalidRows.Count} سطر معیوب شناسایی شد و در فایل خطاها قرار گرفت."
                : null;

            return (imported, 0, invalid, errorFile, errorMsg);
        }
        catch (Exception ex)
        {
            return (0, 0, 0, null, $"خطا در پردازش فایل: {ex.Message}");
        }
    }

    // ===================== اموال =====================
    public static async Task<(int imported, int skipped, int invalid, byte[]? errorFile, string? error)>
        ImportAssetsFromExcelAsync(MyDbContext context, IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var headers = sheet.Row(1).CellsUsed().ToDictionary(
                c => TextHelper.Normalize(c.GetString()),
                c => c.Address.ColumnNumber);

            int GetCol(string fa, string en) =>
                headers.TryGetValue(TextHelper.Normalize(fa), out var col) ? col :
                headers.TryGetValue(TextHelper.Normalize(en), out col) ? col : -1;

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
                return (0, 0, 0, null,
                    "ستون‌های اصلی فایل پیدا نشد. لطفاً فایل نمونه را دانلود و استفاده کنید.\n" +
                    "ستون‌های مورد نیاز: کد اموال، نام اموال، دسته‌بندی، برند");

            int imported = 0, invalid = 0;
            var invalidRows = new List<(IXLRow row, string reason)>();
            var seenCodes = new HashSet<string>();

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var assetCode = row.Cell(cCode).GetString().Trim();
                var name      = row.Cell(cName).GetString().Trim();
                var catName   = row.Cell(cCat).GetString().Trim();
                var brandName = row.Cell(cBrand).GetString().Trim();

                // ردیف کاملاً خالی -> نادیده گرفته شود
                if (string.IsNullOrEmpty(assetCode) && string.IsNullOrEmpty(name)
                    && string.IsNullOrEmpty(catName) && string.IsNullOrEmpty(brandName))
                    continue;

                // فقط فیلدهای اجباری خالی + کد تکراری → نامعتبر
                var reasons = new List<string>();
                if (string.IsNullOrWhiteSpace(assetCode)) reasons.Add("کد اموال خالی است");
                if (string.IsNullOrWhiteSpace(name)) reasons.Add("نام اموال خالی است");
                if (string.IsNullOrWhiteSpace(catName)) reasons.Add("دسته‌بندی خالی است");
                if (string.IsNullOrWhiteSpace(brandName)) reasons.Add("برند خالی است");

                if (!string.IsNullOrWhiteSpace(assetCode) && !seenCodes.Add(assetCode))
                    reasons.Add("کد اموال در همین فایل تکراری است");
                if (!string.IsNullOrWhiteSpace(assetCode) &&
                    await context.Assets.AnyAsync(a => a.AssetCode == assetCode))
                    reasons.Add("کد اموال قبلاً در سیستم ثبت شده");

                if (reasons.Any())
                {
                    invalidRows.Add((row, string.Join(" | ", reasons)));
                    invalid++;
                    continue;
                }

                // دسته‌بندی و برند را خودکار بساز اگر وجود ندارند
                var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == catName);
                if (category == null)
                {
                    category = new Category { Name = catName, Description = "" };
                    context.Categories.Add(category);
                    await context.SaveChangesAsync();
                }

                var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == brandName);
                if (brand == null)
                {
                    brand = new Brand { Name = brandName, Description = "" };
                    context.Brands.Add(brand);
                    await context.SaveChangesAsync();
                }

                // فیلدهای غیر-اجباری: نرمال‌سازی به جای رد کردن
                context.Assets.Add(new Asset
                {
                    AssetCode    = assetCode,
                    OldAssetCode = cOldCode > 0 ? row.Cell(cOldCode).GetString() : null,
                    Name         = name,
                    SerialNumber = cSerial > 0
                                    ? ImportNormalizer.NormalizeSerial(row.Cell(cSerial).GetString())
                                    : "ندارد",
                    Description  = cDesc > 0 ? row.Cell(cDesc).GetString() : null,
                    CategoryId   = category.Id,
                    BrandId      = brand.Id,
                    Owner        = cOwner > 0
                                    ? ImportNormalizer.NormalizeOwner(row.Cell(cOwner).GetString())
                                    : AssetOwner.Unknown,
                    Status       = cStatus > 0
                                    ? ImportNormalizer.NormalizeStatus(row.Cell(cStatus).GetString())
                                    : AssetStatus.InStock,
                    CreatedAt    = DateTime.Now
                });
                imported++;
            }

            await context.SaveChangesAsync();

            byte[]? errorFile = invalidRows.Any()
                ? BuildErrorWorkbook(sheet, invalidRows, headers.Count)
                : null;
            string? errorMsg = invalidRows.Any()
                ? $"{invalidRows.Count} سطر معیوب شناسایی شد و در فایل خطاها قرار گرفت."
                : null;

            return (imported, 0, invalid, errorFile, errorMsg);
        }
        catch (Exception ex)
        {
            return (0, 0, 0, null, $"خطا در پردازش فایل: {ex.Message}");
        }
    }

    // ===================== ساخت فایل خطا =====================
    private static byte[] BuildErrorWorkbook(IXLWorksheet originalSheet,
        List<(IXLRow row, string reason)> invalidRows, int colCount)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ردیف‌های معیوب");

        for (int c = 1; c <= colCount; c++)
            ws.Cell(1, c).Value = originalSheet.Cell(1, c).GetString();
        ws.Cell(1, colCount + 1).Value = "دلیل خطا";
        ws.Row(1).Style.Font.Bold = true;
        ws.Cell(1, colCount + 1).Style.Font.FontColor = XLColor.Red;

        int destRow = 2;
        foreach (var (row, reason) in invalidRows)
        {
            for (int c = 1; c <= colCount; c++)
                ws.Cell(destRow, c).Value = row.Cell(c).GetString();
            ws.Cell(destRow, colCount + 1).Value = reason;
            ws.Cell(destRow, colCount + 1).Style.Font.FontColor = XLColor.Red;
            destRow++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}