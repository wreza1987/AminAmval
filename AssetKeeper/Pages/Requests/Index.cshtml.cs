using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Pages.Requests;

[Authorize(Policy = "AdminOrWarehouse")]
public class IndexModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(MyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<UserRequest> Requests { get; set; } = new();

    [BindProperty] public int ReplyEmployeeId { get; set; }
    [BindProperty] public string ReplyMessage { get; set; } = "";

    public async Task OnGetAsync()
    {
        Requests = await _context.UserRequests
            .Include(r => r.Employee)
            .Include(r => r.SenderEmployee)   // ✅ اضافه کن
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostReplyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.EmployeeId == null || string.IsNullOrWhiteSpace(ReplyMessage))
            return RedirectToPage();

        _context.UserRequests.Add(new UserRequest
        {
            EmployeeId = ReplyEmployeeId,   // کارمندی که پیامش بوده
            Message = ReplyMessage,
            // SenderEmployeeId رو از user می‌خونیم در view
        });

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}