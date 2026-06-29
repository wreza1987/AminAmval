using AssetKeeper.Context;
using AssetKeeper.Domain.Entities;
using AssetKeeper.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AssetKeeper.Middleware;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    // صفحاتی که انباردار به آن‌ها دسترسی دارد
    private static readonly string[] WarehousePages = new[]
    {
        "/",
        "/index",
        "/assets",
        "/assets/index",
        "/assets/create",
        "/assets/edit",
        "/assets/details",
        "/assets/status",
        "/employees",
        "/employees/index",
        "/employees/create",
        "/employees/edit",
        "/employees/details",
        "/assignments",
        "/assignments/index",
        "/assignments/create",
        "/assignments/return",
        "/requests",
        "/requests/index",
        "/categories",
        "/categories/index",
        "/brands",
        "/brands/index",
        "/logs/assetlogs",
    };

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        UserManager<ApplicationUser> userManager,
        MyDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower().TrimEnd('/') ?? "";
        if (string.IsNullOrEmpty(path)) path = "/";

        // فایل static یا خالی → رد بشه
        if (path.Contains('.') || path.StartsWith("/_"))
        {
            await _next(context);
            return;
        }

        // صفحات Account و Admin/AccessLevels → رد بشه (Identity و Authorize خودشون چک می‌کنن)
        if (path.StartsWith("/account"))
        {
            await _next(context);
            return;
        }

        // فقط برای کاربران لاگین‌شده چک کن
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var user = await userManager.GetUserAsync(context.User);
        if (user?.EmployeeId == null)
        {
            await _next(context);
            return;
        }

        var employee = await db.Employees.FindAsync(user.EmployeeId);
        if (employee == null)
        {
            await _next(context);
            return;
        }

        switch (employee.AccessLevel)
        {
            case EmployeeAccessLevel.Disable:
                await context.SignOutAndRedirect("/Account/Login");
                return;

            case EmployeeAccessLevel.Admin:
                // ادمین به همه چیز دسترسی داره
                await _next(context);
                return;

            case EmployeeAccessLevel.Normal:
                // فقط به Account دسترسی داره
                if (!path.StartsWith("/account"))
                {
                    context.Response.Redirect("/Account/MyAssets");
                    return;
                }
                await _next(context);
                return;

            case EmployeeAccessLevel.WarehouseKeeper:
                // Admin/AccessLevels ممنوع
                if (path.StartsWith("/admin"))
                {
                    await Forbidden(context, path);
                    return;
                }

                // چک لیست صفحات مجاز
                // path مثلاً "/assets/edit" هست، چک می‌کنیم با prefix
                bool allowed = WarehousePages.Any(p =>
                    path == p || path.StartsWith(p + "/") || path.StartsWith(p + "?"));

                if (!allowed)
                {
                    await Forbidden(context, path);
                    return;
                }

                await _next(context);
                return;
        }

        await _next(context);
    }

    private static async Task Forbidden(HttpContext context, string path)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(
            "<div style='font-family:sans-serif;text-align:center;margin-top:100px'>" +
            "<h2>⛔ دسترسی ندارید</h2>" +
            $"<p style='color:gray'>مسیر: {path}</p>" +
            "<a href='/'>بازگشت به خانه</a></div>");
    }
}

// Extension برای SignOut در Middleware
public static class HttpContextExtensions
{
    public static Task SignOutAndRedirect(this HttpContext context, string url)
    {
        context.Response.Redirect(url);
        return Task.CompletedTask;
    }
}