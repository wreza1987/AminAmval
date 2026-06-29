using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Categories;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MyDbContext _context;

    public IndexModel(MyDbContext context)
    {
        _context = context;
    }

    public IList<Category> Categories { get; set; } = new List<Category>();

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .Include(c => c.Assets)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}