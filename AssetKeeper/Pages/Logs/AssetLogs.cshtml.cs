using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Logs;

[Authorize(Policy = "AdminOrWarehouse")]
public class AssetLogsModel : PageModel
{
    private readonly MyDbContext _context;
    public AssetLogsModel(MyDbContext context) => _context = context;

    public List<AssetHistory> Logs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Logs = await _context.AssetHistory
            .Include(h => h.Asset)
            .Include(h => h.ChangedByEmployee)
            .OrderByDescending(h => h.ChangeDate)
            .ToListAsync();
    }
}