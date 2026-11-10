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
    [Area("Sales")]
    [Route("Sales/TransactionHistory")]
    public class TransactionHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public TransactionHistoryController(ApplicationDbContext context, IMemoryCache cache)
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
        // GET: /Sales/TransactionHistory
        // =====================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? keyword)
        {
            try
            {
                // ĐÃ XÓA KHỐI CODE TỰ ĐỘNG ÉP COMPLETED Ở ĐÂY.
                // Chỉ lấy đúng những đơn thực sự đã hoàn tất (Completed / Đã xong) trong CSDL

                var completedOrders = await _context.SaleOrders
                    .AsNoTracking()
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .Include(o => o.ProductExportReceipts)
                    .Where(o => o.Status == "Completed"
                             || o.Status == "Đã xong" 
                             || o.Status == "Đã hoàn thành" 
                             || o.Status == "Đã soạn xong"
                             || o.ProductExportReceipts.Any(r => 
                                 r.Status == "Completed" ||
                                 r.Status == "Exported" || 
                                 r.Status == "Approved" || 
                                 r.Status == "Tán thành" || 
                                 r.Status == "Đã xuất kho" || 
                                 r.Status == "Đã hoàn thành"))
                    .OrderByDescending(o => o.Orderdate)
                    .ToListAsync();

                var list = new List<TransactionItemDto>();

                foreach (var ord in completedOrders)
                {
                    int totalQty = ord.SaleOrderDetails?.Sum(d => d.Quantity) ?? 0;
                    var prodNames = ord.SaleOrderDetails?
                        .Select(d => $"{d.Product?.Productname ?? "Thuốc"} (x{d.Quantity})")
                        .ToList() ?? new List<string>();

                    // Ưu tiên lấy phiếu hợp lệ gần nhất (không lấy phiếu Rejected làm đại diện nếu có phiếu khác)
                    var latestReceipt = ord.ProductExportReceipts?
                        .Where(r => r.Status != "Rejected" && r.Status != "Từ chối")
                        .OrderByDescending(r => r.Createddate)
                        .FirstOrDefault() ?? ord.ProductExportReceipts?.OrderByDescending(r => r.Createddate).FirstOrDefault();

                    string receiptCode = latestReceipt?.Expreceiptcode ?? "Chưa lập phiếu";

                    list.Add(new TransactionItemDto
                    {
                        Id = ord.Ordercode ?? ("DH-" + ord.Orderid),
                        OrderId = ord.Orderid,
                        Type = "Xuất bán thành phẩm",
                        Date = ord.Orderdate.HasValue ? ord.Orderdate.Value.ToString("dd/MM/yyyy HH:mm") : "—",
                        RawDate = ord.Orderdate ?? DateTime.MinValue,
                        ReceiptCode = receiptCode,
                        Partner = ord.Customername ?? "Khách hàng",
                        PartnerPhone = ord.Customerphone ?? "—",
                        PartnerAddress = ord.Shippingaddress ?? "Nhận tại kho trung tâm",
                        ItemCount = ord.SaleOrderDetails?.Count ?? 0,
                        TotalQuantity = totalQty,
                        TotalAmount = ord.Totalamount ?? 0,
                        Status = "Đã xong",
                        ProductSummary = string.Join(", ", prodNames),
                        Products = ord.SaleOrderDetails?.Select(d => new TransactionProductDto
                        {
                            Name = d.Product?.Productname ?? "Thuốc",
                            Unit = d.Product?.Unit ?? "Hộp",
                            Quantity = d.Quantity,
                            Price = d.Unitprice ?? 0,
                            Total = (d.Unitprice ?? 0) * d.Quantity
                        }).ToList() ?? new List<TransactionProductDto>()
                    });
                }

                // Sắp xếp ngày mới nhất lên đầu
                list = list.OrderByDescending(x => x.RawDate).ToList();

                // Lọc tìm kiếm
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    list = list.Where(x => x.Id.ToLower().Contains(kw) 
                                        || x.ReceiptCode.ToLower().Contains(kw)
                                        || x.Partner.ToLower().Contains(kw)
                                        || x.ProductSummary.ToLower().Contains(kw)).ToList();
                }

                // Thống kê tổng kết
                decimal totalRevenue = list.Sum(o => o.TotalAmount);
                int totalExportReceipts = completedOrders
                    .SelectMany(o => o.ProductExportReceipts ?? new List<ProductExportReceipt>())
                    .Count(r => r.Status == "Completed" || r.Status == "Exported" || r.Status == "Approved" || r.Status == "Tán thành" || r.Status == "Đã xuất kho" || r.Status == "Đã hoàn thành");

                ViewBag.TotalDoneOrders = list.Count;
                ViewBag.TotalExportReceipts = totalExportReceipts > 0 ? totalExportReceipts : list.Count;
                ViewBag.TotalRevenue = totalRevenue;

                return View("~/Areas/Sales/Views/TransactionHistory/TransactionHistory.cshtml", list);
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Content($"[LỖI TẢI LỊCH SỬ ĐƠN HÀNG]: {inner}");
            }
        }
    }

    public class TransactionItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public DateTime RawDate { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public string Partner { get; set; } = string.Empty;
        public string PartnerPhone { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ProductSummary { get; set; } = string.Empty;
        public List<TransactionProductDto> Products { get; set; } = new List<TransactionProductDto>();
    }

    public class TransactionProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}