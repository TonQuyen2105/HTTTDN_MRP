using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;
using httt.Services;

namespace httt.Areas.Productions.Controllers
{
    [Area("Productions")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MaterialRequirementService _mrpService;

        public HomeController(ApplicationDbContext context, MaterialRequirementService mrpService)
        {
            _context = context;
            _mrpService = mrpService;
        }

        private async Task<UserAccount> GetCurrentUserAsync()
        {
            return await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.Username == "minhtai")
                ?? await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync()
                ?? new UserAccount { UserID = 11, FullName = "DS. Nguyễn Minh Tài", Role = "Admin" };
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var requests = await _context.ProductionRequests.AsNoTracking().ToListAsync();
                var ingredients = await _context.Ingredients.AsNoTracking().ToListAsync();
                var products = await _context.Products.AsNoTracking().OrderByDescending(p => p.Quantity).Take(5).ToListAsync();

                ViewBag.TotalRequests = requests.Count;
                ViewBag.PendingRequests = requests.Count(r => r.Status != "Completed");
                ViewBag.TotalQuantityRequired = requests.Sum(r => (long)r.QuantityRequired);
                ViewBag.TotalIngredients = ingredients.Count;
                ViewBag.TotalStock = ingredients.Sum(i => (long)i.Quantity);
                ViewBag.TopProducts = products;
                ViewBag.StatusCounts = requests.GroupBy(r => r.Status ?? "Pending")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();
            }
            catch
            {
                ViewBag.TotalRequests = 5;
                ViewBag.PendingRequests = 4;
                ViewBag.TotalQuantityRequired = 1000;
                ViewBag.TotalIngredients = 8;
                ViewBag.TotalStock = 890;
            }

