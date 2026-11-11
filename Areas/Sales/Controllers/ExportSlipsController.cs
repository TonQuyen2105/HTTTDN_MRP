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

                var allSlips = await query
                    .OrderByDescending(r => r.Createddate)
                    .Take(120)
                    .ToListAsync();

                // 4. LOẠI BỎ BẢN GHI TRÙNG LẶP MÃ PHIẾU
                allSlips = allSlips
                    .GroupBy(s => s.Expreceiptcode ?? ("PX-" + s.Exportreceiptid))
                    .Select(g => g.OrderByDescending(x => x.ProductExportReceiptDetails?.Count ?? 0)
                                  .ThenByDescending(x => x.Createddate)
                                  .First())
                    .ToList();

                // Lưu danh sách toàn bộ phiếu vào ViewBag để view thống kê đúng các thẻ
                ViewBag.AllSlips = allSlips;

                // 5. LỌC THEO TRẠNG THÁI CỦA PHIẾU XUẤT (HOÀN TOÀN ĐỘC LẬP VỚI ĐƠN HÀNG)
                var filteredSlips = allSlips.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(status))
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
                            string.Equals(s.Status, "Rejected", StringComparison.OrdinalIgnoreCase) || s.Status == "Từ chối");
                    }
                    else if (st == "waiting" || st == "pending")
                    {
                        filteredSlips = filteredSlips.Where(s => 
                            !string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Exported", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                            s.Status != "Đã xong" && s.Status != "Đã xuất kho" && s.Status != "Từ chối" && s.Status != "Đã hủy");
                    }
                    else if (st == "cancelled")
                    {
                        filteredSlips = filteredSlips.Where(s => 
                            string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                            s.Status == "Đã hủy");
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
        // (XÁC NHẬN XUẤT KHO HOÀN TẤT & TRỪ TỒN KHO THỰC TẾ)
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

                    // Tránh trừ kho lặp lại nếu phiếu đã Completed trước đó
                    if (string.Equals(receipt.Status, "Completed", StringComparison.OrdinalIgnoreCase) || receipt.Status == "Đã xong")
                    {
                        throw new InvalidOperationException("Phiếu xuất này đã được duyệt hoàn tất trước đó!");
                    }

                    expReceiptCode = receipt.Expreceiptcode ?? $"PX-{receipt.Exportreceiptid}";

                    // 1. Cập nhật trạng thái phiếu xuất
                    receipt.Status = "Completed";

                    // 2. Cập nhật chi tiết phiếu và TRỪ TỒN KHO THÀNH PHẨM
                    if (receipt.ProductExportReceiptDetails != null)
                    {
                        var productIds = receipt.ProductExportReceiptDetails.Select(d => d.Productid).Distinct().ToList();
                        var products = await _context.Products.Where(p => productIds.Contains(p.Productid)).ToListAsync();

                        foreach (var detail in receipt.ProductExportReceiptDetails)
                        {
                            detail.Status = "Completed";

                            var prod = products.FirstOrDefault(p => p.Productid == detail.Productid);
                            if (prod != null)
                            {
                                int currentQty = prod.Quantity ?? 0;
                                prod.Quantity = Math.Max(0, currentQty - detail.Quantity);
                            }
                        }
                    }

                    // 3. Nếu đơn hàng chưa bị hủy thì đồng bộ thành Completed (Nếu đơn đã hủy thì giữ nguyên Cancelled)
                    SaleOrder? targetOrder = receipt.SaleOrder;
                    if (targetOrder == null && receipt.Orderid.HasValue)
                    {
                        targetOrder = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Orderid == receipt.Orderid.Value);
                    }

                    if (targetOrder != null && targetOrder.Status != "Cancelled" && targetOrder.Status != "Đã hủy")
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
                    message = $"Đã duyệt xuất kho {expReceiptCode}! Đã cập nhật trừ tồn kho thành phẩm thành công."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi duyệt xuất kho: " + inner });
            }
        }

        // =====================================================================
        // POST: /Sales/ExportSlips/RejectExportSlip
        // (TỪ CHỐI PHIẾU XUẤT KHO & HOÀN KHO NẾU TRƯỚC ĐÓ ĐÃ TRỪ)
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

                    bool wasCompleted = string.Equals(receipt.Status, "Completed", StringComparison.OrdinalIgnoreCase) || receipt.Status == "Đã xong";

                    receipt.Status = "Rejected";

                    if (receipt.ProductExportReceiptDetails != null)
                    {
                        var productIds = receipt.ProductExportReceiptDetails.Select(d => d.Productid).Distinct().ToList();
                        var products = await _context.Products.Where(p => productIds.Contains(p.Productid)).ToListAsync();

                        foreach (var detail in receipt.ProductExportReceiptDetails)
                        {
                            detail.Status = "Rejected";

                            // Nếu trước đó phiếu đã xuất (Completed) mà giờ từ chối/hủy -> Cộng hoàn lại tồn kho
                            if (wasCompleted)
                            {
                                var prod = products.FirstOrDefault(p => p.Productid == detail.Productid);
                                if (prod != null)
                                {
                                    prod.Quantity = (prod.Quantity ?? 0) + detail.Quantity;
                                }
                            }
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
                    message = $"Đã từ chối phiếu {expReceiptCode}." + (expReceiptCode != "" ? " Tồn kho đã được đồng bộ chính xác." : "")
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