using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using httt.Data;
using httt.Models;

namespace httt.Areas.Sales.Controllers
{
    // =====================================================================
    // DTO MODELS (ĐẶT ĐÚNG NAMESPACE ĐỂ VIEW VÀ CONTROLLER ĐỀU DÙNG ĐƯỢC)
    // =====================================================================
    public class TopProductReportDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal UnitSellingPrice { get; set; }
        public decimal UnitBomCost { get; set; }
        public decimal ProfitPerUnit { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public bool HasBom { get; set; }
        public decimal MarginPercent { get; set; }
        public List<BomIngredientItemDto> Ingredients { get; set; } = new List<BomIngredientItemDto>();
    }

    public class BomIngredientItemDto
    {
        public int IngrId { get; set; }
        public string IngrName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    [Area("Sales")]
    [Route("Sales/RevenueReport")]
    public class RevenueReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public RevenueReportController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private void InvalidateOrderCaches()
        {
            _cache.Remove("ALL_ORDERS_CACHE");
            _cache.Remove("ALL_PRODUCTS_CACHE");
            _cache.Remove("TRANSACTION_HISTORY_CACHE");
            _cache.Remove("TRANSACTION_HISTORY_ORDERS");
            _cache.Remove("EXPORT_SLIPS_CACHE");
            _cache.Remove("REVENUE_REPORT_CACHE");
        }

        // =====================================================================
        // GET: /Sales/RevenueReport
        // =====================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. TÍNH CHI TIẾT NGUYÊN LIỆU BOM (public.bom + public.ingredient)
                var bomCostDict = new Dictionary<int, decimal>();
                var bomIngredientsDict = new Dictionary<int, List<BomIngredientItemDto>>();

                try
                {
                    using var command = _context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = @"
                        SELECT b.productid,
                               b.ingrid,
                               COALESCE(i.ingrname, 'Nguyên liệu #' || b.ingrid) AS ingr_name,
                               COALESCE(b.materialquantity, 0) AS qty,
                               COALESCE(i.unit, 'kg') AS unit,
                               COALESCE(i.price, 0) AS unit_price,
                               (COALESCE(b.materialquantity, 0) * COALESCE(i.price, 0)) AS sub_total
                        FROM public.bom b
                        INNER JOIN public.ingredient i ON b.ingrid = i.ingrid
                        ORDER BY b.productid, b.bomid;
                    ";

                    if (command.Connection?.State != System.Data.ConnectionState.Open)
                    {
                        await _context.Database.OpenConnectionAsync();
                    }

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int pId = reader.GetInt32(0);
                        int ingrId = reader.GetInt32(1);
                        string ingrName = reader.GetString(2);
                        decimal qty = reader.GetDecimal(3);
                        string unit = reader.GetString(4);
                        decimal unitPrice = reader.GetDecimal(5);
                        decimal subTotal = reader.GetDecimal(6);

                        if (!bomCostDict.ContainsKey(pId))
                        {
                            bomCostDict[pId] = 0;
                            bomIngredientsDict[pId] = new List<BomIngredientItemDto>();
                        }

                        bomCostDict[pId] += subTotal;
                        bomIngredientsDict[pId].Add(new BomIngredientItemDto
                        {
                            IngrId = ingrId,
                            IngrName = ingrName,
                            Quantity = qty,
                            Unit = unit,
                            UnitPrice = unitPrice,
                            SubTotal = subTotal
                        });
                    }
                }
                catch
                {
                    // Bỏ qua nếu bảng bom chưa cấu hình
                }

                // 2. LẤY TẤT CẢ SẢN PHẨM TRỰC TIẾP TỪ BẢNG PRODUCT (ĐỌC CẢ COST LẪN MATERIALCOST)
                var allProducts = await _context.Products.AsNoTracking().ToListAsync();

                // 3. LẤY DANH SÁCH CHI TIẾT ĐƠN HÀNG ĐÃ HOÀN TẤT
                var completedOrderIds = await _context.SaleOrders
                    .AsNoTracking()
                    .Where(o => o.Status == "Completed" 
                             || o.Status == "Đã hoàn thành" 
                             || o.Status == "Đã xong"
                             || o.ProductExportReceipts.Any(r => 
                                 r.Status == "Completed" || 
                                 r.Status == "Exported" || 
                                 r.Status == "Approved" || 
                                 r.Status == "Đã xuất kho"))
                    .Select(o => o.Orderid)
                    .ToListAsync();