            return View("~/Areas/Productions/Views/Overview.cshtml");
        }

        public IActionResult Overview() => RedirectToAction("Index");

        public async Task<IActionResult> BomList()
        {
            try
            {
                var boms = await _context.Boms
                    .AsNoTracking()
                    .Include(b => b.Product)
                    .Include(b => b.Ingredient)
                    .OrderBy(b => b.BomId)
                    .ToListAsync();
                return View("~/Areas/Productions/Views/BomList.cshtml", boms);
            }
            catch
            {
                return View("~/Areas/Productions/Views/BomList.cshtml", new List<Bom>());
            }
        }

        public async Task<IActionResult> BomCreate()
        {
            try
            {
                ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
                ViewBag.Ingredients = await _context.Ingredients.AsNoTracking().ToListAsync();
            }
            catch {}
            return View("~/Areas/Productions/Views/BomCreate.cshtml");
        }

        public async Task<IActionResult> BomEdit(int? id)
        {
            try
            {
                ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
                ViewBag.Ingredients = await _context.Ingredients.AsNoTracking().ToListAsync();
            }
            catch {}
            return View("~/Areas/Productions/Views/BomEdit.cshtml");
        }

        public async Task<IActionResult> ProductionOrder()
        {
            try
            {
                var orders = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .Include(p => p.Manager)
                    .OrderByDescending(p => p.RequestId)
                    .ToListAsync();

                try
                {
                    ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
                    ViewBag.Boms = await _context.Boms.AsNoTracking().Include(b => b.Product).ToListAsync();
                }
                catch
                {
                    ViewBag.Products = new List<Product>();
                    ViewBag.Boms = new List<Bom>();
                }

                return View("~/Areas/Productions/Views/ProductionOrder.cshtml", orders);
            }
            catch
            {
                ViewBag.Products = new List<Product>();
                return View("~/Areas/Productions/Views/ProductionOrder.cshtml", new List<ProductionRequest>());
            }
        }

        public async Task<IActionResult> MaterialRequisitionList()
        {
            try
            {
                var requisitions = await _context.MaterialReceipts
                    .AsNoTracking()
                    .Include(m => m.Requester)
                    .Include(m => m.Approver)
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
                        return View("~/Areas/Productions/Views/MaterialRequisitionCreate.cshtml", receipt);
                    }
                }
            }
            catch {}
            return View("~/Areas/Productions/Views/MaterialRequisitionCreate.cshtml", (MaterialReceipt?)null);
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
                    ? $"LSX-GMP-0{req.RequestId}"
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
                    requestCode = $"YCSX-{req.RequestId:D4}",
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
                    existingReceiptCode = existingSlip != null ? $"XK-DPH-{existingSlip.ExpReceiptId:D4}" : null,
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
                var (success, message, receiptId, receiptCode) = await _mrpService.CreateRequisitionSlipAsync(req, currentUser.UserID);

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
                return BadRequest(new { success = false, message = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        #endregion

        public async Task<IActionResult> ProductionStatisticsList()
        {
            try
            {
                var requests = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .OrderByDescending(p => p.RequestId)
                    .ToListAsync();

                return View("~/Areas/Productions/Views/ProductionStatisticsList.cshtml", requests);
            }
            catch
            {
                return View("~/Areas/Productions/Views/ProductionStatisticsList.cshtml", new List<ProductionRequest>());
            }
        }

        public async Task<IActionResult> ProductionStatisticsCreate()
        {
            try
            {
                ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
                ViewBag.Orders = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .OrderByDescending(p => p.RequestId)
                    .ToListAsync();
            }
            catch {}
            return View("~/Areas/Productions/Views/ProductionStatisticsCreate.cshtml");
        }

        public async Task<IActionResult> ProductionStatisticsDetail(int? id)
        {
            try
            {
                ViewBag.Products = await _context.Products.AsNoTracking().ToListAsync();
                if (id.HasValue)
                {
                    var req = await _context.ProductionRequests
                        .AsNoTracking()
                        .Include(p => p.Product)
                        .Include(p => p.Manager)
                        .FirstOrDefaultAsync(p => p.RequestId == id.Value);

                    if (req != null)
                    {
                        return View("~/Areas/Productions/Views/ProductionStatisticsDetail.cshtml", req);
                    }
                }

                // Nếu không truyền ID, lấy phiếu đầu tiên làm mẫu chi tiết
                var firstReq = await _context.ProductionRequests
                    .AsNoTracking()
                    .Include(p => p.Product)
                    .Include(p => p.Manager)
                    .FirstOrDefaultAsync();

                return View("~/Areas/Productions/Views/ProductionStatisticsDetail.cshtml", firstReq);
            }
            catch
            {
                return View("~/Areas/Productions/Views/ProductionStatisticsDetail.cshtml", (ProductionRequest?)null);
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

        public async Task<IActionResult> HandoverList()
        {
            try
            {
                var receipts = await _context.ProductReceipts
                    .AsNoTracking()
                    .Include(p => p.Requester)
                    .Include(p => p.Approver)
                    .Include(p => p.Details)
                    .ThenInclude(d => d.Product)
                    .OrderByDescending(p => p.ProductReceiptId)
                    .ToListAsync();

                return View("~/Areas/Productions/Views/HandoverList.cshtml", receipts);
            }
            catch
            {
                return View("~/Areas/Productions/Views/HandoverList.cshtml", new List<ProductReceipt>());
            }
        }

        public async Task<IActionResult> HandoverDetail(int? id)
        {
            try
            {
                var receipt = await _context.ProductReceipts
                    .AsNoTracking()
                    .Include(p => p.Requester)
                    .Include(p => p.Approver)
                    .Include(p => p.Details)
                    .ThenInclude(d => d.Product)
                    .OrderByDescending(p => p.ProductReceiptId)
                    .FirstOrDefaultAsync(p => id == null || p.ProductReceiptId == id);

                return View("~/Areas/Productions/Views/HandoverDetail.cshtml", receipt);
            }
            catch
            {
                return View("~/Areas/Productions/Views/HandoverDetail.cshtml", (ProductReceipt?)null);
            }
        }

        public async Task<IActionResult> UserProfile()
        {
            try
            {
                var user = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.Username == "minhtai")
                           ?? await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync();

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
                    .Where(p => p.RequestId.HasValue)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    var orderReceipts = receipts.Where(r => r.RequestId == order.RequestId).ToList();
                    var produced = orderReceipts.SelectMany(r => r.Details).Sum(d => d.Quantity);
                    if (produced == 0 && order.Status == "Completed")
                    {
                        produced = order.QuantityRequired;
                    }
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
                    .Where(p => p.RequestId == id)
                    .ToListAsync();

                int producedQty = receipts.SelectMany(r => r.Details).Sum(d => d.Quantity);
                if (producedQty == 0 && order.Status == "Completed")
                {
                    producedQty = order.QuantityRequired;
                }

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
                        stock = b.Ingredient != null ? b.Ingredient.Quantity : 0
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    orderId = order.RequestId,
                    orderCode = $"ĐH-KHO-{order.RequestId:D4}",
                    requestCode = $"YCSX-{order.RequestId:D4}",
                    productionOrderCode = $"LSX-GMP-0{order.RequestId}",
                    createdDate = order.CreatedDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                    status = order.Status,
                    managerName = order.Manager?.FullName ?? "DS. Quản lý kho",
                    managerPhone = order.Manager?.Phone ?? "0912 345 678",
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
        /// API đánh dấu thành phẩm sản xuất được của từng đơn
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkProducedGoods([FromBody] MarkProducedGoodsRequest req)
        {
            if (req == null || req.RequestId <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });

            try
            {
                var order = await _context.ProductionRequests.FirstOrDefaultAsync(p => p.RequestId == req.RequestId);
                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

                // Cập nhật trạng thái
                if (req.IsCompleted || req.ProducedQuantity >= order.QuantityRequired)
                {
                    order.Status = "Completed";
                }
                else if (req.ProducedQuantity > 0)
                {
                    order.Status = "In Production";
                }
                else
                {
                    order.Status = "Pending";
                }

                // Ghi nhận thành phẩm vào productreceipt & productreceiptdetail
                var receipt = await _context.ProductReceipts
                    .Include(p => p.Details)
                    .FirstOrDefaultAsync(p => p.RequestId == req.RequestId);

                var currentUser = await GetCurrentUserAsync();

                if (receipt == null)
                {
                    receipt = new ProductReceipt
                    {
                        RequestId = req.RequestId,
                        RequestedBy = order.ManagerId ?? currentUser.UserID,
                        ApprovedBy = currentUser.UserID,
                        CreatedDate = DateTime.UtcNow,
                        Status = order.Status
                    };
                    _context.ProductReceipts.Add(receipt);
                    await _context.SaveChangesAsync();

                    var detail = new ProductReceiptDetail
                    {
                        ProductReceiptId = receipt.ProductReceiptId,
                        ProductId = order.ProductId ?? 1,
                        Quantity = req.ProducedQuantity,
                        Status = order.Status,
                        Note = !string.IsNullOrWhiteSpace(req.Note)
                            ? req.Note
                            : $"Ghi nhận thành phẩm sản xuất cho đơn ĐH-KHO-{order.RequestId:D4}",
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.ProductReceiptDetails.Add(detail);
                }
                else
                {
                    receipt.Status = order.Status;
                    var detail = receipt.Details.FirstOrDefault();
                    if (detail != null)
                    {
                        detail.Quantity = req.ProducedQuantity;
                        detail.Status = order.Status;
                        if (!string.IsNullOrWhiteSpace(req.Note))
                            detail.Note = req.Note;
                    }
                    else
                    {
                        receipt.Details.Add(new ProductReceiptDetail
                        {
                            ProductReceiptId = receipt.ProductReceiptId,
                            ProductId = order.ProductId ?? 1,
                            Quantity = req.ProducedQuantity,
                            Status = order.Status,
                            Note = !string.IsNullOrWhiteSpace(req.Note)
                                ? req.Note
                                : $"Ghi nhận thành phẩm sản xuất cho đơn ĐH-KHO-{order.RequestId:D4}",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã cập nhật thành phẩm sản xuất đơn ĐH-KHO-{order.RequestId:D4} thành công!",
                    status = order.Status,
                    producedQuantity = req.ProducedQuantity
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        /// <summary>
        /// API tiếp nhận phiếu sản xuất mới từ kho
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDrugOrder([FromBody] CreateDrugOrderRequest req)
        {
            if (req == null || req.ProductId <= 0 || req.QuantityRequired <= 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn sản phẩm và nhập số lượng hợp lệ." });

            try
            {
                var currentUser = await GetCurrentUserAsync();
                var newOrder = new ProductionRequest
                {
                    ProductId = req.ProductId,
                    QuantityRequired = req.QuantityRequired,
                    ManagerId = req.ManagerId ?? currentUser.UserID,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                _context.ProductionRequests.Add(newOrder);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã tiếp nhận thành công đơn đặt hàng ĐH-KHO-{newOrder.RequestId:D4} từ phiếu sản xuất kho!",
                    orderId = newOrder.RequestId,
                    orderCode = $"ĐH-KHO-{newOrder.RequestId:D4}"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Lỗi khi lưu: {ex.Message}" });
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
                        requestCode = $"YCSX-{p.RequestId:D4}",
                        orderCode = $"ĐH-KHO-{p.RequestId:D4}",
                        productId = p.ProductId,
                        productName = p.Product != null ? p.Product.ProductName : "Sản phẩm thuốc dược dụng",
                        productUnit = p.Product != null ? p.Product.Unit : "Hộp",
                        quantityRequired = p.QuantityRequired,
                        createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy"),
                        managerName = p.Manager != null ? p.Manager.FullName : "DS. Nguyễn Minh Tài (Quản lý kho)",
                        status = p.Status,
                        priority = p.QuantityRequired >= 600 ? "Khẩn cấp" : "Tiêu chuẩn",
                        note = p.QuantityRequired >= 600 ? "Bổ sung lượng tồn kho gấp cho đơn thầu bệnh viện." : "Sản xuất bổ sung theo hạn mức tồn kho an toàn tháng."
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
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API duyệt tiếp nhận một phiếu sản xuất từ kho vào kế hoạch sản xuất
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AcceptWarehouseRequest([FromBody] AcceptWarehouseOrderRequest req)
        {
            if (req == null || req.RequestId <= 0)
                return BadRequest(new { success = false, message = "Mã phiếu không hợp lệ." });

            try
            {
                var order = await _context.ProductionRequests.FirstOrDefaultAsync(p => p.RequestId == req.RequestId);
                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy phiếu yêu cầu trong cơ sở dữ liệu." });

                // Tiếp nhận phiếu: chuyển trạng thái từ Pending sang In Production
                order.Status = "In Production";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã tiếp nhận thành công phiếu YCSX-{order.RequestId:D4} từ kho thành phẩm vào quy trình sản xuất!",
                    requestId = order.RequestId,
                    newStatus = order.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        /// <summary>
        /// API tiếp nhận toàn bộ các phiếu kho đang chờ duyệt
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AcceptAllWarehouseRequests()
        {
            try
            {
                var pendingOrders = await _context.ProductionRequests
                    .Where(p => p.Status == "Pending")
                    .ToListAsync();

                if (!pendingOrders.Any())
                    return Ok(new { success = true, count = 0, message = "Không có phiếu nào đang chờ tiếp nhận." });

                foreach (var order in pendingOrders)
                {
                    order.Status = "In Production";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    count = pendingOrders.Count,
                    message = $"Đã tiếp nhận toàn bộ {pendingOrders.Count} phiếu yêu cầu sản xuất từ kho vào phân xưởng!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        #endregion
    }
}
