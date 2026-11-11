using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using httt.Data;
using httt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "Sales,BanHang")]
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
                // Nạp tất cả phiếu xuất kho kèm Đơn hàng và Chi tiết mặt hàng (giữ lại cả đơn bị hủy để đồng bộ với CreateOrder)
                var allSlips = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedDate)
                    .ThenByDescending(r => r.ExportReceiptId)
                    .ToListAsync();

                // Lưu danh sách toàn bộ phiếu vào ViewBag để hiển thị thẻ thống kê
                ViewBag.AllSlips = allSlips;

                IEnumerable<ProductExportReceipt> filtered = allSlips;

                // 1. Lọc theo từ khóa: Mã phiếu, Mã đơn, hoặc Tên khách hàng
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

                // 3. Lọc theo trạng thái ĐỘC LẬP của phiếu xuất (không phụ thuộc vào việc hủy đơn)
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var st = status.Trim().ToLower();
                    if (st == "cancelled")
                    {
                        // Lọc các phiếu có trạng thái phiếu bị hủy HOẶC đơn hàng gốc bị hủy
                        filtered = filtered.Where(r =>
                            string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                            r.Status == "Đã hủy" ||
                            (r.SaleOrder != null && (
                                string.Equals(r.SaleOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                                r.SaleOrder.Status == "Đã hủy"
                            ))
                        );
                    }
                    else if (st == "completed")
                    {
                        filtered = filtered.Where(r => IsSlipCompleted(r));
                    }
                    else if (st == "ready")
                    {
                        filtered = filtered.Where(r => IsSlipReadyToComplete(r) && !IsSlipCompleted(r));
                    }
                    else if (st == "rejected")
                    {
                        filtered = filtered.Where(r => IsSlipRejected(r));
                    }
                    else if (st == "waiting")
                    {
                        filtered = filtered.Where(r => !IsSlipCompleted(r) && !IsSlipReadyToComplete(r) && !IsSlipRejected(r) && !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) && r.Status != "Đã hủy");
                    }
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

        private static bool IsSlipCompleted(ProductExportReceipt r)
        {
            var s = r.Status?.Trim() ?? "";
            return string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase)
                || s == "Đã hoàn thành"
                || s == "Đã xong";
        }

        private static bool IsSlipReadyToComplete(ProductExportReceipt r)
        {
            var s = r.Status?.Trim() ?? "";
            return string.Equals(s, "Exported", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Approved", StringComparison.OrdinalIgnoreCase)
                || s == "Tán thành"
                || s == "Đã duyệt"
                || s == "Đã xuất kho";
        }

        private static bool IsSlipRejected(ProductExportReceipt r)
        {
            var s = r.Status?.Trim() ?? "";
            return string.Equals(s, "Rejected", StringComparison.OrdinalIgnoreCase)
                || s == "Từ chối";
        }

        [HttpPost("ApproveCompletedOrder")]
        public async Task<IActionResult> ApproveCompletedOrder([FromQuery] int receiptId)
        {
            try
            {
                var receipt = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                    .FirstOrDefaultAsync(r => r.ExportReceiptId == receiptId);

                if (receipt == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu xuất kho!" });
                }

                receipt.Status = "Completed";

                // Cập nhật tồn kho sản phẩm
                if (receipt.ProductExportReceiptDetails != null)
                {
                    var productIds = receipt.ProductExportReceiptDetails.Select(d => d.ProductId).Distinct().ToList();
                    var products = await _context.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();

                    foreach (var detail in receipt.ProductExportReceiptDetails)
                    {
                        detail.Status = "Completed";
                        var prod = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                        if (prod != null)
                        {
                            prod.Quantity = Math.Max(0, (prod.Quantity ?? 0) - detail.Quantity);
                        }
                    }
                }

                // Nếu đơn hàng chưa bị hủy thì đồng bộ Completed (Đơn đã hủy thì giữ nguyên Cancelled)
                if (receipt.SaleOrder != null && receipt.SaleOrder.Status != "Cancelled" && receipt.SaleOrder.Status != "Đã hủy")
                {
                    receipt.SaleOrder.Status = "Completed";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã duyệt hoàn tất phiếu xuất {receipt.ExpReceiptCode ?? ("PX-" + receipt.ExportReceiptId)} thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi cập nhật CSDL: " + ex.Message });
            }
        }

        [HttpPost("RejectExportSlip")]
        public async Task<IActionResult> RejectExportSlip([FromQuery] int receiptId)
        {
            try
            {
                var receipt = await _context.ProductExportReceipts
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                    .FirstOrDefaultAsync(r => r.ExportReceiptId == receiptId);

                if (receipt == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu xuất kho!" });
                }

                bool wasCompleted = string.Equals(receipt.Status, "Completed", StringComparison.OrdinalIgnoreCase) || receipt.Status == "Đã xong";
                receipt.Status = "Rejected";

                if (receipt.ProductExportReceiptDetails != null)
                {
                    var productIds = receipt.ProductExportReceiptDetails.Select(d => d.ProductId).Distinct().ToList();
                    var products = await _context.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();

                    foreach (var detail in receipt.ProductExportReceiptDetails)
                    {
                        detail.Status = "Rejected";
                        if (wasCompleted)
                        {
                            var prod = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                            if (prod != null)
                            {
                                prod.Quantity = (prod.Quantity ?? 0) + detail.Quantity;
                            }
                        }
                    }
                }

                if (receipt.SaleOrder != null && receipt.SaleOrder.Status != "Cancelled" && receipt.SaleOrder.Status != "Đã hủy")
                {
                    receipt.SaleOrder.Status = "Pending";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Phiếu xuất {receipt.ExpReceiptCode} đã chuyển sang trạng thái Từ chối."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}