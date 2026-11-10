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
        // GET: /Sales/ExportSlips (DANH SÁCH PHIẾU XUẤT KHO THÀNH PHẨM)
        // =====================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? keyword, string? status)
        {
            try
            {
                ViewBag.Keyword = keyword;
                ViewBag.Status = status;

                var query = _context.ProductExportReceipts
                    .AsNoTracking()
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .Where(r => r.ProductExportReceiptDetails.Any())
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(r => (r.Expreceiptcode != null && r.Expreceiptcode.ToLower().Contains(kw))
                                          || (r.SaleOrder != null && r.SaleOrder.Customername != null && r.SaleOrder.Customername.ToLower().Contains(kw))
                                          || (r.SaleOrder != null && r.SaleOrder.Ordercode != null && r.SaleOrder.Ordercode.ToLower().Contains(kw)));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var slips = await query
                    .OrderByDescending(r => r.Createddate)
                    .Take(50)
                    .ToListAsync();

                // Lọc bỏ trùng lặp mã phiếu
                slips = slips
                    .GroupBy(s => s.Expreceiptcode ?? ("PX-" + s.Exportreceiptid))
                    .Select(g => g.OrderByDescending(x => x.ProductExportReceiptDetails?.Count ?? 0)
                                  .ThenByDescending(x => x.Createddate)
                                  .First())
                    .ToList();

                return View("~/Areas/Sales/Views/ExportSlips/Index.cshtml", slips);
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Content($"[LỖI TẢI DANH SÁCH PHIẾU XUẤT]: {inner}");
            }
        }

        // =====================================================================
        // POST: /Sales/ExportSlips/ApproveCompletedOrder
        // BẤM NÚT DUYỆT THỦ CÔNG -> ĐỒNG BỘ CẢ 2 BẢNG SANG 'Completed'
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
                        .FirstOrDefaultAsync(r => r.Exportreceiptid == receiptId);

                    if (receipt == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy phiếu xuất kho này.");
                    }

                    expReceiptCode = receipt.Expreceiptcode ?? $"PX-{receipt.Exportreceiptid}";

                    // 1. Cập nhật phiếu xuất kho và chi tiết phiếu thành "Completed"
                    receipt.Status = "Completed";
                    if (receipt.ProductExportReceiptDetails != null)
                    {
                        foreach (var detail in receipt.ProductExportReceiptDetails)
                        {
                            detail.Status = "Completed";
                        }
                    }

                    // 2. Cập nhật đơn hàng (SaleOrder) thành "Completed"
                    SaleOrder? targetOrder = receipt.SaleOrder;
                    if (targetOrder == null && receipt.Orderid.HasValue)
                    {
                        targetOrder = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Orderid == receipt.Orderid.Value);
                    }

                    if (targetOrder != null)
                    {
                        targetOrder.Status = "Completed";
                    }

                    // 3. Thực thi lưu đồng thời và commit transaction
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                // 4. Xóa cache liên quan sau khi commit thành công
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
        // KHI KHO TỪ CHỐI: receipt = 'Rejected', order = 'Pending'
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
                        .FirstOrDefaultAsync(r => r.Exportreceiptid == receiptId);

                    if (receipt == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy phiếu xuất kho này.");
                    }

                    expReceiptCode = receipt.Expreceiptcode ?? $"PX-{receipt.Exportreceiptid}";

                    // 1. Cập nhật phiếu xuất kho thành "Rejected"
                    receipt.Status = "Rejected";
                    if (receipt.ProductExportReceiptDetails != null)
                    {
                        foreach (var detail in receipt.ProductExportReceiptDetails)
                        {
                            detail.Status = "Rejected";
                        }
                    }

                    // 2. Trả đơn hàng về "Pending" (Chờ xử lý)
                    SaleOrder? targetOrder = receipt.SaleOrder;
                    if (targetOrder == null && receipt.Orderid.HasValue)
                    {
                        targetOrder = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Orderid == receipt.Orderid.Value);
                    }

                    if (targetOrder != null)
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