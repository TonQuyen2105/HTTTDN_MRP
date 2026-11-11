using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using httt.Data;
using httt.Areas.Admin.Services;
using httt.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

var hasPlaceholderConnectionString = !string.IsNullOrWhiteSpace(connectionString) && (
    connectionString.Contains("your-host", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("your-database", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("your-user", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("your-password", StringComparison.OrdinalIgnoreCase));

if (string.IsNullOrWhiteSpace(connectionString) || hasPlaceholderConnectionString)
{
    throw new InvalidOperationException(
        "Database is not configured. Set ConnectionStrings:DefaultConnection or the DATABASE_URL environment variable to a valid Supabase/PostgreSQL connection string before running the app.");
}

var npgsqlConnectionString = BuildSupabaseConnectionString(connectionString);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    ConfigureNpgsql(options, npgsqlConnectionString));

static void ConfigureNpgsql(DbContextOptionsBuilder options, string connectionString)
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 2,
                maxRetryDelay: TimeSpan.FromSeconds(2),
                errorCodesToAdd: null);

            npgsqlOptions.CommandTimeout(30);
        });
}

static string BuildSupabaseConnectionString(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Pooling = false,
        Timeout = 30,
        CommandTimeout = 30,
        IncludeErrorDetail = false
    };

    return builder.ConnectionString;
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.AccessDeniedPath = "/Admin/Account/Login";
        options.Cookie.Name = "HtttAdminAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events = new CookieAuthenticationEvents
        {
            // Request AJAX (fetch) không được redirect sang trang login/AccessDenied (HTML 200 làm client
            // parse JSON lỗi) mà phải nhận 401/403 để hiển thị "phiên đã hết hạn".
            OnRedirectToLogin = context =>
            {
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                else
                    context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            // Kiểm tra claim trong cookie, sau đó đối chiếu DB (kết quả cache 30s)
            // để tài khoản bị khóa/xóa/đổi vai trò bị thu hồi phiên gần như tức thì.
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                if (principal is null || principal.Identity?.IsAuthenticated != true)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var idValue = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var roleValue = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (!int.TryParse(idValue, out var userId) || string.IsNullOrWhiteSpace(roleValue))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                try
                {
                    var accountService = context.HttpContext.RequestServices.GetRequiredService<UserAccountService>();
                    // Kiểm tra tài khoản còn tồn tại, đang Active và vai trò DB còn khớp với claim
                    if (!await accountService.IsActiveUserSessionAsync(userId, roleValue))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
                catch (Exception ex)
                {
                    // DB tạm thời không truy cập được: giữ phiên thay vì đăng xuất hàng loạt
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("UserSession")
                        .LogWarning(ex, "Could not re-validate session for user {UserId}.", userId);
                }
            }
        };
    });

builder.Services.AddControllersWithViews(options =>
{
    // Protect every unsafe MVC request by default. Individual POST actions still
    // declare ValidateAntiForgeryToken explicitly to make the contract obvious.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

// ?? Application Services ??????????????????????????????????????????????????
builder.Services.AddScoped<UserAccountService>();

var app = builder.Build();

var autoMigrate = builder.Configuration.GetValue("DatabaseAutoMigrate", false);

if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration skipped because startup migration is not available/healthy. The app will continue without auto-migration.");
    }
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        if (!await db.UserAccounts.AsNoTracking().AnyAsync())
        {
            var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";

            db.UserAccounts.Add(new UserAccount
            {
                Username = adminUsername,
                Password = UserAccountService.HashPassword(adminPassword),
                FullName = "System Administrator",
                Phone = "0000000000",
                Role = "Admin",
                Status = "Active"
            });

            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Admin seed check failed. Database is reachable but user seeding was skipped.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "admin-login-alias",
    pattern: "Admin/Login",
    defaults: new { area = "Admin", controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "admin-login",
    pattern: "Admin/Account/Login",
    defaults: new { area = "Admin", controller = "Account", action = "Login" });

app.MapAreaControllerRoute(
    name: "admin_area",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
