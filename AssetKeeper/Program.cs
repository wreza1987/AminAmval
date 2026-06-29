using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Services;
using AssetKeeper.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;


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

// Program.cs — بعد از AddIdentity اضافه کن:
builder.Services.AddScoped<IClaimsTransformation, AccessLevelClaimsTransformer>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",    p => p.RequireClaim("AccessLevel", "Admin"));
    options.AddPolicy("AdminOrWarehouse", p => p.RequireClaim("AccessLevel", "Admin", "WarehouseKeeper"));
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<PermissionService>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Logout");
    // فقط Admin
    options.Conventions.AuthorizePage("/Admin/AccessLevels", "AdminOnly");
});

// رفع خطای TimeProvider (این خط خیلی مهمه)
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

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

app.UseMiddleware<PermissionMiddleware>();
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