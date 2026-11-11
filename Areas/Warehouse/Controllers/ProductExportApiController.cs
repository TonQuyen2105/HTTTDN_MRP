using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;
using httt.Areas.Warehouse.Models;
using httt.Areas.Warehouse.Services;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/product-exports")]
[Produces("application/json")]
[Tags("7. Xuất kho thành phẩm (Product Exports)")]
public class ProductExportApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductExportApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    private sealed record ProductEligibility(
        int StockQty,
        int ExpiredStockQty,
        int ValidStockQty,
        bool IsOutOfStock,
        bool IsEligible,
        string ConditionStatus,
        string ConditionType,
        string BlockMessage);

    private async Task<Dictionary<int, int>> GetActiveExpiredStockByProductAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();

        var disposedBatches = await (
            from pe in _context.ProductExportReceipts.AsNoTracking()
            join ped in _context.ProductExportReceiptDetails.AsNoTracking() on pe.ExportReceiptId equals ped.ExportReceiptId
            where pe.ExpReceiptCode != null && pe.ExpReceiptCode.StartsWith("HSP-") && ids.Contains(ped.ProductId)
            select new { pe.ExpReceiptCode, ped.ProductId }
        ).ToListAsync();

        var approvedReceiptsMap = await _context.ProductReceipts.AsNoTracking()
            .Where(r => r.Status == "Approved" || r.Status == "Approve")
            .ToDictionaryAsync(r => r.ProductReceiptId, r => r.ReceiptCode);

        var approvedReceiptIds = approvedReceiptsMap.Keys.ToList();
        if (approvedReceiptIds.Count == 0) return new Dictionary<int, int>();

        var now = DateTime.UtcNow;
        var rawExpiredBatches = await _context.ProductReceiptDetails
            .AsNoTracking()
            .Where(b => ids.Contains(b.ProductId)
                        && b.ExpiredDate != null
                        && b.ExpiredDate <= now
                        && approvedReceiptIds.Contains(b.ProductReceiptId))
            .ToListAsync();

        return rawExpiredBatches
            .Where(b =>
            {
                var code = approvedReceiptsMap.TryGetValue(b.ProductReceiptId, out var c) && !string.IsNullOrWhiteSpace(c)
                    ? c
                    : $"LNK{b.ProductReceiptId:D5}";
                bool isDisposed = disposedBatches.Any(d =>
                    d.ProductId == b.ProductId
                    && (string.Equals(d.ExpReceiptCode, $"HSP-{code}", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(d.ExpReceiptCode, $"HSP-LNK{b.ProductReceiptId:D5}", StringComparison.OrdinalIgnoreCase)));
                return !isDisposed;
            })
            .GroupBy(b => b.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
    }

    private static ProductEligibility EvaluateProductEligibility(Product? prod, int reqQty, Dictionary<int, int> expiredBatchesByProduct)
    {
        int stockQty = prod?.Quantity ?? 0;
        int expiredBatchSum = prod != null && expiredBatchesByProduct.TryGetValue(prod.ProductId, out var sum) ? sum : 0;
        int expiredStock = Math.Min(stockQty, expiredBatchSum);
        int validStock = Math.Max(0, stockQty - expiredStock);
        string name = prod?.ProductName ?? "N/A";
        string code = prod != null ? $"SP{prod.ProductId:D3}" : "";
        string unit = !string.IsNullOrWhiteSpace(prod?.Unit) ? prod!.Unit : "Hộp";

        if (stockQty <= 0 && expiredStock == 0)
        {
            return new ProductEligibility(
                stockQty, expiredStock, validStock, true, false,
                "Thiếu tồn kho", "danger",
                $"Không đủ tồn kho: '{name}' ({code}) chỉ còn {stockQty:N0} {unit} (yêu cầu xuất {reqQty:N0} {unit}).");
        }
        if (expiredStock > 0 && validStock <= 0)
        {
            return new ProductEligibility(
                stockQty, expiredStock, validStock, false, false,
                "Tồn kho hết hạn", "danger",
                $"Toàn bộ hàng tồn kho ({stockQty:N0} {unit}) của '{name}' ({code}) đã hết hạn sử dụng (Tồn kho hết hạn).");
        }
        if (expiredStock > 0 && validStock < reqQty)
        {
            return new ProductEligibility(
                stockQty, expiredStock, validStock, false, false,
                "Thiếu số lượng đạt chuẩn", "warning",
                $"'{name}' ({code}) thiếu số lượng đạt chuẩn (tồn đạt chuẩn: {validStock:N0} {unit}, đã hết hạn: {expiredStock:N0} {unit}, yêu cầu xuất: {reqQty:N0} {unit}).");
        }
        if (validStock < reqQty)
        {
            return new ProductEligibility(
                stockQty, expiredStock, validStock, true, false,
                $"Thiếu {reqQty - stockQty:N0}", "danger",
                $"Không đủ tồn kho: '{name}' ({code}) chỉ còn {stockQty:N0} {unit} (yêu cầu xuất {reqQty:N0} {unit}).");
        }
        return new ProductEligibility(
            stockQty, expiredStock, validStock, false, true,
            "Đủ điều kiện", "success", "");
    }

    /// <summary>
    /// GET /api/warehouse/product-exports : Danh sách tất cả lệnh/phiếu xuất kho thành phẩm
    /// </summary>
    /// <summary>
    /// Chuẩn hóa trạng thái phiếu xuất kho thành phẩm sang nhãn hiển thị và loại badge UI.
    /// </summary>
    private static (string DisplayStatus, string StatusType, bool IsPending) MapExportStatus(string? status)
    {
        if (SaleOrderCancellationSync.IsCancelStatus(status))
            return ("Cancel", "danger", false);
        if (SaleOrderCancellationSync.IsApprovedStatus(status))
            return ("Đã xuất kho", "success", false);
        if (SaleOrderCancellationSync.IsRejectedStatus(status))
            return ("Từ chối", "danger", false);
        return ("Chờ xuất kho", "warning", true);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductExports([FromQuery] string? status = null, [FromQuery] string? keyword = null)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);

        var query = _context.ProductExportReceipts.AsNoTracking()
            .Where(e => e.ExpReceiptCode == null || !e.ExpReceiptCode.StartsWith("HSP-"));

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
        {
            if (status == "Chờ xuất kho" || status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Status == "Pending" || e.Status == "Chờ xuất kho");
            else if (status == "Đã xuất kho" || status.Equals("Approved", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Status == "Approved" || e.Status == "Approve" || e.Status == "Đã xuất kho" || e.Status == "Completed");
            else if (status == "Từ chối" || status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Status == "Rejected" || e.Status == "Reject" || e.Status == "Từ chối");
            else if (SaleOrderCancellationSync.IsCancelStatus(status))
                query = query.Where(e => e.Status != null && (e.Status.ToLower() == "cancel" || e.Status.ToLower() == "cancelled" || e.Status.ToLower() == "canceled" || e.Status == "Đã hủy"));
        }

        var list = await query.OrderByDescending(e => e.CreatedDate).ToListAsync();
        var receiptIds = list.Select(e => e.ExportReceiptId).ToList();

        var details = await _context.ProductExportReceiptDetails
            .AsNoTracking()
            .Where(d => receiptIds.Contains(d.ExportReceiptId))
            .ToListAsync();

        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products
            .AsNoTracking()
            .Where(p => prodIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var expiredBatchesByProduct = await GetActiveExpiredStockByProductAsync(prodIds);

        var orderIds = list.Select(e => e.OrderId).Where(o => o.HasValue).Select(o => o!.Value).Distinct().ToList();
        var saleOrders = await _context.SaleOrders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.OrderId))
            .ToDictionaryAsync(o => o.OrderId);

        var userIds = list.Select(e => e.RequestedBy).Where(u => u.HasValue).Select(u => u!.Value).Distinct().ToList();
        var users = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserID))
            .ToDictionaryAsync(u => u.UserID);

        var result = list.Select(e =>
        {
            var eDetails = details.Where(d => d.ExportReceiptId == e.ExportReceiptId).ToList();
            saleOrders.TryGetValue(e.OrderId ?? 0, out var so);
            users.TryGetValue(e.RequestedBy ?? 0, out var u);

            string creatorName = u != null ? (!string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username) : "Nguyễn Tiến Huy";
            string customerName = so?.CustomerName ?? "Công ty Dược phẩm / Đại lý";
            string orderCode = so?.OrderCode ?? (e.OrderId.HasValue ? $"DH{e.OrderId:D5}" : "");

            var (displayStatus, statusType, isPending) = MapExportStatus(e.Status);

            var items = eDetails.Select((d, idx) =>
            {
                prods.TryGetValue(d.ProductId, out var prod);
                var cost = d.Price ?? prod?.Cost ?? 0;
                var elig = EvaluateProductEligibility(prod, d.Quantity, expiredBatchesByProduct);

                return new
                {
                    id = idx + 1,
                    productId = d.ProductId,
                    itemCode = $"SP{d.ProductId:D3}",
                    productCode = $"SP{d.ProductId:D3}",
                    itemName = prod?.ProductName ?? "N/A",
                    productName = prod?.ProductName ?? "N/A",
                    unit = prod?.Unit ?? "Hộp",
                    quantity = d.Quantity,
                    price = cost,
                    amount = d.Quantity * cost,
                    status = d.Status,
                    note = d.Note ?? "",
                    stockQty = elig.StockQty,
                    isOutOfStock = elig.IsOutOfStock,
                    expiredStockQty = elig.ExpiredStockQty,
                    validStockQty = elig.ValidStockQty,
                    conditionStatus = elig.ConditionStatus,
                    conditionType = elig.ConditionType,
                    isItemEligible = elig.IsEligible,
                    blockReason = elig.BlockMessage
                };
            }).ToList();

            string desc = eDetails.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note))?.Note
                ?? (so != null ? $"Xuất kho bán hàng cho {so.CustomerName} (Đơn #{orderCode})" : $"Xuất kho thành phẩm theo lệnh {e.ExpReceiptCode}");

            var hasStockIssue = items.Any(it => !it.isItemEligible);
            var hasExpiryIssue = items.Any(it => it.expiredStockQty > 0 && !it.isItemEligible);
            var canApprove = isPending && items.All(it => it.isItemEligible);
            var approvalBlockReason = items.FirstOrDefault(it => !it.isItemEligible)?.blockReason ?? "";

            return new
            {
                id = e.ExportReceiptId,
                exportReceiptId = e.ExportReceiptId,
                voucherNo = e.ExpReceiptCode ?? $"PXK_BH{e.ExportReceiptId:D4}",
                orderNo = e.ExpReceiptCode ?? $"PXK_BH{e.ExportReceiptId:D4}",
                voucherDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                orderDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
                createdDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                warehouse = "Kho thành phẩm GMP",
                partnerName = customerName,
                customer = customerName,
                receiver = customerName,
                customerPhone = so?.CustomerPhone ?? "",
                shippingAddress = so?.ShippingAddress ?? "",
                orderId = e.OrderId,
                orderCode,
                description = desc,
                status = displayStatus,
                rawStatus = e.Status,
                orderStatus = displayStatus,
                statusType,
                creatorName,
                employeeName = creatorName,
                totalItems = items.Count,
                totalQuantity = items.Sum(i => i.quantity),
                totalAmount = items.Sum(i => i.amount),
                hasStockIssue,
                hasExpiryIssue,
                canApprove,
                approvalBlockReason,
                items
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            result = result.Where(r =>
                (r.voucherNo != null && r.voucherNo.ToLower().Contains(kw)) ||
                (r.partnerName != null && r.partnerName.ToLower().Contains(kw)) ||
                (r.description != null && r.description.ToLower().Contains(kw)) ||
                (r.orderStatus != null && r.orderStatus.ToLower().Contains(kw)) ||
                (r.orderCode != null && r.orderCode.ToLower().Contains(kw))
            ).ToList();
        }

        return Ok(result);
    }

    /// <summary>
    /// GET /api/warehouse/product-exports/{id} : Xem chi tiết đơn xuất bán thành phẩm
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductExportById(int id)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);

        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products.Where(p => prodIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);
        var expiredBatchesByProduct = await GetActiveExpiredStockByProductAsync(prodIds);

        SaleOrder? so = null;
        if (receipt.OrderId.HasValue)
        {
            so = await _context.SaleOrders.FindAsync(receipt.OrderId.Value);
        }

        UserAccount? u = null;
        if (receipt.RequestedBy.HasValue)
        {
            u = await _context.UserAccounts.FindAsync(receipt.RequestedBy.Value);
        }

        UserAccount? au = null;
        if (receipt.ApprovedBy.HasValue)
        {
            au = await _context.UserAccounts.FindAsync(receipt.ApprovedBy.Value);
        }

        string creatorName = u != null ? (!string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username) : "Nguyễn Tiến Huy";
        string approverName = au != null ? (!string.IsNullOrWhiteSpace(au.FullName) ? au.FullName : au.Username) : "";
        string customerName = so?.CustomerName ?? "Công ty Dược phẩm / Đại lý";
        string orderCode = so?.OrderCode ?? (receipt.OrderId.HasValue ? $"DH{receipt.OrderId:D5}" : "");

        var (displayStatus, statusType, isPending) = MapExportStatus(receipt.Status);

        var items = details.Select((d, idx) =>
        {
            prods.TryGetValue(d.ProductId, out var prod);
            var cost = d.Price ?? prod?.Cost ?? 0;
            var elig = EvaluateProductEligibility(prod, d.Quantity, expiredBatchesByProduct);

            return new
            {
                id = idx + 1,
                productId = d.ProductId,
                itemCode = $"SP{d.ProductId:D3}",
                productCode = $"SP{d.ProductId:D3}",
                itemName = prod?.ProductName ?? "N/A",
                productName = prod?.ProductName ?? "N/A",
                unit = prod?.Unit ?? "Hộp",
                quantity = d.Quantity,
                reqQty = d.Quantity,
                actualQty = d.Quantity,
                price = cost,
                amount = d.Quantity * cost,
                status = d.Status,
                note = d.Note ?? "",
                stockQty = elig.StockQty,
                isOutOfStock = elig.IsOutOfStock,
                expiredStockQty = elig.ExpiredStockQty,
                validStockQty = elig.ValidStockQty,
                conditionStatus = elig.ConditionStatus,
                conditionType = elig.ConditionType,
                isItemEligible = elig.IsEligible,
                blockReason = elig.BlockMessage
            };
        }).ToList();

        var hasStockIssue = items.Any(it => !it.isItemEligible);
        var hasExpiryIssue = items.Any(it => it.expiredStockQty > 0 && !it.isItemEligible);
        var canApprove = isPending && items.All(it => it.isItemEligible);

        string approvalBlockReason = items.FirstOrDefault(it => !it.isItemEligible)?.blockReason ?? "";

        string desc = details.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note))?.Note
            ?? (so != null ? $"Xuất kho bán hàng cho {so.CustomerName} (Đơn #{orderCode})" : $"Xuất kho thành phẩm theo lệnh {receipt.ExpReceiptCode}");

        return Ok(new
        {
            receiptId = receipt.ExportReceiptId,
            exportReceiptId = receipt.ExportReceiptId,
            voucherNo = receipt.ExpReceiptCode ?? $"PXK_BH{receipt.ExportReceiptId:D4}",
            orderNo = receipt.ExpReceiptCode ?? $"PXK_BH{receipt.ExportReceiptId:D4}",
            voucherType = "outbound",
            voucherDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            orderDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
            createdDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            status = displayStatus,
            rawStatus = receipt.Status,
            orderStatus = displayStatus,
            statusType,
            warehouse = "Kho thành phẩm GMP",
            partnerName = customerName,
            customer = customerName,
            receiver = customerName,
            customerPhone = so?.CustomerPhone ?? "",
            shippingAddress = so?.ShippingAddress ?? "",
            orderId = receipt.OrderId,
            orderCode,
            description = desc,
            creatorName,
            employeeName = creatorName,
            employeeCode = u != null ? $"NV{u.UserID:D4}" : "NV0005",
            approverName,
            canApprove,
            hasStockIssue,
            hasExpiryIssue,
            approvalBlockReason,
            totalItems = items.Count,
            totalQuantity = items.Sum(i => i.quantity),
            totalAmount = items.Sum(i => i.amount),
            items
        });
    }

    /// <summary>
    /// PUT /api/warehouse/product-exports/{id}/approve : Duyệt xuất hàng thành phẩm -> Trừ tồn kho thành phẩm trong CSDL
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveProductExport(int id)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);

        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        if (SaleOrderCancellationSync.IsCancelStatus(receipt.Status))
        {
            return BadRequest(new { success = false, message = "Đơn hàng liên kết đã ở trạng thái Cancel nên phiếu xuất kho này đã bị hủy, không thể duyệt xuất kho." });
        }

        if (SaleOrderCancellationSync.IsRejectedStatus(receipt.Status))
        {
            return BadRequest(new { success = false, message = "Phiếu xuất kho này đang ở trạng thái từ chối (Rejected), không thể duyệt xuất kho." });
        }

        if (SaleOrderCancellationSync.IsApprovedStatus(receipt.Status))
        {
            return BadRequest(new { message = "Lệnh xuất hàng này đã được duyệt xuất kho trước đó." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        var expiredBatchesByProduct = await GetActiveExpiredStockByProductAsync(details.Select(d => d.ProductId));

        // 1. Kiểm tra tồn kho & hạn sử dụng thành phẩm
        foreach (var d in details)
        {
            var prod = await _context.Products.FindAsync(d.ProductId);
            if (prod == null)
            {
                return BadRequest(new { message = $"Không tìm thấy sản phẩm có ID {d.ProductId} trong kho." });
            }
            var elig = EvaluateProductEligibility(prod, d.Quantity, expiredBatchesByProduct);
            if (!elig.IsEligible)
            {
                return BadRequest(new { success = false, message = $"Không thể duyệt phiếu: {elig.BlockMessage}" });
            }
        }

        // 2. Trừ tồn kho và đổi trạng thái
        foreach (var d in details)
        {
            var prod = await _context.Products.FindAsync(d.ProductId);
            if (prod != null)
            {
                prod.Quantity = Math.Max(0, (prod.Quantity ?? 0) - d.Quantity);
            }
            d.Status = "Approved";
        }

        receipt.Status = "Approved";
        var firstUser = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync();
        if (firstUser != null) receipt.ApprovedBy = firstUser.UserID;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Duyệt xuất kho thành phẩm {receipt.ExpReceiptCode} thành công. Đã trừ tồn kho tương ứng.",
            exportReceiptId = receipt.ExportReceiptId,
            status = "Approved",
            orderStatus = "Đã xuất kho"
        });
    }

    /// <summary>
    /// PUT /api/warehouse/product-exports/{id}/reject : Từ chối xuất hàng thành phẩm
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectProductExport(int id, [FromBody] RejectDto? dto)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);

        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        if (SaleOrderCancellationSync.IsCancelStatus(receipt.Status))
        {
            return BadRequest(new { success = false, message = "Đơn hàng liên kết đã ở trạng thái Cancel nên phiếu xuất kho này đã bị hủy." });
        }

        if (SaleOrderCancellationSync.IsApprovedStatus(receipt.Status))
        {
            return BadRequest(new { success = false, message = "Phiếu xuất hàng này đã được duyệt xuất kho, không thể chuyển sang từ chối." });
        }

        receipt.Status = "Rejected";
        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        foreach (var d in details)
        {
            d.Status = "Rejected";
            if (!string.IsNullOrWhiteSpace(dto?.Reason)) d.Note = dto.Reason;
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            success = true,
            message = $"Đã từ chối lệnh xuất hàng {receipt.ExpReceiptCode}.",
            exportReceiptId = receipt.ExportReceiptId,
            status = "Rejected",
            orderStatus = "Từ chối"
        });
    }

    /// <summary>
    /// POST /api/warehouse/product-exports/sync-cancelled-orders : Đồng bộ thủ công các đơn hàng đã chuyển sang trạng thái Cancel
    /// </summary>
    [HttpPost("sync-cancelled-orders")]
    public async Task<IActionResult> SyncCancelledOrders()
    {
        var (pendingCancelled, approvedRefunded) = await SaleOrderCancellationSync.SyncAsync(_context);
        return Ok(new
        {
            success = true,
            pendingCancelled,
            approvedRefunded,
            message = $"Đã đồng bộ đơn hàng hủy: {pendingCancelled} phiếu Pending -> Cancel, {approvedRefunded} phiếu Approved hoàn tồn & Cancel."
        });
    }

    /// <summary>
    /// POST /api/warehouse/product-exports : Tạo mới phiếu xuất kho thành phẩm
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProductExport([FromBody] CreateProductExportDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn ít nhất một thành phẩm cần xuất kho." });
        }

        var now = DateTime.UtcNow;

        // Tạo sale order mới nếu có thông tin khách hàng mà chưa có orderId
        int? orderId = dto.OrderId;
        if (!orderId.HasValue && !string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            var so = new SaleOrder
            {
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone?.Trim(),
                ShippingAddress = dto.ShippingAddress?.Trim(),
                OrderDate = now,
                Status = "Approved",
                OrderCode = $"DH{now:yyyyMMddHHmmss}"
            };
            _context.SaleOrders.Add(so);
            await _context.SaveChangesAsync();
            orderId = so.OrderId;
        }

        var maxId = await _context.ProductExportReceipts.MaxAsync(e => (int?)e.ExportReceiptId) ?? 0;
        var nextId = maxId + 1;

        var receipt = new ProductExportReceipt
        {
            ExportReceiptId = nextId,
            ExpReceiptCode = !string.IsNullOrWhiteSpace(dto.ExpReceiptCode) ? dto.ExpReceiptCode : $"PXK_BH{nextId:D4}",
            OrderId = orderId,
            CreatedDate = now,
            Status = dto.Status ?? "Chờ xuất kho",
            RequestedBy = dto.RequestedBy ?? (await _context.UserAccounts.Select(u => (int?)u.UserID).FirstOrDefaultAsync())
        };

        _context.ProductExportReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var it in dto.Items)
        {
            var detail = new ProductExportReceiptDetail
            {
                ExportReceiptId = receipt.ExportReceiptId,
                ProductId = it.ProductId,
                Quantity = it.Quantity,
                Price = it.Price,
                Status = receipt.Status,
                Note = it.Note,
                CreatedDate = now
            };
            _context.ProductExportReceiptDetails.Add(detail);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Tạo phiếu xuất kho thành phẩm {receipt.ExpReceiptCode} thành công.",
            exportReceiptId = receipt.ExportReceiptId,
            expReceiptCode = receipt.ExpReceiptCode
        });
    }

    /// <summary>
    /// GET /api/warehouse/product-exports/sale-orders : Danh sách đơn bán hàng để liên kết khi tạo phiếu xuất
    /// </summary>
    [HttpGet("sale-orders")]
    public async Task<IActionResult> GetSaleOrders()
    {
        var orders = await _context.SaleOrders.AsNoTracking().OrderByDescending(o => o.OrderDate).ToListAsync();
        return Ok(orders);
    }

    /// <summary>
    /// DELETE /api/warehouse/product-exports/{id} : Xóa phiếu xuất kho thành phẩm
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProductExport(int id)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu xuất kho có ID {id}." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        _context.ProductExportReceiptDetails.RemoveRange(details);
        _context.ProductExportReceipts.Remove(receipt);

        if (receipt.OrderId.HasValue)
        {
            var otherUsingOrder = await _context.ProductExportReceipts.AnyAsync(e => e.OrderId == receipt.OrderId && e.ExportReceiptId != id);
            if (!otherUsingOrder)
            {
                var order = await _context.SaleOrders.FindAsync(receipt.OrderId.Value);
                if (order != null) _context.SaleOrders.Remove(order);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = $"Đã xóa phiếu xuất kho {receipt.ExpReceiptCode} thành công." });
    }
}

public class CreateProductExportDto
{
    public string? ExpReceiptCode { get; set; }
    public int? OrderId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Status { get; set; } = "Chờ xuất kho";
    public int? RequestedBy { get; set; }
    public List<CreateProductExportItemDto> Items { get; set; } = new();
}

public class CreateProductExportItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? Price { get; set; }
    public string? Note { get; set; }
}
