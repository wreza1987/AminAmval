using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Shared;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Employees;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MyDbContext _context;
    public IndexModel(MyDbContext context) => _context = context;

    public IList<Employee> Employees { get; set; } = new List<Employee>();
    public List<string> AllDepartments { get; set; } = new();

    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    private const int PageSize = 20;
    public bool HasSearched { get; set; } = true; //false;

    [BindProperty(SupportsGet = true)] public string? FilterCodeFrom { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterCodeTo { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterName { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterNationalCode { get; set; }
    [BindProperty(SupportsGet = true)] public List<string> FilterDepartments { get; set; } = new();
    [BindProperty(SupportsGet = true)] public List<string> FilterAccessLevels { get; set; } = new();
    [BindProperty(SupportsGet = true)] public new int Page { get; set; } = 1;

    public async Task OnGetAsync()
    {
        AllDepartments = await _context.Employees
            .Select(e => e.Department).Distinct().OrderBy(d => d).ToListAsync();

        // bool anyFilter = !string.IsNullOrWhiteSpace(FilterCodeFrom) ||
        //                 !string.IsNullOrWhiteSpace(FilterCodeTo) ||
        //                 !string.IsNullOrWhiteSpace(FilterName) ||
        //                 !string.IsNullOrWhiteSpace(FilterNationalCode) ||
        //                 FilterDepartments.Any() ||
        //                 FilterAccessLevels.Any();

        // if (!anyFilter) return;

        // HasSearched = true;
        
        var all = await _context.Employees
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var filtered = all.AsEnumerable();

        var NormCodeFrom    = TextHelper.Normalize(FilterCodeFrom);
        var NormCodeTo      = TextHelper.Normalize(FilterCodeTo);
        var NormName        = TextHelper.Normalize(FilterName);
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

        if (!string.IsNullOrWhiteSpace(NormName))
            filtered = filtered.Where(e =>
                TextHelper.Normalize(e.FirstName).Contains(NormName, StringComparison.OrdinalIgnoreCase) ||
                TextHelper.Normalize(e.LastName).Contains(NormName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(NormNationalCode))
            filtered = filtered.Where(e => TextHelper.Normalize(e.NationalCode).Contains(NormNationalCode, StringComparison.OrdinalIgnoreCase));

        if (FilterDepartments.Any())
            filtered = filtered.Where(e => FilterDepartments.Any(fd =>
                TextHelper.Normalize(fd) == TextHelper.Normalize(e.Department)));

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

        // ✅ چک کردن سطح دسترسی کاربر جاری
        bool isWarehouse = User.FindFirst("AccessLevel")?.Value == "WarehouseKeeper";

        var (imported, skipped, error) = await DataSeeder.ImportEmployeesFromExcelAsync(
            _context, ExcelFile, isWarehouse);

        if (error != null)
            TempData["Error"] = error;
        else
            TempData["Success"] = $"✅ {imported} نفر ثبت شد. {(skipped > 0 ? $"⏭ {skipped} نفر تکراری نادیده گرفته شد." : "")}";

        return RedirectToPage();
    }
}