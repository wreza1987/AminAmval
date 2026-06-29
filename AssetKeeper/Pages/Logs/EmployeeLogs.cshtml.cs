using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Logs;

[Authorize(Policy = "AdminOnly")]
public class EmployeeLogsModel : PageModel
{
    private readonly MyDbContext _context;
    public EmployeeLogsModel(MyDbContext context) => _context = context;

    public List<Employee> Employees { get; set; } = new();

    public async Task OnGetAsync()
    {
        Employees = await _context.Employees
            .Where(e => !string.IsNullOrEmpty(e.Description))
            .OrderBy(e => e.PersonnelCode)
            .ToListAsync();
    }
}