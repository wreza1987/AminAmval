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
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MyDbContext _context;

    public ProfileModel(UserManager<ApplicationUser> userManager, MyDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public Employee? Employee { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public ChangePasswordInput Input { get; set; } = new();

    public class ChangePasswordInput
    {
        [Required(ErrorMessage = "رمز فعلی الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور فعلی")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "رمز جدید الزامی است")]
        [MinLength(4, ErrorMessage = "رمز جدید حداقل ۴ کاراکتر باشد")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; } = "";

        [Compare("NewPassword", ErrorMessage = "تکرار رمز مطابقت ندارد")]
        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز جدید")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.EmployeeId != null)
            Employee = await _context.Employees.FindAsync(user.EmployeeId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.EmployeeId != null)
            Employee = await _context.Employees.FindAsync(user.EmployeeId);

        if (!ModelState.IsValid)
            return Page();

        var result = await _userManager.ChangePasswordAsync(user!, Input.CurrentPassword, Input.NewPassword);

        if (result.Succeeded)
        {
            SuccessMessage = "رمز عبور با موفقیت تغییر کرد.";
            ModelState.Clear();
            Input = new();
        }
        else
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Description ?? "خطا در تغییر رمز";
        }

        return Page();
    }
}