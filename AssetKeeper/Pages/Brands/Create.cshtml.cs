using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Brands;

[Authorize]
public class CreateModel : PageModel
{
    private readonly MyDbContext _context;

    public CreateModel(MyDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Brand Brand { get; set; } = new();

    public void OnGet()
    {
        // TempData.Remove("Success");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // TempData.Remove("Success");

        if (await _context.Brands.AnyAsync(b => b.Name == Brand.Name))
        {
            ModelState.AddModelError("Brand.Name", "این نام برند قبلاً ثبت شده است.");
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        _context.Brands.Add(Brand);
        await _context.SaveChangesAsync();

        TempData["Success"] = "برند با موفقیت ثبت شد.";
        return RedirectToPage("Index");
    }
}