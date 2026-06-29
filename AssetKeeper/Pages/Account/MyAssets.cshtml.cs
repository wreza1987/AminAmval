using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Account;

[Authorize]
public class MyAssetsModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyAssetsModel(MyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<AssetAssignment> Assignments { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.EmployeeId == null) return;

        Assignments = await _context.AssetAssignments
            .Include(a => a.Asset)
            .Where(a => a.EmployeeId == user.EmployeeId && a.ReturnDate == null)
            .OrderByDescending(a => a.AssignmentDate)
            .ToListAsync();
    }
}