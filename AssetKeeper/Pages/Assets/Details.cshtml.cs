using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Assets;

[Authorize]
public class DetailsModel : BasePageModel
{
    public DetailsModel(MyDbContext context) : base(context) { }

    public Asset Asset { get; set; } = new();
    public List<AssetHistory> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var asset = await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Brand)
            .Include(a => a.History)
                .ThenInclude(h => h.ChangedByEmployee)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null)
            return NotFound();

        Asset = asset;
        History = asset.History.ToList();

        return Page();
    }
}