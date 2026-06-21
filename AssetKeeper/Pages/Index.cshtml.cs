using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Context;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages;

public class IndexModel : PageModel
{
    private readonly MyDbContext _context;

    public IndexModel(MyDbContext context)
    {
        _context = context;
    }

    public int TotalAssets { get; set; }
    public int AssignedAssets { get; set; }
    public int TotalEmployees { get; set; }

    public async Task OnGetAsync()
    {
        TotalAssets = await _context.Assets.CountAsync();
        AssignedAssets = await _context.AssetAssignments.CountAsync(a => a.ReturnDate == null);
        TotalEmployees = await _context.Employees.CountAsync();
    }
}
