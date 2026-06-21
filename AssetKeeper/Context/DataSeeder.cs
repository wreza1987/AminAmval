using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Context;

public static class DataSeeder
{
    private static readonly string SeedFolder = Path.Combine("wwwroot", "Data", "Seed");

    public static async Task SeedAsync(MyDbContext context)
    {
        EnsureSeedFolderAndTemplates();

        if (await context.Categories.AnyAsync()) return;

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

    // ==================== Seed Methods ====================

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
        await context.SaveChangesAsync();   // ← مهم: ذخیره جداگانه
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
                    StartDate = row.Cell(7).TryGetValue(out DateTime dt) ? dt : DateTime.Now,
                    // AccessLevel = Enum.TryParse<EmployeeAccessLevel>(row.Cell(8).GetString(), out var lvl) ? lvl : EmployeeAccessLevel.Normal,
                    AccessLevel = EmployeeAccessLevel.Normal,
                    IsActive = true
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

            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            var brand = await context.Brands.FirstOrDefaultAsync(b => b.Name == brandName);

            if (category == null || brand == null)
            {
                Console.WriteLine($"Warning: Category or Brand not found for asset {assetCode}");
                continue;
            }

            context.Assets.Add(new Asset
            {
                AssetCode = assetCode,
                OldAssetCode = row.Cell(2).GetString(),
                Name = row.Cell(3).GetString(),
                SerialNumber = row.Cell(4).GetString(),
                Description = row.Cell(5).GetString(),
                CategoryId = category.Id,
                BrandId = brand.Id,
                Owner = Enum.TryParse<AssetOwner>(row.Cell(8).GetString(), out var owner) ? owner : AssetOwner.Unknown,
                Status = Enum.TryParse<AssetStatus>(row.Cell(9).GetString(), out var status) ? status : AssetStatus.InStock,
                CreatedAt = DateTime.Now
            });
        }
    }
}