using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Assignments;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MyDbContext _context;

    public IndexModel(MyDbContext context)
    {
        _context = context;
    }

    public IList<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();

    public async Task OnGetAsync()
    {
        Assignments = await _context.AssetAssignments
            .Include(a => a.Asset)
            .Include(a => a.Employee)
            .Where(a => a.ReturnDate == null)   // فقط تخصیص‌های جاری
            .OrderByDescending(a => a.AssignmentDate)
            .ToListAsync();
    }
}