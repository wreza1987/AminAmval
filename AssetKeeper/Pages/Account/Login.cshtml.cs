using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using AssetKeeper.Domain.Entities;

namespace AssetKeeper.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "کد پرسنلی الزامی است")]
        [Display(Name = "کد پرسنلی")]
        public string PersonnelCode { get; set; } = "";

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = false;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.PersonnelCode, Input.Password, Input.RememberMe, false);

        if (result.Succeeded)
            return LocalRedirect("/");

        ErrorMessage = "کد پرسنلی یا رمز عبور اشتباه است.";
        return Page();
    }
}