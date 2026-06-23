using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace AssetKeeper.Pages.Categories;

[Authorize]
public class CreateModel : PageModel
{
    private readonly MyDbContext _context;

    public CreateModel(MyDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Category Category { get; set; } = new();

    public void OnGet()
    {
        TempData.Remove("Success");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TempData.Remove("Success");

        if (await _context.Categories.AnyAsync(c => c.Name == Category.Name))
        {
            ModelState.AddModelError("Category.Name", "این نام دسته‌بندی قبلاً ثبت شده است.");
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        _context.Categories.Add(Category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "دسته‌بندی با موفقیت ثبت شد.";
        return RedirectToPage("Index");
    }
}