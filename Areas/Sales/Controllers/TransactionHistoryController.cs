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
    public class TransactionProductItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "Hộp";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    public class TransactionItemDto
    {
        public int OrderId { get; set; }
        public string Id { get; set; } = string.Empty;               // Mã đơn hàng (SO-...)
        public string ReceiptCode { get; set; } = string.Empty;      // Mã phiếu xuất kho (PXTP...)
        public string Type { get; set; } = "export";
        public string TypeLabel { get; set; } = "Đơn hàng đã hoàn tất";
        public string Partner { get; set; } = string.Empty;          // Tên khách hàng / Nhà thuốc
        public string PartnerPhone { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public DateTime RawDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Status { get; set; } = "completed";
        public string StatusLabel { get; set; } = "Đã xong";
        public string ProductSummary { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public List<TransactionProductItemDto> Products { get; set; } = new();
    }

    [Area("Sales")]
    [Authorize(Roles = "Sales,BanHang")]
    [IgnoreAntiforgeryToken]
    [Route("Sales/TransactionHistory")]
    public class TransactionHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? keyword)
        {
            var transactions = new List<TransactionItemDto>();

            try
            {
                // 1. Nạp danh sách phiếu xuất kho thành phẩm
                var exportReceipts = await _context.ProductExportReceipts
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedDate)
                    .ToListAsync();

                // Map phiếu xuất theo Orderid để tra cứu nhanh
                var receiptByOrderDict = exportReceipts
                    .Where(r => r.OrderId.HasValue)
                    .GroupBy(r => r.OrderId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                // 2. Nạp danh sách đơn hàng kèm chi tiết sản phẩm
                var allOrders = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.OrderId)
                    .ToListAsync();

                // 3. Lọc lấy các đơn hàng ĐÃ XONG (Completed hoặc kho đã xuất hàng)
                foreach (var order in allOrders)
                {
                    receiptByOrderDict.TryGetValue(order.OrderId, out var linkedReceipt);

                    bool isOrderDone = string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                                    || order.Status == "Đã xong"
                                    || order.Status == "Đã hoàn thành";

                    bool isReceiptExported = linkedReceipt != null && (
                        string.Equals(linkedReceipt.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(linkedReceipt.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(linkedReceipt.Status, "Exported", StringComparison.OrdinalIgnoreCase) ||
                        linkedReceipt.Status == "Đã xuất kho" ||
                        linkedReceipt.Status == "Đã hoàn thành"
                    );

                    // Chỉ đưa vào Lịch sử các đơn hàng ĐÃ XONG
                    if (!isOrderDone && !isReceiptExported)
                    {
                        continue;
                    }

                    var productItems = order.SaleOrderDetails.Select(d => new TransactionProductItemDto
                    {
                        Name = d.Product?.ProductName ?? $"Thuốc #{d.ProductId}",
                        Unit = d.Product?.Unit ?? "Hộp",
                        Quantity = d.Quantity,
                        Price = d.UnitPrice ?? 0,
                        Total = (d.UnitPrice ?? 0) * d.Quantity
                    }).ToList();

                    decimal totalAmt = order.TotalAmount ?? productItems.Sum(x => x.Total);
                    int totalQty = productItems.Sum(x => x.Quantity);

                    string summary = productItems.Any()
                        ? string.Join(", ", productItems.Select(p => $"{p.Name} (x{p.Quantity})"))
                        : "Đơn hàng thành phẩm";

                    string orderCode = !string.IsNullOrWhiteSpace(order.OrderCode) ? order.OrderCode : $"DH-{order.OrderId:D4}";
                    string receiptCode = linkedReceipt?.ExpReceiptCode ?? "Chưa lập phiếu";
                    DateTime displayDate = linkedReceipt?.CreatedDate ?? order.OrderDate ?? DateTime.Now;

                    transactions.Add(new TransactionItemDto
                    {
                        OrderId = order.OrderId,
                        Id = orderCode,
                        ReceiptCode = receiptCode,
                        Type = "export",
                        TypeLabel = "Đơn hàng hoàn tất",
                        Partner = !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : "Khách hàng / Nhà thuốc",
                        PartnerPhone = !string.IsNullOrWhiteSpace(order.CustomerPhone) ? order.CustomerPhone : "—",
                        PartnerAddress = !string.IsNullOrWhiteSpace(order.ShippingAddress) ? order.ShippingAddress : "—",
                        Date = displayDate.ToString("dd/MM/yyyy HH:mm"),
                        RawDate = displayDate,
                        CreatedBy = order.CreatedBy.HasValue ? $"NV #{order.CreatedBy}" : "Bộ phận Bán hàng",
                        Status = "completed",
                        StatusLabel = "Đã xong",
                        ProductSummary = summary,
                        TotalQuantity = totalQty,
                        TotalAmount = totalAmt,
                        Products = productItems
                    });
                }

                // Nếu có phiếu xuất kho độc lập (không gắn Orderid) mà đã hoàn tất thì bổ sung thêm
                var standaloneReceipts = exportReceipts.Where(r => !r.OrderId.HasValue && (
                    string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.Status, "Exported", StringComparison.OrdinalIgnoreCase) ||
                    r.Status == "Đã xuất kho" ||
                    r.Status == "Đã hoàn thành"
                )).ToList();

                foreach (var slip in standaloneReceipts)
                {
                    var items = slip.ProductExportReceiptDetails.Select(d => new TransactionProductItemDto
                    {
                        Name = d.Product?.ProductName ?? $"Thuốc #{d.ProductId}",
                        Unit = d.Product?.Unit ?? "Hộp",
                        Quantity = d.Quantity,
                        Price = d.Price ?? 0,
                        Total = (d.Price ?? 0) * d.Quantity
                    }).ToList();

                    transactions.Add(new TransactionItemDto
                    {
                        OrderId = 0,
                        Id = slip.ExpReceiptCode ?? $"PX-{slip.ExportReceiptId:D4}",
                        ReceiptCode = slip.ExpReceiptCode ?? $"PX-{slip.ExportReceiptId:D4}",
                        Type = "export",
                        TypeLabel = "Phiếu xuất trực tiếp",
                        Partner = "Xuất kho trực tiếp",
                        PartnerPhone = "—",
                        PartnerAddress = "Kho Dược phẩm",
                        Date = slip.CreatedDate?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                        RawDate = slip.CreatedDate ?? DateTime.MinValue,
                        CreatedBy = slip.RequestedBy.HasValue ? $"NV #{slip.RequestedBy}" : "Nhân viên kho",
                        Status = "completed",
                        StatusLabel = "Đã xong",
                        ProductSummary = items.Any() ? string.Join(", ", items.Select(i => $"{i.Name} (x{i.Quantity})")) : "Xuất thành phẩm",
                        TotalQuantity = items.Sum(i => i.Quantity),
                        TotalAmount = items.Sum(i => i.Total),
                        Products = items
                    });
                }
            }
            catch
            {
                // Giữ trang hoạt động ổn định nếu có lỗi kết nối tạm thời
            }

            // Lọc theo từ khóa tìm kiếm nếu người dùng nhập
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                transactions = transactions.Where(t =>
                    t.Id.ToLower().Contains(kw) ||
                    t.ReceiptCode.ToLower().Contains(kw) ||
                    t.Partner.ToLower().Contains(kw) ||
                    t.ProductSummary.ToLower().Contains(kw)
                ).ToList();
            }

            var sortedList = transactions.OrderByDescending(t => t.RawDate).ToList();

            ViewBag.TotalDoneOrders = sortedList.Count;
            ViewBag.TotalExportReceipts = sortedList.Count(x => x.ReceiptCode != "Chưa lập phiếu");
            ViewBag.TotalRevenue = sortedList.Sum(x => x.TotalAmount);

            return View("TransactionHistory", sortedList);
        }
    }
}
