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
    [Area("Sales")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Sales}")]
    [IgnoreAntiforgeryToken]
    [Route("Sales/CreateOrder")]
    public class CreateOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CreateOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? orderId)
        {
            try
            {
                // 1. Nạp toàn bộ đơn hàng (bao gồm cả đơn đã hủy để tra cứu thông tin khi bấm từ danh sách phiếu cũ)
                var allOrdersWithCancelled = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.OrderId)
                    .AsNoTracking()
                    .ToListAsync();

                // Danh sách hiển thị trên dropdown: Không lấy các đơn đã bị hủy
                var activeOrders = allOrdersWithCancelled
                    .Where(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                                o.Status != "Đã hủy")
                    .ToList();

                // Nạp đơn hàng được chọn (có thể là đơn đang hoạt động hoặc đơn đã hủy nếu người dùng bấm vào link từ bảng phiếu cũ)
                var selectedOrder = orderId.HasValue
                    ? allOrdersWithCancelled.FirstOrDefault(o => o.OrderId == orderId.Value)
                    : activeOrders.FirstOrDefault();

                // 2. Nạp danh sách TẤT CẢ phiếu xuất thành phẩm (Không xóa phiếu của đơn bị hủy, chỉ đánh dấu không hiệu lực trên giao diện)
                var exportReceipts = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .OrderByDescending(r => r.CreatedDate)
                    .ThenByDescending(r => r.ExportReceiptId)
                    .AsNoTracking()
                    .ToListAsync();

                // Bảng tra cứu Mã đơn, Tên khách và Trạng thái đơn hàng gốc cho từng phiếu xuất
                var orderLookup = allOrdersWithCancelled.ToDictionary(
                    o => o.OrderId,
                    o => (
                        Code: !string.IsNullOrWhiteSpace(o.OrderCode) ? o.OrderCode : $"DH-{o.OrderId}",
                        Customer: !string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName : "Khách hàng",
                        Status: o.Status ?? "Pending"
                    )
                );

                // Lấy phiếu xuất mới nhất của đơn đang chọn
                var existingReceipt = selectedOrder != null
                    ? exportReceipts.FirstOrDefault(r => r.OrderId == selectedOrder.OrderId)
                    : null;

                ViewBag.SelectedOrder = selectedOrder;
                ViewBag.ExportReceipts = exportReceipts;
                ViewBag.OrderLookupFull = orderLookup;
                // Giữ tương thích với ViewBag.OrderLookup dạng (Code, Customer)
                ViewBag.OrderLookup = allOrdersWithCancelled.ToDictionary(
                    o => o.OrderId,
                    o => (
                        Code: !string.IsNullOrWhiteSpace(o.OrderCode) ? o.OrderCode : $"DH-{o.OrderId}",
                        Customer: !string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName : "Khách hàng"
                    )
                );
                ViewBag.ExistingReceipt = existingReceipt;

                return View("CreateOrder", activeOrders);
            }
            catch
            {
                ViewBag.SelectedOrder = null;
                ViewBag.ExportReceipts = new List<ProductExportReceipt>();
                ViewBag.OrderLookup = new Dictionary<int, (string Code, string Customer)>();
                ViewBag.OrderLookupFull = new Dictionary<int, (string Code, string Customer, string Status)>();
                ViewBag.ExistingReceipt = null;
                return View("CreateOrder", new List<SaleOrder>());
            }
        }

        // API: Lập phiếu yêu cầu xuất kho (Và tạo lại phiếu mới nếu phiếu trước đó bị Kho từ chối)
        [HttpPost("RequestExportSlip")]
        public async Task<IActionResult> RequestExportSlip([FromQuery] int orderId)
        {
            try
            {
                var order = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng cần lập phiếu xuất!" });
                }

                // Chặn lập phiếu nếu đơn hàng đã bị hủy
                if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) || order.Status == "Đã hủy")
                {
                    return Json(new { success = false, message = $"Đơn hàng {order.OrderCode ?? ("DH-" + order.OrderId)} đã bị hủy, không thể lập phiếu xuất kho!" });
                }

                // Kiểm tra phiếu xuất gần nhất của đơn này
                var latestReceipt = await _context.ProductExportReceipts
                    .Where(r => r.OrderId == orderId)
                    .OrderByDescending(r => r.CreatedDate)
                    .ThenByDescending(r => r.ExportReceiptId)
                    .FirstOrDefaultAsync();

                // Nếu đã có phiếu ĐANG CHỜ DUYỆT hoặc ĐÃ XUẤT KHO (không phải bị từ chối/đã hủy) thì chặn gửi trùng
                if (latestReceipt != null)
                {
                    bool isRejected = string.Equals(latestReceipt.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                                   || latestReceipt.Status == "Từ chối";
                    bool isCancelled = string.Equals(latestReceipt.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                                    || latestReceipt.Status == "Đã hủy";

                    if (!isRejected && !isCancelled)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Đơn hàng này đang có phiếu xuất ({latestReceipt.ExpReceiptCode}) đang hoạt động rồi!"
                        });
                    }
                }

                // Kiểm tra tồn kho trước khi cho phép gửi phiếu sang Kho
                foreach (var item in order.SaleOrderDetails)
                {
                    int currentStock = item.Product?.Quantity ?? 0;
                    if (item.Quantity > currentStock)
                    {
                        string prodName = item.Product?.ProductName ?? $"SP #{item.ProductId}";
                        return Json(new
                        {
                            success = false,
                            message = $"Không thể lập phiếu xuất: Sản phẩm '{prodName}' thiếu tồn kho (Cần {item.Quantity}, Tồn kho {currentStock})."
                        });
                    }
                }

                var user = await _context.UserAccounts.FirstOrDefaultAsync();
                int? requestedByUserId = user?.UserId ?? order.CreatedBy;

                string receiptCode = $"PXTP{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

                // 1. Tạo bản ghi vào bảng ProductExportReceipt
                var newReceipt = new ProductExportReceipt
                {
                    OrderId = order.OrderId,
                    ExpReceiptCode = receiptCode,
                    RequestedBy = requestedByUserId,
                    ApprovedBy = null,
                    CreatedDate = DateTime.UtcNow,
                    Status = "Chờ xuất kho"
                };

                _context.ProductExportReceipts.Add(newReceipt);
                await _context.SaveChangesAsync();

                // 2. Lưu danh sách thuốc vào bảng ProductExportReceiptDetail
                if (order.SaleOrderDetails != null && order.SaleOrderDetails.Any())
                {
                    foreach (var item in order.SaleOrderDetails)
                    {
                        var receiptDetail = new ProductExportReceiptDetail
                        {
                            ExportReceiptId = newReceipt.ExportReceiptId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.UnitPrice ?? item.Product?.Cost ?? 0,
                            Status = "Chờ xuất kho",
                            Note = $"Xuất theo đơn {order.OrderCode ?? ("DH-" + order.OrderId)}",
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.ProductExportReceiptDetails.Add(receiptDetail);
                    }
                }

                // 3. Chuyển trạng thái đơn hàng sang Chờ xuất kho (PendingApproval)
                order.Status = "PendingApproval";
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    receiptCode = receiptCode,
                    message = $"Đã tạo và gửi phiếu xuất kho {receiptCode} sang bộ phận Kho thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi lưu CSDL: " + (ex.InnerException?.Message ?? ex.Message)
                });
            }
        }

        // API: Duyệt hoàn tất đơn hàng từ trang Phiếu xuất
        [HttpPost("CompleteOrderFromSlip")]
        public async Task<IActionResult> CompleteOrderFromSlip([FromQuery] int orderId)
        {
            try
            {
                var order = await _context.SaleOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) || order.Status == "Đã hủy")
                {
                    return Json(new { success = false, message = "Đơn hàng này đã bị hủy, không thể duyệt hoàn tất!" });
                }

                order.Status = "Completed";

                var receipt = await _context.ProductExportReceipts
                    .Where(r => r.OrderId == orderId)
                    .OrderByDescending(r => r.CreatedDate)
                    .FirstOrDefaultAsync();

                if (receipt != null && receipt.Status != "Rejected" && receipt.Status != "Từ chối")
                {
                    receipt.Status = "Completed";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đơn hàng {order.OrderCode ?? ("DH-" + order.OrderId)} đã được cập nhật thành ĐÃ XONG (Completed)!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // API phụ trợ: Xóa phiếu xuất của 1 đơn để kiểm thử lại
        [HttpPost("ClearReceiptForTest")]
        public async Task<IActionResult> ClearReceiptForTest([FromQuery] int orderId)
        {
            try
            {
                var receipts = await _context.ProductExportReceipts
                    .Where(r => r.OrderId == orderId)
                    .ToListAsync();

                if (receipts.Any())
                {
                    var receiptIds = receipts.Select(r => r.ExportReceiptId).ToList();
                    var details = await _context.ProductExportReceiptDetails
                        .Where(d => receiptIds.Contains(d.ExportReceiptId))
                        .ToListAsync();

                    _context.ProductExportReceiptDetails.RemoveRange(details);
                    _context.ProductExportReceipts.RemoveRange(receipts);
                }

                var order = await _context.SaleOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order != null && order.Status != "Cancelled" && order.Status != "Đã hủy")
                {
                    order.Status = "Pending";
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
