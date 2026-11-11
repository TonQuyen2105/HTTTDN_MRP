using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;
using httt.Areas.Warehouse.Models;
using httt.Areas.Warehouse.Services;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Authorize(Roles = "Warehouse,Kho")]
[IgnoreAntiforgeryToken]
[Route("api/warehouse/product-receipts")]
[Produces("application/json")]
[Tags("6. Nhập kho thành phẩm (Product Receipts)")]
public class ProductReceiptApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductReceiptApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/product-receipts : Danh sách tất cả lệnh/phiếu nhập kho thành phẩm từ xưởng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProductReceipts([FromQuery] string? status = null, [FromQuery] string? keyword = null)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);

        // Điều kiện: status = ['Approve', 'Pending', 'Rejected'] và receiptcode khác NULL
        var query = _context.ProductReceipts.AsNoTracking()
            .Where(p => p.ReceiptCode != null && p.ReceiptCode.Trim() != "" &&
                       (p.Status == "Pending" || p.Status == "Chờ nhập kho" ||
                        p.Status == "Approved" || p.Status == "Approve" || p.Status == "Đã nhập kho" ||
                        p.Status == "Rejected" || p.Status == "Reject" || p.Status == "Từ chối"));

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
        {
            if (status == "Chờ nhập kho" || status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => p.Status == "Pending" || p.Status == "Chờ nhập kho");
            else if (status == "Đã nhập kho" || status.Equals("Approved", StringComparison.OrdinalIgnoreCase) || status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => p.Status == "Approved" || p.Status == "Approve" || p.Status == "Đã nhập kho");
            else if (status == "Từ chối" || status.Equals("Rejected", StringComparison.OrdinalIgnoreCase) || status.Equals("Reject", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => p.Status == "Rejected" || p.Status == "Reject" || p.Status == "Từ chối");
        }

        var list = await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
        var receiptIds = list.Select(p => p.ProductReceiptId).ToList();

        var details = await _context.ProductReceiptDetails
            .AsNoTracking()
            .Where(d => receiptIds.Contains(d.ProductReceiptId))
            .ToListAsync();

        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products
            .AsNoTracking()
            .Where(p => prodIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var reqIds = list.Select(p => p.RequestId).Where(r => r.HasValue).Select(r => r!.Value).Distinct().ToList();
        var prodRequests = await _context.ProductionRequests
            .AsNoTracking()
            .Where(r => reqIds.Contains(r.RequestId))
            .ToDictionaryAsync(r => r.RequestId);

        var requestCodes = prodRequests.Values.Where(r => !string.IsNullOrWhiteSpace(r.RequestCode)).Select(r => r.RequestCode!).Distinct().ToList();
        var siblingRequests = requestCodes.Count > 0
            ? await _context.ProductionRequests.AsNoTracking().Where(r => requestCodes.Contains(r.RequestCode!)).ToListAsync()
            : new List<ProductionRequest>();
        var allRequests = prodRequests.Values.UnionBy(siblingRequests, r => r.RequestId).ToList();

        var assignedUserIds = prodRequests.Values.Select(r => r.AssignedTo).Where(u => u.HasValue).Select(u => u!.Value);
        var userIds = list.Select(p => p.RequestedBy).Where(u => u.HasValue).Select(u => u!.Value)
            .Concat(assignedUserIds).Distinct().ToList();

        var users = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        var result = list.Select(p =>
        {
            var pDetails = details.Where(d => d.ProductReceiptId == p.ProductReceiptId).ToList();

            // Tên người lập
            string creatorName = "Nguyễn Tiến Huy";
            string employeeCode = "NV0005";
            if (p.RequestedBy.HasValue && users.TryGetValue(p.RequestedBy.Value, out var u))
            {
                creatorName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
                employeeCode = $"NV{u.UserID:D4}";
            }

            // Tên hàng hóa
            var prodNames = pDetails.Select(d =>
            {
                prods.TryGetValue(d.ProductId, out var pr);
                return pr?.ProductName ?? $"SP{d.ProductId:D3}";
            }).Distinct().ToList();
            string productNameStr = prodNames.Count > 0 ? string.Join(", ", prodNames) : "Thành phẩm sản xuất";

            // Mã lệnh (LNK...)
            string orderNo = !string.IsNullOrWhiteSpace(p.ReceiptCode) ? p.ReceiptCode : $"LNK{p.ProductReceiptId:D5}";

            // Ngày lệnh & Hạn nhập
            var orderDt = p.CreatedDate ?? DateTime.UtcNow;
            string orderDateStr = orderDt.ToString("dd/MM/yyyy");
            string deliveryDateStr = orderDt.ToString("dd/MM/yyyy");

            // Thông tin từ Lệnh sản xuất (nếu có)
            int? assignedTo = null;
            string? assignedToName = null;
            string? assignedToEmployeeCode = null;
            string? requestCode = null;
            prodRequests.TryGetValue(p.RequestId ?? 0, out var prq);
            if (prq != null)
            {
                requestCode = !string.IsNullOrWhiteSpace(prq.RequestCode) ? prq.RequestCode : $"LSX{prq.RequestId:D5}";
                assignedTo = prq.AssignedTo;
                if (prq.AssignedTo.HasValue && users.TryGetValue(prq.AssignedTo.Value, out var au))
                {
                    assignedToName = !string.IsNullOrWhiteSpace(au.FullName) ? au.FullName : au.Username;
                    assignedToEmployeeCode = $"NV{au.UserID:D4}";
                }
            }

            // Diễn giải
            string description = "";
            var firstDetailWithNote = pDetails.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note));
            if (firstDetailWithNote != null && !string.IsNullOrWhiteSpace(firstDetailWithNote.Note))
            {
                description = firstDetailWithNote.Note;
            }
            else if (prq != null)
            {
                description = $"Nhập thành phẩm từ lệnh sản xuất {prq.RequestCode ?? $"LSX#{prq.RequestId}"}";
            }
            else
            {
                description = $"Nhập kho thành phẩm theo lệnh bàn giao {orderNo}";
            }

            // Trạng thái hiển thị
            string displayStatus = "Chờ nhập kho";
            string statusType = "warning";
            if (p.Status == "Approved" || p.Status == "Approve" || p.Status == "Đã nhập kho")
            {
                displayStatus = "Đã nhập kho";
                statusType = "success";
            }
            else if (p.Status == "Rejected" || p.Status == "Reject" || p.Status == "Từ chối")
            {
                displayStatus = "Từ chối";
                statusType = "danger";
            }

            var pSiblingReqIds = new HashSet<int>();
            if (p.RequestId.HasValue)
            {
                pSiblingReqIds.Add(p.RequestId.Value);
                var myReq = allRequests.FirstOrDefault(r => r.RequestId == p.RequestId.Value);
                if (myReq != null && !string.IsNullOrWhiteSpace(myReq.RequestCode))
                {
                    foreach (var sib in allRequests.Where(r => r.RequestCode == myReq.RequestCode))
                    {
                        pSiblingReqIds.Add(sib.RequestId);
                    }
                }
            }

            var items = pDetails.Select((d, idx) =>
            {
                prods.TryGetValue(d.ProductId, out var pr);
                int months = pr?.ShelfLifeMonths ?? 24;
                string expStr = d.ExpiredDate.HasValue 
                    ? d.ExpiredDate.Value.ToString("dd/MM/yyyy") 
                    : orderDt.AddMonths(months).ToString("dd/MM/yyyy");

                string itemCode = $"SP{d.ProductId:D3}";
                decimal itemCost = (d.Cost.HasValue && d.Cost.Value > 0) ? d.Cost.Value : (pr?.Cost ?? 0);

                // Lấy quantityrequired từ ProductionRequest
                int quantityRequired = d.Quantity;
                if (pSiblingReqIds.Count > 0)
                {
                    var matchingReq = allRequests.FirstOrDefault(r => pSiblingReqIds.Contains(r.RequestId) && r.ProductId == d.ProductId);
                    if (matchingReq != null) quantityRequired = matchingReq.QuantityRequired;
                }

                // Tính số lượng thực nhập lũy kế
                int priorApprovedQty = 0;
                if (pSiblingReqIds.Count > 0)
                {
                    var priorApprovedReceiptIds = list
                        .Where(r => r.RequestId.HasValue && pSiblingReqIds.Contains(r.RequestId.Value) && r.ProductReceiptId < p.ProductReceiptId && (r.Status == "Approved" || r.Status == "Đã nhập kho"))
                        .Select(r => r.ProductReceiptId)
                        .ToList();

                    priorApprovedQty = details
                        .Where(rd => priorApprovedReceiptIds.Contains(rd.ProductReceiptId) && rd.ProductId == d.ProductId)
                        .Sum(rd => rd.Quantity);
                }

                int accumulatedActualQty = priorApprovedQty + d.Quantity;
                string actualQtyDisplay = $"{accumulatedActualQty}/{quantityRequired}";

                return new
                {
                    id = idx + 1,
                    detailId = d.ProductReceiptId,
                    productId = d.ProductId,
                    itemCode,
                    productCode = itemCode,
                    itemName = pr?.ProductName ?? "N/A",
                    productName = pr?.ProductName ?? "N/A",
                    unit = pr?.Unit ?? "Hộp",
                    reqQty = quantityRequired,
                    actualQty = accumulatedActualQty,
                    actualQtyDisplay,
                    quantityRequired,
                    receiptQty = d.Quantity,
                    quantity = d.Quantity,
                    cost = itemCost,
                    costFormatted = itemCost.ToString("N0"),
                    amount = itemCost * d.Quantity,
                    amountFormatted = (itemCost * d.Quantity).ToString("N0"),
                    status = d.Status,
                    expiryDate = expStr,
                    note = d.Note ?? "",
                    isFull = (accumulatedActualQty >= quantityRequired)
                };
            }).ToList();

            return new
            {
                id = p.ProductReceiptId,
                productReceiptId = p.ProductReceiptId,
                orderNo,
                receiptCode = orderNo,
                orderDate = orderDateStr,
                deliveryDate = deliveryDateStr,
                createdDate = orderDt.ToString("dd/MM/yyyy HH:mm:ss"),
                dueDate = orderDt.ToString("dd/MM/yyyy 00:00:00"),
                supplier = creatorName,
                creatorName,
                employeeCode,
                employeeName = creatorName,
                productName = productNameStr,
                description,
                status = p.Status,
                rawStatus = p.Status,
                orderStatus = displayStatus,
                statusType,
                requestId = p.RequestId,
                requestCode,
                productionRequestCode = requestCode,
                assignedTo,
                assignedToName,
                assignedToEmployeeCode,
                totalItems = items.Count,
                totalQuantity = items.Sum(i => i.quantity),
                items
            };
        });

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            result = result.Where(r =>
                r.orderNo.ToLower().Contains(kw) ||
                r.supplier.ToLower().Contains(kw) ||
                r.productName.ToLower().Contains(kw) ||
                r.description.ToLower().Contains(kw)
            );
        }

        return Ok(result.ToList());
    }

    /// <summary>
    /// GET /api/warehouse/product-receipts/{id} : Xem chi tiết phiếu nhập thành phẩm
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductReceiptById(int id)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);

        var receipt = await _context.ProductReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu có ID {id}." });
        }

        var details = await _context.ProductReceiptDetails.Where(d => d.ProductReceiptId == id).ToListAsync();
        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products.Where(p => prodIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

        string creatorName = "Nguyễn Tiến Huy";
        if (receipt.RequestedBy.HasValue)
        {
            var u = await _context.UserAccounts.FindAsync(receipt.RequestedBy.Value);
            if (u != null) creatorName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
        }

        var orderDt = receipt.CreatedDate ?? DateTime.UtcNow;
        string orderNo = !string.IsNullOrWhiteSpace(receipt.ReceiptCode) ? receipt.ReceiptCode : $"LNK{receipt.ProductReceiptId:D5}";

        string displayStatus = "Chờ nhập kho";
        if (receipt.Status == "Approved" || receipt.Status == "Đã nhập kho") displayStatus = "Đã nhập kho";
        else if (receipt.Status == "Rejected" || receipt.Status == "Từ chối") displayStatus = "Từ chối";

        // Tải thông tin Lệnh sản xuất và các phiếu nhập thành phẩm cùng lệnh sản xuất
        ProductionRequest? prq = null;
        var relatedReceipts = new List<ProductReceipt>();
        var relatedDetails = new List<ProductReceiptDetail>();
        var siblingProdRequests = new List<ProductionRequest>();

        if (receipt.RequestId.HasValue)
        {
            prq = await _context.ProductionRequests.FindAsync(receipt.RequestId.Value);
            var reqIds = new HashSet<int> { receipt.RequestId.Value };

            if (prq != null && !string.IsNullOrWhiteSpace(prq.RequestCode))
            {
                var sibs = await _context.ProductionRequests
                    .AsNoTracking()
                    .Where(r => r.RequestCode == prq.RequestCode)
                    .ToListAsync();
                siblingProdRequests.AddRange(sibs);
                foreach (var sib in sibs) reqIds.Add(sib.RequestId);
            }
            else if (prq != null)
            {
                siblingProdRequests.Add(prq);
            }

            relatedReceipts = await _context.ProductReceipts
                .AsNoTracking()
                .Where(r => r.RequestId.HasValue && reqIds.Contains(r.RequestId.Value))
                .OrderBy(r => r.ProductReceiptId)
                .ToListAsync();

            var relatedIds = relatedReceipts.Select(r => r.ProductReceiptId).ToList();
            relatedDetails = await _context.ProductReceiptDetails
                .AsNoTracking()
                .Where(d => relatedIds.Contains(d.ProductReceiptId))
                .ToListAsync();
        }

        var items = details.Select((d, idx) =>
        {
            prods.TryGetValue(d.ProductId, out var prod);
            int months = prod?.ShelfLifeMonths ?? 24;
            string expStr = d.ExpiredDate.HasValue 
                ? d.ExpiredDate.Value.ToString("dd/MM/yyyy") 
                : orderDt.AddMonths(months).ToString("dd/MM/yyyy");

            string itemCode = $"SP{d.ProductId:D3}";
            decimal itemCost = (d.Cost.HasValue && d.Cost.Value > 0) ? d.Cost.Value : (prod?.Cost ?? 0);

            // 1. Cột SL nhập lấy từ quantityrequired trong bảng productionrequest
            int quantityRequired = d.Quantity;
            var matchingReq = siblingProdRequests.FirstOrDefault(r => r.ProductId == d.ProductId)
                ?? (prq != null && prq.ProductId == d.ProductId ? prq : null);
            if (matchingReq != null)
            {
                quantityRequired = matchingReq.QuantityRequired;
            }
            else if (prq != null && prq.QuantityRequired > 0)
            {
                quantityRequired = prq.QuantityRequired;
            }

            // 2. Cột thực nhập so sánh với quantityrequired (lũy kế từ các phiếu trước đã duyệt + số lượng phiếu này)
            int priorApprovedQty = 0;
            if (relatedReceipts.Count > 0)
            {
                var priorApprovedReceiptIds = relatedReceipts
                    .Where(r => r.ProductReceiptId < receipt.ProductReceiptId && (r.Status == "Approved" || r.Status == "Đã nhập kho"))
                    .Select(r => r.ProductReceiptId)
                    .ToList();

                priorApprovedQty = relatedDetails
                    .Where(rd => priorApprovedReceiptIds.Contains(rd.ProductReceiptId) && rd.ProductId == d.ProductId)
                    .Sum(rd => rd.Quantity);
            }

            int accumulatedActualQty = priorApprovedQty + d.Quantity;
            string actualQtyDisplay = $"{accumulatedActualQty}/{quantityRequired}";

            return new
            {
                id = idx + 1,
                productId = d.ProductId,
                itemCode,
                productCode = itemCode,
                itemName = prod?.ProductName ?? "N/A",
                productName = prod?.ProductName ?? "N/A",
                unit = prod?.Unit ?? "Hộp",
                reqQty = quantityRequired,
                actualQty = accumulatedActualQty,
                actualQtyDisplay,
                quantityRequired,
                receiptQty = d.Quantity,
                quantity = d.Quantity,
                cost = itemCost,
                costFormatted = itemCost.ToString("N0"),
                amount = itemCost * d.Quantity,
                amountFormatted = (itemCost * d.Quantity).ToString("N0"),
                status = d.Status,
                expiryDate = expStr,
                note = d.Note ?? "",
                isFull = (accumulatedActualQty >= quantityRequired)
            };
        }).ToList();

        int? assignedTo = null;
        string? assignedToName = null;
        string? assignedToEmployeeCode = null;
        string? requestCode = null;
        if (prq != null)
        {
            requestCode = !string.IsNullOrWhiteSpace(prq.RequestCode) ? prq.RequestCode : $"LSX{prq.RequestId:D5}";
            assignedTo = prq.AssignedTo;
            if (prq.AssignedTo.HasValue)
            {
                var au = await _context.UserAccounts.FindAsync(prq.AssignedTo.Value);
                if (au != null)
                {
                    assignedToName = !string.IsNullOrWhiteSpace(au.FullName) ? au.FullName : au.Username;
                    assignedToEmployeeCode = $"NV{au.UserID:D4}";
                }
            }
        }

        var prodNames = items.Select(i => i.productName).Distinct().ToList();
        string productNameStr = prodNames.Count > 0 ? string.Join(", ", prodNames) : "Thành phẩm sản xuất";

        string description = details.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note))?.Note
            ?? $"Nhập kho thành phẩm theo lệnh bàn giao {orderNo}";

        return Ok(new
        {
            docType = "inbound-command",
            title = $"Lệnh nhập kho {orderNo}",
            commandNo = orderNo,
            receiptId = receipt.ProductReceiptId,
            orderDate = orderDt.ToString("dd/MM/yyyy"),
            deliveryDate = orderDt.ToString("dd/MM/yyyy"),
            dueDate = orderDt.ToString("dd/MM/yyyy 00:00:00"),
            status = receipt.Status,
            rawStatus = receipt.Status,
            statusText = displayStatus,
            employeeName = creatorName,
            department = orderNo.StartsWith("HOAN-", StringComparison.OrdinalIgnoreCase) ? "Kho thành phẩm (Hoàn hàng)" : "Phân xưởng Sản xuất GMP",
            productName = productNameStr,
            description,
            requestId = receipt.RequestId,
            requestCode,
            productionRequestCode = requestCode,
            assignedTo,
            assignedToName,
            assignedToEmployeeCode,
            reqQtyColLabel = "SL nhập",
            actualQtyColLabel = "Thực nhập",
            items
        });
    }

    /// <summary>
    /// PUT /api/warehouse/product-receipts/{id}/approve : Duyệt nhận thành phẩm
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveProductReceipt(int id)
    {
        var receipt = await _context.ProductReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu có ID {id}." });
        }

        if (receipt.Status == "Approved" || receipt.Status == "Đã nhập kho")
        {
            return BadRequest(new { message = "Phiếu này đã được duyệt nhận thành phẩm." });
        }

        var details = await _context.ProductReceiptDetails.Where(d => d.ProductReceiptId == id).ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var d in details)
        {
            var prod = await _context.Products.FindAsync(d.ProductId);
            if (prod != null)
            {
                prod.Quantity = (prod.Quantity ?? 0) + d.Quantity;
                int months = prod.ShelfLifeMonths.HasValue && prod.ShelfLifeMonths.Value > 0 ? prod.ShelfLifeMonths.Value : 24;
                d.ExpiredDate = now.AddMonths(months);

                if (d.Cost.HasValue && d.Cost.Value > 0 && d.Cost.Value > (prod.MaterialCost ?? 0))
                {
                    prod.MaterialCost = d.Cost.Value;
                }
            }

            d.Status = "Approved";

            if (receipt.RequestId.HasValue)
            {
                var wip = await _context.WipInventories
                    .FirstOrDefaultAsync(w => w.RequestId == receipt.RequestId.Value && w.ProductId == d.ProductId);
                if (wip != null)
                {
                    wip.Quantity = Math.Max(0, (wip.Quantity ?? 0) - d.Quantity);
                    wip.LastUpdated = now;
                }
            }
        }

        receipt.Status = "Approved";
        await _context.SaveChangesAsync();

        string code = !string.IsNullOrWhiteSpace(receipt.ReceiptCode) ? receipt.ReceiptCode : $"LNK{receipt.ProductReceiptId:D5}";
        return Ok(new { success = true, message = $"Duyệt nhận thành phẩm phiếu {code} thành công. Đã cộng kho chính và cập nhật HSD." });
    }

    /// <summary>
    /// PUT /api/warehouse/product-receipts/{id}/reject : Từ chối nhận hàng
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectProductReceipt(int id, [FromBody] RejectDto? dto)
    {
        var receipt = await _context.ProductReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu có ID {id}." });
        }

        receipt.Status = "Rejected";
        var details = await _context.ProductReceiptDetails.Where(d => d.ProductReceiptId == id).ToListAsync();
        foreach (var d in details)
        {
            d.Status = "Rejected";
            if (!string.IsNullOrWhiteSpace(dto?.Reason)) d.Note = dto.Reason;
        }

        await _context.SaveChangesAsync();
        string code = !string.IsNullOrWhiteSpace(receipt.ReceiptCode) ? receipt.ReceiptCode : $"LNK{receipt.ProductReceiptId:D5}";
        return Ok(new { success = true, message = $"Đã từ chối nhận thành phẩm phiếu {code}." });
    }
}

