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
    [Route("Sales/CreateOrder")]
    [Route("Sales/ExportSlips/Create")]
    public class CreateOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CreateOrderController(ApplicationDbContext context, IMemoryCache cache)
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
        // GET: /Sales/CreateOrder
        // =====================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? orderId)
        {
            try
            {
                // ĐÃ XÓA KHỐI AUTO-SYNC ÉP COMPLETED Ở ĐÂY.
                // Đảm bảo không ghi đè phiếu bị từ chối và giữ quyền duyệt thủ công.

                var orders = await _context.SaleOrders
                    .AsNoTracking()
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .Include(o => o.ProductExportReceipts)
                    .Where(o => o.Ordercode == null || !o.Ordercode.StartsWith("DH000"))
                    .Where(o => o.SaleOrderDetails.Any())
                    .OrderByDescending(o => o.Orderdate)
                    .Take(50)
                    .ToListAsync();

                SaleOrder? selectedOrder = null;
                if (orderId.HasValue)
                {
                    selectedOrder = orders.FirstOrDefault(o => o.Orderid == orderId.Value);
                }
                if (selectedOrder == null && orders.Any())
                {
                    selectedOrder = orders.First();
                }

                // Lấy toàn bộ phiếu xuất (Bao gồm cả các phiếu bị Rejected và phiếu mới)
                var rawReceipts = await _context.ProductExportReceipts
                    .AsNoTracking()
                    .Include(r => r.SaleOrder)
                    .Include(r => r.ProductExportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .OrderByDescending(r => r.Createddate)
                    .Take(50)
                    .ToListAsync();

                var exportReceipts = rawReceipts
                    .GroupBy(r => !string.IsNullOrEmpty(r.Expreceiptcode) ? r.Expreceiptcode : r.Exportreceiptid.ToString())
                    .Select(g => g.First())
                    .OrderByDescending(r => r.Createddate)
                    .Take(30)
                    .ToList();

                var orderLookup = orders.ToDictionary(
                    o => o.Orderid, 
                    o => (Code: o.Ordercode ?? ("DH-" + o.Orderid), Customer: o.Customername ?? "Khách hàng")
                );

                // Lấy phiếu mới nhất của đơn hàng đang chọn
                ProductExportReceipt? existingReceipt = null;
                if (selectedOrder != null)
                {
                    existingReceipt = await _context.ProductExportReceipts
                        .AsNoTracking()
                        .Where(r => r.Orderid == selectedOrder.Orderid)
                        .OrderByDescending(r => r.Createddate)
                        .FirstOrDefaultAsync();
                }

                ViewBag.Orders = orders;
                ViewBag.SelectedOrder = selectedOrder;
                ViewBag.ExistingReceipt = existingReceipt;
                ViewBag.ExportReceipts = exportReceipts;
                ViewBag.OrderLookup = orderLookup;

                return View("~/Areas/Sales/Views/CreateOrder/CreateOrder.cshtml", orders);
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Content($"[LỖI TẠO PHIẾU XUẤT]: {inner}");
            }
        }

        // =====================================================================
        // POST: /Sales/CreateOrder/RequestExportSlip
        // =====================================================================
        [HttpPost("RequestExportSlip")]
        public async Task<IActionResult> RequestExportSlip(int orderId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                string receiptCode = "";

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var order = await _context.SaleOrders
                        .Include(o => o.SaleOrderDetails)
                        .Include(o => o.ProductExportReceipts)
                        .FirstOrDefaultAsync(o => o.Orderid == orderId);

                    if (order == null) 
                        throw new InvalidOperationException("Không tìm thấy đơn hàng này.");

                    if (!order.SaleOrderDetails.Any()) 
                        throw new InvalidOperationException("Đơn hàng chưa có danh mục sản phẩm.");

                    // CHỈ CHẶN NẾU ĐƠN ĐANG CÓ PHIẾU ĐANG CHỜ HOẶC ĐÃ HOÀN TẤT
                    // Nếu phiếu trước đó là Rejected (Từ chối) -> Cho phép tạo thêm phiếu mới!
                    var activeReceipt = order.ProductExportReceipts?
                        .FirstOrDefault(r => r.Status != "Rejected" && r.Status != "Từ chối");

                    if (activeReceipt != null)
                    {
                        throw new InvalidOperationException($"Đơn này đang có phiếu xuất ({activeReceipt.Expreceiptcode}) đang chờ xử lý hoặc đã duyệt!");
                    }

                    var now = DateTime.Now;
                    receiptCode = "PXTP" + Guid.NewGuid().ToString("N")[..8].ToUpper();

                    var newReceipt = new ProductExportReceipt
                    {
                        Orderid = order.Orderid,
                        Expreceiptcode = receiptCode,
                        Createddate = now,
                        Status = "Pending", 
                        Requestedby = null
                    };

                    var groupedDetails = order.SaleOrderDetails
                        .GroupBy(d => d.Productid)
                        .Select(g => new
                        {
                            ProductId = g.Key,
                            TotalQty = g.Sum(x => x.Quantity),
                            UnitPrice = g.First().Unitprice
                        })
                        .ToList();

                    foreach (var item in groupedDetails)
                    {
                        newReceipt.ProductExportReceiptDetails.Add(new ProductExportReceiptDetail
                        {
                            Productid = item.ProductId,
                            Quantity = item.TotalQty,
                            Price = item.UnitPrice,
                            Status = "Pending",
                            Createddate = now,
                            Note = $"Yêu cầu xuất từ đơn {order.Ordercode ?? ("DH-" + order.Orderid)}"
                        });
                    }

                    _context.ProductExportReceipts.Add(newReceipt);

                    order.Status = "PendingApproval";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                InvalidateOrderCaches();

                return Json(new
                {
                    success = true,
                    message = $"Đã tạo phiếu xuất {receiptCode} và gửi yêu cầu duyệt sang Kho thành công!",
                    receiptCode = receiptCode
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi hệ thống: " + detailedError });
            }
        }

        // =====================================================================
        // POST: /Sales/CreateOrder/CompleteOrderFromSlip
        // =====================================================================
        [HttpPost("CompleteOrderFromSlip")]
        public async Task<IActionResult> CompleteOrderFromSlip(int orderId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var order = await _context.SaleOrders
                        .Include(o => o.ProductExportReceipts)
                        .FirstOrDefaultAsync(o => o.Orderid == orderId);

                    if (order == null)
                        throw new InvalidOperationException("Không tìm thấy đơn hàng.");

                    order.Status = "Completed";
                    // Giữ nguyên các phiếu cũ bị từ chối
                    foreach (var r in order.ProductExportReceipts)
                    {
                        if (r.Status != "Rejected" && r.Status != "Từ chối")
                        {
                            r.Status = "Completed";
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                InvalidateOrderCaches();

                return Json(new
                {
                    success = true,
                    message = "Đơn hàng đã cập nhật thành công: ĐÃ XONG (Completed)!"
                });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi cập nhật: " + inner });
            }
        }

        // =====================================================================
        // POST: /Sales/CreateOrder/ClearReceiptForTest
        // =====================================================================
        [HttpPost("ClearReceiptForTest")]
        public async Task<IActionResult> ClearReceiptForTest(int orderId)
        {
            try
            {
                var receipts = await _context.ProductExportReceipts
                    .Include(r => r.ProductExportReceiptDetails)
                    .Where(r => r.Orderid == orderId)
                    .ToListAsync();

                if (receipts.Any())
                {
                    foreach (var r in receipts)
                    {
                        if (r.ProductExportReceiptDetails.Any())
                        {
                            _context.ProductExportReceiptDetails.RemoveRange(r.ProductExportReceiptDetails);
                        }
                    }
                    _context.ProductExportReceipts.RemoveRange(receipts);
                }

                var order = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Orderid == orderId);
                if (order != null)
                {
                    order.Status = "Pending";
                }

                await _context.SaveChangesAsync();
                InvalidateOrderCaches();

                return Json(new { success = true, message = "Đã xóa phiếu xuất để kiểm tra lại!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}