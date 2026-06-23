using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AssetKeeper.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MyDbContext _context;

    public LoginModel(SignInManager<ApplicationUser> signInManager,
                      UserManager<ApplicationUser> userManager,
                      MyDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "کد پرسنلی الزامی است")]
        public string PersonnelCode { get; set; } = "";

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = false;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByNameAsync(Input.PersonnelCode);
        if (user == null)
        {
            ErrorMessage = "کد پرسنلی یا رمز عبور اشتباه است.";
            return Page();
        }

        // چک غیرفعال بودن
        var employee = await _context.Employees.FindAsync(user.EmployeeId);
        if (employee == null || employee.AccessLevel == EmployeeAccessLevel.Disable)
        {
            ErrorMessage = "حساب کاربری شما غیرفعال است.";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.PersonnelCode, Input.Password, Input.RememberMe, false);

        if (result.Succeeded)
            return LocalRedirect("/");

        ErrorMessage = "کد پرسنلی یا رمز عبور اشتباه است.";
        return Page();
    }
}