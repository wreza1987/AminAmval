using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AssetKeeper.Pages.Account;

[Authorize]
public class MyRequestsModel : PageModel
{
    private readonly MyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyRequestsModel(MyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<UserRequest> Requests { get; set; } = new();
    public Employee? Employee { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "متن درخواست الزامی است")]
    public string Message { get; set; } = "";

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        Employee = user?.EmployeeId != null
            ? await _context.Employees.FindAsync(user.EmployeeId)
            : null;

        if (!ModelState.IsValid || Employee == null)
        {
            await LoadDataAsync();
            return Page();
        }

        _context.UserRequests.Add(new UserRequest
        {
            EmployeeId = Employee.Id,
            SenderEmployeeId = Employee.Id,
            Message = Message,
        });

        await _context.SaveChangesAsync();
        Message = "";
        ModelState.Clear();
        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.EmployeeId == null) return;

        Employee = await _context.Employees.FindAsync(user.EmployeeId);

        Requests = await _context.UserRequests
            .Include(r => r.Employee)
            .Include(r => r.SenderEmployee)
            .Where(r => r.EmployeeId == user.EmployeeId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }
}