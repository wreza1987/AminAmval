using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Shared;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using ClosedXML.Excel;

namespace AssetKeeper.Pages.Employees;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public IndexModel(MyDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public IList<Employee> Employees { get; set; } = new List<Employee>();
    public List<string> AllDepartments { get; set; } = new();

    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    private const int PageSize = 20;
    public bool HasSearched { get; set; } = true; //false;

    [BindProperty(SupportsGet = true)] public string? FilterCodeFrom { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterCodeTo { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterFirstName { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterLastName { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterNationalCode { get; set; }
    [BindProperty(SupportsGet = true)] public List<string> FilterDepartments { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<string> FilterAccessLevels { get; set; } = new();
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public List<string> FilterVicePresidencies { get; set; } = new();
    public List<string> AllVicePresidencies { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllDepartments = await _context.Employees
            .Select(e => e.Department).Distinct().OrderBy(d => d).ToListAsync();

        // لیست معاونت‌ها (با "ندارد" برای خالی‌ها)
        AllVicePresidencies = await _context.Employees
            .Select(e => string.IsNullOrWhiteSpace(e.VicePresidency) ? "ندارد" : e.VicePresidency)
            .Distinct().OrderBy(v => v).ToListAsync();
        
        var all = await _context.Employees
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var filtered = all.AsEnumerable();

        var NormCodeFrom    = TextHelper.Normalize(FilterCodeFrom);
        var NormCodeTo      = TextHelper.Normalize(FilterCodeTo);
        var NormFirstName        = TextHelper.Normalize(FilterFirstName);
        var NormLastName        = TextHelper.Normalize(FilterLastName);
        var NormNationalCode = TextHelper.Normalize(FilterNationalCode);

        if (!string.IsNullOrWhiteSpace(NormCodeFrom) && string.IsNullOrWhiteSpace(NormCodeTo))
            filtered = filtered.Where(e => TextHelper.Normalize(e.PersonnelCode).Contains(NormCodeFrom, StringComparison.OrdinalIgnoreCase));
        else
        {
            if (!string.IsNullOrWhiteSpace(NormCodeFrom))
                filtered = filtered.Where(e => string.Compare(TextHelper.Normalize(e.PersonnelCode), NormCodeFrom, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(NormCodeTo))
                filtered = filtered.Where(e => string.Compare(TextHelper.Normalize(e.PersonnelCode), NormCodeTo, StringComparison.OrdinalIgnoreCase) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(NormFirstName))
        {
            filtered = filtered.Where(e => 
                TextHelper.Normalize(e.FirstName).Contains(NormFirstName, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(NormLastName))
        {
            filtered = filtered.Where(e => 
                TextHelper.Normalize(e.LastName).Contains(NormLastName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(NormNationalCode))
            filtered = filtered.Where(e => TextHelper.Normalize(e.NationalCode).Contains(NormNationalCode, StringComparison.OrdinalIgnoreCase));

        if (FilterDepartments.Any())
            filtered = filtered.Where(e => FilterDepartments.Any(fd =>
                TextHelper.Normalize(fd) == TextHelper.Normalize(e.Department)));

        // فیلتر معاونت
        if (FilterVicePresidencies.Any())
        {
            filtered = filtered.Where(e =>
            {
                var vp = string.IsNullOrWhiteSpace(e.VicePresidency) ? "ندارد" : e.VicePresidency;
                return FilterVicePresidencies.Contains(vp);
            });
        }    

        if (FilterAccessLevels.Any())
        {
            var levels = new List<EmployeeAccessLevel>();
            foreach (var l in FilterAccessLevels)
                if (Enum.TryParse<EmployeeAccessLevel>(l, out var parsed))
                    levels.Add(parsed);
            if (levels.Any())
                filtered = filtered.Where(e => levels.Contains(e.AccessLevel));
        }

        var list = filtered.ToList();
        TotalCount = list.Count;
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        PageNumber = Math.Max(1, Math.Min(Page, TotalPages == 0 ? 1 : TotalPages));

        Employees = list.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile ExcelFile)
    {
        if (ExcelFile == null || ExcelFile.Length == 0)
        {
            TempData["Error"] = "لطفاً فایل اکسل انتخاب کنید.";
            return RedirectToPage();
        }

        bool isWarehouse = User.FindFirst("AccessLevel")?.Value == "WarehouseKeeper";

        var (imported, skipped, invalid, errorFile, error) =
            await ExcelImportHelper.ImportEmployeesFromExcelAsync(_context, ExcelFile, isWarehouse);

        if (error != null) TempData["Error"] = error;

        TempData["Success"] = $"✅ {imported} نفر با موفقیت ثبت شد." +
                            (invalid > 0 ? $" ⚠️ {invalid} سطر معیوب بود." : "");

        if (errorFile != null)
        {
            var fileName = $"خطاهای_پرسنل_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var path = Path.Combine(_webHostEnvironment.WebRootPath, "Data", "Errors", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await System.IO.File.WriteAllBytesAsync(path, errorFile);
            TempData["ErrorFileUrl"] = $"/Data/Errors/{fileName}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var employees = await _context.Employees.OrderBy(e => e.PersonnelCode).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("پرسنل");

        var headers = new[] { "کد پرسنلی", "نام", "نام خانوادگی", "کد ملی",
            "دپارتمان/واحد", "معاونت", "تاریخ شروع", "سطح دسترسی" };

        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var e in employees)
        {
            sheet.Cell(row, 1).Value = e.PersonnelCode;
            sheet.Cell(row, 2).Value = e.FirstName;
            sheet.Cell(row, 3).Value = e.LastName;
            sheet.Cell(row, 4).Value = e.NationalCode;
            sheet.Cell(row, 5).Value = e.Department;
            sheet.Cell(row, 6).Value = e.VicePresidency;
            sheet.Cell(row, 7).Value = e.StartDate.ToPersian();
            sheet.Cell(row, 8).Value = EnumHelper.GetDisplayName(e.AccessLevel);
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"پرسنل_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}