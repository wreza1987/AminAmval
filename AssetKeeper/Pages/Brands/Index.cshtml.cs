using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;

namespace AssetKeeper.Pages.Brands;

public class IndexModel : PageModel
{
    private readonly MyDbContext _context;

    public IndexModel(MyDbContext context)
    {
        _context = context;
    }

    public IList<Brand> Brands { get; set; } = new List<Brand>();

    public async Task OnGetAsync()
    {
        Brands = await _context.Brands
            .Include(b => b.Assets)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }
}