                // DÙNG SELECT CỤ THỂ ĐỂ TRÁNH TRUY VẤN VÀO CỘT orderdetailid KHÔNG TỒN TẠI
                var completedDetails = await _context.SaleOrderDetails
                    .AsNoTracking()
                    .Where(d => completedOrderIds.Contains(d.Orderid))
                    .Select(d => new 
                    {
                        d.Orderid,
                        d.Productid,
                        d.Quantity,
                        Unitprice = d.Unitprice ?? 0m
                    })
                    .ToListAsync();

                // 4. TỔNG HỢP THEO TỪNG THÀNH PHẨM DỰA TRÊN CỘT MATERIALCOST
                var productReports = allProducts.Select(prod =>
                {
                    int pId = prod.Productid;

                    var soldItems = completedDetails.Where(d => d.Productid == pId).ToList();
                    int totalSoldQty = soldItems.Sum(x => x.Quantity);
                    decimal totalSales = soldItems.Sum(x => x.Quantity * x.Unitprice);

                    decimal unitSellingPrice = 0;
                    if (totalSoldQty > 0)
                    {
                        unitSellingPrice = totalSales / totalSoldQty;
                    }
                    else if (prod.Cost.HasValue && prod.Cost.Value > 0)
                    {
                        unitSellingPrice = prod.Cost.Value;
                    }

                    // Ưu tiên đọc cột materialcost trong bảng Product
                    decimal unitMaterialCost = 0;
                    bool hasMaterialCost = false;

                    if (prod.Materialcost.HasValue && prod.Materialcost.Value > 0)
                    {
                        unitMaterialCost = prod.Materialcost.Value;
                        hasMaterialCost = true;
                    }
                    else if (bomCostDict.TryGetValue(pId, out decimal bCost) && bCost > 0)
                    {
                        unitMaterialCost = bCost;
                        hasMaterialCost = true;
                    }

                    decimal totalCostForProd = unitMaterialCost * totalSoldQty;
                    decimal totalProfitForProd = totalSales - totalCostForProd;
                    decimal profitPerUnit = unitSellingPrice > 0 ? (unitSellingPrice - unitMaterialCost) : 0;

                    return new TopProductReportDto
                    {
                        ProductId = pId,
                        ProductName = prod.Productname,
                        Unit = prod.Unit ?? "Hộp",
                        TotalQuantity = totalSoldQty,
                        UnitSellingPrice = unitSellingPrice,
                        UnitBomCost = unitMaterialCost,
                        ProfitPerUnit = profitPerUnit,
                        TotalSales = totalSales,
                        TotalCost = totalCostForProd,
                        TotalProfit = totalProfitForProd,
                        HasBom = hasMaterialCost || (bomIngredientsDict.ContainsKey(pId) && bomIngredientsDict[pId].Any()),
                        MarginPercent = totalSales > 0 ? Math.Round((totalProfitForProd / totalSales) * 100, 1) : 0,
                        Ingredients = bomIngredientsDict.ContainsKey(pId) ? bomIngredientsDict[pId] : new List<BomIngredientItemDto>()
                    };
                })
                .Where(p => p.HasBom || p.TotalQuantity > 0 || p.UnitSellingPrice > 0)
                .OrderByDescending(p => p.TotalProfit)
                .ThenByDescending(p => p.TotalQuantity)
                .ToList();

                // 5. TÍNH CHỈ SỐ THỐNG KÊ (Ép kiểu cụ thể)
                decimal realRevenue = productReports.Sum(p => p.TotalSales);
                decimal realCost = productReports.Sum(p => p.TotalCost);
                decimal realProfit = productReports.Sum(p => p.TotalProfit);

                ViewBag.TotalOrders = (int)completedOrderIds.Count;
                ViewBag.CompletedOrders = (int)completedOrderIds.Count;
                ViewBag.RealRevenue = (decimal)realRevenue;
                ViewBag.RealCost = (decimal)realCost;
                ViewBag.RealProfit = (decimal)realProfit;
                ViewBag.ProductReports = productReports;

                return View("~/Areas/Sales/Views/RevenueReport/Index.cshtml", new List<SaleOrder>());
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Content($"[LỖI TÍNH TOÁN BÁO CÁO LỢI NHUẬN]: {inner}");
            }
        }
    }
}