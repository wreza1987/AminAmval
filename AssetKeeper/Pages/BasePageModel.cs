using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Context;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages;

public abstract class BasePageModel : PageModel
{
    protected readonly MyDbContext _context;

    protected BasePageModel(MyDbContext context)
    {
        _context = context;
    }

    // ====================== جستجوی آنلاین پرسنل ======================
    public async Task<JsonResult> OnGetSearchEmployeesAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new JsonResult(new List<object>());

        term = term.Trim().ToLower();

        var employees = await _context.Employees
            .Where(e => e.IsActive &&
                (e.PersonnelCode.ToLower().Contains(term) ||
                 e.FirstName.ToLower().Contains(term) ||
                 e.LastName.ToLower().Contains(term) ||
                 (e.FirstName + " " + e.LastName).ToLower().Contains(term) ||
                 (e.PersonnelCode + " " + e.FirstName + " " + e.LastName).ToLower().Contains(term)))
            .OrderBy(e => e.PersonnelCode)
            .Select(e => new
            {
                id = e.Id,
                text = e.PersonnelCode + " - " + e.FirstName + " " + e.LastName
            })
            .Take(15)
            .ToListAsync();

        return new JsonResult(employees);
    }

    // ====================== جستجوی آنلاین اموال ======================
    public async Task<JsonResult> OnGetSearchAssetsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new JsonResult(new List<object>());

        term = term.Trim().ToLower();

        var assets = await _context.Assets
            .Where(a => a.AssetCode.ToLower().Contains(term) ||
                        a.Name.ToLower().Contains(term) ||
                        (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term)))
            .OrderBy(a => a.AssetCode)
            .Select(a => new
            {
                id = a.Id,
                text = a.AssetCode + " - " + a.Name
            })
            .Take(15)
            .ToListAsync();

        return new JsonResult(assets);
    }
}