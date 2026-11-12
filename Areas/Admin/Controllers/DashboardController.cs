using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using httt.Areas.Admin.ViewModels;
using httt.Constants;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Controllers;

public sealed class DashboardData
{
    public int TotalUsers { get; set; }
    public int ActiveAccounts { get; set; }
    public int AdminCount { get; set; }
    public int SalesCount { get; set; }
    public int WarehouseCount { get; set; }
    public int ProductionCount { get; set; }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController : Controller
{
    private const string DashboardCacheKey = "admin:dashboard:metrics";

    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardController> _logger;
    private readonly IMemoryCache _cache;

    public DashboardController(
        ApplicationDbContext context,
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardController> logger,
        IMemoryCache cache)
        => (_context, _scopeFactory, _logger, _cache) = (context, scopeFactory, logger, cache);

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            if (!_cache.TryGetValue(DashboardCacheKey, out DashboardViewModel? cachedVm))
            {
                var sql = """
SELECT
    (SELECT COUNT(*) FROM public.useraccount) AS "TotalUsers",
    (SELECT COUNT(*) FROM public.useraccount WHERE status = 'Active') AS "ActiveAccounts",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Admin') AS "AdminCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Sales') AS "SalesCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Warehouse') AS "WarehouseCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Production') AS "ProductionCount"
""";

                var dashboardTask = GetDashboardMetricsAsync(sql, cancellationToken);
                var recentUsersTask = GetRecentUsersAsync(cancellationToken);

                await Task.WhenAll(dashboardTask, recentUsersTask);

                var dashboard = await dashboardTask;
                if (dashboard is null)
                {
                    return View(new DashboardViewModel());
                }

                var vm = new DashboardViewModel
                {
                    TotalAccounts = dashboard.TotalUsers,
                    ActiveAccounts = dashboard.ActiveAccounts,
                    LockedAccounts = dashboard.TotalUsers - dashboard.ActiveAccounts,
                    AdminCount = dashboard.AdminCount,
                    SalesCount = dashboard.SalesCount,
                    WarehouseCount = dashboard.WarehouseCount,
                    ProductionCount = dashboard.ProductionCount,
                    RecentUsers = await recentUsersTask
                };

                vm.RoleLabels = JsonSerializer.Serialize(new[] { "Admin", "Sales", "Warehouse", "Production" });
                vm.RoleValues = JsonSerializer.Serialize(new[]
                {
                    vm.AdminCount,
                    vm.SalesCount,
                    vm.WarehouseCount,
                    vm.ProductionCount
                });

                _cache.Set(DashboardCacheKey, vm, TimeSpan.FromMinutes(2));
                cachedVm = vm;
            }

            return View(cachedVm ?? new DashboardViewModel());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load Admin dashboard metrics.");
            TempData["ErrorMessage"] = "Không thể tải số liệu Dashboard. Vui lòng thử lại.";
            return View(new DashboardViewModel());
        }
    }

    private async Task<DashboardData?> GetDashboardMetricsAsync(string sql, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Database
            .SqlQueryRaw<DashboardData>(sql)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<List<RecentUserItem>> GetRecentUsersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.UserAccounts
            .AsNoTracking()
            .OrderByDescending(u => u.UserId)
            .Take(8)
            .Select(u => new RecentUserItem
            {
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                Status = u.Status
            })
            .ToListAsync(cancellationToken);
    }

    // ── GET: /Admin/Dashboard/DownloadBackup ──────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadBackup(CancellationToken cancellationToken)
    {
        try
        {
            var backupPayload = new
            {
                System = "HTTT MRP Dược phẩm",
                BackedUpAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                BackedUpBy = User.Identity?.Name ?? "admin",
                Tables = new
                {
                    UserAccounts = await _context.UserAccounts.AsNoTracking().OrderBy(u => u.UserId).ToListAsync(cancellationToken),
                    Suppliers = await _context.Suppliers.AsNoTracking().OrderBy(s => s.SupplierId).ToListAsync(cancellationToken),
                    Ingredients = await _context.Ingredients.AsNoTracking().OrderBy(i => i.IngrId).ToListAsync(cancellationToken),
                    Products = await _context.Products.AsNoTracking().OrderBy(p => p.ProductId).ToListAsync(cancellationToken),
                    Boms = await _context.Boms.AsNoTracking().OrderBy(b => b.BomId).ToListAsync(cancellationToken),
                    ProductionRequests = await _context.ProductionRequests.AsNoTracking().OrderBy(r => r.RequestId).ToListAsync(cancellationToken),
                    SaleOrders = await _context.SaleOrders.AsNoTracking().OrderBy(o => o.OrderId).ToListAsync(cancellationToken),
                    ImportReceipts = await _context.ImportReceipts.AsNoTracking().ToListAsync(cancellationToken),
                    MaterialReceipts = await _context.MaterialReceipts.AsNoTracking().ToListAsync(cancellationToken),
                    ProductReceipts = await _context.ProductReceipts.AsNoTracking().OrderBy(r => r.ProductReceiptId).ToListAsync(cancellationToken),
                    ProductExportReceipts = await _context.ProductExportReceipts.AsNoTracking().OrderBy(r => r.ExportReceiptId).ToListAsync(cancellationToken)
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(backupPayload, jsonOptions));
            var fileName = $"MRP_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            return File(jsonBytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database backup.");
            TempData["ErrorMessage"] = "Không thể tạo file sao lưu cơ sở dữ liệu. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── POST: /Admin/Dashboard/RestoreBackup ──────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreBackup(IFormFile? backupFile, CancellationToken cancellationToken)
    {
        if (backupFile is null || backupFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file sao lưu (.json) hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (!backupFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Định dạng file không hợp lệ. Vui lòng tải lên file .json.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = backupFile.OpenReadStream();
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("Tables", out var tablesElement) ||
                tablesElement.ValueKind != JsonValueKind.Object)
            {
                TempData["ErrorMessage"] = "Cấu trúc file sao lưu không đúng định dạng chuẩn của hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            // Đồng bộ an toàn các bản ghi UserAccounts còn thiếu từ bản sao lưu mà không gây xung đột khóa ngoại
            if (tablesElement.TryGetProperty("UserAccounts", out var usersElement) &&
                usersElement.ValueKind == JsonValueKind.Array)
            {
                var backupUsers = JsonSerializer.Deserialize<List<UserAccount>>(
                    usersElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

                if (backupUsers.Count > 0)
                {
                    var existingUsernames = await _context.UserAccounts
                        .AsNoTracking()
                        .Select(u => u.Username.ToLower())
                        .ToHashSetAsync(cancellationToken);

                    foreach (var bu in backupUsers)
                    {
                        if (string.IsNullOrWhiteSpace(bu.Username) || string.IsNullOrWhiteSpace(bu.Password))
                            continue;

                        var key = bu.Username.Trim().ToLowerInvariant();
                        if (!existingUsernames.Contains(key))
                        {
                            _context.UserAccounts.Add(new UserAccount
                            {
                                Username = bu.Username.Trim(),
                                Password = bu.Password,
                                FullName = string.IsNullOrWhiteSpace(bu.FullName) ? bu.Username.Trim() : bu.FullName.Trim(),
                                Phone = bu.Phone,
                                Role = string.IsNullOrWhiteSpace(bu.Role) ? AppRoles.Production : (AppRoles.NormalizeRole(bu.Role) ?? AppRoles.Production),
                                Status = string.IsNullOrWhiteSpace(bu.Status) ? "Active" : bu.Status.Trim()
                            });
                            existingUsernames.Add(key);
                        }
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            _cache.Remove(DashboardCacheKey);
            TempData["SuccessMessage"] = "Phục hồi cơ sở dữ liệu thành công từ bản sao lưu!";
            return RedirectToAction(nameof(Index));
        }
        catch (JsonException)
        {
            TempData["ErrorMessage"] = "File tải lên không phải là định dạng JSON hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database backup.");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi phục hồi cơ sở dữ liệu từ bản sao lưu.";
            return RedirectToAction(nameof(Index));
        }
    }
}
