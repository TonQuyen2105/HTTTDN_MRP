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
        // Chỉ hiển thị các phiếu xuất kho nguyên vật liệu, không hiển thị phiếu hủy NVL hết hạn (mã HMY-...)
        var query = _context.MaterialReceipts.AsNoTracking()
            .Where(m => m.ExpReceiptCode == null || !m.ExpReceiptCode.StartsWith("HMY-"));

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

        // Loại bỏ thêm các phiếu có toàn bộ chi tiết mang trạng thái/ghi chú hủy NVL hết hạn
        var disposedReceiptIds = details
            .GroupBy(d => d.ExpReceiptId)
            .Where(g => g.All(d => d.Status == "Đã hủy" || (d.Note != null && d.Note.StartsWith("Hủy nguyên vật liệu hết hạn", StringComparison.OrdinalIgnoreCase))))
            .Select(g => g.Key)
            .ToHashSet();

        if (disposedReceiptIds.Count > 0)
        {
            list = list.Where(m => !disposedReceiptIds.Contains(m.ExpReceiptId)).ToList();
            details = details.Where(d => !disposedReceiptIds.Contains(d.ExpReceiptId)).ToList();
        }

        var ingrIds = details.Select(d => d.IngrId).Distinct().ToList();
        var ingrs = await _context.Ingredients
            .AsNoTracking()
            .Where(i => ingrIds.Contains(i.IngrId))
            .ToDictionaryAsync(i => i.IngrId);

        var disposedBatches = await (
            from mr in _context.MaterialReceipts.AsNoTracking()
            join mrd in _context.MaterialReceiptDetails.AsNoTracking() on mr.ExpReceiptId equals mrd.ExpReceiptId
            where mr.ExpReceiptCode != null && mr.ExpReceiptCode.StartsWith("HMY-")
            select new { mr.ExpReceiptCode, mrd.IngrId }
        ).ToListAsync();

        var importReceiptsMap = await _context.ImportReceipts.AsNoTracking()
            .ToDictionaryAsync(r => r.IpReceiptId, r => r.IpReceiptCode);

        var rawImportBatches = await _context.ImportReceiptDetails
            .AsNoTracking()
            .Where(b => ingrIds.Contains(b.IngrId) && b.ExpiredDate != null)
            .ToListAsync();

        var importBatches = rawImportBatches.Where(b =>
        {
            var code = importReceiptsMap.TryGetValue(b.IpReceiptId, out var c) ? c : $"PNK{b.IpReceiptId:D5}";
            bool isDisposed = disposedBatches.Any(d => d.IngrId == b.IngrId && (d.ExpReceiptCode == $"HMY-{code}" || d.ExpReceiptCode == $"HMY-PNK{b.IpReceiptId:D5}"));
            return !isDisposed;
        }).ToList();

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
                requestCode = !string.IsNullOrWhiteSpace(prq.RequestCode) ? prq.RequestCode : $"LSX{prq.RequestId:D5}";
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
                var stockQty = ingr?.Quantity ?? 0m;
                var reqQty = d.Quantity;
                var isOutOfStock = stockQty < reqQty;
                var stockShortage = isOutOfStock ? (reqQty - stockQty) : 0m;

                // Lô hàng của nguyên vật liệu
                var ingrBatches = importBatches.Where(b => b.IngrId == d.IngrId).ToList();
                var expiredBatchSum = ingrBatches
                    .Where(b => b.ExpiredDate != null && b.ExpiredDate.Value <= now)
                    .Sum(b => b.Quantity);

                var expiredStock = Math.Min(stockQty, expiredBatchSum);
                var validStock = Math.Max(0m, stockQty - expiredStock);

                string conditionStatus;
                string conditionType;
                bool isItemEligible = false;

                if (stockQty <= 0 && expiredStock == 0)
                {
                    conditionStatus = "Thiếu tồn kho";
                    conditionType = "danger";
                }
                else if (validStock <= 0)
                {
                    conditionStatus = "Tồn kho hết hạn";
                    conditionType = "danger";
                }
                else if (validStock < reqQty)
                {
                    conditionStatus = "Thiếu số lượng đạt chuẩn";
                    conditionType = "warning";
                }
                else
                {
                    conditionStatus = "Đủ điều kiện";
                    conditionType = "success";
                    isItemEligible = true;
                }

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
                    var validBatches = ingrBatches.Where(b => b.ExpiredDate != null && b.ExpiredDate.Value > now).OrderBy(b => b.ExpiredDate).ToList();
                    if (validBatches.Any())
                    {
                        expDate = validBatches.First().ExpiredDate;
                    }
                    else if (ingrBatches.Any(b => b.ExpiredDate != null))
                    {
                        expDate = ingrBatches.OrderBy(b => b.ExpiredDate).First().ExpiredDate;
                    }
                    else if (ingr?.ShelfLifeMonths != null && ingr.ShelfLifeMonths > 0)
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
                    expiredStockQty = expiredStock,
                    validStockQty = validStock,
                    conditionStatus = conditionStatus,
                    conditionType = conditionType,
                    isItemEligible = isItemEligible,
                    expiryDate = expDate.Value.ToString("dd/MM/yyyy"),
                    daysRemaining = daysRemaining,
                    isExpiringSoon = isExpiringSoon,
                    expiryWarning = expiryWarning,
                    isFull = true
                };
            }).ToList();

            var hasStockIssue = items.Any(it => it.conditionStatus == "Thiếu tồn kho");
            var hasExpiryIssue = items.Any(it => it.conditionStatus == "Tồn kho hết hạn" || it.conditionStatus == "Hàng tồn đã hết hạn" || it.conditionStatus == "Thiếu số lượng đạt chuẩn");
            var isPending = m.Status == "Pending" || m.Status == "Chờ xuất kho";
            var canApprove = isPending && items.All(it => it.isItemEligible);

            string approvalBlockReason = "";
            var expiredItem = items.FirstOrDefault(it => it.conditionStatus == "Tồn kho hết hạn" || it.conditionStatus == "Hàng tồn đã hết hạn");
            var partialExpiredItem = items.FirstOrDefault(it => it.conditionStatus == "Thiếu số lượng đạt chuẩn");
            var outOfStockItem = items.FirstOrDefault(it => it.conditionStatus == "Thiếu tồn kho");

            if (expiredItem != null)
            {
                approvalBlockReason = $"Hàng tồn kho của '{expiredItem.itemName}' đã hết hạn sử dụng (Tồn kho hết hạn).";
            }
            else if (partialExpiredItem != null)
            {
                approvalBlockReason = $"Nguyên vật liệu '{partialExpiredItem.itemName}' thiếu số lượng đạt chuẩn (tồn đạt chuẩn: {partialExpiredItem.validStockQty:N2} {partialExpiredItem.unit}, hết hạn: {partialExpiredItem.expiredStockQty:N2} {partialExpiredItem.unit}, yêu cầu: {partialExpiredItem.reqQty:N2} {partialExpiredItem.unit}).";
            }
            else if (outOfStockItem != null)
            {
                approvalBlockReason = $"Không đủ tồn kho: '{outOfStockItem.itemName}' chỉ còn {outOfStockItem.stockQty:N2} {outOfStockItem.unit} (yêu cầu {outOfStockItem.reqQty:N2} {outOfStockItem.unit}).";
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
                productionRequestCode = requestCode,
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

        var disposedBatches = await (
            from mr in _context.MaterialReceipts.AsNoTracking()
            join mrd in _context.MaterialReceiptDetails.AsNoTracking() on mr.ExpReceiptId equals mrd.ExpReceiptId
            where mr.ExpReceiptCode != null && mr.ExpReceiptCode.StartsWith("HMY-")
            select new { mr.ExpReceiptCode, mrd.IngrId }
        ).ToListAsync();

        var importReceiptsMap = await _context.ImportReceipts.AsNoTracking()
            .ToDictionaryAsync(r => r.IpReceiptId, r => r.IpReceiptCode);

        var rawImportBatches = await _context.ImportReceiptDetails
            .AsNoTracking()
            .Where(b => ingrIds.Contains(b.IngrId) && b.ExpiredDate != null)
            .ToListAsync();

        var importBatches = rawImportBatches.Where(b =>
        {
            var code = importReceiptsMap.TryGetValue(b.IpReceiptId, out var c) ? c : $"PNK{b.IpReceiptId:D5}";
            bool isDisposed = disposedBatches.Any(d => d.IngrId == b.IngrId && (d.ExpReceiptCode == $"HMY-{code}" || d.ExpReceiptCode == $"HMY-PNK{b.IpReceiptId:D5}"));
            return !isDisposed;
        }).ToList();

        int? assignedTo = null;
        string? assignedToName = null;
        string? assignedToEmployeeCode = null;
        string? requestCode = null;
        if (receipt.RequestId.HasValue)
        {
            var prq = await _context.ProductionRequests.FindAsync(receipt.RequestId.Value);
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
        }

        var now = DateTime.UtcNow;

        var items = details.Select(d =>
        {
            ingrs.TryGetValue(d.IngrId, out var ingr);
            var cost = d.UnitCost ?? ingr?.Price ?? 0;
            var stockQty = ingr?.Quantity ?? 0m;
            var reqQty = d.Quantity;
            var isOutOfStock = stockQty < reqQty;
            var stockShortage = isOutOfStock ? (reqQty - stockQty) : 0m;

            var ingrBatches = importBatches.Where(b => b.IngrId == d.IngrId).ToList();
            var expiredBatchSum = ingrBatches
                .Where(b => b.ExpiredDate != null && b.ExpiredDate.Value <= now)
                .Sum(b => b.Quantity);

            var expiredStock = Math.Min(stockQty, expiredBatchSum);
            var validStock = Math.Max(0m, stockQty - expiredStock);

            string conditionStatus;
            string conditionType;
            bool isItemEligible = false;

            if (stockQty <= 0 && expiredStock == 0)
            {
                conditionStatus = "Thiếu tồn kho";
                conditionType = "danger";
            }
            else if (validStock <= 0)
            {
                conditionStatus = "Tồn kho hết hạn";
                conditionType = "danger";
            }
            else if (validStock < reqQty)
            {
                conditionStatus = "Thiếu số lượng đạt chuẩn";
                conditionType = "warning";
            }
            else
            {
                conditionStatus = "Đủ điều kiện";
                conditionType = "success";
                isItemEligible = true;
            }

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
                var validBatches = ingrBatches.Where(b => b.ExpiredDate != null && b.ExpiredDate.Value > now).OrderBy(b => b.ExpiredDate).ToList();
                if (validBatches.Any())
                {
                    expDate = validBatches.First().ExpiredDate;
                }
                else if (ingrBatches.Any(b => b.ExpiredDate != null))
                {
                    expDate = ingrBatches.OrderBy(b => b.ExpiredDate).First().ExpiredDate;
                }
                else if (ingr?.ShelfLifeMonths != null && ingr.ShelfLifeMonths > 0)
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
                expiredStockQty = expiredStock,
                validStockQty = validStock,
                conditionStatus = conditionStatus,
                conditionType = conditionType,
                isItemEligible = isItemEligible,
                expiryDate = expDate.Value.ToString("dd/MM/yyyy"),
                daysRemaining = daysRemaining,
                isExpiringSoon = isExpiringSoon,
                expiryWarning = expiryWarning
            };
        }).ToList();

        var hasStockIssue = items.Any(it => it.conditionStatus == "Thiếu tồn kho");
        var hasExpiryIssue = items.Any(it => it.conditionStatus == "Tồn kho hết hạn" || it.conditionStatus == "Hàng tồn đã hết hạn" || it.conditionStatus == "Thiếu số lượng đạt chuẩn");
        var isPending = receipt.Status == "Pending" || receipt.Status == "Chờ xuất kho";
        var canApprove = isPending && items.All(it => it.isItemEligible);

        string approvalBlockReason = "";
        var expiredItem = items.FirstOrDefault(it => it.conditionStatus == "Tồn kho hết hạn" || it.conditionStatus == "Hàng tồn đã hết hạn");
        var partialExpiredItem = items.FirstOrDefault(it => it.conditionStatus == "Thiếu số lượng đạt chuẩn");
        var outOfStockItem = items.FirstOrDefault(it => it.conditionStatus == "Thiếu tồn kho");

        if (expiredItem != null)
        {
            approvalBlockReason = $"Hàng tồn kho của '{expiredItem.itemName}' đã hết hạn sử dụng (Tồn kho hết hạn).";
        }
        else if (partialExpiredItem != null)
        {
            approvalBlockReason = $"Nguyên vật liệu '{partialExpiredItem.itemName}' thiếu số lượng đạt chuẩn (tồn đạt chuẩn: {partialExpiredItem.validStockQty:N2} {partialExpiredItem.unit}, hết hạn: {partialExpiredItem.expiredStockQty:N2} {partialExpiredItem.unit}, yêu cầu: {partialExpiredItem.reqQty:N2} {partialExpiredItem.unit}).";
        }
        else if (outOfStockItem != null)
        {
            approvalBlockReason = $"Không đủ tồn kho: '{outOfStockItem.itemName}' chỉ còn {outOfStockItem.stockQty:N2} {outOfStockItem.unit} (yêu cầu {outOfStockItem.reqQty:N2} {outOfStockItem.unit}).";
        }

        return Ok(new
        {
            receipt.ExpReceiptId,
            expReceiptCode = receipt.ExpReceiptCode ?? $"PXK{receipt.ExpReceiptId:D5}",
            receipt.CreatedDate,
            receipt.Status,
            receipt.RequestId,
            requestCode,
            productionRequestCode = requestCode,
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

        var disposedBatches = await (
            from mr in _context.MaterialReceipts.AsNoTracking()
            join mrd in _context.MaterialReceiptDetails.AsNoTracking() on mr.ExpReceiptId equals mrd.ExpReceiptId
            where mr.ExpReceiptCode != null && mr.ExpReceiptCode.StartsWith("HMY-")
            select new { mr.ExpReceiptCode, mrd.IngrId }
        ).ToListAsync();

        var importReceiptsMap = await _context.ImportReceipts.AsNoTracking()
            .ToDictionaryAsync(r => r.IpReceiptId, r => r.IpReceiptCode);

        // --- 1. KIỂM TRA ĐIỀU KIỆN TỒN KHO & HẠN SỬ DỤNG ---
        foreach (var d in details)
        {
            var ingr = await _context.Ingredients.FindAsync(d.IngrId);
            if (ingr == null)
            {
                return BadRequest(new { success = false, message = $"Không tìm thấy nguyên vật liệu có ID {d.IngrId}." });
            }

            var currentStock = ingr.Quantity ?? 0m;
            var reqQty = d.Quantity;

            // Lấy tất cả các lô nhập còn hiệu lực (chưa bị hủy)
            var rawBatches = await _context.ImportReceiptDetails
                .AsNoTracking()
                .Where(b => b.IngrId == d.IngrId && b.ExpiredDate != null)
                .ToListAsync();

            var activeBatches = rawBatches.Where(b =>
            {
                var code = importReceiptsMap.TryGetValue(b.IpReceiptId, out var c) ? c : $"PNK{b.IpReceiptId:D5}";
                bool isDisposed = disposedBatches.Any(db => db.IngrId == b.IngrId && (db.ExpReceiptCode == $"HMY-{code}" || db.ExpReceiptCode == $"HMY-PNK{b.IpReceiptId:D5}"));
                return !isDisposed;
            }).ToList();

            // Tính tổng số lượng từ các lô đã hết hạn
            var expiredBatchSum = activeBatches
                .Where(b => b.ExpiredDate != null && b.ExpiredDate.Value <= now)
                .Sum(b => b.Quantity);

            var expiredStock = Math.Min(currentStock, expiredBatchSum);
            var validStock = Math.Max(0m, currentStock - expiredStock);

            // Điều kiện 1: Tồn kho <= 0 và không có lô hết hạn -> thiếu tồn kho
            if (currentStock <= 0 && expiredStock == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Không thể duyệt phiếu: Nguyên vật liệu '{ingr.IngrName}' (NVL{ingr.IngrId:D3}) đã hết tồn kho."
                });
            }

            // Điều kiện 2: Tồn kho hết hạn (tổng tồn - hết hạn <= 0)
            if (validStock <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Không thể duyệt phiếu: Toàn bộ hàng tồn kho ({currentStock:N2} {ingr.Unit}) của nguyên vật liệu '{ingr.IngrName}' (NVL{ingr.IngrId:D3}) đã hết hạn sử dụng (Tồn kho hết hạn)."
                });
            }

            // Điều kiện 3: Thiếu số lượng đạt chuẩn (tổng tồn - hết hạn < yêu cầu)
            if (validStock < reqQty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Không thể duyệt phiếu: Nguyên vật liệu '{ingr.IngrName}' (NVL{ingr.IngrId:D3}) thiếu số lượng đạt chuẩn (tồn đạt chuẩn: {validStock:N2} {ingr.Unit}, đã hết hạn: {expiredStock:N2} {ingr.Unit}, yêu cầu xuất: {reqQty:N2} {ingr.Unit})."
                });
            }

            // Điều kiện 4: Hạn sử dụng của nguyên vật liệu nếu chỉ còn <= 1 ngày thì không duyệt được
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
                var batchExpiry = activeBatches
                    .Where(b => b.ExpiredDate != null && b.ExpiredDate.Value > now)
                    .OrderBy(b => b.ExpiredDate)
                    .Select(b => b.ExpiredDate)
                    .FirstOrDefault();

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

