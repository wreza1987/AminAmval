using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;

namespace AssetKeeper.Pages.Employees;

public class DetailsModel : BasePageModel
{
    public DetailsModel(MyDbContext context) : base(context) { }

    public Employee Employee { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Assignments)
                .ThenInclude(a => a.Asset)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        Employee = employee;
        return Page();
    }
}