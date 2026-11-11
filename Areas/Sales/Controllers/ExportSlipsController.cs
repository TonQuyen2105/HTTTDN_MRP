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
    [Route("Sales/ExportSlips")]
    public class ExportSlipsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExportSlipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? keyword, string? status, string? date)
        {
            try
            {
                // Nạp tất cả phiếu xuất kho kèm Đơn hàng và Chi tiết mặt hàng
                var allSlips = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedDate)
                    .ThenByDescending(r => r.ExportReceiptId)
                    .ToListAsync();

                // Tách danh sách chưa bị hủy để tính toán thống kê mặc định
                var activeSlips = allSlips.Where(r =>
                    !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                    r.Status != "Đã hủy" &&
                    (r.SaleOrder == null || (
                        !string.Equals(r.SaleOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                        r.SaleOrder.Status != "Đã hủy"
                    ))
                ).ToList();

                ViewBag.AllSlips = activeSlips;

                IEnumerable<ProductExportReceipt> filtered = allSlips;

                // 1. Lọc theo từ khóa: Mã phiếu xuất, Mã đơn hàng, hoặc Tên khách hàng
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    filtered = filtered.Where(r =>
                        (r.ExpReceiptCode != null && r.ExpReceiptCode.ToLower().Contains(kw)) ||
                        ($"px-{r.ExportReceiptId}".Contains(kw)) ||
                        (r.SaleOrder != null && (
                            (r.SaleOrder.OrderCode != null && r.SaleOrder.OrderCode.ToLower().Contains(kw)) ||
                            (r.SaleOrder.CustomerName != null && r.SaleOrder.CustomerName.ToLower().Contains(kw)) ||
                            (r.SaleOrder.CustomerPhone != null && r.SaleOrder.CustomerPhone.ToLower().Contains(kw))
                        ))
                    );
                }

                // 2. Lọc theo ngày tạo phiếu
                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
                {
                    filtered = filtered.Where(r =>
                        r.CreatedDate.HasValue && r.CreatedDate.Value.Date == parsedDate.Date
                    );
                }

                // 3. Lọc theo trạng thái tiến trình của phiếu xuất
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var st = status.Trim().ToLower();
                    if (st == "cancelled")
                    {
                        // Xem các phiếu đã hủy hoặc có đơn hàng gốc bị hủy
                        filtered = filtered.Where(r =>
                            string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                            r.Status == "Đã hủy" ||
                            (r.SaleOrder != null && (
                                string.Equals(r.SaleOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                                r.SaleOrder.Status == "Đã hủy"
                            ))
                        );
                    }
                    else
                    {
                        // Chỉ lọc trên các phiếu còn hiệu lực
                        filtered = filtered.Where(r =>
                            !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                            r.Status != "Đã hủy" &&
                            (r.SaleOrder == null || (
                                !string.Equals(r.SaleOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                                r.SaleOrder.Status != "Đã hủy"
                            ))
                        );

                        if (st == "completed")
                        {
                            // Đã hoàn tất 100%
                            filtered = filtered.Where(r => IsSlipCompleted(r));
                        }
                        else if (st == "ready")
                        {
                            // Kho đã duyệt xuất hàng (Approved / Exported), chờ bên bán bấm xác nhận hoàn tất
                            filtered = filtered.Where(r => IsSlipReadyToComplete(r) && !IsSlipCompleted(r));
                        }
                        else if (st == "rejected")
                        {
                            // Kho từ chối xuất hàng
                            filtered = filtered.Where(r => IsSlipRejected(r));
                        }
                        else if (st == "waiting")
                        {
                            // Phiếu mới tạo, đang chờ bên kho xử lý
                            filtered = filtered.Where(r => !IsSlipCompleted(r) && !IsSlipReadyToComplete(r) && !IsSlipRejected(r));
                        }
                    }
                }
                else
                {
                    // Mặc định: Không hiển thị phiếu của đơn đã hủy để danh sách gọn sạch
                    filtered = filtered.Where(r =>
                        !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                        r.Status != "Đã hủy" &&
                        (r.SaleOrder == null || (
                            !string.Equals(r.SaleOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                            r.SaleOrder.Status != "Đã hủy"
                        ))
                    );
                }

                ViewBag.Keyword = keyword;
                ViewBag.Status = status;
                ViewBag.Date = date;

                return View("Index", filtered.ToList());
            }
            catch
            {
                ViewBag.AllSlips = new List<ProductExportReceipt>();
                return View("Index", new List<ProductExportReceipt>());
            }
        }

        // Helper: Kiểm tra phiếu đã hoàn tất (Completed)
        private static bool IsSlipCompleted(ProductExportReceipt r)
        {
            var s = r.Status?.Trim() ?? "";
            return string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase)
                || s == "Đã hoàn thành"
                || s == "Đã xong";
        }

        // Helper: Kiểm tra kho đã duyệt / xuất hàng (Chờ bên bán xác nhận Đã xong)
        private static bool IsSlipReadyToComplete(ProductExportReceipt r)
        {
            var s = r.Status?.Trim() ?? "";
            return string.Equals(s, "Exported", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Approved", StringComparison.OrdinalIgnoreCase)
                || s == "Tán thành"
                || s == "Đã duyệt"
                || s == "Đã xuất kho";
        }

        // Helper: Kiểm tra phiếu bị kho từ chối
        private static bool IsSlipRejected(ProductExportReceipt r)
        {
            var s = r.Status?.Trim() ?? "";
            return string.Equals(s, "Rejected", StringComparison.OrdinalIgnoreCase)
                || s == "Từ chối";
        }

        // API: Nhân viên bán hàng xác nhận hoàn tất đơn hàng (khi kho đã xuất hàng)
        [HttpPost("ApproveCompletedOrder")]
        public async Task<IActionResult> ApproveCompletedOrder([FromQuery] int receiptId)
        {
            try
            {
                var receipt = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .FirstOrDefaultAsync(r => r.ExportReceiptId == receiptId);

                if (receipt == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu xuất kho!" });
                }

                // Cập nhật trạng thái phiếu xuất sang Completed
                receipt.Status = "Completed";

                // Cập nhật trạng thái đơn hàng gốc sang Completed để đồng bộ toàn hệ thống
                if (receipt.SaleOrder != null)
                {
                    receipt.SaleOrder.Status = "Completed";
                }
                else if (receipt.OrderId.HasValue)
                {
                    var order = await _context.SaleOrders.FirstOrDefaultAsync(o => o.OrderId == receipt.OrderId.Value);
                    if (order != null)
                    {
                        order.Status = "Completed";
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã duyệt hoàn tất phiếu xuất {receipt.ExpReceiptCode ?? ("PX-" + receipt.ExportReceiptId)} và cập nhật đơn hàng thành ĐÃ XONG!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi cập nhật CSDL: " + ex.Message });
            }
        }

        // API: Đánh dấu từ chối phiếu xuất và trả đơn hàng về trạng thái Chờ xử lý (Pending)
        [HttpPost("RejectExportSlip")]
        public async Task<IActionResult> RejectExportSlip([FromQuery] int receiptId)
        {
            try
            {
                var receipt = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .FirstOrDefaultAsync(r => r.ExportReceiptId == receiptId);

                if (receipt == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu xuất kho!" });
                }

                receipt.Status = "Rejected";

                // Khi phiếu xuất bị từ chối, đơn hàng gốc quay về trạng thái Đang chờ xử lý (Pending) để lập lại phiếu mới
                if (receipt.SaleOrder != null && receipt.SaleOrder.Status != "Completed")
                {
                    receipt.SaleOrder.Status = "Pending";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Phiếu xuất {receipt.ExpReceiptCode} đã chuyển sang trạng thái Từ chối. Đơn hàng đã trả về danh sách Chờ xử lý."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
