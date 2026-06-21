using AssetKeeper.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// === DbContext ===
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Identity Configuration ===
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<MyDbContext>()
.AddDefaultUI()                    // این خط برای صفحات لاگین آماده
.AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // ← خیلی مهم: قبل از Authorization
app.UseAuthorization();

app.MapRazorPages();

// === Seed Data ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MyDbContext>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    context.Database.Migrate();

    await DataSeeder.SeedAsync(context);
    // await IdentitySeeder.SeedUsersAsync(userManager, context);   // فعلاً کامنت است
}

app.Run();