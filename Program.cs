using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Npgsql;
using httt.Areas.Admin.Services;
using httt.Constants;
using httt.Data;
using httt.Models;
using httt.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối CSDL PostgreSQL & Services nghiệp vụ
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
                maxRetryCount: 3,
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
            // Request AJAX (fetch) không được redirect sang trang login/AccessDenied (HTML 200 làm client
            // parse JSON lỗi) mà phải nhận 401/403 để hiển thị "phiên đã hết hạn".
            OnRedirectToLogin = context =>
            {
                if (IsAjaxOrApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"success\":false,\"message\":\"Chưa đăng nhập hoặc phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.\",\"requireLogin\":true}");
                }

                var reqPath = context.Request.Path.Value ?? string.Empty;
                var targetLoginPath = AppRoles.GetLoginUrlForPath(reqPath);
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                var redirectUrl = string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/"
                    ? targetLoginPath
                    : $"{targetLoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}";

                context.Response.Redirect(redirectUrl);
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

                // Khi người dùng đang đăng nhập ở phân hệ khác và gõ URL sang phân hệ mới,
                // điều hướng về trang đăng nhập riêng của phân hệ đích để họ đăng nhập tài khoản phù hợp.
                var reqPath = context.Request.Path.Value ?? string.Empty;
                var targetLoginPath = AppRoles.GetLoginUrlForPath(reqPath);
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                var redirectUrl = string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/"
                    ? targetLoginPath
                    : $"{targetLoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}";

                context.Response.Redirect(redirectUrl);
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

                var idValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleValue = principal.FindFirst(ClaimTypes.Role)?.Value;

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
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

// ── Swagger & API Explorer (phân hệ Warehouse) ────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MRP Warehouse & Production Web API",
        Version = "v1",
        Description = "Hệ thống Web API quản lý kho nguyên vật liệu, thành phẩm, nhà cung cấp và báo cáo tồn kho MRP."
    });
    // Chỉ hiển thị các controller API có [ApiController], ẩn các Controller View MVC thông thường
    c.DocInclusionPredicate((docName, apiDesc) =>
        apiDesc.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any());
});

// ── Application Services ──────────────────────────────────────────────────
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<MaterialRequirementService>();
builder.Services.AddScoped<ProductionHandoverService>();

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
            if (u.Password.StartsWith("$2", StringComparison.Ordinal))
            {
                isHashed = true;
            }
            else if (u.Password.StartsWith("AQAAAA") && u.Password.Length >= 40)
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
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE public.supplier ADD COLUMN IF NOT EXISTS import_cost NUMERIC(5,2) DEFAULT 0;");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not ensure supplier.import_cost column on startup.");
    }

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

// 4. Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MRP Warehouse Web API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "MRP Warehouse API Documentation";
});

app.UseStaticFiles();

app.UseRouting();

// Xác thực & Phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Bảo vệ các tài nguyên tĩnh trong /Areas/Warehouse (đặc biệt là /Areas/Warehouse/Views/Index.html)
// để chỉ tài khoản có vai trò Warehouse/Kho mới được truy cập trực tiếp.
app.Use(async (context, next) =>
{
    var reqPath = context.Request.Path.Value ?? string.Empty;
    if (reqPath.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase))
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var isWarehouseRole = isAuthenticated &&
            (context.User.IsInRole(AppRoles.Warehouse) ||
             context.User.IsInRole("Kho") ||
             AppRoles.NormalizeRole(context.User.FindFirstValue(ClaimTypes.Role)) == AppRoles.Warehouse);

        if (!isWarehouseRole)
        {
            if (IsAjaxOrApiRequest(context.Request))
            {
                context.Response.StatusCode = isAuthenticated ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"success\":false,\"message\":\"Vui lòng đăng nhập tài khoản Phân hệ Kho dược phẩm.\"}");
                return;
            }

            var returnUrl = Uri.EscapeDataString("/Warehouse/Dashboard");
            context.Response.Redirect($"/Warehouse/Account/Login?returnUrl={returnUrl}");
            return;
        }
    }

    await next();
});

// Cho phép phục vụ các file HTML, CSS, JS trong thư mục Areas sau khi đã qua bước kiểm tra quyền
var areasPath = Path.Combine(builder.Environment.ContentRootPath, "Areas");
if (Directory.Exists(areasPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(areasPath),
        RequestPath = "/Areas",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    });
}

app.MapStaticAssets();
app.MapControllers();

// 5. Route mappings
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

// 5.3 Route cho phân hệ Warehouse (/Warehouse/Dashboard, /Warehouse/Home/Index...)
app.MapAreaControllerRoute(
    name: "warehouse_area",
    areaName: "Warehouse",
    pattern: "Warehouse/{controller=Dashboard}/{action=Index}/{id?}");

// 5.4 Route cho phân hệ Sales (/Sales/Order, /Sales/Orders, /Sales/CreateOrder...)
app.MapAreaControllerRoute(
    name: "sales_area",
    areaName: "Sales",
    pattern: "Sales/{controller=Order}/{action=Index}/{id?}");

// 5.5 Route ngắn cho các Area: /Admin, /Warehouse, /Sales
app.MapControllerRoute(
    name: "areas_short",
    pattern: "{area:exists}/{action=Index}/{id?}",
    defaults: new { controller = "Home" });

// 5.6 Route chuẩn cho các Area: /{area:exists}/{controller=Home}/{action=Index}/{id?}
app.MapControllerRoute(
    name: "areas_home",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 5.7 Route chuẩn cho các Area dùng Dashboard controller
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

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
