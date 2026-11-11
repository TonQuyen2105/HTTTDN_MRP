using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Constants;
using httt.Data;
using httt.Models;
using httt.Services;

namespace httt.Areas.Productions.Controllers
{
    [Area("Productions")]
    [Authorize(Roles = "Production,Manager,Employee")]
    public partial class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MaterialRequirementService _mrpService;
        private readonly ProductionHandoverService _handoverService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context, 
            MaterialRequirementService mrpService,
            ProductionHandoverService handoverService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _mrpService = mrpService;
            _handoverService = handoverService;
            _logger = logger;
        }

        private async Task<UserAccount?> GetCurrentUserAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int userId))
            {
                var user = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
                if (user != null && (string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Status, "true", StringComparison.OrdinalIgnoreCase)))
                {
                    return user;
                }
            }

            return null;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var requests = await _context.ProductionRequests.AsNoTracking().ToListAsync();
                var ingredients = await _context.Ingredients.AsNoTracking().ToListAsync();
                var products = await _context.Products.AsNoTracking().OrderByDescending(p => p.Quantity).Take(5).ToListAsync();

                ViewBag.HasError = false;
                ViewBag.TotalRequests = requests.Count;
                ViewBag.PendingRequests = requests.Count(r => r.Status != "Completed");
                ViewBag.TotalQuantityRequired = requests.Sum(r => (long)r.QuantityRequired);
                ViewBag.TotalIngredients = ingredients.Count;
                ViewBag.TotalStock = ingredients.Sum(i => i.Quantity ?? 0m);
                ViewBag.TopProducts = products;
                ViewBag.StatusCounts = requests.GroupBy(r => r.Status ?? "Pending")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi truy vấn dữ liệu trang Tổng quan (Dashboard AMIS Sản xuất)");
                ViewBag.HasError = true;
                ViewBag.ErrorMessage = "Không tải được dữ liệu từ cơ sở dữ liệu. Vui lòng thử lại sau.";
                ViewBag.TotalRequests = null;
                ViewBag.PendingRequests = null;
                ViewBag.TotalQuantityRequired = null;
                ViewBag.TotalIngredients = null;
                ViewBag.TotalStock = null;
                ViewBag.TopProducts = new List<Product>();
                ViewBag.StatusCounts = new List<object>();
            }

            return View("~/Areas/Productions/Views/Overview.cshtml");
        }

        public IActionResult Overview() => RedirectToAction("Index");

        /// <summary>
        /// Trang Lệnh sản xuất đã được gộp/bỏ theo yêu cầu, chuyển hướng toàn bộ sang Đơn đặt hàng thuốc
        /// </summary>
        public IActionResult ProductionOrder()
        {
            return RedirectToAction("DrugOrder", "Home", new { area = "Productions" });
        }

        public async Task<IActionResult> MaterialRequisitionList()
        {
            try
            {
                var requisitions = await _context.MaterialReceipts
                    .AsNoTracking()
                    // Only show receipts linked to an existing production request.
                    // Display the request code stored by Warehouse.
                    .Where(m => m.RequestId.HasValue && m.RequestId > 0 && m.ProductionRequest != null)
                    .Include(m => m.Requester)
                    .Include(m => m.Approver)
                    .Include(m => m.ProductionRequest)
                        .ThenInclude(r => r!.Product)
                    .Include(m => m.Details)
                        .ThenInclude(d => d.Ingredient)
                    .OrderByDescending(m => m.ExpReceiptId)
                    .ToListAsync();

                return View("~/Areas/Productions/Views/MaterialRequisitionList.cshtml", requisitions);
            }
            catch
            {
                return View("~/Areas/Productions/Views/MaterialRequisitionList.cshtml", new List<MaterialReceipt>());
            }
        }

        public async Task<IActionResult> MaterialRequisitionCreate(int? id)
        {
            try
            {
                ViewBag.Ingredients = await _context.Ingredients.AsNoTracking().ToListAsync();
                ViewBag.ProductionRequests = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(r => r.Product)
                    .OrderByDescending(r => r.RequestId)
                    .ToListAsync();
                ViewBag.Users = await _context.UserAccounts.AsNoTracking().ToListAsync();

                if (id.HasValue && id.Value > 0)
                {
                    var receipt = await _context.MaterialReceipts
                        .AsNoTracking()
                        .Include(m => m.Requester)
                        .Include(m => m.Approver)
                        .Include(m => m.ProductionRequest)
                            .ThenInclude(r => r!.Product)
                        .Include(m => m.Details)
                            .ThenInclude(d => d.Ingredient)
                        .FirstOrDefaultAsync(m => m.ExpReceiptId == id.Value);

                    if (receipt != null)
                    {
                        // Lấy tồn kho sản xuất (WIP) từ bảng wip_inventory theo Lệnh sản xuất tương ứng
                        if (receipt.RequestId.HasValue && receipt.RequestId.Value > 0)
                        {
                            var wipItems = await _context.WipInventories
                                .AsNoTracking()
                                .Where(w => w.RequestId == receipt.RequestId.Value && w.IngrId.HasValue)
                                .ToListAsync();

                            ViewBag.WipDict = wipItems
                                .GroupBy(w => w.IngrId!.Value)
                                .ToDictionary(g => g.Key, g => g.Sum(w => w.Quantity ?? 0m));
                        }
                        else
                        {
                            ViewBag.WipDict = new Dictionary<int, decimal>();
                        }

                        return View("~/Areas/Productions/Views/MaterialRequisitionCreate.cshtml", receipt);
                    }
                }
            }
            catch {}
            return View("~/Areas/Productions/Views/MaterialRequisitionCreate.cshtml", (MaterialReceipt?)null);
        }

        /// <summary>
        /// API xuất kho nguyên vật liệu từ kho tổng chuyển về kho sản xuất (WIP) cho Lệnh sản xuất
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportMaterialsToWip([FromBody] ExportToWipRequest req)
        {
            if (req == null || req.ExpReceiptId <= 0)
                return BadRequest(new { success = false, message = "Mã phiếu yêu cầu xuất kho không hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể thực hiện xuất kho."
                    });
                }

                var receipt = await _context.MaterialReceipts
                    .Include(m => m.Details)
                        .ThenInclude(d => d.Ingredient)
                    .Include(m => m.ProductionRequest)
                    .FirstOrDefaultAsync(m => m.ExpReceiptId == req.ExpReceiptId);

                if (receipt == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu yêu cầu xuất kho trong cơ sở dữ liệu." });

                if (receipt.Status == "Completed")
                    return BadRequest(new { success = false, message = $"Phiếu {receipt.DisplayCode} đã được xuất kho trước đó." });

                if (!receipt.RequestId.HasValue || receipt.RequestId.Value <= 0)
                    return BadRequest(new { success = false, message = "Phiếu yêu cầu chưa liên kết với Lệnh sản xuất hợp lệ." });

                if (receipt.Details == null || !receipt.Details.Any())
                    return BadRequest(new { success = false, message = "Phiếu xuất kho không có nguyên vật liệu chi tiết." });

                // Kiểm tra tồn kho thực tế xem có đủ xuất cho từng dòng không
                var insufficientLines = new List<string>();
                foreach (var detail in receipt.Details)
                {
                    var ingr = detail.Ingredient;
                    var actualStock = ingr?.Quantity ?? 0m;
                    if (actualStock < detail.Quantity)
                    {
                        insufficientLines.Add($"'{ingr?.IngrName ?? $"NVL #{detail.IngrId}"}': yêu cầu {detail.Quantity:N2} {detail.Unit}, kho thực tế chỉ có {actualStock:N2} {detail.Unit} (thiếu {detail.Quantity - actualStock:N2} {detail.Unit})");
                    }
                }

                if (insufficientLines.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        insufficient = true,
                        message = "Kho thực tế chưa đủ số lượng để xuất:\n• " + string.Join("\n• ", insufficientLines) + "\n\nVui lòng nhập thêm vào kho thực tế trước khi xuất về xưởng sản xuất."
                    });
                }

                // Thực hiện trừ kho thực tế và cộng vào kho sản xuất (WIP) trong Transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    int reqId = receipt.RequestId.Value;

                    foreach (var detail in receipt.Details)
                    {
                        var ingr = detail.Ingredient;
                        if (ingr != null)
                        {
                            ingr.Quantity = (ingr.Quantity ?? 0m) - detail.Quantity;
                        }

                        // Tìm hoặc tạo bản ghi tồn kho sản xuất (wip_inventory)
                        var wip = await _context.WipInventories
                            .FirstOrDefaultAsync(w => w.RequestId == reqId && w.IngrId == detail.IngrId);

                        if (wip != null)
                        {
                            wip.Quantity = (wip.Quantity ?? 0m) + detail.Quantity;
                            wip.LastUpdated = DateTime.UtcNow;
                        }
                        else
                        {
                            wip = new WipInventory
                            {
                                RequestId = reqId,
                                IngrId = detail.IngrId,
                                Quantity = detail.Quantity,
                                LastUpdated = DateTime.UtcNow
                            };
                            _context.WipInventories.Add(wip);
                        }

                        detail.Status = "Completed";
                        detail.ExportDate = DateTime.UtcNow;
                    }

                    receipt.Status = "Completed";
                    receipt.ApprovedBy = currentUser.UserID;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var orderCode = receipt.ProductionRequest?.ProductionDisplayCode ?? "Chưa có mã";
                    return Ok(new
                    {
                        success = true,
                        message = $"Xuất kho thành công {receipt.DisplayCode}! Đã chuyển toàn bộ nguyên vật liệu về kho sản xuất (WIP) cho {orderCode}.",
                        receiptCode = receipt.DisplayCode,
                        requestId = receipt.RequestId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi xuất kho nguyên vật liệu sang WIP");
                    return BadRequest(new { success = false, message = "Lỗi khi lưu dữ liệu xuất kho: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API nhập bổ sung nguyên vật liệu vào kho thực tế
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplenishIngredientStock([FromBody] ReplenishIngredientRequest req)
        {
            if (req == null || req.IngrId <= 0 || req.Quantity <= 0)
                return BadRequest(new { success = false, message = "Thông tin nhập kho không hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể thực hiện nhập kho."
                    });
                }

                var ingr = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngrId == req.IngrId);
                if (ingr == null)
                    return NotFound(new { success = false, message = "Không tìm thấy nguyên vật liệu trong cơ sở dữ liệu." });

                ingr.Quantity = (ingr.Quantity ?? 0m) + req.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã nhập bổ sung thành công {req.Quantity:N2} {ingr.Unit} vào kho thực tế. Tồn kho thực tế mới: {ingr.Quantity:N2} {ingr.Unit}.",
                    ingrId = ingr.IngrId,
                    newQuantity = ingr.Quantity
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API giả lập nạp nguyên vật liệu trực tiếp vào kho sản xuất (wip_inventory) cho một phiếu yêu cầu xuất
        /// Tăng số lượng trong wip_inventory thay vì kho tổng; sau khi nạp đầy, trạng thái đổi thành "Completed" (hiển thị là "Được cấp")
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplenishAllForReceipt([FromBody] ReplenishAllForReceiptRequest req)
        {
            if (req == null || req.ExpReceiptId <= 0)
                return BadRequest(new { success = false, message = "Mã phiếu không hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập)."
                    });
                }

                var receipt = await _context.MaterialReceipts
                    .Include(m => m.Details)
                        .ThenInclude(d => d.Ingredient)
                    .Include(m => m.ProductionRequest)
                    .FirstOrDefaultAsync(m => m.ExpReceiptId == req.ExpReceiptId);

                if (receipt == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu yêu cầu xuất kho." });

                if (!receipt.RequestId.HasValue || receipt.RequestId.Value <= 0)
                    return BadRequest(new { success = false, message = "Phiếu yêu cầu chưa liên kết với Lệnh sản xuất hợp lệ." });

                int reqId = receipt.RequestId.Value;
                var orderCode = receipt.ProductionRequest?.ProductionDisplayCode ?? "Chưa có mã";

                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var replenishedList = new List<string>();

                    foreach (var detail in receipt.Details)
                    {
                        var ingr = detail.Ingredient;
                        var ingrName = ingr?.IngrName ?? $"NVL #{detail.IngrId}";
                        var ingrUnit = detail.Unit ?? ingr?.Unit ?? "kg";

                        // Tìm hoặc tạo bản ghi tồn kho sản xuất (wip_inventory)
                        var wip = await _context.WipInventories
                            .FirstOrDefaultAsync(w => w.RequestId == reqId && w.IngrId == detail.IngrId);

                        var currentWip = wip?.Quantity ?? 0m;
                        if (currentWip < detail.Quantity)
                        {
                            var deficit = detail.Quantity - currentWip;
                            if (wip == null)
                            {
                                wip = new WipInventory
                                {
                                    RequestId = reqId,
                                    IngrId = detail.IngrId,
                                    ProductId = null,
                                    Quantity = detail.Quantity,
                                    LastUpdated = DateTime.UtcNow
                                };
                                _context.WipInventories.Add(wip);
                            }
                            else
                            {
                                wip.Quantity = detail.Quantity;
                                wip.LastUpdated = DateTime.UtcNow;
                            }
                            replenishedList.Add($"{ingrName}: +{deficit:N2} {ingrUnit} (Tồn kho SX: {detail.Quantity:N2} {ingrUnit})");
                        }

                        detail.Status = "Completed";
                        detail.ExportDate = DateTime.UtcNow;
                    }

                    // Đổi trạng thái phiếu sang Completed (hiển thị "Được cấp")
                    receipt.Status = "Completed";
                    receipt.ApprovedBy = currentUser.UserID;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    string message = replenishedList.Any()
                        ? $"Đã giả lập nạp đủ nguyên vật liệu vào Kho sản xuất (WIP) cho {orderCode}!\n• " + string.Join("\n• ", replenishedList) + "\n\nTrạng thái phiếu đã chuyển sang: 'Được cấp'."
                        : $"Kho sản xuất (WIP) đã có đủ toàn bộ nguyên vật liệu theo yêu cầu của phiếu. Trạng thái phiếu: 'Được cấp'.";

                    return Ok(new
                    {
                        success = true,
                        message,
                        receiptCode = receipt.DisplayCode,
                        requestId = reqId,
                        status = "Được cấp"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi nạp NVL vào kho sản xuất cho phiếu {ReceiptId}", req.ExpReceiptId);
                    return BadRequest(new { success = false, message = "Lỗi khi lưu dữ liệu nạp kho sản xuất: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #region Tính nhu cầu nguyên vật liệu (MRP)

        /// <summary>
        /// Trang Tính nhu cầu nguyên vật liệu
        /// </summary>
        public async Task<IActionResult> MaterialRequirement(int? requestId, int? productId)
        {
            try
            {
                var vm = await _mrpService.GetInitialViewModelAsync(requestId, productId);
                return View("~/Areas/Productions/Views/MaterialRequirement.cshtml", vm);
            }
            catch (Exception ex)
            {
                var vm = new MaterialRequirementViewModel
                {
                    ErrorMessage = $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}"
                };
                return View("~/Areas/Productions/Views/MaterialRequirement.cshtml", vm);
            }
        }

        /// <summary>
        /// Đường dẫn phụ tương thích
        /// </summary>
        public IActionResult CalculateMaterialRequirement(int? requestId, int? productId)
        {
            return RedirectToAction("MaterialRequirement", new { requestId, productId });
        }

        /// <summary>
        /// API lấy thông tin chi tiết của Yêu cầu sản xuất
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int requestId)
        {
            try
            {
                var req = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (req == null)
                    return NotFound(new { message = "Không tìm thấy yêu cầu sản xuất trong hệ thống." });

                string? orderCode = (req.Status == "Approved" || req.Status == "In Production")
                    ? (req.ProductionDisplayCode)
                    : null;

                var boms = await _context.Boms
                    .AsNoTracking()
                    .Where(b => b.ProductId == req.ProductId)
                    .ToListAsync();

                var groupedBoms = boms
                    .GroupBy(b => !string.IsNullOrWhiteSpace(b.BomCode) ? b.BomCode : b.BomName)
                    .Select(g => new
                    {
                        bomCode = g.Key,
                        bomName = $"Định mức tiêu chuẩn {req.Product?.ProductName ?? g.Key} ({g.Count()} nguyên liệu)",
                        standardOutput = 1m,
                        unit = req.Product?.Unit ?? "Hộp",
                        materialCount = g.Count()
                    }).ToList();

                var existingSlip = await _context.MaterialReceipts
                    .AsNoTracking()
                    .Where(m => m.RequestId == requestId
                             && m.Status != "Cancelled" && m.Status != "Rejected")
                    .OrderByDescending(m => m.ExpReceiptId)
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    requestId = req.RequestId,
                    requestCode = req.ProductionDisplayCode,
                    productId = req.ProductId,
                    productName = req.Product?.ProductName ?? "",
                    quantityRequired = req.QuantityRequired,
                    unit = req.Product?.Unit ?? "Hộp",
                    status = req.Status,
                    productionOrderCode = orderCode,
                    isApprovedOrder = !string.IsNullOrEmpty(orderCode),
                    hasBom = groupedBoms.Any(),
                    boms = groupedBoms,
                    hasExistingSlip = existingSlip != null,
                    existingReceiptId = existingSlip?.ExpReceiptId,
                    existingReceiptCode = existingSlip?.DisplayCode,
                    existingReceiptStatus = existingSlip?.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// API lấy danh sách BOM của một sản phẩm
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductBoms(int productId)
        {
            try
            {
                var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
                if (prod == null)
                    return NotFound(new { message = "Không tìm thấy sản phẩm." });

                var boms = await _context.Boms
                    .AsNoTracking()
                    .Where(b => b.ProductId == productId)
                    .ToListAsync();

                var groupedBoms = boms
                    .GroupBy(b => !string.IsNullOrWhiteSpace(b.BomCode) ? b.BomCode : b.BomName)
                    .Select(g => new
                    {
                        bomCode = g.Key,
                        bomName = $"Định mức tiêu chuẩn {prod.ProductName} ({g.Count()} nguyên liệu)",
                        standardOutput = 1m,
                        unit = prod.Unit ?? "Hộp",
                        materialCount = g.Count()
                    }).ToList();

                return Json(new
                {
                    productId = prod.ProductId,
                    productName = prod.ProductName,
                    unit = prod.Unit ?? "Hộp",
                    hasBom = groupedBoms.Any(),
                    boms = groupedBoms
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// API tính toán lại nhu cầu nguyên vật liệu trên máy chủ
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateRequirementApi([FromBody] CalculateRequirementApiRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Dữ liệu yêu cầu không hợp lệ." });

            try
            {
                var result = await _mrpService.CalculateAsync(req);
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi tính toán: {ex.Message}" });
            }
        }

        /// <summary>
        /// API tạo phiếu yêu cầu cấp nguyên vật liệu gửi kho
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaterialRequisitionFromMRP([FromBody] CreateRequisitionApiRequest req)
        {
            if (req == null)
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể tạo phiếu yêu cầu cấp nguyên vật liệu."
                    });
                }

                int requestedByUserId = currentUser.UserID;
                var (success, message, receiptId, receiptCode) = await _mrpService.CreateRequisitionSlipAsync(req, requestedByUserId);

                if (!success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message,
                        existingReceiptId = receiptId,
                        existingReceiptCode = receiptCode
                    });
                }

                return Ok(new
                {
                    success = true,
                    message,
                    receiptId,
                    receiptCode,
                    redirectUrl = Url.Action("MaterialRequisitionCreate", "Home", new { area = "Productions", id = receiptId })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phiếu yêu cầu cấp nguyên vật liệu từ MRP");
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi trong quá trình tạo phiếu yêu cầu. Vui lòng thử lại sau." });
            }
        }

        #endregion

        private async Task<List<ProductionStatisticsViewModel>> LoadProductionStatisticsAsync(int? requestId = null)
        {
            var query = _context.ProductionRequests.AsNoTracking()
                .Where(r => r.Status == "Approved" || r.Status == "In Production" || r.Status == "Completed")
                .Include(r => r.Product).Include(r => r.Manager).AsQueryable();
            if (requestId.HasValue) query = query.Where(r => r.RequestId == requestId.Value);
            var requests = await query.OrderByDescending(r => r.RequestId).ToListAsync();
            var ids = requests.Select(r => r.RequestId).ToList();
            var logs = await _context.ProductionLogs.AsNoTracking().Include(l => l.Creator)
                .Where(l => ids.Contains(l.RequestId)).OrderByDescending(l => l.ProducedAt).ThenByDescending(l => l.LogId).ToListAsync();
            var receipts = await _context.ProductReceipts.AsNoTracking().Include(r => r.Details)
                .Where(r => r.RequestId.HasValue && ids.Contains(r.RequestId.Value) && string.IsNullOrWhiteSpace(r.ReceiptCode)).ToListAsync();
            return requests.Select(r => new ProductionStatisticsViewModel
            {
                Request = r,
                Logs = logs.Where(l => l.RequestId == r.RequestId && l.ProductId == r.ProductId).ToList(),
                AccumulatedQuantity = receipts.Where(x => x.RequestId == r.RequestId).SelectMany(x => x.Details)
                    .Where(d => d.ProductId == r.ProductId).Sum(d => (long)d.Quantity)
            }).ToList();
        }

        public async Task<IActionResult> ProductionStatisticsList()
        {
            try
            {
                return View("~/Areas/Productions/Views/ProductionStatisticsList.cshtml", await LoadProductionStatisticsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không tải được thống kê sản xuất");
                ViewBag.LoadError = "Không tải được dữ liệu thống kê. Vui lòng tải lại trang.";
                Response.StatusCode = 503;
                return View("~/Areas/Productions/Views/ProductionStatisticsList.cshtml", new List<ProductionStatisticsViewModel>());
            }
        }

        public IActionResult ProductionStatisticsCreate() => RedirectToAction(nameof(ProductionStatisticsList));

        public async Task<IActionResult> ProductionStatisticsDetail(int? id)
        {
            try
            {
                var model = id is > 0 ? (await LoadProductionStatisticsAsync(id)).FirstOrDefault() : null;
                if (model == null) Response.StatusCode = 404;
                return View("~/Areas/Productions/Views/ProductionStatisticsDetail.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không tải được lịch sử sản xuất {RequestId}", id);
                ViewBag.LoadError = "Không tải được lịch sử sản xuất. Vui lòng tải lại trang.";
                Response.StatusCode = 503;
                return View("~/Areas/Productions/Views/ProductionStatisticsDetail.cshtml", (ProductionStatisticsViewModel?)null);
            }
        }

        /*
        // Tạm thời ẩn / comment trang "Nhập kho thành phẩm" theo yêu cầu
        public async Task<IActionResult> FinishedGoodsReceiptList()
        {
            try
            {
                var receipts = await _context.ProductReceipts
                    .AsNoTracking()
                    .Include(p => p.Requester)
                    .Include(p => p.Approver)
                    .Include(p => p.ProductionRequest)
                    .Include(p => p.Details)
                        .ThenInclude(d => d.Product)
                    .OrderByDescending(p => p.ProductReceiptId)
                    .ToListAsync();

                return View("~/Areas/Productions/Views/FinishedGoodsReceiptList.cshtml", receipts);
            }
            catch
            {
                return View("~/Areas/Productions/Views/FinishedGoodsReceiptList.cshtml", new List<ProductReceipt>());
            }
        }

        public async Task<IActionResult> FinishedGoodsReceiptCreate()
        {
            try
            {
                ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
            }
            catch {}
            return View("~/Areas/Productions/Views/FinishedGoodsReceiptCreate.cshtml");
        }
        */

        public async Task<IActionResult> HandoverList(string? search = null, string? status = null, string? tab = "pending")
        {
            try
            {
                var model = await _handoverService.GetHandoverPageDataAsync(search, status, tab);
                ViewBag.SearchKeyword = search ?? string.Empty;
                ViewBag.StatusFilter = status ?? "All";
                return View("~/Areas/Productions/Views/HandoverList.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hiển thị trang HandoverList");
                return View("~/Areas/Productions/Views/HandoverList.cshtml", new HandoverPageViewModel());
            }
        }

        /// <summary>
        /// API lập và gửi phiếu bàn giao thành phẩm riêng biệt sang Kho (hỗ trợ bàn giao từng phần)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHandoverSlip([FromBody] HandoverSlipCreateRequest req)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg));

                return BadRequest(new
                {
                    success = false,
                    message = !string.IsNullOrWhiteSpace(firstError)
                        ? firstError
                        : "Dữ liệu lập phiếu bàn giao không hợp lệ hoặc thiếu trường bắt buộc."
                });
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Chưa xác định được danh tính người dùng hoặc phiên đăng nhập đã hết hạn."
                });
            }

            var (success, message, receiptCode, receiptId) = await _handoverService.CreateHandoverSlipAsync(req, currentUser.UserID);
            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message,
                receiptCode,
                receiptId
            });
        }

        public async Task<IActionResult> HandoverDetail(int? id)
        {
            try
            {
                if (!id.HasValue || id.Value <= 0)
                {
                    return View("~/Areas/Productions/Views/HandoverDetail.cshtml", (ProductReceipt?)null);
                }

                var receipt = await _handoverService.GetHandoverSlipDetailAsync(id.Value);
                return View("~/Areas/Productions/Views/HandoverDetail.cshtml", receipt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hiển thị chi tiết phiếu bàn giao {Id}", id);
                return View("~/Areas/Productions/Views/HandoverDetail.cshtml", (ProductReceipt?)null);
            }
        }

        public async Task<IActionResult> UserProfile()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                return View("~/Areas/Productions/Views/UserProfile.cshtml", user);
            }
            catch
            {
                return View("~/Areas/Productions/Views/UserProfile.cshtml", (UserAccount?)null);
            }
        }

        #region Đơn đặt hàng thuốc (Nhận từ phiếu sản xuất bên kho)

        /// <summary>
        /// Trang danh sách Đơn đặt hàng thuốc tiếp nhận từ phiếu sản xuất kho
        /// </summary>
        public async Task<IActionResult> DrugOrder()
        {
            try
            {
                var orders = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .Include(p => p.Manager)
                    .OrderByDescending(p => p.RequestId)
                    .ToListAsync();

                var receipts = await _context.ProductReceipts
                    .AsNoTracking()
                    .Include(p => p.Details)
                    .Where(p => p.RequestId.HasValue && string.IsNullOrWhiteSpace(p.ReceiptCode))
                    .ToListAsync();

                foreach (var order in orders)
                {
                    var orderReceipts = receipts.Where(r => r.RequestId == order.RequestId).ToList();
                    var produced = orderReceipts.SelectMany(r => r.Details).Sum(d => d.Quantity);
                    order.ProducedQuantity = produced;
                }

                ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
                ViewBag.Managers = await _context.UserAccounts.AsNoTracking().ToListAsync();
                ViewBag.PendingRequests = orders.Where(o => o.Status == "Pending").ToList();

                return View("~/Areas/Productions/Views/DrugOrder.cshtml", orders);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Products = new List<Product>();
                ViewBag.Managers = new List<UserAccount>();
                ViewBag.PendingRequests = new List<ProductionRequest>();
                return View("~/Areas/Productions/Views/DrugOrder.cshtml", new List<ProductionRequest>());
            }
        }

        public IActionResult MedicineOrder() => RedirectToAction("DrugOrder");

        /// <summary>
        /// API lấy chi tiết đơn đặt hàng thuốc và công thức BOM
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDrugOrderDetail(int id)
        {
            try
            {
                var order = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .Include(p => p.Manager)
                    .FirstOrDefaultAsync(p => p.RequestId == id);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng trong hệ thống." });

                var receipts = await _context.ProductReceipts
                    .AsNoTracking()
                    .Include(p => p.Details)
                    .Where(p => p.RequestId == id && string.IsNullOrWhiteSpace(p.ReceiptCode))
                    .ToListAsync();

                int producedQty = receipts.SelectMany(r => r.Details).Sum(d => d.Quantity);

                var boms = await _context.Boms
                    .AsNoTracking()
                    .Include(b => b.Ingredient)
                    .Where(b => b.ProductId == order.ProductId)
                    .Select(b => new
                    {
                        bomCode = b.BomCode,
                        bomName = b.BomName,
                        ingredientName = b.Ingredient != null ? b.Ingredient.IngrName : "Hoạt chất tiêu chuẩn",
                        unit = b.Ingredient != null ? b.Ingredient.Unit : "kg",
                        itemQuantity = b.ItemQuantity,
                        materialQuantity = b.MaterialQuantity,
                        stock = b.Ingredient != null ? b.Ingredient.Quantity : null
                    })
                    .ToListAsync();

                var reqDisplayCode = order.ProductionDisplayCode;
                var lsxDisplayCode = order.ProductionDisplayCode;

                return Json(new
                {
                    success = true,
                    orderId = order.RequestId,
                    orderCode = reqDisplayCode,
                    requestCode = reqDisplayCode,
                    productionOrderCode = lsxDisplayCode,
                    createdDate = order.CreatedDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                    status = order.Status,
                    managerName = order.Manager?.FullName ?? "Chưa cập nhật",
                    managerPhone = order.Manager?.Phone ?? "—",
                    productId = order.ProductId,
                    productName = order.Product?.ProductName ?? "Sản phẩm thuốc",
                    productUnit = order.Product?.Unit ?? "Hộp",
                    cost = order.Product?.Cost,
                    quantityRequired = order.QuantityRequired,
                    producedQuantity = producedQty,
                    percent = order.QuantityRequired > 0 ? (int)Math.Min(100, Math.Round((double)producedQty * 100 / order.QuantityRequired)) : 0,
                    description = order.Product?.Description ?? "Đơn sản xuất thuốc tiếp nhận từ phiếu yêu cầu của kho",
                    boms
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API kiểm tra dự toán NVL tiêu hao trong kho sản xuất (WIP) khi ghi nhận sản xuất
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PreviewProductionMaterials(int requestId, int quantityToRecord)
        {
            if (requestId <= 0)
                return BadRequest(new { success = false, message = "Mã đơn hàng không hợp lệ." });

            try
            {
                var order = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .FirstOrDefaultAsync(p => p.RequestId == requestId);

                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

                var receipts = await _context.ProductReceipts
                    .AsNoTracking()
                    .Include(p => p.Details)
                    .Where(p => p.RequestId == requestId && string.IsNullOrWhiteSpace(p.ReceiptCode))
                    .ToListAsync();

                int currentProduced = receipts.SelectMany(r => r.Details)
                    .Where(d => d.ProductId == order.ProductId)
                    .Sum(d => d.Quantity);

                int remainingAllowed = Math.Max(0, order.QuantityRequired - currentProduced);
                int batchQty = Math.Max(0, quantityToRecord);
                bool exceedsRequired = (batchQty > remainingAllowed);
                int newTotalProduced = currentProduced + batchQty;
                int percent = order.QuantityRequired > 0 ? (int)Math.Min(100, Math.Round((double)newTotalProduced * 100 / order.QuantityRequired)) : 0;

                var bomItems = await _context.Boms
                    .AsNoTracking()
                    .Include(b => b.Ingredient)
                    .Where(b => b.ProductId == order.ProductId)
                    .ToListAsync();

                var wipRows = await _context.WipInventories
                    .AsNoTracking()
                    .Where(w => w.RequestId == requestId && w.IngrId.HasValue)
                    .ToListAsync();

                var wipDict = wipRows
                    .GroupBy(w => w.IngrId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(w => w.Quantity ?? 0m));

                var finishedGoodsInWip = await _context.WipInventories
                    .AsNoTracking()
                    .Where(w => w.RequestId == requestId && w.ProductId == order.ProductId && w.IngrId == null)
                    .SumAsync(w => w.Quantity ?? 0m);

                var materials = new List<object>();
                bool allMaterialsSufficient = true;
                int missingCount = 0;

                foreach (var b in bomItems)
                {
                    if (b.Ingredient == null) continue;
                    var ingr = b.Ingredient;
                    var ingrUnit = ingr.Unit?.Trim() ?? "kg";
                    bool isDiscrete = BomUnitQuantity.DiscreteUnits.Contains(ingrUnit);

                    decimal norm = (b.ItemQuantity > 0) ? (b.MaterialQuantity / b.ItemQuantity) : 0m;
                    decimal requiredForBatch = (batchQty > 0 && b.ItemQuantity > 0 && b.MaterialQuantity > 0)
                        ? BomUnitQuantity.Required(b.MaterialQuantity, b.ItemQuantity, batchQty, isDiscrete)
                        : 0m;

                    decimal wipStock = wipDict.TryGetValue(ingr.IngrId, out var ws) ? ws : 0m;
                    decimal shortage = Math.Max(0m, requiredForBatch - wipStock);
                    bool isSufficient = (wipStock >= requiredForBatch);

                    if (!isSufficient && batchQty > 0)
                    {
                        allMaterialsSufficient = false;
                        missingCount++;
                    }

                    materials.Add(new
                    {
                        ingrId = ingr.IngrId,
                        ingrName = ingr.IngrName,
                        unit = ingrUnit,
                        norm = norm,
                        requiredForBatch = requiredForBatch,
                        wipStock = wipStock,
                        shortage = shortage,
                        isSufficient = isSufficient
                    });
                }

                bool canProduce = !exceedsRequired && allMaterialsSufficient && bomItems.Any() && batchQty > 0;

                return Ok(new
                {
                    success = true,
                    requestId = order.RequestId,
                    orderCode = order.ProductionDisplayCode,
                    productId = order.ProductId,
                    productName = order.Product?.ProductName ?? "Sản phẩm thuốc",
                    productUnit = order.Product?.Unit ?? "Hộp",
                    quantityRequired = order.QuantityRequired,
                    currentProduced = currentProduced,
                    remainingAllowed = remainingAllowed,
                    quantityToRecord = batchQty,
                    newTotalProduced = newTotalProduced,
                    percent = percent,
                    exceedsRequired = exceedsRequired,
                    finishedGoodsInWip = finishedGoodsInWip,
                    canProduce = canProduce,
                    missingCount = missingCount,
                    hasBom = bomItems.Any(),
                    materials = materials
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra dự toán NVL tiêu hao cho đơn {RequestId}", requestId);
                return BadRequest(new { success = false, message = "Không thể kiểm tra dự toán NVL." });
            }
        }

        /// <summary>
        /// API ghi nhận sản lượng & sản xuất thành phẩm:
        /// - Ghi nhận bao nhiêu là sản xuất và trừ NVL bấy nhiêu
        /// - Không được vượt quá số lượng yêu cầu của đơn
        /// - Khấu trừ NVL trong kho sản xuất (wip_inventory) theo BOM
        /// - Đưa thành phẩm sản xuất được vào kho sản xuất chờ ngày bàn giao
        /// - Cập nhật phiếu bàn giao thành phẩm (productreceipt) và trạng thái đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkProducedGoods([FromBody] MarkProducedGoodsRequest req)
        {
            // 1. Kiểm tra tính hợp lệ của Model
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg));

                return BadRequest(new
                {
                    success = false,
                    message = !string.IsNullOrWhiteSpace(firstError)
                        ? firstError
                        : "Dữ liệu sản lượng hoặc mã đơn hàng không đúng định dạng hoặc vượt quá giới hạn cho phép."
                });
            }

            if (req == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            if (req.RequestId <= 0)
            {
                return BadRequest(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            int batchQty = req.QuantityToProduce ?? req.ProducedQuantity;
            if (batchQty <= 0 || batchQty > 100000000 || req.OperationKey == Guid.Empty)
                return BadRequest(new { success = false, message = "Sản lượng phải từ 1 đến 100.000.000 và phải có mã thao tác. Vui lòng tải lại trang nếu cần." });

            try
            {
                // 2. Xác thực người dùng
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể ghi nhận sản lượng thành phẩm."
                    });
                }

                // Khóa đơn trước khi đọc tiến độ để các lần ghi nhận cùng đơn chạy tuần tự.
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var orders = await _context.ProductionRequests
                    .FromSqlInterpolated($"SELECT * FROM public.productionrequest WHERE requestid = {req.RequestId} FOR UPDATE")
                    .ToListAsync();
                var order = orders.SingleOrDefault();
                if (order == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng trong hệ thống." });
                }

                var previous = await _context.ProductionLogs.AsNoTracking().FirstOrDefaultAsync(l => l.OperationKey == req.OperationKey);
                if (previous != null)
                {
                    if (previous.RequestId != req.RequestId || previous.ProductId != order.ProductId || previous.ProducedQuantity != batchQty || previous.CreatedBy != currentUser.UserID || previous.Note != req.Note)
                        return Conflict(new { success = false, message = "Mã thao tác đã được sử dụng cho nội dung khác. Vui lòng tải lại trang." });
                    return Ok(new { success = true, message = "Lần sản xuất này đã được lưu trước đó. Không ghi nhận thêm lần nữa.", recordedQuantity = previous.ProducedQuantity, status = order.Status });
                }

                // 4. Kiểm tra tính toàn vẹn của đơn hàng
                if (order.QuantityRequired <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Số lượng yêu cầu của đơn hàng không hợp lệ ({order.QuantityRequired}). Không thể ghi nhận sản lượng."
                    });
                }

                if (order.ProductId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Đơn hàng chưa có thông tin sản phẩm thuốc hợp lệ. Vui lòng bổ sung hoặc sửa sản phẩm của đơn trước khi ghi nhận thành phẩm."
                    });
                }

                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == order.ProductId);
                if (product == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Sản phẩm thuốc (Mã ID: {order.ProductId}) của đơn hàng không tồn tại trong hệ thống. Vui lòng kiểm tra lại."
                    });
                }

                // 5. Kiểm tra trạng thái đơn: Không cho phép ghi nhận thêm khi đơn đã Completed
                if (string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Đơn hàng đã hoàn thành đầy đủ số lượng yêu cầu (Completed). Không thể ghi nhận thêm qua chức năng này."
                    });
                }

                // 6. Tra cứu sản lượng đã ghi nhận trước đó (loại trừ các phiếu bàn giao kho riêng biệt)
                var existingReceipts = await _context.ProductReceipts
                    .Include(p => p.Details)
                    .Where(p => p.RequestId == req.RequestId && string.IsNullOrWhiteSpace(p.ReceiptCode))
                    .ToListAsync();

                var receipt = existingReceipts.FirstOrDefault();

                int oldProduced = existingReceipts.SelectMany(r => r.Details)
                    .Where(d => d.ProductId == order.ProductId)
                    .Sum(d => d.Quantity);

                // Số lượng ghi nhận lần này (tui ghi nhận bao nhiêu là nó sản xuất ra bấy nhiêu)


                if (batchQty <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Số lượng ghi nhận phải lớn hơn 0."
                    });
                }

                // 7. QUY TẮC CỐT LÕI: PHẦN GHI NHẬN KHÔNG ĐƯỢC VƯỢT QUÁ SỐ LƯỢNG YÊU CẦU
                int remainingAllowed = Math.Max(0, order.QuantityRequired - oldProduced);
                if (batchQty > remainingAllowed)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Số lượng ghi nhận ({batchQty:N0} {product.Unit ?? "Hộp"}) vượt quá số lượng yêu cầu còn lại của đơn hàng! Đơn này chỉ còn thiếu {remainingAllowed:N0} {product.Unit ?? "Hộp"}. Phần ghi nhận không được vượt quá số lượng yêu cầu ({order.QuantityRequired:N0} {product.Unit ?? "Hộp"})."
                    });
                }

                int newTotalProduced = oldProduced + batchQty;

                // 8. Tính toán định mức BOM và kiểm tra tồn kho sản xuất (WIP) cho đúng số lượng ghi nhận lần này
                var bomItems = await _context.Boms
                    .Include(b => b.Ingredient)
                    .Where(b => b.ProductId == order.ProductId)
                    .ToListAsync();

                if (!bomItems.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Sản phẩm '{product.ProductName}' chưa có công thức định mức BOM trong hệ thống. Không thể tính toán tiêu hao NVL để ghi nhận sản xuất."
                    });
                }

                if (bomItems.Any(b => b.ItemQuantity <= 0 || b.MaterialQuantity <= 0 || b.Ingredient == null))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Công thức định mức BOM của sản phẩm '{product.ProductName}' không hợp lệ (thiếu nguyên liệu hoặc số lượng <= 0). Vui lòng kiểm tra lại BOM."
                    });
                }

                if (bomItems.Select(b => string.IsNullOrWhiteSpace(b.BomCode) ? b.BomName : b.BomCode).Distinct().Count() > 1
                    || bomItems.GroupBy(b => b.IngrId).Any(g => g.Count() > 1))
                    return BadRequest(new { success = false, message = "BOM có nhiều công thức hoặc nguyên liệu trùng. Vui lòng xác định một công thức cho sản phẩm trước khi ghi nhận." });

                // Lấy tồn kho sản xuất (WIP) cho đơn/lệnh sản xuất này
                var lockedWip = await _context.WipInventories
                    .FromSqlInterpolated($"SELECT * FROM public.wip_inventory WHERE requestid = {req.RequestId} ORDER BY wipid FOR UPDATE")
                    .ToListAsync();
                var wipRawMaterials = lockedWip.Where(w => w.IngrId.HasValue).ToList();

                var shortages = new List<string>();
                var deductions = new List<(WipInventory WipItem, decimal DeductQty, string IngrName, string Unit)>();

                foreach (var bom in bomItems)
                {
                    var ingr = bom.Ingredient!;
                    var ingrUnit = ingr.Unit?.Trim() ?? "kg";
                    bool isDiscrete = BomUnitQuantity.DiscreteUnits.Contains(ingrUnit);
                    decimal requiredQty = BomUnitQuantity.Required(bom.MaterialQuantity, bom.ItemQuantity, batchQty, isDiscrete);

                    var wipItems = wipRawMaterials.Where(w => w.IngrId == bom.IngrId).ToList();
                    decimal availableWip = wipItems.Sum(w => w.Quantity ?? 0m);

                    if (availableWip < requiredQty)
                    {
                        decimal missing = requiredQty - availableWip;
                        shortages.Add($"• {ingr.IngrName}: Cần {requiredQty:N2} {ingrUnit}, Kho sản xuất có {availableWip:N2} {ingrUnit} (Thiếu {missing:N2} {ingrUnit})");
                    }
                    else
                    {
                        var toDeduct = requiredQty;
                        foreach (var wipItem in wipItems)
                        {
                            var take = Math.Min(Math.Max(0m, wipItem.Quantity ?? 0m), toDeduct);
                            if (take > 0) deductions.Add((wipItem, take, ingr.IngrName, ingrUnit));
                            toDeduct -= take;
                            if (toDeduct == 0) break;
                        }
                    }
                }

                if (shortages.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Kho sản xuất không đủ nguyên vật liệu để ghi nhận sản xuất {batchQty:N0} {product.Unit ?? "Hộp"}!\n" +
                                  string.Join("\n", shortages) +
                                  "\n\nVui lòng lập phiếu yêu cầu xuất NVL sang kho sản xuất (MRP) trước khi ghi nhận."
                    });
                }

                // 9. Xác định trạng thái nghiệp vụ mới:
                // Nếu đã đủ số lượng yêu cầu của đơn -> Completed, ngược lại In Production
                string newStatus = newTotalProduced >= order.QuantityRequired ? "Completed" : "In Production";

                // 10. Lưu đồng bộ toàn bộ lần ghi nhận trong một Transaction duy nhất
                try
                {
                    order.Status = newStatus;

                    // A. Khấu trừ NVL trong kho sản xuất (WIP)
                    foreach (var item in deductions)
                    {
                        item.WipItem.Quantity = (item.WipItem.Quantity ?? 0m) - item.DeductQty;
                        item.WipItem.LastUpdated = DateTime.UtcNow;
                    }

                    // B. Đưa đúng số lượng thành phẩm sản xuất được (batchQty) vào kho sản xuất (wip_inventory với ProductId) chờ ngày bàn giao
                    var wipProduct = lockedWip.FirstOrDefault(w => w.ProductId == order.ProductId && w.IngrId == null);

                    if (wipProduct == null)
                    {
                        wipProduct = new WipInventory
                        {
                            RequestId = req.RequestId,
                            ProductId = order.ProductId,
                            IngrId = null,
                            Quantity = batchQty,
                            LastUpdated = DateTime.UtcNow
                        };
                        _context.WipInventories.Add(wipProduct);
                    }
                    else
                    {
                        wipProduct.Quantity = (wipProduct.Quantity ?? 0m) + batchQty;
                        wipProduct.LastUpdated = DateTime.UtcNow;
                    }

                    // C. Cập nhật phiếu bàn giao thành phẩm (ProductReceipt & Detail)
                    if (receipt == null)
                    {
                        receipt = new ProductReceipt
                        {
                            RequestId = req.RequestId,
                            RequestedBy = currentUser.UserID,
                            ApprovedBy = null,
                            CreatedDate = DateTime.UtcNow,
                            Status = newStatus
                        };

                        var detail = new ProductReceiptDetail
                        {
                            ProductId = order.ProductId,
                            Quantity = newTotalProduced,
                            Status = newStatus,
                            Note = !string.IsNullOrWhiteSpace(req.Note)
                                ? req.Note
                                : $"Ghi nhận sản xuất {batchQty:N0} {product.Unit ?? "Hộp"} cho {order.ProductionDisplayCode}",
                            CreatedDate = DateTime.UtcNow
                        };

                        receipt.Details.Add(detail);
                        _context.ProductReceipts.Add(receipt);
                    }
                    else
                    {
                        receipt.Status = newStatus;

                        var detail = receipt.Details.FirstOrDefault(d => d.ProductId == order.ProductId);

                        if (detail != null)
                        {
                            detail.Quantity += batchQty;
                            detail.Status = newStatus;
                            if (!string.IsNullOrWhiteSpace(req.Note))
                            {
                                detail.Note = req.Note;
                            }
                        }
                        else
                        {
                            receipt.Details.Add(new ProductReceiptDetail
                            {
                                ProductReceiptId = receipt.ProductReceiptId,
                                ProductId = order.ProductId,
                                Quantity = newTotalProduced,
                                Status = newStatus,
                                Note = !string.IsNullOrWhiteSpace(req.Note)
                                    ? req.Note
                                    : $"Ghi nhận sản xuất {batchQty:N0} {product.Unit ?? "Hộp"} cho {order.ProductionDisplayCode}",
                                CreatedDate = DateTime.UtcNow
                            });
                        }
                    }

                    var recordedAt = DateTimeOffset.UtcNow;
                    DateOnly? expiry;
                    try { expiry = ProductionExpiry.Calculate(recordedAt, product.ShelfLifeMonths); }
                    catch (ArgumentOutOfRangeException)
                    {
                        return BadRequest(new { success = false, message = "Thời hạn sử dụng quá lớn. Vui lòng cập nhật lại trong BOM." });
                    }
                    _context.ProductionLogs.Add(new ProductionLog
                    {
                        RequestId = order.RequestId,
                        ProductId = order.ProductId,
                        ProducedQuantity = batchQty,
                        ProducedAt = recordedAt,
                        ExpiredDate = expiry,
                        CreatedAt = recordedAt,
                        CreatedBy = currentUser.UserID,
                        OperationKey = req.OperationKey,
                        Note = req.Note
                    });

                    // Lưu toàn bộ thay đổi cùng lúc
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    string successMessage = $"Đã ghi nhận sản xuất thành công {batchQty:N0} {product.Unit ?? "Hộp"} cho đơn {order.ProductionDisplayCode}! Đã trừ NVL tiêu hao trong kho sản xuất và đưa {batchQty:N0} {product.Unit ?? "Hộp"} vào kho sản xuất chờ ngày bàn giao. (Tổng tiến độ: {newTotalProduced:N0}/{order.QuantityRequired:N0} {product.Unit ?? "Hộp"})";

                    return Ok(new
                    {
                        success = true,
                        message = successMessage,
                        status = order.Status,
                        recordedQuantity = batchQty,
                        totalProduced = newTotalProduced
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi lưu đồng bộ sản xuất cho đơn {RequestId}", req.RequestId);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Đã xảy ra lỗi khi lưu kết quả ghi nhận sản lượng vào hệ thống. Vui lòng thử lại sau."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi xử lý sản xuất cho đơn {RequestId}", req.RequestId);
                return BadRequest(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi thực hiện ghi nhận sản lượng thành phẩm. Vui lòng thử lại sau."
                });
            }
        }

        /// <summary>
        /// API tiếp nhận phiếu sản xuất mới từ kho
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDrugOrder([FromBody] CreateDrugOrderRequest req)
        {
            if (req == null || req.ProductId <= 0 || req.QuantityRequired <= 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn sản phẩm và nhập số lượng hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể tiếp nhận đơn đặt hàng thuốc."
                    });
                }

                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == req.ProductId);
                if (product == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Sản phẩm thuốc (ID: {req.ProductId}) không tồn tại trong hệ thống. Vui lòng kiểm tra lại."
                    });
                }

                var newOrder = new ProductionRequest
                {
                    ProductId = req.ProductId,
                    QuantityRequired = req.QuantityRequired,
                    ManagerId = currentUser.UserID,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    if (newOrder.RequestId > 0)
                    {
                        // Recover a successful commit whose acknowledgement was lost.
                        var committed = await _context.ProductionRequests.AsNoTracking()
                            .AnyAsync(r => r.RequestId == newOrder.RequestId && r.RequestCode == newOrder.RequestCode
                                && r.RequestCode != null);
                        if (committed) return;
                        _context.Entry(newOrder).State = EntityState.Detached;
                        newOrder.RequestId = 0;
                        newOrder.RequestCode = null;
                    }

                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    _context.ProductionRequests.Add(newOrder);
                    await _context.SaveChangesAsync();

                    var baseCode = $"LSX-{newOrder.RequestId}";
                    var code = baseCode;
                    var suffix = 1;
                    while (await _context.ProductionRequests.AsNoTracking()
                        .AnyAsync(r => r.RequestId != newOrder.RequestId && r.RequestCode == code))
                        code = $"{baseCode}-{suffix++}";
                    newOrder.RequestCode = code;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                return Ok(new
                {
                    success = true,
                    message = $"Đã tạo phiếu yêu cầu sản xuất {newOrder.RequestCode} thành công! Phiếu đang chờ tiếp nhận.",
                    orderId = newOrder.RequestId,
                    orderCode = newOrder.ProductionDisplayCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tiếp nhận đơn đặt hàng thuốc mới");
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi trong quá trình tiếp nhận đơn hàng. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// API lấy danh sách các phiếu sản xuất từ kho đang chờ phân xưởng tiếp nhận
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingWarehouseRequests()
        {
            try
            {
                var pendingRequests = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .Include(p => p.Manager)
                    .Where(p => p.Status == "Pending")
                    .OrderByDescending(p => p.RequestId)
                    .Select(p => new
                    {
                        requestId = p.RequestId,
                        requestCode = p.ProductionDisplayCode,
                        orderCode = p.ProductionDisplayCode,
                        productId = p.ProductId,
                        productName = p.Product != null ? p.Product.ProductName : "Sản phẩm thuốc dược dụng",
                        productUnit = p.Product != null ? p.Product.Unit : "Hộp",
                        quantityRequired = p.QuantityRequired,
                        createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("dd/MM/yyyy") : "—",
                        managerName = p.Manager != null ? p.Manager.FullName : "Chưa cập nhật",
                        status = p.Status,
                        priority = p.QuantityRequired >= 600 ? "Khẩn cấp (Quy tắc số lượng: ≥ 600)" : "Tiêu chuẩn",
                        note = (string?)null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = pendingRequests.Count,
                    data = pendingRequests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phiếu sản xuất từ kho");
                return BadRequest(new { success = false, message = "Không thể tải danh sách phiếu chờ tiếp nhận." });
            }
        }

        /// <summary>
        /// API duyệt tiếp nhận một phiếu sản xuất từ kho vào kế hoạch sản xuất
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptWarehouseRequest([FromBody] AcceptWarehouseOrderRequest req)
        {
            if (req == null || req.RequestId <= 0)
                return BadRequest(new { success = false, message = "Mã phiếu không hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể tiếp nhận phiếu yêu cầu sản xuất từ kho."
                    });
                }

                var order = await _context.ProductionRequests.AsNoTracking().FirstOrDefaultAsync(p => p.RequestId == req.RequestId);
                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu yêu cầu trong cơ sở dữ liệu." });

                // Update both fields atomically; a concurrent acceptance must not overwrite the recipient.
                var accepted = await _context.ProductionRequests
                    .Where(p => p.RequestId == req.RequestId && p.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Status, "In Production")
                        .SetProperty(p => p.AssignedTo, (int?)currentUser.UserID));
                if (accepted == 0)
                    return Conflict(new { success = false, message = "Phiếu không còn ở trạng thái chờ tiếp nhận. Vui lòng tải lại danh sách." });

                return Ok(new
                {
                    success = true,
                    message = $"Đã tiếp nhận thành công phiếu {order.ProductionDisplayCode} từ kho thành phẩm vào quy trình sản xuất!",
                    requestId = order.RequestId,
                    newStatus = "In Production",
                    assignedTo = currentUser.UserID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tiếp nhận phiếu yêu cầu sản xuất từ kho {RequestId}", req.RequestId);
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi tiếp nhận phiếu kho. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// API tiếp nhận toàn bộ các phiếu kho đang chờ duyệt
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptAllWarehouseRequests()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chưa xác định được danh tính người dùng (chưa đăng nhập). Không thể tiếp nhận các phiếu yêu cầu từ kho."
                    });
                }

                // The Pending condition is checked by the database during the update.
                var acceptedCount = await _context.ProductionRequests
                    .Where(p => p.Status == "Pending")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Status, "In Production")
                        .SetProperty(p => p.AssignedTo, (int?)currentUser.UserID));

                return Ok(new
                {
                    success = true,
                    count = acceptedCount,
                    assignedTo = currentUser.UserID,
                    message = acceptedCount == 0
                        ? "Không có phiếu nào đang chờ tiếp nhận."
                        : $"Đã tiếp nhận toàn bộ {acceptedCount} phiếu yêu cầu sản xuất từ kho vào phân xưởng!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tiếp nhận toàn bộ phiếu kho");
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi tiếp nhận toàn bộ phiếu kho. Vui lòng thử lại sau." });
            }
        }

        #endregion
    }
}
