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
    [Route("Sales/ExportSlips")]
    public class ExportSlipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public ExportSlipsController(ApplicationDbContext context, IMemoryCache cache)
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
        // GET: /Sales/ExportSlips (DANH SÁCH & BỘ LỌC THEO STATUS)
        // =====================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? keyword, string? status, string? date)
        {
            try
            {
                ViewBag.Keyword = keyword;
                ViewBag.Status = status;
                ViewBag.Date = date;

                // 1. CHỈ LẤY CÁC PHIẾU CÓ ORDERID KHÁC NULL VÀ CÓ CHI TIẾT SẢN PHẨM
                var query = _context.ProductExportReceipts
                    .AsNoTracking()
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .Where(r => r.Orderid != null && r.ProductExportReceiptDetails.Any())
                    .AsQueryable();

                // 2. LỌC THEO TỪ KHÓA
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(r => (r.Expreceiptcode != null && r.Expreceiptcode.ToLower().Contains(kw))
                                          || (r.SaleOrder != null && r.SaleOrder.Customername != null && r.SaleOrder.Customername.ToLower().Contains(kw))
                                          || (r.SaleOrder != null && r.SaleOrder.Ordercode != null && r.SaleOrder.Ordercode.ToLower().Contains(kw)));
                }

                // 3. LỌC THEO NGÀY LẬP PHIẾU
                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out DateTime parsedDate))
                {
                    query = query.Where(r => r.Createddate.HasValue && r.Createddate.Value.Date == parsedDate.Date);
                }

                // 4. QUY TẮC ĐỐI VỚI ĐƠN HÀNG/PHIẾU ĐÃ HỦY:
                // Nếu người dùng KHÔNG chọn lọc 'cancelled', mặc định LOẠI BỎ các phiếu bị hủy hoặc thuộc đơn hàng bị hủy
                if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == "Cancelled" || r.Status == "Đã hủy" 
                                          || (r.SaleOrder != null && (r.SaleOrder.Status == "Cancelled" || r.SaleOrder.Status == "Đã hủy")));
                }
                else
                {
                    query = query.Where(r => r.Status != "Cancelled" && r.Status != "Đã hủy"
                                          && (r.SaleOrder == null || (r.SaleOrder.Status != "Cancelled" && r.SaleOrder.Status != "Đã hủy")));
                }

                var allSlips = await query
                    .OrderByDescending(r => r.Createddate)
                    .Take(100)
                    .ToListAsync();

                // 5. LOẠI BỎ BẢN GHI TRÙNG LẶP MÃ PHIẾU
                allSlips = allSlips
                    .GroupBy(s => s.Expreceiptcode ?? ("PX-" + s.Exportreceiptid))
                    .Select(g => g.OrderByDescending(x => x.ProductExportReceiptDetails?.Count ?? 0)
                                  .ThenByDescending(x => x.Createddate)
                                  .First())
                    .ToList();

                ViewBag.AllSlips = allSlips;

                // 6. LỌC THEO TRẠNG THÁI CỤ THỂ
                var filteredSlips = allSlips.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    var st = status.Trim().ToLower();

                    if (st == "completed")
                    {
                        filteredSlips = filteredSlips.Where(s => 
                            string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                            s.Status == "Đã hoàn thành" || s.Status == "Đã xong");
                    }
                    else if (st == "ready" || st == "exported")
                    {
                        filteredSlips = filteredSlips.Where(s => 
                            (string.Equals(s.Status, "Exported", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                             s.Status == "Tán thành" || s.Status == "Đã duyệt" || s.Status == "Đã xuất kho") &&
                            !string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                            s.Status != "Đã xong");
                    }
                    else if (st == "rejected")
                    {
                        filteredSlips = filteredSlips.Where(s => 
                            string.Equals(s.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                            s.Status == "Từ chối");
                    }
                    else if (st == "waiting" || st == "pending")
                    {
                        filteredSlips = filteredSlips.Where(s => 
                            !string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Exported", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
                            s.Status != "Đã xong" && s.Status != "Đã xuất kho" && s.Status != "Từ chối");
                    }
                }

                return View("~/Areas/Sales/Views/ExportSlips/Index.cshtml", filteredSlips.ToList());
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Content($"[LỖI TẢI DANH SÁCH PHIẾU XUẤT]: {inner}");
            }
        }

        // =====================================================================
        // POST: /Sales/ExportSlips/ApproveCompletedOrder
        // =====================================================================
        [HttpPost("ApproveCompletedOrder")]
        public async Task<IActionResult> ApproveCompletedOrder(int receiptId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                string expReceiptCode = "";

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var receipt = await _context.ProductExportReceipts
                        .Include(r => r.SaleOrder)
                        .Include(r => r.ProductExportReceiptDetails)
                        .FirstOrDefaultAsync(r => r.Exportreceiptid == receiptId && r.Orderid != null);

                    if (receipt == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy phiếu xuất kho hợp lệ.");
                    }

                    if (receipt.SaleOrder != null && (receipt.SaleOrder.Status == "Cancelled" || receipt.SaleOrder.Status == "Đã hủy"))
                    {
                        throw new InvalidOperationException("Đơn hàng này đã bị hủy, không thể duyệt xuất kho!");
                    }

                    expReceiptCode = receipt.Expreceiptcode ?? $"PX-{receipt.Exportreceiptid}";

                    receipt.Status = "Completed";
                    if (receipt.ProductExportReceiptDetails != null)
                    {
                        foreach (var detail in receipt.ProductExportReceiptDetails)
                        {
                            detail.Status = "Completed";
                        }
                    }

                    SaleOrder? targetOrder = receipt.SaleOrder;
                    if (targetOrder == null && receipt.Orderid.HasValue)
                    {
                        targetOrder = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Orderid == receipt.Orderid.Value);
                    }

                    if (targetOrder != null)
                    {
                        targetOrder.Status = "Completed";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                InvalidateOrderCaches();

                return Json(new
                {
                    success = true,
                    message = $"Đã duyệt hoàn tất đơn hàng {expReceiptCode}! Cả đơn hàng và phiếu xuất đã cập nhật thành ĐÃ XONG (Completed)."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi cập nhật: " + inner });
            }
        }

        // =====================================================================
        // POST: /Sales/ExportSlips/RejectExportSlip
        // =====================================================================
        [HttpPost("RejectExportSlip")]
        public async Task<IActionResult> RejectExportSlip(int receiptId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                string expReceiptCode = "";

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var receipt = await _context.ProductExportReceipts
                        .Include(r => r.SaleOrder)
                        .Include(r => r.ProductExportReceiptDetails)
                        .FirstOrDefaultAsync(r => r.Exportreceiptid == receiptId && r.Orderid != null);

                    if (receipt == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy phiếu xuất kho hợp lệ.");
                    }

                    expReceiptCode = receipt.Expreceiptcode ?? $"PX-{receipt.Exportreceiptid}";

                    receipt.Status = "Rejected";
                    if (receipt.ProductExportReceiptDetails != null)
                    {
                        foreach (var detail in receipt.ProductExportReceiptDetails)
                        {
                            detail.Status = "Rejected";
                        }
                    }

                    SaleOrder? targetOrder = receipt.SaleOrder;
                    if (targetOrder == null && receipt.Orderid.HasValue)
                    {
                        targetOrder = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Orderid == receipt.Orderid.Value);
                    }

                    if (targetOrder != null && targetOrder.Status != "Cancelled" && targetOrder.Status != "Đã hủy")
                    {
                        targetOrder.Status = "Pending";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                InvalidateOrderCaches();

                return Json(new
                {
                    success = true,
                    message = $"Đã từ chối phiếu {expReceiptCode}. Đơn hàng đã được trả về trạng thái Chờ xử lý!"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi từ chối phiếu: " + inner });
            }
        }
    }
}