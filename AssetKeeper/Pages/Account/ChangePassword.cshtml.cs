using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;


namespace AssetKeeper.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangePasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (Input.NewPassword != Input.ConfirmPassword)
        {
            ModelState.AddModelError("", "رمزهای جدید مطابقت ندارند");
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);

        if (result.Succeeded)
        {
            TempData["Success"] = "رمز عبور با موفقیت تغییر یافت.";
            return RedirectToPage("/Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return Page();
    }
}