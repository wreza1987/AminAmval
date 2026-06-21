using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
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

            // === ایجاد خودکار دسته‌بندی ===
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null && !string.IsNullOrEmpty(categoryName))
            {
                category = new Category { Name = categoryName, Description = "" };
                context.Categories.Add(category);
                await context.SaveChangesAsync();
            }

            // === ایجاد خودکار برند ===
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
        await context.SaveChangesAsync();
    }

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
                StartDate = row.Cell(7).TryGetValue(out DateTime dt) ? dt.Date : DateTime.Now,
                AccessLevel = MappingHelper.NormalizeAccessLevel(row.Cell(8).GetString()),
                IsActive = true
            });
        }
        await context.SaveChangesAsync();
    }
}