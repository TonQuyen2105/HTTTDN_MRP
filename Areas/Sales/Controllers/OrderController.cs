using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
    [Authorize(Roles = "Sales,BanHang")]
    [IgnoreAntiforgeryToken]
    [Route("Sales/Orders")]
    [Route("Sales/Order")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        [HttpGet("Orders")]
        public async Task<IActionResult> Index(string? tab, string? keyword, string? status, string? date)
        {
            try
            {
                string activeTab = string.IsNullOrWhiteSpace(tab) ? "sales" : tab.ToLower();
                ViewBag.ActiveTab = activeTab;
                ViewBag.Keyword = keyword ?? string.Empty;
                ViewBag.Status = status ?? string.Empty;
                ViewBag.Date = date ?? string.Empty;

                if (activeTab == "supplier")
                {
                    var suppliersQuery = _context.Suppliers.AsNoTracking().AsQueryable();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        var kw = keyword.Trim().ToLower();
                        suppliersQuery = suppliersQuery.Where(s =>
                            s.SupplierName.ToLower().Contains(kw) ||
                            (s.Phone != null && s.Phone.Contains(kw)) ||
                            (s.Mail != null && s.Mail.ToLower().Contains(kw)) ||
                            (s.Address != null && s.Address.ToLower().Contains(kw))
                        );
                    }

                    var suppliers = await suppliersQuery
                        .OrderBy(s => s.SupplierId)
                        .ToListAsync();

                    ViewBag.Suppliers = suppliers;
                    return View("Orders", new List<SaleOrder>());
                }

                // 1. Nạp danh sách phiếu xuất kho
                var exportReceipts = await _context.ProductExportReceipts
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedDate)
                    .ThenByDescending(r => r.ExportReceiptId)
                    .ToListAsync();

                // Chỉ lưu trạng thái của phiếu xuất MỚI NHẤT cho mỗi đơn hàng
                var receiptStatusDict = new Dictionary<int, string>();
                foreach (var r in exportReceipts)
                {
                    if (r.OrderId.HasValue && !receiptStatusDict.ContainsKey(r.OrderId.Value))
                    {
                        receiptStatusDict[r.OrderId.Value] = r.Status ?? "Chờ xuất kho";
                    }
                }
                ViewBag.ReceiptStatusDict = receiptStatusDict;

                // 2. Nạp tất cả đơn hàng để tính tồn khả dụng
                var allOrders = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.OrderId)
                    .ToListAsync();

                // ĐỒNG BỘ TRẠNG THÁI CHÍNH XÁC
                bool needSaveSync = false;
                foreach (var ord in allOrders)
                {
                    bool isOrderCancelled = string.Equals(ord.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                                         || ord.Status == "Đã hủy";
                    if (isOrderCancelled) continue;

                    if (receiptStatusDict.TryGetValue(ord.OrderId, out var rStatus))
                    {
                        bool isReceiptFullyCompleted = string.Equals(rStatus, "Completed", StringComparison.OrdinalIgnoreCase)
                                                    || rStatus == "Đã hoàn thành";

                        bool isReceiptRejected = string.Equals(rStatus, "Rejected", StringComparison.OrdinalIgnoreCase)
                                              || rStatus == "Từ chối";

                        bool isOrderDone = string.Equals(ord.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                                        || ord.Status == "Đã xong"
                                        || ord.Status == "Đã hoàn thành";

                        if (isReceiptFullyCompleted && !isOrderDone)
                        {
                            ord.Status = "Completed";
                            needSaveSync = true;
                        }
                        else if (isReceiptRejected && !isOrderDone && ord.Status != "Pending")
                        {
                            ord.Status = "Pending";
                            needSaveSync = true;
                        }
                    }
                }

                if (needSaveSync)
                {
                    await _context.SaveChangesAsync();
                }

                // 3. Tính số lượng tồn kho đang bị giữ chỗ bởi các đơn đang chờ xuất kho HỢP LỆ
                var reservedStock = new Dictionary<int, int>();
                foreach (var o in allOrders)
                {
                    if (IsOrderWaitingExport(o, receiptStatusDict))
                    {
                        foreach (var d in o.SaleOrderDetails)
                        {
                            if (!reservedStock.ContainsKey(d.ProductId)) reservedStock[d.ProductId] = 0;
                            reservedStock[d.ProductId] += d.Quantity;
                        }
                    }
                }

                // Kiểm tra tình trạng thiếu hàng khả dụng cho từng đơn
                var orderShortageDict = new Dictionary<int, bool>();
                foreach (var o in allOrders)
                {
                    bool isDone = IsOrderDone(o, receiptStatusDict);
                    bool isCancelled = string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) || o.Status == "Đã hủy";

                    if (isDone || isCancelled)
                    {
                        orderShortageDict[o.OrderId] = false;
                        continue;
                    }

                    bool isThisWaiting = IsOrderWaitingExport(o, receiptStatusDict);
                    bool hasShortage = false;

                    foreach (var d in o.SaleOrderDetails)
                    {
                        int physical = d.Product?.Quantity ?? 0;
                        int totalReserved = reservedStock.ContainsKey(d.ProductId) ? reservedStock[d.ProductId] : 0;
                        int otherReserved = isThisWaiting ? (totalReserved - d.Quantity) : totalReserved;
                        int availableForThis = Math.Max(0, physical - otherReserved);

                        if (d.Quantity > availableForThis)
                        {
                            hasShortage = true;
                            break;
                        }
                    }
                    orderShortageDict[o.OrderId] = hasShortage;
                }
                ViewBag.OrderShortageDict = orderShortageDict;

                // 4. Lọc hiển thị danh sách theo bộ lọc tìm kiếm (Keyword, Status, Date)
                IEnumerable<SaleOrder> filteredOrders = allOrders;

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    filteredOrders = filteredOrders.Where(o =>
                        (!string.IsNullOrEmpty(o.OrderCode) && o.OrderCode.ToLower().Contains(kw)) ||
                        (!string.IsNullOrEmpty(o.CustomerName) && o.CustomerName.ToLower().Contains(kw)) ||
                        (!string.IsNullOrEmpty(o.CustomerPhone) && o.CustomerPhone.Contains(kw)) ||
                        (!string.IsNullOrEmpty(o.ShippingAddress) && o.ShippingAddress.ToLower().Contains(kw))
                    );
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var st = status.Trim();
                    if (string.Equals(st, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        filteredOrders = filteredOrders.Where(o =>
                            string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                            o.Status == "Đã hủy"
                        );
                    }
                    else
                    {
                        filteredOrders = filteredOrders.Where(o =>
                            !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                            o.Status != "Đã hủy"
                        );

                        if (string.Equals(st, "Completed", StringComparison.OrdinalIgnoreCase))
                        {
                            filteredOrders = filteredOrders.Where(o => IsOrderDone(o, receiptStatusDict));
                        }
                        else if (string.Equals(st, "PendingApproval", StringComparison.OrdinalIgnoreCase))
                        {
                            filteredOrders = filteredOrders.Where(o => IsOrderWaitingExport(o, receiptStatusDict));
                        }
                        else if (string.Equals(st, "Holding", StringComparison.OrdinalIgnoreCase))
                        {
                            filteredOrders = filteredOrders.Where(o =>
                                !IsOrderDone(o, receiptStatusDict) &&
                                (string.Equals(o.Status, "Holding", StringComparison.OrdinalIgnoreCase) || o.Status == "Đã giữ chốt số lượng"));
                        }
                        else if (string.Equals(st, "Pending", StringComparison.OrdinalIgnoreCase))
                        {
                            filteredOrders = filteredOrders.Where(o => !IsOrderDone(o, receiptStatusDict) && !IsOrderWaitingExport(o, receiptStatusDict));
                        }
                    }
                }
                else
                {
                    // Mặc định: Không hiển thị đơn đã hủy ở màn hình chính
                    filteredOrders = filteredOrders.Where(o =>
                        !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                        o.Status != "Đã hủy"
                    );
                }

                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
                {
                    filteredOrders = filteredOrders.Where(o =>
                        o.OrderDate.HasValue && o.OrderDate.Value.Date == parsedDate.Date
                    );
                }

                ViewBag.Suppliers = new List<Supplier>();
                return View("Orders", filteredOrders.ToList());
            }
            catch
            {
                ViewBag.ActiveTab = "sales";
                ViewBag.Suppliers = new List<Supplier>();
                ViewBag.OrderShortageDict = new Dictionary<int, bool>();
                ViewBag.ReceiptStatusDict = new Dictionary<int, string>();
                return View("Orders", new List<SaleOrder>());
            }
        }

        private static bool IsOrderDone(SaleOrder o, Dictionary<int, string> receiptStatusDict)
        {
            if (string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                o.Status == "Đã xong" ||
                o.Status == "Đã hoàn thành") return true;

            if (receiptStatusDict.TryGetValue(o.OrderId, out var rStatus))
            {
                if (string.Equals(rStatus, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    rStatus == "Đã hoàn thành") return true;
            }
            return false;
        }

        private static bool IsOrderWaitingExport(SaleOrder o, Dictionary<int, string> receiptStatusDict)
        {
            if (IsOrderDone(o, receiptStatusDict)) return false;
            if (string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) || o.Status == "Đã hủy") return false;

            if (receiptStatusDict.TryGetValue(o.OrderId, out var rStatus))
            {
                if (string.Equals(rStatus, "Rejected", StringComparison.OrdinalIgnoreCase) || rStatus == "Từ chối")
                {
                    return false;
                }
                if (string.Equals(rStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) || rStatus == "Đã hủy")
                {
                    return false;
                }
                if (rStatus != "Chưa gửi" && rStatus != "Chưa gửi phiếu")
                {
                    return true;
                }
            }

            return string.Equals(o.Status, "Holding", StringComparison.OrdinalIgnoreCase)
                || string.Equals(o.Status, "PendingApproval", StringComparison.OrdinalIgnoreCase)
                || o.Status == "Đã giữ chốt số lượng"
                || o.Status == "Chờ xuất kho"
                || o.Status == "Đã gửi yêu cầu duyệt";
        }

        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder([FromQuery] int orderId)
        {
            try
            {
                var order = await _context.SaleOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng cần hủy!" });
                }

                if (string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    order.Status == "Đã xong" ||
                    order.Status == "Đã hoàn thành")
                {
                    return Json(new { success = false, message = "Đơn hàng này đã xuất kho hoàn tất, không thể hủy!" });
                }

                order.Status = "Cancelled";
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã hủy đơn hàng {order.OrderCode ?? ("DH-" + order.OrderId)} thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng: " + ex.Message });
            }
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return RedirectToAction("Index");
                }

                var latestReceipt = await _context.ProductExportReceipts
                    .AsNoTracking()
                    .Where(r => r.OrderId == id)
                    .OrderByDescending(r => r.CreatedDate)
                    .FirstOrDefaultAsync();

                ViewBag.LatestReceipt = latestReceipt;

                var otherWaitingOrders = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                    .AsNoTracking()
                    .Where(o => o.OrderId != id &&
                                o.Status != "Cancelled" &&
                                o.Status != "Đã hủy" &&
                                (o.Status == "Holding" ||
                                 o.Status == "PendingApproval" ||
                                 o.Status == "Đã giữ chốt số lượng" ||
                                 o.Status == "Chờ xuất kho" ||
                                 o.Status == "Đã gửi yêu cầu duyệt"))
                    .ToListAsync();

                var reservedByOthers = new Dictionary<int, int>();
                foreach (var other in otherWaitingOrders)
                {
                    foreach (var d in other.SaleOrderDetails)
                    {
                        if (!reservedByOthers.ContainsKey(d.ProductId))
                            reservedByOthers[d.ProductId] = 0;

                        reservedByOthers[d.ProductId] += d.Quantity;
                    }
                }

                var availableStockDict = new Dictionary<int, int>();
                foreach (var item in order.SaleOrderDetails)
                {
                    int physicalStock = item.Product?.Quantity ?? 0;
                    int heldQty = reservedByOthers.ContainsKey(item.ProductId) ? reservedByOthers[item.ProductId] : 0;
                    availableStockDict[item.ProductId] = Math.Max(0, physicalStock - heldQty);
                }

                ViewBag.AvailableStock = availableStockDict;
                ViewBag.ReservedByOthers = reservedByOthers;

                return View("OrderDetails", order);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost("ToggleOrderHold/{id}")]
        public async Task<IActionResult> ToggleOrderHold(int id)
        {
            try
            {
                var order = await _context.SaleOrders
                    .Include(o => o.SaleOrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) || order.Status == "Đã hủy")
                {
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy, không thể giữ chốt kho!" });
                }

                bool isDone = string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                              order.Status == "Đã xong" || order.Status == "Đã hoàn thành";

                if (isDone)
                {
                    return Json(new { success = false, message = "Đơn hàng đã xuất kho hoàn tất, không thể thay đổi!" });
                }

                bool isCurrentlyHolding = string.Equals(order.Status, "Holding", StringComparison.OrdinalIgnoreCase) ||
                                          order.Status == "Đã giữ chốt số lượng";

                if (isCurrentlyHolding)
                {
                    order.Status = "Pending";
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        isHolding = false,
                        newStatusText = "Đang chờ xử lý",
                        message = "Đã hủy giữ chỗ tồn kho. Số lượng đã được nhả lại cho kho chung!"
                    });
                }
                else
                {
                    var otherHoldingOrders = await _context.SaleOrders
                        .Include(o => o.SaleOrderDetails)
                        .AsNoTracking()
                        .Where(o => o.OrderId != id &&
                                    o.Status != "Cancelled" &&
                                    o.Status != "Đã hủy" &&
                                    (o.Status == "Holding" ||
                                     o.Status == "PendingApproval" ||
                                     o.Status == "Đã giữ chốt số lượng" ||
                                     o.Status == "Chờ xuất kho"))
                        .ToListAsync();

                    var reservedByOthers = new Dictionary<int, int>();
                    foreach (var other in otherHoldingOrders)
                    {
                        foreach (var d in other.SaleOrderDetails)
                        {
                            if (!reservedByOthers.ContainsKey(d.ProductId)) reservedByOthers[d.ProductId] = 0;
                            reservedByOthers[d.ProductId] += d.Quantity;
                        }
                    }

                    foreach (var item in order.SaleOrderDetails)
                    {
                        int physicalStock = item.Product?.Quantity ?? 0;
                        int heldByOthers = reservedByOthers.ContainsKey(item.ProductId) ? reservedByOthers[item.ProductId] : 0;
                        int available = Math.Max(0, physicalStock - heldByOthers);

                        if (item.Quantity > available)
                        {
                            string pName = item.Product?.ProductName ?? $"SP #{item.ProductId}";
                            return Json(new
                            {
                                success = false,
                                message = $"Không thể chốt giữ: Thuốc '{pName}' cần {item.Quantity} nhưng tồn khả dụng chỉ còn {available}."
                            });
                        }
                    }

                    order.Status = "Holding";
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        isHolding = true,
                        newStatusText = "Đã giữ chốt kho",
                        message = "Đã chốt giữ số lượng tồn kho cho đơn hàng này thành công!"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi cập nhật CSDL: " + ex.Message });
            }
        }

        [HttpPost("CompleteOrder")]
        public async Task<IActionResult> CompleteOrder([FromQuery] int orderId)
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
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy, không thể hoàn tất!" });
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
                    message = $"Đơn hàng {order.OrderCode ?? ("DH-" + order.OrderId)} đã được cập nhật sang trạng thái ĐÃ XONG (Completed)!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
            }
        }

        [HttpPost("GenerateMockOrders")]
        public async Task<IActionResult> GenerateMockOrders()
        {
            try
            {
                var bomProductIds = await _context.Database
                    .SqlQueryRaw<int>("SELECT DISTINCT productid FROM public.bom WHERE productid IS NOT NULL")
                    .ToListAsync();

                List<Product> targetProducts;

                if (bomProductIds.Any())
                {
                    targetProducts = await _context.Products
                        .Where(p => bomProductIds.Contains(p.ProductId))
                        .ToListAsync();
                }
                else
                {
                    targetProducts = await _context.Products.ToListAsync();
                }

                if (!targetProducts.Any())
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thành phẩm nào trong kho dữ liệu để tạo đơn hàng!";
                    return RedirectToAction("Index", new { tab = "sales" });
                }

                int? currentUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;
                int? createdByUserId = currentUserId
                    ?? (await _context.UserAccounts.AsNoTracking().Select(u => (int?)u.UserId).FirstOrDefaultAsync());

                var pharmacies = new[]
                {
                    new { Name = "Hệ thống Nhà thuốc Long Châu - CN Quận 1", Phone = "0901234567", Address = "375 Hai Bà Trưng, P. Võ Thị Sáu, Quận 1, TP.HCM" },
                    new { Name = "Nhà thuốc Pharmacity - CN Cộng Hòa", Phone = "0902345678", Address = "128 Cộng Hòa, Phường 4, Quận Tân Bình, TP.HCM" },
                    new { Name = "Nhà thuốc An Khang - CN Thủ Đức", Phone = "0903456789", Address = "215 Võ Văn Ngân, P. Linh Chiểu, TP. Thủ Đức" },
                    new { Name = "Kho Dược Bệnh viện Đa khoa Tâm Anh", Phone = "0904567890", Address = "2B Phổ Quang, Phường 2, Quận Tân Bình, TP.HCM" },
                    new { Name = "Nhà thuốc Phano Pharmacy - CN Quận 3", Phone = "0905678901", Address = "45 Nguyễn Thông, Phường 9, Quận 3, TP.HCM" },
                    new { Name = "Trung tâm Dược phẩm Hapulico", Phone = "0988112233", Address = "Số 1 Nguyễn Huy Tưởng, Thanh Xuân, Hà Nội" }
                };

                var random = new Random();
                var customer = pharmacies[random.Next(pharmacies.Length)];
                var orderDate = DateTime.UtcNow;

                var orderCode = $"SO-{orderDate:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

                var newOrder = new SaleOrder
                {
                    OrderCode = orderCode,
                    CustomerName = customer.Name,
                    CustomerPhone = customer.Phone,
                    ShippingAddress = customer.Address,
                    OrderDate = orderDate,
                    Status = "Pending",
                    CreatedBy = createdByUserId,
                    TotalAmount = 0
                };

                int maxCount = Math.Min(3, targetProducts.Count);
                int minCount = Math.Min(2, maxCount);
                int itemCount = random.Next(minCount, maxCount + 1);

                var selectedProducts = targetProducts.OrderBy(_ => random.Next()).Take(itemCount).ToList();

                decimal totalOrderAmount = 0;

                foreach (var prod in selectedProducts)
                {
                    int currentStock = prod.Quantity ?? 0;
                    int qty = currentStock > 10 ? random.Next(3, 12) : random.Next(2, 6);
                    decimal unitPrice = (prod.Cost.HasValue && prod.Cost.Value > 0) ? prod.Cost.Value : 95000m;

                    var detail = new SaleOrderDetail
                    {
                        ProductId = prod.ProductId,
                        Quantity = qty,
                        UnitPrice = unitPrice
                    };

                    totalOrderAmount += qty * unitPrice;
                    newOrder.SaleOrderDetails.Add(detail);
                }

                newOrder.TotalAmount = totalOrderAmount;
                _context.SaleOrders.Add(newOrder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo mới 1 đơn hàng ({orderCode}) thành công!";
                return RedirectToAction("Index", new { tab = "sales" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tạo đơn hàng giả lập: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("Index", new { tab = "sales" });
            }
        }

        [HttpPost("ResetOrders")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetOrders()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM public.productexportreceiptdetail;");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM public.productexportreceipt;");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM public.saleorderdetail;");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM public.saleorder;");

                TempData["SuccessMessage"] = "Đã xóa sạch toàn bộ đơn hàng và phiếu xuất kho. Dữ liệu đã được reset về 0!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi reset đơn hàng: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index", new { tab = "sales" });
        }
    }
}