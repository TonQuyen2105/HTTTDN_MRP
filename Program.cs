using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using httt.Constants;
using httt.Data;
using httt.Models;
using httt.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối CSDL PostgreSQL & Services nghiệp vụ
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<MaterialRequirementService>();
builder.Services.AddMemoryCache();

// 2. Chống giả mạo yêu cầu (Antiforgery)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// 3. Xác thực bằng Cookie Authentication chuẩn MVC
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".AMIS.MRP.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (IsAjaxOrApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"success\":false,\"message\":\"Chưa đăng nhập hoặc phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.\",\"requireLogin\":true}");
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (IsAjaxOrApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"success\":false,\"message\":\"Bạn không có quyền thực hiện thao tác này.\",\"accessDenied\":true}");
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnValidatePrincipal = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int userId))
                {
                    var user = await db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId);
                    if (user == null || (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase) && !string.Equals(user.Status, "true", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
                else
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

// CLI Tools: Xử lý lệnh CLI quản trị một lần an toàn (nếu có)
if (args.Length > 0)
{
    if (args[0] == "--migrate-passwords")
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();
        var users = await db.UserAccounts.ToListAsync();
        int migrated = 0;
        int alreadyHashed = 0;

        foreach (var u in users)
        {
            if (string.IsNullOrWhiteSpace(u.Password)) continue;

            bool isHashed = false;
            if (u.Password.StartsWith("AQAAAA") && u.Password.Length >= 40)
            {
                try
                {
                    var bytes = Convert.FromBase64String(u.Password);
                    if (bytes.Length >= 17 && bytes[0] == 0x01) isHashed = true;
                }
                catch { }
            }

            if (!isHashed)
            {
                u.Password = hasher.HashPassword(u, u.Password);
                migrated++;
            }
            else
            {
                alreadyHashed++;
            }
        }

        if (migrated > 0)
        {
            await db.SaveChangesAsync();
        }

        Console.WriteLine($"[MIGRATION-SUCCESS] Hoan tat chuyen doi: {migrated} tai khoan da duoc ma hoa bang PasswordHasher, {alreadyHashed} tai khoan da duoc ma hoa tu truoc.");
        return;
    }
    else if (args[0] == "--init-user" && args.Length >= 5)
    {
        // Cú pháp: dotnet run -- --init-user <username> <role> <fullname> <password>
        var username = args[1].Trim().ToLowerInvariant();
        var rawRole = args[2].Trim();
        var fullName = args[3].Trim();
        var password = args[4];

        var normalizedRole = AppRoles.NormalizeRole(rawRole);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();

        var existing = await db.UserAccounts.FirstOrDefaultAsync(u => u.Username.ToLower() == username);
        if (existing != null)
        {
            existing.Role = normalizedRole;
            existing.FullName = fullName;
            existing.Status = "Active";
            existing.Password = hasher.HashPassword(existing, password);
            await db.SaveChangesAsync();
            Console.WriteLine($"[INIT-USER-SUCCESS] Da cap nhat tai khoan '{username}' (Vai tro: {normalizedRole}, Ho ten: {fullName}) thanh cong.");
        }
        else
        {
            var newUser = new UserAccount
            {
                Username = username,
                Role = normalizedRole,
                FullName = fullName,
                Status = "Active",
                Password = string.Empty
            };
            newUser.Password = hasher.HashPassword(newUser, password);
            db.UserAccounts.Add(newUser);
            await db.SaveChangesAsync();
            Console.WriteLine($"[INIT-USER-SUCCESS] Da khoi tao tai khoan moi '{username}' (Vai tro: {normalizedRole}, Ho ten: {fullName}) thanh cong.");
        }
        return;
    }
}

// 4. Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Xác thực & Phân quyền
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// 5. Route mappings
// 5.1 Route chi tiết cho phân hệ Productions
app.MapAreaControllerRoute(
    name: "productions_full",
    areaName: "Productions",
    pattern: "Productions/{controller=Home}/{action=Index}/{id?}");

// 5.2 Route ngắn cho Productions (ví dụ /Productions/DrugOrder, /Productions/MaterialRequirement)
app.MapAreaControllerRoute(
    name: "productions_short",
    areaName: "Productions",
    pattern: "Productions/{action=Index}/{id?}",
    defaults: new { controller = "Home" });

// 5.3 Route ngắn cho các Area: /Admin, /Warehouse, /Sales
app.MapControllerRoute(
    name: "areas_short",
    pattern: "{area:exists}/{action=Index}/{id?}",
    defaults: new { controller = "Home" });

// 5.4 Route chuẩn cho các Area: /{area:exists}/{controller=Home}/{action=Index}/{id?}
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 5.5 Route mặc định (chuyển hướng người dùng vào trang đăng nhập hoặc trang chính phân hệ)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool IsAjaxOrApiRequest(HttpRequest request)
{
    if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
        return true;

    var accept = request.Headers.Accept.ToString();
    if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        return true;

    if (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        return true;

    if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        return true;

    return false;
}
