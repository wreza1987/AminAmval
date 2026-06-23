using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Categories;

[Authorize]
public class EditModel : PageModel
{
    private readonly MyDbContext _context;

    public EditModel(MyDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Category Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        TempData.Remove("Success");

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        Category = category;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TempData.Remove("Success");

        if (await _context.Categories.AnyAsync(c => c.Name == Category.Name && c.Id != Category.Id))
        {
            ModelState.AddModelError("Category.Name", "این نام دسته‌بندی قبلاً ثبت شده است.");
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        _context.Attach(Category).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = "دسته‌بندی با موفقیت ویرایش شد.";
        return RedirectToPage("Index");
    }
}