using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using httt.Areas.Admin.ViewModels;
using httt.Data;

namespace httt.Areas.Admin.Controllers;

public sealed class DashboardData
{
    public int TotalUsers { get; set; }
    public int ActiveAccounts { get; set; }
    public int AdminCount { get; set; }
    public int ManagerCount { get; set; }
    public int EmployeeCount { get; set; }
    public int ProductionCount { get; set; }
    public int WarehouseCount { get; set; }
    public int SalesCount { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalProductInventoryValue { get; set; }
    public int TotalIngredients { get; set; }
    public decimal TotalIngredientInventoryValue { get; set; }
    public int MonthOrders { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public decimal TotalWipQuantity { get; set; }
    public int PendingProductionRequests { get; set; }
    public int InProgressProductionRequests { get; set; }
    public int OpenProductionRequests { get; set; }
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
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Manager') AS "ManagerCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Employee') AS "EmployeeCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Production') AS "ProductionCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Warehouse') AS "WarehouseCount",
    (SELECT COUNT(*) FROM public.useraccount WHERE role = 'Sales') AS "SalesCount",
    (SELECT COUNT(*) FROM public.product) AS "TotalProducts",
    (SELECT COALESCE(SUM(quantity * cost), 0) FROM public.product) AS "TotalProductInventoryValue",
    (SELECT COUNT(*) FROM public.ingredient) AS "TotalIngredients",
    (SELECT COALESCE(SUM(quantity * price), 0) FROM public.ingredient) AS "TotalIngredientInventoryValue",
    (SELECT COUNT(*) FROM public.productreceipt WHERE createddate >= DATE_TRUNC('month', CURRENT_DATE) AND createddate < DATE_TRUNC('month', CURRENT_DATE) + INTERVAL '1 month') AS "MonthOrders",
    (SELECT COALESCE(SUM(totalamount), 0) FROM public.saleorder WHERE orderdate >= DATE_TRUNC('month', CURRENT_DATE) AND orderdate < DATE_TRUNC('month', CURRENT_DATE) + INTERVAL '1 month') AS "CurrentMonthRevenue",
    (SELECT COALESCE(SUM(quantity), 0) FROM public.wip_inventory) AS "TotalWipQuantity",
    (SELECT COUNT(*) FROM public.productionrequest WHERE status = 'Pending') AS "PendingProductionRequests",
    (SELECT COUNT(*) FROM public.productionrequest WHERE status = 'Processing') AS "InProgressProductionRequests",
    (SELECT COUNT(*) FROM public.productionrequest WHERE status IN ('Pending', 'Processing')) AS "OpenProductionRequests"
""";

                var dashboardTask = GetDashboardMetricsAsync(sql, cancellationToken);
                var recentUsersTask = GetRecentUsersAsync(cancellationToken);
                var recentProductionTask = GetRecentProductionRequestsAsync(cancellationToken);

                await Task.WhenAll(dashboardTask, recentUsersTask, recentProductionTask);

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
                    ManagerCount = dashboard.ManagerCount,
                    EmployeeCount = dashboard.EmployeeCount,
                    ProductionCount = dashboard.ProductionCount,
                    WarehouseCount = dashboard.WarehouseCount,
                    SalesCount = dashboard.SalesCount,
                    TotalProducts = dashboard.TotalProducts,
                    TotalProductInventoryValue = dashboard.TotalProductInventoryValue,
                    TotalIngredients = dashboard.TotalIngredients,
                    TotalIngredientInventoryValue = dashboard.TotalIngredientInventoryValue,
                    MonthOrders = dashboard.MonthOrders,
                    CurrentMonthRevenue = dashboard.CurrentMonthRevenue,
                    TotalWipQuantity = dashboard.TotalWipQuantity,
                    PendingProductionRequests = dashboard.PendingProductionRequests,
                    InProgressProductionRequests = dashboard.InProgressProductionRequests,
                    OpenProductionRequests = dashboard.OpenProductionRequests,
                    RecentUsers = await recentUsersTask,
                    RecentProductionRequests = await recentProductionTask
                };

                vm.RoleLabels = JsonSerializer.Serialize(new[] { "Admin", "Manager", "Employee", "Production", "Warehouse", "Sales" });
                vm.RoleValues = JsonSerializer.Serialize(new[]
                {
                    vm.AdminCount,
                    vm.ManagerCount,
                    vm.EmployeeCount,
                    vm.ProductionCount,
                    vm.WarehouseCount,
                    vm.SalesCount
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
            .Take(5)
            .Select(u => new RecentUserItem
            {
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                Status = u.Status
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<RecentProductionRequestItem>> GetRecentProductionRequestsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.ProductionRequests
            .AsNoTracking()
            .Include(r => r.AssignedUser)
            .Include(r => r.Product)
            .OrderByDescending(r => r.RequestId)
            .Take(5)
            .Select(r => new RecentProductionRequestItem
            {
                RequestId = r.RequestId,
                ProductName = r.Product != null ? r.Product.ProductName : "—",
                Status = r.Status,
                CreatedDate = r.CreatedDate,
                RequestedByUsername = r.AssignedUser != null ? r.AssignedUser.Username : "—",
                RequestedByFullName = r.AssignedUser != null ? r.AssignedUser.FullName : "—"
            })
            .ToListAsync(cancellationToken);
    }
}
