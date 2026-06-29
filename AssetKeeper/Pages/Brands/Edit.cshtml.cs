using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Brands;

[Authorize]
public class EditModel : PageModel
{
    private readonly MyDbContext _context;

    public EditModel(MyDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Brand Brand { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // TempData.Remove("Success");

        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return NotFound();

        Brand = brand;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // TempData.Remove("Success");

        if (await _context.Brands.AnyAsync(b => b.Name == Brand.Name && b.Id != Brand.Id))
        {
            ModelState.AddModelError("Brand.Name", "این نام برند قبلاً ثبت شده است.");
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        _context.Attach(Brand).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = "برند با موفقیت ویرایش شد.";
        return RedirectToPage("Index");
    }
}