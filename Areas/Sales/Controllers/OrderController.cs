using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using httt.Data;
using httt.Models;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Route("Sales/Orders")]
    [Route("Sales/Order")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        private static readonly string[] ReservedStatuses = new[]
        {
            "Chờ xuất kho",
            "Đã giữ chốt số lượng",
            "Đã chốt lô",
            "Đã gửi yêu cầu duyệt",
            "Pending",
            "Holding",
            "PendingApproval"
        };

        public OrderController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private static string GenerateShortOrderCode()
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Span<char> result = stackalloc char[10];
            for (int i = 0; i < 10; i++)
            {
                result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
            return "HD" + new string(result);
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
        // GET: /Sales/Orders
        // =====================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? keyword, string? status, string? date, string tab = "sales")
        {
            try
            {
                bool isSupplierTab = string.Equals(tab, "supplier", StringComparison.OrdinalIgnoreCase) 
                                  || string.Equals(tab, "purchase", StringComparison.OrdinalIgnoreCase);

                ViewBag.ActiveTab = isSupplierTab ? "supplier" : "sales";
                ViewBag.Keyword = keyword;
                ViewBag.Status = status;
                ViewBag.Date = date;

                if (isSupplierTab)
                {
                    ViewBag.Orders = null;
                    if (!_cache.TryGetValue("ALL_SUPPLIERS_CACHE", out List<Supplier>? allSuppliers) || allSuppliers == null)
                    {
                        allSuppliers = await _context.Suppliers
                            .AsNoTracking()
                            .OrderBy(s => s.Supplierid)
                            .ToListAsync();
                        _cache.Set("ALL_SUPPLIERS_CACHE", allSuppliers, TimeSpan.FromMinutes(10));
                    }

                    var suppliers = allSuppliers.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        var kw = keyword.Trim().ToLower();
                        suppliers = suppliers.Where(s => 
                            (s.Suppliername != null && s.Suppliername.ToLower().Contains(kw)) ||
                            (s.Phone != null && s.Phone.ToLower().Contains(kw)) ||
                            (s.Address != null && s.Address.ToLower().Contains(kw)) ||
                            (s.Mail != null && s.Mail.ToLower().Contains(kw)));
                    }

                    ViewBag.Suppliers = suppliers.ToList();
                    return View("~/Areas/Sales/Views/Order/Orders.cshtml", new List<SaleOrder>());
                }

                ViewBag.Suppliers = null;

                var supplierNames = await _context.Suppliers
                    .AsNoTracking()
                    .Where(s => s.Suppliername != null && s.Suppliername != "")
                    .Select(s => s.Suppliername!.ToLower().Trim())
                    .ToListAsync();

                var query = _context.SaleOrders
                    .AsNoTracking()
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .Include(o => o.ProductExportReceipts)
                    .Where(o => o.SaleOrderDetails.Any())
                    .Where(o => string.IsNullOrEmpty(o.Customername) || !supplierNames.Contains(o.Customername.ToLower().Trim()))
                    .AsQueryable();

                // Lọc theo từ khóa
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(o => (o.Ordercode != null && o.Ordercode.ToLower().Contains(kw))
                                          || (o.Customername != null && o.Customername.ToLower().Contains(kw))
                                          || (o.Customerphone != null && o.Customerphone.ToLower().Contains(kw)));
                }

                // QUY TẮC HIỂN THỊ ĐƠN ĐÃ HỦY:
                // Nếu người dùng KHÔNG chọn lọc 'Cancelled', mặc định LOẠI BỎ các đơn đã hủy ra khỏi UI
                if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) || status == "Đã hủy")
                {
                    query = query.Where(o => o.Status == "Cancelled" || o.Status == "Đã hủy");
                }
                else
                {
                    query = query.Where(o => o.Status != "Cancelled" && o.Status != "Đã hủy");

                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        query = query.Where(o => o.Status == status);
                    }
                }

                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out DateTime parsedDate))
                {
                    query = query.Where(o => o.Orderdate.HasValue && o.Orderdate.Value.Date == parsedDate.Date);
                }

                var orders = await query
                    .OrderByDescending(o => o.Orderdate)
                    .Take(50)
                    .ToListAsync();

                // Lấy đúng phiếu xuất kho MỚI NHẤT của đơn hàng
                var receiptStatusDict = new Dictionary<int, string>();
                foreach (var ord in orders)
                {
                    var latestReceipt = ord.ProductExportReceipts?.OrderByDescending(r => r.Createddate).FirstOrDefault();
                    receiptStatusDict[ord.Orderid] = latestReceipt?.Status ?? "Chưa gửi phiếu";
                }
                ViewBag.ReceiptStatusDict = receiptStatusDict;

                var allProducts = await _context.Products.AsNoTracking().ToDictionaryAsync(p => p.Productid, p => p.Quantity ?? 0);
                var allocatedQuantities = new Dictionary<int, int>();

                var chronologicalOrders = orders.OrderBy(o => o.Orderdate).ToList();
                var orderShortageDict = new Dictionary<int, bool>();

                foreach (var ord in chronologicalOrders)
                {
                    bool isShort = false;
                    foreach (var d in ord.SaleOrderDetails)
                    {
                        int physical = allProducts.ContainsKey(d.Productid) ? allProducts[d.Productid] : 0;
                        int alreadyTaken = allocatedQuantities.ContainsKey(d.Productid) ? allocatedQuantities[d.Productid] : 0;
                        int availableForThis = physical - alreadyTaken;

                        if (d.Quantity > availableForThis)
                        {
                            isShort = true;
                        }

                        if (ord.Status != null && ReservedStatuses.Contains(ord.Status))
                        {
                            allocatedQuantities[d.Productid] = alreadyTaken + d.Quantity;
                        }
                    }
                    orderShortageDict[ord.Orderid] = isShort;
                }

                ViewBag.OrderShortageDict = orderShortageDict;
                ViewBag.Orders = orders;
                return View("~/Areas/Sales/Views/Order/Orders.cshtml", orders);
            }
            catch (Exception ex)
            {
                string fullError = ex.Message;
                Exception? current = ex.InnerException;
                while (current != null)
                {
                    fullError += "\n--> [CHI TIẾT]: " + current.Message;
                    current = current.InnerException;
                }
                return Content($"[LỖI TẢI DANH SÁCH]:\n{fullError}");
            }
        }

        // =====================================================================
        // POST: /Sales/Order/CancelOrder (HỦY ĐƠN HÀNG THỦ CÔNG)
        // =====================================================================
        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                string orderCode = "";

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var order = await _context.SaleOrders
                        .Include(o => o.ProductExportReceipts)
                            .ThenInclude(r => r.ProductExportReceiptDetails)
                        .FirstOrDefaultAsync(o => o.Orderid == orderId);

                    if (order == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy đơn hàng.");
                    }

                    // Đơn đã xong không được phép hủy
                    if (string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase) 
                        || order.Status == "Đã xong" || order.Status == "Đã hoàn thành")
                    {
                        throw new InvalidOperationException("Đơn hàng đã xuất kho hoàn tất (Đã xong), không thể hủy!");
                    }

                    orderCode = order.Ordercode ?? ("HD" + order.Orderid);

                    // 1. Cập nhật trạng thái đơn thành Cancelled
                    order.Status = "Cancelled";

                    // 2. Đồng thời hủy các phiếu xuất kho liên quan của đơn này
                    if (order.ProductExportReceipts != null)
                    {
                        foreach (var receipt in order.ProductExportReceipts)
                        {
                            if (receipt.Status != "Completed" && receipt.Status != "Đã xong")
                            {
                                receipt.Status = "Cancelled";
                                if (receipt.ProductExportReceiptDetails != null)
                                {
                                    foreach (var detail in receipt.ProductExportReceiptDetails)
                                    {
                                        detail.Status = "Cancelled";
                                    }
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                InvalidateOrderCaches();

                return Json(new
                {
                    success = true,
                    message = $"Đã hủy thành công đơn hàng {orderCode}! Đơn hàng đã được lưu trạng thái Đã hủy và ẩn khỏi màn hình chính."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi hủy đơn: " + inner });
            }
        }

        // =====================================================================
        // GET: /Sales/Order/Details/{id}
        // =====================================================================
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.SaleOrders
                .AsNoTracking()
                .Include(o => o.SaleOrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.ProductExportReceipts)
                .FirstOrDefaultAsync(o => o.Orderid == id);

            if (order == null) return NotFound("Không tìm thấy thông tin đơn hàng này.");

            var productIds = order.SaleOrderDetails.Select(d => d.Productid).Distinct().ToList();

            var reservedQuantities = new Dictionary<int, int>();
            if (productIds.Any())
            {
                var reservedList = await _context.SaleOrderDetails
                    .AsNoTracking()
                    .Where(d => productIds.Contains(d.Productid)
                             && d.Orderid != id
                             && d.SaleOrder != null
                             && d.SaleOrder.Status != null
                             && ReservedStatuses.Contains(d.SaleOrder.Status)
                             && (order.Orderdate == null || d.SaleOrder.Orderdate < order.Orderdate))
                    .GroupBy(d => d.Productid)
                    .Select(g => new { ProductId = g.Key, ReservedQty = g.Sum(x => x.Quantity) })
                    .ToListAsync();

                reservedQuantities = reservedList.ToDictionary(x => x.ProductId, x => x.ReservedQty);
            }

            bool isCurrentHolding = order.Status != null && ReservedStatuses.Contains(order.Status);

            var availableStockDict = new Dictionary<int, int>();
            foreach (var detail in order.SaleOrderDetails)
            {
                int physicalStock = detail.Product?.Quantity ?? 0;
                int otherReserved = reservedQuantities.ContainsKey(detail.Productid) ? reservedQuantities[detail.Productid] : 0;
                int available = physicalStock - otherReserved;

                if (isCurrentHolding)
                {
                    available -= detail.Quantity;
                }

                availableStockDict[detail.Productid] = available > 0 ? available : 0;
            }

            ViewBag.AvailableStock = availableStockDict;
            ViewBag.LatestReceipt = order.ProductExportReceipts?.OrderByDescending(r => r.Createddate).FirstOrDefault();

            return View("~/Areas/Sales/Views/Order/OrderDetails.cshtml", order);
        }

        // =====================================================================
        // POST/GET: /Sales/Order/ToggleOrderHold/{id}
        // =====================================================================
        [HttpPost("ToggleOrderHold/{id}")]
        [HttpGet("ToggleOrderHold/{id}")]
        public async Task<IActionResult> ToggleOrderHold(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                string updatedStatus = "";
                bool isNowHolding = false;

                await strategy.ExecuteAsync(async () =>
                {
                    var order = await _context.SaleOrders
                        .Include(o => o.SaleOrderDetails)
                        .FirstOrDefaultAsync(o => o.Orderid == id);

                    if (order == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy đơn hàng.");
                    }

                    bool isHolding = string.Equals(order.Status, "Holding", StringComparison.OrdinalIgnoreCase) 
                                  || order.Status == "Đã giữ chốt số lượng";

                    if (isHolding)
                    {
                        order.Status = "Pending";
                    }
                    else
                    {
                        order.Status = "Holding";
                    }

                    updatedStatus = order.Status;
                    isNowHolding = (order.Status == "Holding");

                    await _context.SaveChangesAsync();
                });

                InvalidateOrderCaches();

                return Json(new { 
                    success = true, 
                    status = updatedStatus, 
                    isHolding = isNowHolding,
                    message = isNowHolding ? "Đã chốt giữ số lượng thành công!" : "Đã hủy chốt giữ kho!"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi chốt kho: " + inner });
            }
        }

        // =====================================================================
        // POST: /Sales/Order/CompleteOrder
        // =====================================================================
        [HttpPost("CompleteOrder")]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                string orderCode = "";

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var order = await _context.SaleOrders
                        .Include(o => o.ProductExportReceipts)
                        .FirstOrDefaultAsync(o => o.Orderid == orderId);

                    if (order == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy đơn hàng.");
                    }

                    orderCode = order.Ordercode ?? ("HD" + order.Orderid);
                    order.Status = "Completed";

                    foreach (var receipt in order.ProductExportReceipts)
                    {
                        if (receipt.Status != "Rejected" && receipt.Status != "Từ chối" && receipt.Status != "Cancelled")
                        {
                            receipt.Status = "Completed";
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                InvalidateOrderCaches();

                return Json(new
                {
                    success = true,
                    message = $"Đơn hàng {orderCode} đã cập nhật thành công: ĐÃ XONG (Completed)!",
                    orderId = orderId
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi hoàn tất: " + inner });
            }
        }

        // =====================================================================
        // TẠO ĐƠN GIẢ LẬP
        // =====================================================================
        [HttpGet("GenerateMockOrders")]
        [HttpPost("GenerateMockOrders")]
        public async Task<IActionResult> GenerateMockOrders()
        {
            try
            {
                var productIdsWithBom = new List<int>();

                try
                {
                    productIdsWithBom = await _context.Database
                        .SqlQueryRaw<int>(@"
                            SELECT DISTINCT p.productid AS ""Value""
                            FROM public.product p
                            WHERE EXISTS (
                                SELECT 1
                                FROM public.bom b
                                WHERE b.productid = p.productid
                            )
                        ")
                        .ToListAsync();
                }
                catch
                {
                    try
                    {
                        using var cmd = _context.Database.GetDbConnection().CreateCommand();
                        cmd.CommandText = "SELECT DISTINCT productid FROM public.bom;";
                        if (cmd.Connection != null && cmd.Connection.State != System.Data.ConnectionState.Open)
                        {
                            await cmd.Connection.OpenAsync();
                        }
                        using var rdr = await cmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                        {
                            productIdsWithBom.Add(rdr.GetInt32(0));
                        }
                    }
                    catch { }
                }

                if (!productIdsWithBom.Any())
                {
                    TempData["ErrorMessage"] = "Chưa có sản phẩm nào được thiết lập công thức điều chế (BOM).";
                    return RedirectToAction(nameof(Index), new { tab = "sales" });
                }

                var availableProducts = await _context.Products
                    .AsNoTracking()
                    .Where(p => productIdsWithBom.Contains(p.Productid))
                    .OrderBy(p => p.Productid)
                    .ToListAsync();

                if (!availableProducts.Any())
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dữ liệu thuốc trong kho ứng với các công thức BOM.";
                    return RedirectToAction(nameof(Index), new { tab = "sales" });
                }

                var random = Random.Shared;
                var pharmacies = new[]
                {
                    new { Name = "NT SỐ 71 - XUÂN THỚI SƠN", Phone = "0901234567", Addr = "3/138 Đường Tân Thới Nhì 9, Hóc Môn, HCM" },
                    new { Name = "QT DƯỢC LÀNH - TÂY NINH", Phone = "0912345678", Addr = "Ấp Tân Qui Hạ, xã Hiệp Hoà, Tây Ninh" },
                    new { Name = "NT THỊ CHÂU - BÌNH THẠNH", Phone = "0987654321", Addr = "685/45 Xô Viết Nghệ Tĩnh, Bình Thạnh, HCM" },
                    new { Name = "HKDNT MINH KHANG - THỚI AN", Phone = "0934567890", Addr = "326 Trần Thị Cờ, Quận 12, HCM" },
                    new { Name = "NHÀ THUỐC AN KHANG PHÚ NHUẬN", Phone = "0977889900", Addr = "125 Phan Xích Long, Phú Nhuận, HCM" },
                    new { Name = "NHÀ THUỐC LONG CHÂU - GÒ VẤP", Phone = "0988112233", Addr = "456 Quang Trung, P.10, Gò VẤp, HCM" },
                    new { Name = "NHÀ THUỐC PHARMACITY - QUẬN 1", Phone = "0911223344", Addr = "78 Nguyễn Huệ, P. Bến Nghé, Quận 1, HCM" }
                };

                var pharmacy = pharmacies[random.Next(pharmacies.Length)];
                var now = DateTime.Now;

                for (int attempt = 1; attempt <= 10; attempt++)
                {
                    try
                    {
                        _context.ChangeTracker.Clear();

                        string uniqueCode = GenerateShortOrderCode();
                        bool isCodeExists = await _context.SaleOrders.AnyAsync(o => o.Ordercode == uniqueCode);
                        if (isCodeExists) continue;

                        var order = new SaleOrder
                        {
                            Customername = pharmacy.Name,
                            Customerphone = pharmacy.Phone,
                            Shippingaddress = pharmacy.Addr,
                            Orderdate = now,
                            Status = "Pending",
                            Ordercode = uniqueCode,
                            Totalamount = 0,
                            Createdby = null,
                            SaleOrderDetails = new List<SaleOrderDetail>()
                        };

                        int targetCount = random.Next(1, 4);
                        int maxItems = Math.Min(availableProducts.Count, targetCount);

                        var chosenProducts = availableProducts
                            .OrderBy(_ => Guid.NewGuid())
                            .Take(maxItems)
                            .ToList();

                        decimal total = 0;
                        foreach (var prod in chosenProducts)
                        {
                            int qty = random.Next(5, 25);
                            decimal sellingPrice = (prod.Cost.HasValue && prod.Cost.Value > 0) ? prod.Cost.Value : 100000;

                            order.SaleOrderDetails.Add(new SaleOrderDetail
                            {
                                Productid = prod.Productid,
                                Quantity = qty,
                                Unitprice = sellingPrice
                            });

                            total += (sellingPrice * qty);
                        }

                        order.Totalamount = total;

                        _context.SaleOrders.Add(order);
                        await _context.SaveChangesAsync();
                        InvalidateOrderCaches();

                        TempData["SuccessMessage"] = $"Đã tạo đơn hàng giả lập {uniqueCode} thành công với đúng đơn giá thành phẩm!";
                        return RedirectToAction(nameof(Index), new { tab = "sales" });
                    }
                    catch (DbUpdateException) when (attempt < 10)
                    {
                        _context.ChangeTracker.Clear();
                        await Task.Delay(50 * attempt);
                    }
                }

                TempData["ErrorMessage"] = "Trùng mã đơn ngẫu nhiên sau 10 lần thử. Vui lòng bấm tạo lại.";
                return RedirectToAction(nameof(Index), new { tab = "sales" });
            }
            catch (Exception ex)
            {
                string innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = $"[LỖI TẠO ĐƠN GIẢ LẬP]: {innerMsg}";
                return RedirectToAction(nameof(Index), new { tab = "sales" });
            }
        }

        // =====================================================================
        // NÚT RESET ĐƠN HÀNG
        // =====================================================================
        [HttpGet("ResetOrders")]
        [HttpPost("ResetOrders")]
        public async Task<IActionResult> ResetOrders()
        {
            try
            {
                _context.ChangeTracker.Clear();

                await _context.ProductExportReceiptDetails.ExecuteDeleteAsync();
                await _context.ProductExportReceipts.ExecuteDeleteAsync();
                await _context.SaleOrderDetails.ExecuteDeleteAsync();
                await _context.SaleOrders.ExecuteDeleteAsync();

                InvalidateOrderCaches();
                TempData["SuccessMessage"] = "Đã dọn dẹp sạch toàn bộ dữ liệu đơn hàng và phiếu xuất kho.";
                return RedirectToAction(nameof(Index), new { tab = "sales" });
            }
            catch
            {
                try
                {
                    _context.ChangeTracker.Clear();

                    await _context.Database.ExecuteSqlRawAsync(@"
                        DELETE FROM public.productexportreceiptdetail;
                        DELETE FROM public.productexportreceipt;
                        DELETE FROM public.saleorderdetail;
                        DELETE FROM public.saleorder;
                    ");

                    InvalidateOrderCaches();
                    TempData["SuccessMessage"] = "Đã dọn dẹp sạch toàn bộ dữ liệu đơn hàng và phiếu xuất kho.";
                    return RedirectToAction(nameof(Index), new { tab = "sales" });
                }
                catch (Exception fallbackEx)
                {
                    string innerMsg = fallbackEx.InnerException != null ? fallbackEx.InnerException.Message : fallbackEx.Message;
                    TempData["ErrorMessage"] = $"[LỖI RESET ĐƠN HÀNG]: {innerMsg}";
                    return RedirectToAction(nameof(Index), new { tab = "sales" });
                }
            }
        }
    }
}