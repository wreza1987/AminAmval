using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<MyDbContext>()
.AddDefaultTokenProviders();


// رفع خطای TimeProvider (این خط خیلی مهمه)
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// builder.Services.AddRazorPages();


builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");          // همه صفحات نیاز به login
    options.Conventions.AllowAnonymousToPage("/Account/Login");  // به جز Login
    options.Conventions.AllowAnonymousToPage("/Account/Logout");
});



builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// app.MapGet("/", () => Results.Redirect("/Account/Login")).RequireAuthorization();
app.MapRazorPages();

// Seed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    context.Database.Migrate();
    await DataSeeder.SeedAsync(context);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // if (!userManager.Users.Any())
    // {
    //     var adminUser = new ApplicationUser
    //     {
    //         UserName = "admin",        // کد پرسنلی برای لاگین
    //         Email = "admin@admin.com",
    //         EmailConfirmed = true,
    //     };
    //     await userManager.CreateAsync(adminUser, "admin123");
    // }
}

app.Run();