namespace httt.Areas.Admin.ViewModels;

public class LowStockItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Threshold { get; set; }
    public string Type { get; set; } = string.Empty; // "product" | "ingredient"
}

public class DashboardViewModel
{
    // ── Accounts ─────────────────────────────────────────────────────────
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public int LockedAccounts { get; set; }
    public int AdminCount { get; set; }
    public int ManagerCount { get; set; }
    public int EmployeeCount { get; set; }
    public int ProductionCount { get; set; }
    public int WarehouseCount { get; set; }
    public int SalesCount { get; set; }

    // ── Inventory ────────────────────────────────────────────────────────
    public int TotalProducts { get; set; }
    public int TotalIngredients { get; set; }
    public decimal TotalProductInventoryValue { get; set; }
    public decimal TotalIngredientInventoryValue { get; set; }
    public List<LowStockItem> LowStockWarnings { get; set; } = new();
    public List<LowStockItem> HighStockWarnings { get; set; } = new();

    // ── Production / MRP ─────────────────────────────────────────────────
    public int PendingProductionRequests { get; set; }
    public int InProgressProductionRequests { get; set; }
    public int OpenProductionRequests { get; set; }

    // ── Sales ────────────────────────────────────────────────────────────
    public int TodayOrders { get; set; }
    public int MonthOrders { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public decimal TotalWipQuantity { get; set; }
    public int PendingApprovalOrders { get; set; }
    public decimal TodayRevenue { get; set; }

    // ── Chart data (JSON strings for Chart.js) ───────────────────────────
    public string RoleLabels { get; set; } = "[]";
    public string RoleValues { get; set; } = "[]";

    // ── Recent activity feed ─────────────────────────────────────────────
    public List<RecentUserItem> RecentUsers { get; set; } = new();

    // ── Recent requests with UserAccount FK (Eager loaded via .Include) ──
    public List<RecentProductionRequestItem> RecentProductionRequests { get; set; } = new();
}

public class RecentUserItem
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RecentProductionRequestItem
{
    public int RequestId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string RequestedByUsername { get; set; } = string.Empty;
    public string RequestedByFullName { get; set; } = string.Empty;
}
