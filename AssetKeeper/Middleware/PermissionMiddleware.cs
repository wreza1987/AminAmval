using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using AssetKeeper.Services;
using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Middleware;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        PermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        MyDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value?.TrimStart('/') ?? "";

            // صفحات Account همیشه مجاز
            if (!path.StartsWith("Account") && !path.StartsWith("Admin"))
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user?.EmployeeId != null)
                {
                    var employee = await db.Employees.FindAsync(user.EmployeeId);
                    if (employee != null)
                    {
                        // غیرفعال — اخراج
                        if (employee.AccessLevel == EmployeeAccessLevel.Disable)
                        {
                            context.Response.Redirect("/Account/Login");
                            return;
                        }

                        // pageKey رو از path استخراج کن
                        var pageKey = ExtractPageKey(path);
                        if (!string.IsNullOrEmpty(pageKey) && pageKey != "")
                        {
                            var allowed = await permissionService.IsAllowedAsync(pageKey, employee.AccessLevel);
                            if (!allowed)
                            {
                                context.Response.StatusCode = 403;
                                context.Response.ContentType = "text/html; charset=utf-8";
                                await context.Response.WriteAsync(
                                    "<h2 style='font-family:sans-serif;text-align:center;margin-top:100px'>⛔ دسترسی ندارید</h2>");
                                return;
                            }
                        }
                    }
                }
            }
        }

        await _next(context);
    }

    private string ExtractPageKey(string path)
    {
        // "Assets/Index", "Assets/Create?id=1" → "Assets/Create"
        var clean = path.Split('?')[0].Trim('/');
        return clean;
    }
}