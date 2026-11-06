using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/material-receipts")]
[Produces("application/json")]
[Tags("5. Xuất nguyên vật liệu (Material Receipts)")]
public class MaterialReceiptApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MaterialReceiptApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/material-receipts : Danh sách tất cả lệnh/phiếu xuất NVL với đầy đủ chi tiết
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMaterialReceipts([FromQuery] string? status = null, [FromQuery] string? keyword = null)
    {
        var query = _context.MaterialReceipts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
        {
            if (status == "Chờ xuất kho" || status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => m.Status == "Pending" || m.Status == "Chờ xuất kho");
            else if (status == "Đã xuất kho" || status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => m.Status == "Approved" || m.Status == "Đã xuất kho");
            else if (status == "Từ chối" || status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => m.Status == "Rejected" || m.Status == "Từ chối");
        }

        var list = await query.OrderByDescending(m => m.CreatedDate).ToListAsync();
        var receiptIds = list.Select(m => m.ExpReceiptId).ToList();

        var details = await _context.MaterialReceiptDetails
            .AsNoTracking()
            .Where(d => receiptIds.Contains(d.ExpReceiptId))
            .ToListAsync();

        var ingrIds = details.Select(d => d.IngrId).Distinct().ToList();
        var ingrs = await _context.Ingredients
            .AsNoTracking()
            .Where(i => ingrIds.Contains(i.IngrId))
            .ToDictionaryAsync(i => i.IngrId);

        var batchExpiries = await _context.ImportReceiptDetails
            .AsNoTracking()
            .Where(b => ingrIds.Contains(b.IngrId) && b.ExpiredDate != null)
            .OrderBy(b => b.ExpiredDate)
            .GroupBy(b => b.IngrId)
            .ToDictionaryAsync(g => g.Key, g => g.First().ExpiredDate);

        var reqIds = list.Select(m => m.RequestId).Where(r => r.HasValue).Select(r => r!.Value).Distinct().ToList();
        var prodRequests = await _context.ProductionRequests
            .AsNoTracking()
            .Where(r => reqIds.Contains(r.RequestId))
            .ToDictionaryAsync(r => r.RequestId);

        var assignedUserIds = prodRequests.Values.Select(r => r.AssignedTo).Where(u => u.HasValue).Select(u => u!.Value).Distinct().ToList();
        var assignedUsers = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => assignedUserIds.Contains(u.UserID))
            .ToDictionaryAsync(u => u.UserID);

        var now = DateTime.UtcNow;

        var result = list.Select(m =>
        {
            var receiptDetails = details.Where(d => d.ExpReceiptId == m.ExpReceiptId).ToList();

            string department = "Phân xưởng Viên nén GMP";
            var firstNote = receiptDetails.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note))?.Note ?? "";
            if (firstNote.Contains("Cốm thuốc", StringComparison.OrdinalIgnoreCase))
                department = "Tổ Pha chế Cốm thuốc";
            else if (firstNote.Contains("Viên nén", StringComparison.OrdinalIgnoreCase))
                department = "Phân xưởng Viên nén GMP";
            else if (firstNote.Contains("Viên nang", StringComparison.OrdinalIgnoreCase))
                department = "Phân xưởng Viên nang GMP";
            else if (firstNote.Contains("Xưởng", StringComparison.OrdinalIgnoreCase))
                department = "Phân xưởng Sản xuất";

            string description = "";
            var loxMatch = System.Text.RegularExpressions.Regex.Match(firstNote, @"(Lô SX|LSX)[\w\-]+");
            if (loxMatch.Success)
            {
                description = $"Xuất nguyên liệu cho {loxMatch.Value}";
            }
            else if (!string.IsNullOrWhiteSpace(firstNote))
            {
                description = firstNote;
            }
            else
            {
                var names = receiptDetails.Take(2).Select(d => ingrs.TryGetValue(d.IngrId, out var ingr) ? ingr.IngrName : $"NVL{d.IngrId}");
                description = "Xuất " + string.Join(", ", names);
            }

            string displayStatus = "Chờ xuất kho";
            if (m.Status == "Approved" || m.Status == "Đã xuất kho" || m.Status == "Completed") displayStatus = "Đã xuất kho";
            else if (m.Status == "Rejected" || m.Status == "Từ chối") displayStatus = "Từ chối";

            int? assignedTo = null;
            string? assignedToName = null;
            string? assignedToEmployeeCode = null;
            string? requestCode = null;
            if (m.RequestId.HasValue && prodRequests.TryGetValue(m.RequestId.Value, out var prq))
            {
                requestCode = prq.RequestCode;
                assignedTo = prq.AssignedTo;
                if (prq.AssignedTo.HasValue && assignedUsers.TryGetValue(prq.AssignedTo.Value, out var au))
                {
                    assignedToName = !string.IsNullOrWhiteSpace(au.FullName) ? au.FullName : au.Username;
                    assignedToEmployeeCode = $"NV{au.UserID:D4}";
                }
            }

            var items = receiptDetails.Select(d =>
            {
                ingrs.TryGetValue(d.IngrId, out var ingr);
                var cost = d.UnitCost ?? ingr?.Price ?? 0;
                var stockQty = ingr?.Quantity ?? 0;
                var reqQty = d.Quantity;
                var isOutOfStock = stockQty < reqQty;
                var stockShortage = isOutOfStock ? (reqQty - stockQty) : 0;

                DateTime? expDate = null;
                if (!string.IsNullOrWhiteSpace(d.Note))
                {
                    var matchHsd = System.Text.RegularExpressions.Regex.Match(d.Note, @"HSD:\s*(\d{1,2}/\d{1,2}/\d{4})");
                    if (matchHsd.Success && DateTime.TryParseExact(matchHsd.Groups[1].Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedHsd))
                    {
                        expDate = DateTime.SpecifyKind(parsedHsd, DateTimeKind.Utc);
                    }
                }
                if (!expDate.HasValue && batchExpiries.TryGetValue(d.IngrId, out var bExp))
                {
                    expDate = bExp;
                }
                if (!expDate.HasValue)
                {
                    if (ingr?.ShelfLifeMonths != null && ingr.ShelfLifeMonths > 0)
                    {
                        expDate = now.AddMonths(ingr.ShelfLifeMonths.Value);
                    }
                    else
                    {
                        expDate = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc);
                    }
                }

                var daysRemaining = (expDate.Value.Date - now.Date).TotalDays;
                var isExpiringSoon = daysRemaining <= 1;
                string expiryWarning = "";
                if (daysRemaining <= 0) expiryWarning = "Đã hết hạn sử dụng";
                else if (daysRemaining <= 1) expiryWarning = "HSD chỉ còn <= 1 ngày";

                return new
                {
                    id = d.IngrId,
                    ingrId = d.IngrId,
                    itemCode = $"NVL{d.IngrId:D3}",
                    ingrCode = $"NVL{d.IngrId:D3}",
                    itemName = ingr?.IngrName ?? "N/A",
                    ingrName = ingr?.IngrName ?? "N/A",
                    unit = ingr?.Unit ?? "kg",
                    reqQty = d.Quantity,
                    actualQty = d.Quantity,
                    quantity = d.Quantity,
                    unitCost = cost,
                    price = cost,
                    amount = d.Quantity * cost,
                    status = d.Status,
                    note = d.Note,
                    stockQty = stockQty,
                    stockQtyFormatted = stockQty.ToString("N2"),
                    isOutOfStock = isOutOfStock,
                    stockShortage = stockShortage,
                    expiryDate = expDate.Value.ToString("dd/MM/yyyy"),
                    daysRemaining = daysRemaining,
                    isExpiringSoon = isExpiringSoon,
                    expiryWarning = expiryWarning,
                    isFull = true
                };
            }).ToList();

            var hasStockIssue = items.Any(it => it.isOutOfStock);
            var hasExpiryIssue = items.Any(it => it.isExpiringSoon);
            var isPending = m.Status == "Pending" || m.Status == "Chờ xuất kho";
            var canApprove = isPending && !hasStockIssue && !hasExpiryIssue;

            string approvalBlockReason = "";
            if (hasStockIssue)
            {
                var firstStockIssue = items.First(it => it.isOutOfStock);
                approvalBlockReason = $"Không đủ tồn kho: '{firstStockIssue.itemName}' chỉ còn {firstStockIssue.stockQty:N2} {firstStockIssue.unit} (yêu cầu {firstStockIssue.reqQty:N2} {firstStockIssue.unit}).";
            }
            else if (hasExpiryIssue)
            {
                var firstExpIssue = items.First(it => it.isExpiringSoon);
                approvalBlockReason = $"Hạn sử dụng không hợp lệ: '{firstExpIssue.itemName}' {firstExpIssue.expiryWarning} (HSD: {firstExpIssue.expiryDate}).";
            }

            return new
            {
                id = m.ExpReceiptId,
                expReceiptId = m.ExpReceiptId,
                orderNo = m.ExpReceiptCode ?? $"PXK{m.ExpReceiptId:D5}",
                orderDate = m.CreatedDate.HasValue ? m.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
                createdDate = m.CreatedDate.HasValue ? m.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                customer = department,
                department,
                deliveryDue = m.CreatedDate.HasValue ? m.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
                dueDate = m.CreatedDate.HasValue ? m.CreatedDate.Value.ToString("dd/MM/yyyy 00:00:00") : "",
                description,
                status = m.Status,
                orderStatus = displayStatus,
                employeeName = "Nguyễn Tiến Huy",
                employeeCode = "NV0005",
                requestId = m.RequestId,
                requestCode,
                assignedTo,
                assignedToName,
                assignedToEmployeeCode,
                totalItems = items.Count,
                totalQuantity = items.Sum(i => i.quantity),
                totalAmount = items.Sum(i => i.amount),
                canApprove,
                hasStockIssue,
                hasExpiryIssue,
                approvalBlockReason,
                items
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            result = result.Where(r =>
                (r.orderNo != null && r.orderNo.ToLower().Contains(kw)) ||
                (r.customer != null && r.customer.ToLower().Contains(kw)) ||
                (r.description != null && r.description.ToLower().Contains(kw)) ||
                (r.orderStatus != null && r.orderStatus.ToLower().Contains(kw))
            ).ToList();
        }

        return Ok(result);
    }

    /// <summary>
    /// GET /api/warehouse/material-receipts/pending : Danh sách phiếu xin vật tư từ xưởng chờ duyệt
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingMaterialReceipts()
    {
        var list = await _context.MaterialReceipts
            .AsNoTracking()
            .Where(m => m.Status == "Pending" || m.Status == "Chờ xuất kho")
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

        var result = list.Select(m => new
        {
            expReceiptId = m.ExpReceiptId,
            expReceiptCode = m.ExpReceiptCode ?? $"PXK{m.ExpReceiptId:D5}",
            createdDate = m.CreatedDate.HasValue ? m.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            status = m.Status,
            requestId = m.RequestId
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/warehouse/material-receipts/{id} : Xem chi tiết phiếu xuất NVL
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMaterialReceiptById(int id)
    {
        var receipt = await _context.MaterialReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu xuất có ID {id}." });
        }

        var details = await _context.MaterialReceiptDetails.Where(d => d.ExpReceiptId == id).ToListAsync();
        var ingrIds = details.Select(d => d.IngrId).Distinct().ToList();
        var ingrs = await _context.Ingredients.Where(i => ingrIds.Contains(i.IngrId)).ToDictionaryAsync(i => i.IngrId);

        var batchExpiries = await _context.ImportReceiptDetails
            .AsNoTracking()
            .Where(b => ingrIds.Contains(b.IngrId) && b.ExpiredDate != null)
            .OrderBy(b => b.ExpiredDate)
            .GroupBy(b => b.IngrId)
            .ToDictionaryAsync(g => g.Key, g => g.First().ExpiredDate);

        int? assignedTo = null;
        string? assignedToName = null;
        string? assignedToEmployeeCode = null;
        string? requestCode = null;
        if (receipt.RequestId.HasValue)
        {
            var prq = await _context.ProductionRequests.FindAsync(receipt.RequestId.Value);
            if (prq != null)
            {
                requestCode = prq.RequestCode;
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
        }

        var now = DateTime.UtcNow;

        var items = details.Select(d =>
        {
            ingrs.TryGetValue(d.IngrId, out var ingr);
            var cost = d.UnitCost ?? ingr?.Price ?? 0;
            var stockQty = ingr?.Quantity ?? 0;
            var reqQty = d.Quantity;
            var isOutOfStock = stockQty < reqQty;
            var stockShortage = isOutOfStock ? (reqQty - stockQty) : 0;

            DateTime? expDate = null;
            if (!string.IsNullOrWhiteSpace(d.Note))
            {
                var matchHsd = System.Text.RegularExpressions.Regex.Match(d.Note, @"HSD:\s*(\d{1,2}/\d{1,2}/\d{4})");
                if (matchHsd.Success && DateTime.TryParseExact(matchHsd.Groups[1].Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedHsd))
                {
                    expDate = DateTime.SpecifyKind(parsedHsd, DateTimeKind.Utc);
                }
            }
            if (!expDate.HasValue && batchExpiries.TryGetValue(d.IngrId, out var bExp))
            {
                expDate = bExp;
            }
            if (!expDate.HasValue)
            {
                if (ingr?.ShelfLifeMonths != null && ingr.ShelfLifeMonths > 0)
                {
                    expDate = now.AddMonths(ingr.ShelfLifeMonths.Value);
                }
                else
                {
                    expDate = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc);
                }
            }

            var daysRemaining = (expDate.Value.Date - now.Date).TotalDays;
            var isExpiringSoon = daysRemaining <= 1;
            string expiryWarning = "";
            if (daysRemaining <= 0) expiryWarning = "Đã hết hạn sử dụng";
            else if (daysRemaining <= 1) expiryWarning = "HSD chỉ còn <= 1 ngày";

            return new
            {
                id = d.IngrId,
                ingrId = d.IngrId,
                itemCode = $"NVL{d.IngrId:D3}",
                ingrCode = $"NVL{d.IngrId:D3}",
                itemName = ingr?.IngrName ?? "N/A",
                ingrName = ingr?.IngrName ?? "N/A",
                unit = ingr?.Unit ?? "kg",
                quantity = d.Quantity,
                reqQty = d.Quantity,
                actualQty = d.Quantity,
                unitCost = cost,
                price = cost,
                amount = d.Quantity * cost,
                status = d.Status,
                note = d.Note,
                stockQty = stockQty,
                stockQtyFormatted = stockQty.ToString("N2"),
                isOutOfStock = isOutOfStock,
                stockShortage = stockShortage,
                expiryDate = expDate.Value.ToString("dd/MM/yyyy"),
                daysRemaining = daysRemaining,
                isExpiringSoon = isExpiringSoon,
                expiryWarning = expiryWarning
            };
        }).ToList();

        var hasStockIssue = items.Any(it => it.isOutOfStock);
        var hasExpiryIssue = items.Any(it => it.isExpiringSoon);
        var isPending = receipt.Status == "Pending" || receipt.Status == "Chờ xuất kho";
        var canApprove = isPending && !hasStockIssue && !hasExpiryIssue;

        string approvalBlockReason = "";
        if (hasStockIssue)
        {
            var firstStockIssue = items.First(it => it.isOutOfStock);
            approvalBlockReason = $"Không đủ tồn kho: '{firstStockIssue.itemName}' chỉ còn {firstStockIssue.stockQty:N2} {firstStockIssue.unit} (yêu cầu {firstStockIssue.reqQty:N2} {firstStockIssue.unit}).";
        }
        else if (hasExpiryIssue)
        {
            var firstExpIssue = items.First(it => it.isExpiringSoon);
            approvalBlockReason = $"Hạn sử dụng không hợp lệ: '{firstExpIssue.itemName}' {firstExpIssue.expiryWarning} (HSD: {firstExpIssue.expiryDate}).";
        }

        return Ok(new
        {
            receipt.ExpReceiptId,
            expReceiptCode = receipt.ExpReceiptCode ?? $"PXK{receipt.ExpReceiptId:D5}",
            receipt.CreatedDate,
            receipt.Status,
            receipt.RequestId,
            requestCode,
            assignedTo,
            assignedToName,
            assignedToEmployeeCode,
            canApprove,
            hasStockIssue,
            hasExpiryIssue,
            approvalBlockReason,
            items
        });
    }

    /// <summary>
    /// PUT /api/warehouse/material-receipts/{id}/approve : Duyệt cấp phát vật tư
    /// Trừ kho chính -> Đẩy vào Tồn kho xưởng (WIP)
    /// Kiểm tra ràng buộc: Tồn kho >= Yêu cầu & HSD còn trên 1 ngày
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveMaterialReceipt(int id)
    {
        var receipt = await _context.MaterialReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu xuất có ID {id}." });
        }

        if (receipt.Status == "Approved" || receipt.Status == "Đã xuất kho")
        {
            return BadRequest(new { message = "Phiếu này đã được duyệt xuất kho trước đó." });
        }

        var details = await _context.MaterialReceiptDetails.Where(d => d.ExpReceiptId == id).ToListAsync();
        var now = DateTime.UtcNow;

        // --- 1. KIỂM TRA ĐIỀU KIỆN TỒN KHO & HẠN SỬ DỤNG ---
        foreach (var d in details)
        {
            var ingr = await _context.Ingredients.FindAsync(d.IngrId);
            if (ingr == null)
            {
                return BadRequest(new { success = false, message = $"Không tìm thấy nguyên vật liệu có ID {d.IngrId}." });
            }

            // Điều kiện 1: Tồn kho < Số lượng yêu cầu xuất
            var currentStock = ingr.Quantity ?? 0;
            if (currentStock < d.Quantity)
            {
                var shortage = d.Quantity - currentStock;
                return BadRequest(new
                {
                    success = false,
                    message = $"Không thể duyệt phiếu: Nguyên vật liệu '{ingr.IngrName}' (NVL{ingr.IngrId:D3}) chỉ còn tồn kho {currentStock:N2} {ingr.Unit}, không đủ số lượng yêu cầu xuất ({d.Quantity:N2} {ingr.Unit}). Thiếu {shortage:N2} {ingr.Unit}."
                });
            }

            // Điều kiện 2: Hạn sử dụng của nguyên vật liệu nếu chỉ còn <= 1 ngày thì không duyệt được
            DateTime? expDate = null;
            if (!string.IsNullOrWhiteSpace(d.Note))
            {
                var matchHsd = System.Text.RegularExpressions.Regex.Match(d.Note, @"HSD:\s*(\d{1,2}/\d{1,2}/\d{4})");
                if (matchHsd.Success && DateTime.TryParseExact(matchHsd.Groups[1].Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedHsd))
                {
                    expDate = DateTime.SpecifyKind(parsedHsd, DateTimeKind.Utc);
                }
            }

            if (!expDate.HasValue)
            {
                var batchExpiry = await _context.ImportReceiptDetails
                    .Where(b => b.IngrId == d.IngrId && b.ExpiredDate != null)
                    .OrderBy(b => b.ExpiredDate)
                    .Select(b => b.ExpiredDate)
                    .FirstOrDefaultAsync();

                if (batchExpiry.HasValue)
                {
                    expDate = batchExpiry.Value;
                }
                else if (ingr.ShelfLifeMonths.HasValue && ingr.ShelfLifeMonths.Value > 0)
                {
                    expDate = now.AddMonths(ingr.ShelfLifeMonths.Value);
                }
            }

            if (expDate.HasValue)
            {
                var daysRemaining = (expDate.Value.Date - now.Date).TotalDays;
                if (daysRemaining <= 1)
                {
                    var warningText = daysRemaining <= 0
                        ? $"đã hết hạn sử dụng vào ngày {expDate.Value:dd/MM/yyyy}"
                        : $"hạn sử dụng chỉ còn {Math.Max(0, (int)daysRemaining)} ngày (hết hạn ngày {expDate.Value:dd/MM/yyyy})";

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Không thể duyệt phiếu: Nguyên vật liệu '{ingr.IngrName}' (NVL{ingr.IngrId:D3}) {warningText}, không đủ điều kiện xuất kho (yêu cầu HSD phải còn trên 1 ngày)."
                    });
                }
            }
        }

        // --- 2. HỢP LỆ -> TIẾN HÀNH TRỪ TỒN KHO & CẬP NHẬT WIP ---
        foreach (var d in details)
        {
            var ingr = await _context.Ingredients.FindAsync(d.IngrId);
            if (ingr != null)
            {
                ingr.Quantity = Math.Max(0, (ingr.Quantity ?? 0) - d.Quantity);
            }

            d.Status = "Approved";
            d.ExportDate = now;

            if (receipt.RequestId.HasValue)
            {
                var existingWip = await _context.WipInventories
                    .FirstOrDefaultAsync(w => w.RequestId == receipt.RequestId.Value && w.IngrId == d.IngrId);

                if (existingWip != null)
                {
                    existingWip.Quantity = (existingWip.Quantity ?? 0) + d.Quantity;
                    existingWip.LastUpdated = now;
                }
                else
                {
                    _context.WipInventories.Add(new WipInventory
                    {
                        RequestId = receipt.RequestId.Value,
                        IngrId = d.IngrId,
                        Quantity = d.Quantity,
                        LastUpdated = now
                    });
                }
            }
        }

        receipt.Status = "Approved";
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Duyệt cấp phát vật tư phiếu {receipt.ExpReceiptCode} thành công. Đã chuyển NVL sang WIP xưởng." });
    }

    /// <summary>
    /// PUT /api/warehouse/material-receipts/{id}/reject : Từ chối cấp vật tư
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectMaterialReceipt(int id, [FromBody] RejectDto? dto)
    {
        var receipt = await _context.MaterialReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu xuất có ID {id}." });
        }

        receipt.Status = "Rejected";
        var details = await _context.MaterialReceiptDetails.Where(d => d.ExpReceiptId == id).ToListAsync();
        foreach (var d in details)
        {
            d.Status = "Rejected";
            if (!string.IsNullOrWhiteSpace(dto?.Reason)) d.Note = dto.Reason;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = $"Đã từ chối cấp phát vật tư cho phiếu {receipt.ExpReceiptCode}." });
    }
}

