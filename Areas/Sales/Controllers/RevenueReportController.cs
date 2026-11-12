using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using httt.Constants;
using httt.Data;
using httt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace httt.Areas.Sales.Controllers
{
    public class BomItemDetailDto
    {
        public int IngrId { get; set; }
        public string IngrName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class TopProductReportDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int TotalQuantity { get; set; }

        public decimal UnitSellingPrice { get; set; }     // Giá bán 1 ĐV (product.cost)
        public decimal UnitBomCost { get; set; }          // Giá vốn NVL 1 ĐV (product.materialcost)
        public decimal ProfitPerUnit { get; set; }        // Tiền lời / 1 ĐV = Giá bán - Giá vốn NVL

        public decimal TotalSales { get; set; }           // Tổng doanh thu = SL xuất * Giá bán
        public decimal TotalCost { get; set; }            // Tổng giá vốn NVL = SL xuất * materialcost
        public decimal TotalProfit { get; set; }          // Tổng tiền lời = Tổng doanh thu - Tổng giá vốn
        public decimal MarginPercent { get; set; }        // Tỷ suất lợi nhuận (%)
        public bool HasBom { get; set; }
        public List<BomItemDetailDto> Ingredients { get; set; } = new();
    }

    public class RawBomIngredientRow
    {
        public int ProductId { get; set; }
        public int IngrId { get; set; }
        public string? IngrName { get; set; }
        public string? Unit { get; set; }
        public decimal BomQty { get; set; }
        public decimal UnitCost { get; set; }
    }

    [Area("Sales")]
    [Authorize(Roles = "Sales,BanHang")]
    [IgnoreAntiforgeryToken]
    [Route("Sales/RevenueReport")]
    public class RevenueReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RevenueReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Nạp danh mục sản phẩm (Chứa giá bán: cost, giá vốn NVL: materialcost)
                var allProducts = await _context.Products
                    .AsNoTracking()
                    .OrderBy(p => p.ProductId)
                    .ToListAsync();

                // 2. Nạp chi tiết công thức định mức BOM từ CSDL
                var bomDetailsByProduct = new Dictionary<int, List<BomItemDetailDto>>();
                try
                {
                    var sqlBom = @"
                        SELECT 
                            b.productid AS ""ProductId"",
                            b.ingrid AS ""IngrId"",
                            COALESCE(i.ingrname, 'Nguyên liệu #' || b.ingrid::text) AS ""IngrName"",
                            COALESCE(i.unit, 'ĐV') AS ""Unit"",
                            CAST(COALESCE(b.materialquantity, 0) / NULLIF(COALESCE(b.itemquantity, 1), 0) AS numeric(18,4)) AS ""BomQty"",
                            CAST(COALESCE(
                                (SELECT AVG(d.cost) FROM public.importreceiptdetail d WHERE d.ingrid = b.ingrid AND d.cost > 0),
                                i.cost,
                                0
                            ) AS numeric(18,2)) AS ""UnitCost""
                        FROM public.bom b
                        LEFT JOIN public.ingredient i ON b.ingrid = i.ingrid
                        WHERE b.productid IS NOT NULL";

                    var rawRows = await _context.Database.SqlQueryRaw<RawBomIngredientRow>(sqlBom).ToListAsync();

                    foreach (var row in rawRows)
                    {
                        if (!bomDetailsByProduct.ContainsKey(row.ProductId))
                        {
                            bomDetailsByProduct[row.ProductId] = new List<BomItemDetailDto>();
                        }

                        bomDetailsByProduct[row.ProductId].Add(new BomItemDetailDto
                        {
                            IngrId = row.IngrId,
                            IngrName = row.IngrName ?? $"NVL #{row.IngrId}",
                            Unit = row.Unit ?? "ĐV",
                            Quantity = row.BomQty,
                            UnitPrice = row.UnitCost,
                            SubTotal = Math.Round(row.BomQty * row.UnitCost, 0)
                        });
                    }
                }
                catch
                {
                    // Dự phòng nếu bảng BOM trống, hệ thống sử dụng product.materialcost
                }

                // 3. Nạp danh sách đơn hàng
                var allOrders = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                // ĐIỀU KIỆN CHẶT CHẼ:
                // Chỉ những đơn có trạng thái "Completed" (hoặc "Đã xong", "Đã hoàn thành") mới được ghi nhận vào doanh thu.
                // Nếu đơn vẫn ở trạng thái khác (Pending, Holding, PendingApproval, Cancelled) thì TUYỆT ĐỐI KHÔNG tính,
                // kể cả khi phiếu xuất kho ở trạng thái đã xuất vì đơn hàng vẫn có thể bị hủy hoặc chưa bàn giao xong.
                var strictlyCompletedOrders = allOrders.Where(o =>
                    !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                    o.Status != "Đã hủy" &&
                    (string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                     o.Status == "Đã xong" ||
                     o.Status == "Đã hoàn thành")
                ).ToList();

                // 4. Tổng hợp số lượng và doanh thu bán theo từng mã sản phẩm
                var soldItemsLookup = strictlyCompletedOrders
                    .SelectMany(o => o.SaleOrderDetails)
                    .GroupBy(d => d.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            SoldQty = g.Sum(x => x.Quantity),
                            Revenue = g.Sum(x => (x.UnitPrice ?? x.Product?.Cost ?? 0m) * x.Quantity)
                        }
                    );

                // 5. Tính toán chỉ số tài chính từng sản phẩm
                var productReports = new List<TopProductReportDto>();
                decimal grandTotalRevenue = 0m;
                decimal grandTotalCost = 0m;

                foreach (var prod in allProducts)
                {
                    var ingredients = bomDetailsByProduct.ContainsKey(prod.ProductId)
                        ? bomDetailsByProduct[prod.ProductId]
                        : new List<BomItemDetailDto>();

                    decimal unitSellingPrice = prod.Cost ?? 0m;
                    decimal unitMaterialCost = (prod.MaterialCost.HasValue && prod.MaterialCost.Value > 0)
                        ? prod.MaterialCost.Value
                        : (ingredients.Any() ? ingredients.Sum(i => i.SubTotal) : 0m);

                    int soldQty = 0;
                    decimal totalSales = 0m;

                    if (soldItemsLookup.TryGetValue(prod.ProductId, out var soldData))
                    {
                        soldQty = soldData.SoldQty;
                        totalSales = soldData.Revenue > 0 ? soldData.Revenue : (soldQty * unitSellingPrice);
                        if (soldQty > 0 && unitSellingPrice == 0)
                        {
                            unitSellingPrice = Math.Round(totalSales / soldQty, 0);
                        }
                    }

                    decimal totalCost = soldQty * unitMaterialCost;
                    decimal totalProfit = totalSales - totalCost;
                    decimal profitPerUnit = unitSellingPrice - unitMaterialCost;
                    decimal marginPercent = totalSales > 0
                        ? Math.Round((totalProfit / totalSales) * 100, 1)
                        : (unitSellingPrice > 0 ? Math.Round((profitPerUnit / unitSellingPrice) * 100, 1) : 0);

                    grandTotalRevenue += totalSales;
                    grandTotalCost += totalCost;

                    productReports.Add(new TopProductReportDto
                    {
                        ProductId = prod.ProductId,
                        ProductName = !string.IsNullOrWhiteSpace(prod.ProductName) ? prod.ProductName : $"Sản phẩm #{prod.ProductId}",
                        Unit = !string.IsNullOrWhiteSpace(prod.Unit) ? prod.Unit : "Hộp",
                        CurrentStock = prod.Quantity ?? 0,
                        TotalQuantity = soldQty,
                        UnitSellingPrice = unitSellingPrice,
                        UnitBomCost = unitMaterialCost,
                        ProfitPerUnit = profitPerUnit,
                        TotalSales = totalSales,
                        TotalCost = totalCost,
                        TotalProfit = totalProfit,
                        MarginPercent = marginPercent,
                        HasBom = unitMaterialCost > 0 || ingredients.Any(),
                        Ingredients = ingredients
                    });
                }

                productReports = productReports
                    .OrderByDescending(p => p.TotalSales)
                    .ThenByDescending(p => p.TotalProfit)
                    .ThenBy(p => p.ProductId)
                    .ToList();

                decimal grandTotalProfit = grandTotalRevenue - grandTotalCost;

                ViewBag.TotalOrders = allOrders.Count;
                ViewBag.CompletedOrders = strictlyCompletedOrders.Count;
                ViewBag.ProcessingOrders = allOrders.Count - strictlyCompletedOrders.Count;

                ViewBag.RealRevenue = grandTotalRevenue;
                ViewBag.RealCost = grandTotalCost;
                ViewBag.RealProfit = grandTotalProfit;
                ViewBag.ProductReports = productReports;

                return View("Index", strictlyCompletedOrders);
            }
            catch
            {
                ViewBag.TotalOrders = 0;
                ViewBag.CompletedOrders = 0;
                ViewBag.ProcessingOrders = 0;
                ViewBag.RealRevenue = 0m;
                ViewBag.RealCost = 0m;
                ViewBag.RealProfit = 0m;
                ViewBag.ProductReports = new List<TopProductReportDto>();
                return View("Index", new List<SaleOrder>());
            }
        }
    }
}