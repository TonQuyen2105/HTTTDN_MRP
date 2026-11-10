using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using httt.Data;
using httt.Models;

namespace httt.Services;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý Bàn giao thành phẩm:
/// 1. Tổng hợp số lượng có thể bàn giao (đối soát 2 chiều giữa Đã sản xuất - Đã nhận và WIP thành phẩm).
/// 2. Bàn giao từng phần (Partial Handover) an toàn.
/// 3. Lập và quản lý phiếu bàn giao riêng biệt (tách rời sản lượng xưởng khỏi phiếu kho).
/// </summary>
public class ProductionHandoverService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductionHandoverService> _logger;

    public ProductionHandoverService(ApplicationDbContext context, ILogger<ProductionHandoverService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lấy dữ liệu cho trang Bàn giao thành phẩm (2 tab: Chờ bàn giao & Phiếu bàn giao)
    /// </summary>
    public async Task<HandoverPageViewModel> GetHandoverPageDataAsync(string? searchKeyword = null, string? statusFilter = null, string? activeTab = "pending")
    {
        var model = new HandoverPageViewModel
        {
            ActiveTab = string.Equals(activeTab, "slips", StringComparison.OrdinalIgnoreCase) ? "slips" : "pending"
        };

        try
        {
            // 1. Tải toàn bộ đơn sản xuất (loại trừ đã hủy nếu có)
            var requests = await _context.ProductionRequests
                .AsNoTracking()
                .Include(r => r.Product)
                .Include(r => r.Manager)
                .OrderByDescending(r => r.RequestId)
                .ToListAsync();

            // 2. Tải tất cả phiếu ProductReceipt và chi tiết
            var allReceipts = await _context.ProductReceipts
                .AsNoTracking()
                .Include(r => r.Requester)
                .Include(r => r.Approver)
                .Include(r => r.ProductionRequest)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(r => r.ProductReceiptId)
                .ToListAsync();

            // 3. Tải tồn kho sản xuất (WIP) của thành phẩm: ingrid == null && productid != null
            var wipFinishedGoods = await _context.WipInventories
                .AsNoTracking()
                .Where(w => w.IngrId == null && w.ProductId != null)
                .ToListAsync();

            // Phân loại:
            // a) Phiếu bàn giao độc lập mới: ReceiptCode != null
            var handoverSlips = allReceipts.Where(r => !string.IsNullOrWhiteSpace(r.ReceiptCode)).ToList();

            // b) Dòng tích lũy sản lượng cũ: ReceiptCode == null
            var legacyAccumulators = allReceipts.Where(r => string.IsNullOrWhiteSpace(r.ReceiptCode)).ToList();

            var productionLogs = await _context.ProductionLogs.AsNoTracking().OrderBy(l => l.ProducedAt).ThenBy(l => l.LogId).ToListAsync();

            // --- TAB 1: XÂY DỰNG DANH SÁCH CHỜ BÀN GIAO ---
            var pendingItems = new List<PendingHandoverItemDto>();

            foreach (var req in requests)
            {
                var prod = req.Product;
                if (prod == null && req.ProductId > 0)
                {
                    prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == req.ProductId);
                }

                int prodId = req.ProductId;
                string prodName = prod?.ProductName ?? "Sản phẩm thuốc";
                string unit = prod?.Unit ?? "Hộp";

                // Tồn kho sản xuất (WIP) thực tế tại xưởng
                var wipItem = wipFinishedGoods.FirstOrDefault(w => w.RequestId == req.RequestId && w.ProductId == prodId);
                decimal wipQty = wipItem?.Quantity ?? 0m;
                int wipQtyInt = (int)Math.Floor(wipQty);

                // Tổng đã sản xuất từ dòng tích lũy cũ (hoặc sau này từ bảng production_log)
                var legacyRows = legacyAccumulators.Where(r => r.RequestId == req.RequestId).ToList();
                int totalProduced = legacyRows.SelectMany(r => r.Details)
                    .Where(d => d.ProductId == prodId)
                    .Sum(d => d.Quantity);

                // Phiếu bàn giao của yêu cầu này
                var slipsForReq = HandoverStatus.ForAllocation(handoverSlips.Where(r => r.RequestId == req.RequestId)).ToList();

                // Đang chờ kho nhận: phiếu chưa được kho duyệt (chưa Approved / Completed / Received)
                int pendingHandover = slipsForReq
                    .Where(r => !string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(r.Status, "Received", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(r => r.Details)
                    .Where(d => d.ProductId == prodId)
                    .Sum(d => d.Quantity);

                // Kho đã duyệt nhận: phiếu có trạng thái Approved (hoặc Completed / Received)
                int warehouseReceived = slipsForReq
                    .Where(r => string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.Status, "Received", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(r => r.Details)
                    .Where(d => d.ProductId == prodId)
                    .Sum(d => d.Quantity);

                // --- ĐỐI SOÁT 2 CHIỀU CHẶT CHẼ ---
                // Công thức 1: Có thể lập bàn giao = Đã sản xuất − Kho đã nhận − Đang chờ kho nhận
                int availByFormula1 = totalProduced - warehouseReceived - pendingHandover;

                // Công thức 2: Có thể lập bàn giao = WIP thành phẩm − Đang chờ kho nhận
                int availByFormula2 = wipQtyInt - pendingHandover;

                // Kiểm tra tính nhất quán: (Đã sản xuất - Kho đã nhận) phải bằng WIP thành phẩm tại xưởng
                int expectedWip = totalProduced - warehouseReceived;
                int diff = expectedWip - wipQtyInt;
                bool isConsistent = (diff == 0);

                int availableForHandover = 0;
                string? discrepancyReason = null;

                if (isConsistent)
                {
                    if (availByFormula1 < 0)
                    {
                        // Bất thường: Số lượng gửi chờ nhận vượt quá tổng tồn
                        isConsistent = false;
                        availableForHandover = 0;
                        discrepancyReason = $"Số lượng đang chờ kho nhận ({pendingHandover:N0}) vượt quá tồn xưởng ({wipQtyInt:N0}). Dữ liệu chưa hợp lệ!";
                    }
                    else
                    {
                        availableForHandover = availByFormula1;
                    }
                }
                else
                {
                    // Dữ liệu lệch tồn -> CHẶN lập phiếu và báo rõ lý do, KHÔNG dùng Math.Max che giấu
                    availableForHandover = 0;
                    discrepancyReason = $"Lệch tồn dữ liệu: Đã sản xuất ({totalProduced:N0}) - Kho đã nhận ({warehouseReceived:N0}) = {expectedWip:N0}, nhưng WIP thành phẩm tại xưởng = {wipQtyInt:N0} (Chênh lệch: {diff:+0;-0;0}). Cần đối soát số dư ban đầu trước khi lập phiếu!";
                }

                pendingItems.Add(new PendingHandoverItemDto
                {
                    RequestId = req.RequestId,
                    RequestCode = req.DisplayCode,
                    ProductId = prodId,
                    ProductName = prodName,
                    Unit = unit,
                    QuantityRequired = req.QuantityRequired,
                    TotalProduced = totalProduced,
                    WarehouseReceived = warehouseReceived,
                    PendingHandover = pendingHandover,
                    AvailableForHandover = availableForHandover,
                    ProductionOptions = slipsForReq.SelectMany(s => s.Details).Any(d => d.ProductId == prodId && d.Quantity > 0 && d.ProductionLogId == null)
                        ? new() : productionLogs.Where(l => l.RequestId == req.RequestId && l.ProductId == prodId)
                            .Select(l => new HandoverProductionOption {
                                LogId = l.LogId,
                                ProducedDate = ProductionStatisticsViewModel.DisplayTime(l.ProducedAt),
                                ExpiryDate = l.ExpiredDate?.ToString("dd/MM/yyyy") ?? "Chưa cập nhật",
                                Available = (int)Math.Max(0, Math.Min(availableForHandover, ProductionExpiry.Remaining(l.ProducedQuantity, slipsForReq.SelectMany(s => s.Details), l.LogId)))
                            }).Where(l => l.Available > 0).ToList(),
                    ProductionWarning = slipsForReq.SelectMany(s => s.Details).Any(d => d.ProductId == prodId && d.Quantity > 0 && d.ProductionLogId == null)
                        ? "Có phiếu bàn giao cũ chưa xác định lần sản xuất. Cần đối soát trước khi bàn giao tiếp." : null,
                    WipQuantity = wipQty,
                    IsConsistent = isConsistent,
                    DiscrepancyReason = discrepancyReason
                });
            }

            // --- TAB 2: XÂY DỰNG DANH SÁCH PHIẾU BÀN GIAO ---
            var slipDtos = new List<HandoverSlipItemDto>();
            foreach (var s in handoverSlips)
            {
                var detail = s.Details.FirstOrDefault();
                var prod = detail?.Product;
                var rawNote = detail?.Note ?? string.Empty;

                // Loại bỏ tiền tố [IK:...] khỏi ghi chú hiển thị
                string cleanNote = Regex.Replace(rawNote, @"^\[IK:[^\]]+\]\s*", string.Empty).Trim();

                string statusText = HandoverStatus.IsRejected(s.Status) ? "Bị từ chối" : "Chờ kho nhận";
                if (string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Status, "Received", StringComparison.OrdinalIgnoreCase))
                {
                    statusText = "Đã duyệt";
                }

                slipDtos.Add(new HandoverSlipItemDto
                {
                    ExpiredDate = detail?.ExpiredDate,
                    ProductionLogId = detail?.ProductionLogId,
                    ProductReceiptId = s.ProductReceiptId,
                    ReceiptCode = s.DisplayCode,
                    CreatedDate = s.CreatedDate,
                    RequestId = s.RequestId,
                    RequestCode = s.ProductionRequest?.DisplayCode ?? (s.RequestId.HasValue ? $"YCSX-{s.RequestId}" : "—"),
                    ProductId = detail?.ProductId ?? 0,
                    ProductName = prod?.ProductName ?? "Sản phẩm thuốc",
                    Unit = prod?.Unit ?? "Hộp",
                    Quantity = detail?.Quantity ?? 0,
                    RequesterName = s.Requester?.FullName ?? "Chưa cập nhật",
                    ApproverName = s.Approver?.FullName,
                    Status = HandoverStatus.IsRejected(s.Status) ? "Rejected" : s.Status ?? "Pending",
                    StatusText = statusText,
                    Note = cleanNote
                });
            }

            // Áp dụng bộ lọc tìm kiếm & trạng thái nếu có
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var kw = searchKeyword.Trim().ToLower();
                pendingItems = pendingItems.Where(p =>
                    p.RequestCode.ToLower().Contains(kw) ||
                    p.ProductName.ToLower().Contains(kw) ||
                    p.RequestId.ToString().Contains(kw)
                ).ToList();

                slipDtos = slipDtos.Where(s =>
                    s.ReceiptCode.ToLower().Contains(kw) ||
                    s.RequestCode.ToLower().Contains(kw) ||
                    s.ProductName.ToLower().Contains(kw) ||
                    s.RequesterName.ToLower().Contains(kw)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (HandoverStatus.IsRejected(statusFilter))
                {
                    slipDtos = slipDtos.Where(s => HandoverStatus.IsRejected(s.Status)).ToList();
                }
                else if (statusFilter == "Pending")
                {
                    slipDtos = slipDtos.Where(s => s.Status == "Pending").ToList();
                }
                else if (statusFilter == "Approved" || statusFilter == "Completed")
                {
                    slipDtos = slipDtos.Where(s => string.Equals(s.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(s.Status, "Received", StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            model.PendingItems = pendingItems;
            model.HandoverSlips = slipDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tổng hợp dữ liệu trang Bàn giao thành phẩm.");
        }

        return model;
    }

    /// <summary>
    /// Lập và gửi phiếu bàn giao thành phẩm riêng biệt
    /// Thực hiện trong Transaction an toàn với Row Lock (FOR UPDATE) và Idempotency Key
    /// </summary>
    public async Task<(bool Success, string Message, string? ReceiptCode, int ReceiptId)> CreateHandoverSlipAsync(
        HandoverSlipCreateRequest req, int currentUserId)
    {
        // 1. Kiểm tra tham số cơ bản
        if (req == null)
            return (false, "Dữ liệu yêu cầu không hợp lệ.", null, 0);

        if (req.RequestId <= 0)
            return (false, "Mã yêu cầu sản xuất không hợp lệ.", null, 0);

        if (req.ProductId <= 0)
            return (false, "Mã sản phẩm thuốc không hợp lệ.", null, 0);

        if (req.ProductionLogId <= 0)
            return (false, "Vui lòng chọn lần sản xuất để bàn giao.", null, 0);

        if (req.Quantity <= 0)
            return (false, "Số lượng bàn giao phải lớn hơn 0.", null, 0);

        if (currentUserId <= 0)
            return (false, "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.", null, 0);

        string ik = string.IsNullOrWhiteSpace(req.IdempotencyKey) ? Guid.NewGuid().ToString("N") : req.IdempotencyKey.Trim();

        // 2. Kiểm tra Idempotency trước khi mở Transaction để tránh xử lý trùng lặp
        var existingSlip = await _context.ProductReceipts
            .Include(r => r.Details)
            .Where(r => r.RequestId == req.RequestId &&
                        !string.IsNullOrWhiteSpace(r.ReceiptCode) &&
                        r.Details.Any(d => d.ProductId == req.ProductId && d.Note != null && d.Note.Contains($"[IK:{ik}]")))
            .FirstOrDefaultAsync();

        if (existingSlip != null)
        {
            var matchedDetail = existingSlip.Details.FirstOrDefault(d => d.ProductId == req.ProductId);
            if (matchedDetail != null && matchedDetail.Quantity == req.Quantity && matchedDetail.ProductionLogId == req.ProductionLogId)
            {
                // Trả về phiếu đã tạo thành công trước đó (Idempotent response)
                return (true, $"Phiếu bàn giao {existingSlip.DisplayCode} đã được gửi thành công trước đó.",
                    existingSlip.DisplayCode, existingSlip.ProductReceiptId);
            }
            else
            {
                return (false, "Mã thao tác (Idempotency Key) đã tồn tại nhưng có số lượng bàn giao khác. Vui lòng làm mới trang và thử lại.", null, 0);
            }
        }

        // 3. ReadCommitted + khóa đơn: đọc lại số lượng sau khi chờ thao tác trước hoàn tất.
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // Khóa dòng yêu cầu sản xuất để tuần tự hóa các thao tác đồng thời trên cùng 1 yêu cầu
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT requestid FROM public.productionrequest WHERE requestid = {req.RequestId} FOR UPDATE;");

            // Khóa dòng WIP thành phẩm nếu đã tồn tại
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT wipid FROM public.wip_inventory WHERE requestid = {req.RequestId} AND productid = {req.ProductId} AND ingrid IS NULL FOR UPDATE;");

            var retrySlip = await _context.ProductReceipts.Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.RequestId == req.RequestId && r.ReceiptCode != null && r.Details.Any(d => d.Note != null && d.Note.StartsWith($"[IK:{ik}]")));
            if (retrySlip != null)
            {
                var same = retrySlip.Details.Any(d => d.ProductId == req.ProductId && d.ProductionLogId == req.ProductionLogId && d.Quantity == req.Quantity);
                return same ? (true, "Phiếu đã được gửi trước đó.", retrySlip.DisplayCode, retrySlip.ProductReceiptId)
                    : (false, "Mã thao tác đã dùng cho nội dung khác.", null, 0);
            }

            // 4. Đọc lại dữ liệu mới nhất trong phạm vi khóa Transaction
            var order = await _context.ProductionRequests
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.RequestId == req.RequestId);

            if (order == null)
            {
                await transaction.RollbackAsync();
                return (false, "Không tìm thấy yêu cầu sản xuất trong hệ thống.", null, 0);
            }

            if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync();
                return (false, "Yêu cầu sản xuất đã bị hủy. Không thể lập phiếu bàn giao.", null, 0);
            }

            if (order.ProductId != req.ProductId)
            {
                await transaction.RollbackAsync();
                return (false, $"Sản phẩm yêu cầu ({req.ProductId}) không khớp với sản phẩm của đơn hàng ({order.ProductId}).", null, 0);
            }

            var prod = order.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.ProductId == req.ProductId);
            string prodName = prod?.ProductName ?? "Sản phẩm thuốc";
            string unit = prod?.Unit ?? "Hộp";

            // Tồn WIP thành phẩm thực tế tại xưởng
            var wipProduct = await _context.WipInventories
                .FirstOrDefaultAsync(w => w.RequestId == req.RequestId && w.ProductId == req.ProductId && w.IngrId == null);
            decimal wipQty = wipProduct?.Quantity ?? 0m;
            int wipQtyInt = (int)Math.Floor(wipQty);

            // Tổng sản lượng đã sản xuất từ dòng tích lũy cũ
            var legacyReceipts = await _context.ProductReceipts
                .Include(r => r.Details)
                .Where(r => r.RequestId == req.RequestId && string.IsNullOrWhiteSpace(r.ReceiptCode))
                .ToListAsync();

            int totalProduced = legacyReceipts.SelectMany(r => r.Details)
                .Where(d => d.ProductId == req.ProductId)
                .Sum(d => d.Quantity);

            // Các phiếu bàn giao độc lập đã lập trước đó
            var activeSlips = await _context.ProductReceipts
                .Include(r => r.Details)
                .Where(r => r.RequestId == req.RequestId && !string.IsNullOrWhiteSpace(r.ReceiptCode))
                .ToListAsync();

            activeSlips = HandoverStatus.ForAllocation(activeSlips).ToList();

            int pendingHandover = activeSlips
                .Where(r => !string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(r.Status, "Received", StringComparison.OrdinalIgnoreCase))
                .SelectMany(r => r.Details)
                .Where(d => d.ProductId == req.ProductId)
                .Sum(d => d.Quantity);

            int warehouseReceived = activeSlips
                .Where(r => string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.Status, "Received", StringComparison.OrdinalIgnoreCase))
                .SelectMany(r => r.Details)
                .Where(d => d.ProductId == req.ProductId)
                .Sum(d => d.Quantity);

            // 5. Kiểm tra đối soát 2 chiều
            int expectedWip = totalProduced - warehouseReceived;
            int diff = expectedWip - wipQtyInt;
            if (diff != 0)
            {
                await transaction.RollbackAsync();
                return (false, $"Dữ liệu sản xuất và tồn xưởng chưa nhất quán (Đã SX: {totalProduced:N0}, Kho đã nhận: {warehouseReceived:N0}, WIP xưởng: {wipQtyInt:N0}, Chênh lệch: {diff:+0;-0;0}). Cần đối soát số dư trước khi lập phiếu bàn giao!", null, 0);
            }

            int availableForHandover = totalProduced - warehouseReceived - pendingHandover;
            if (availableForHandover <= 0)
            {
                await transaction.RollbackAsync();
                return (false, $"Đơn hàng không còn thành phẩm có thể bàn giao (Đã sản xuất: {totalProduced:N0}, Kho đã nhận: {warehouseReceived:N0}, Đang chờ kho nhận: {pendingHandover:N0}).", null, 0);
            }

            if (req.Quantity > availableForHandover)
            {
                await transaction.RollbackAsync();
                return (false, $"Số lượng bàn giao yêu cầu ({req.Quantity:N0} {unit}) vượt quá số lượng có thể bàn giao hiện tại ({availableForHandover:N0} {unit}).", null, 0);
            }

            var productionLog = await _context.ProductionLogs.AsNoTracking()
                .SingleOrDefaultAsync(l => l.LogId == req.ProductionLogId && l.RequestId == req.RequestId && l.ProductId == req.ProductId);
            if (productionLog == null)
                return (false, "Lần sản xuất không thuộc đơn hàng và sản phẩm đã chọn.", null, 0);
            if (activeSlips.SelectMany(s => s.Details).Any(d => d.ProductId == req.ProductId && d.Quantity > 0 && d.ProductionLogId == null))
                return (false, "Có phiếu bàn giao cũ chưa xác định lần sản xuất. Cần đối soát trước khi bàn giao tiếp.", null, 0);
            var remainingForLog = ProductionExpiry.Remaining(productionLog.ProducedQuantity, activeSlips.SelectMany(s => s.Details), productionLog.LogId);
            if (req.Quantity > remainingForLog)
                return (false, "Số lượng vượt quá phần còn lại của lần sản xuất này. Vui lòng tải lại danh sách.", null, 0);

            // 6. Sinh mã phiếu bàn giao độc lập duy nhất (VD: BG-DPH-0001)
            var maxId = await _context.ProductReceipts.MaxAsync(p => (int?)p.ProductReceiptId) ?? 0;
            string receiptCode = $"BG-DPH-{(maxId + 1):D4}";
            while (await _context.ProductReceipts.AnyAsync(r => r.ReceiptCode == receiptCode))
            {
                maxId++;
                receiptCode = $"BG-DPH-{(maxId + 1):D4}";
            }

            // 7. Tạo phiếu ProductReceipt mới đại diện cho lần bàn giao này (Trạng thái Pending: Chờ kho nhận)
            var newReceipt = new ProductReceipt
            {
                ReceiptCode = receiptCode,
                RequestId = req.RequestId,
                RequestedBy = currentUserId,
                ApprovedBy = null,
                CreatedDate = DateTime.UtcNow,
                Status = "Pending"
            };
            _context.ProductReceipts.Add(newReceipt);
            await _context.SaveChangesAsync();

            // 8. Tạo chi tiết phiếu ProductReceiptDetail
            string userNote = req.Note?.Trim() ?? string.Empty;
            string storedNote = string.IsNullOrEmpty(userNote)
                ? $"[IK:{ik}]"
                : $"[IK:{ik}] {userNote}";

            if (storedNote.Length > 255)
            {
                storedNote = storedNote.Substring(0, 255);
            }

            var newDetail = new ProductReceiptDetail
            {
                ProductionLogId = productionLog.LogId,
                ExpiredDate = productionLog.ExpiredDate?.ToDateTime(TimeOnly.MinValue),
                ProductReceiptId = newReceipt.ProductReceiptId,
                ProductId = req.ProductId,
                Quantity = req.Quantity,
                Status = "Pending",
                Note = storedNote,
                CreatedDate = DateTime.UtcNow
            };
            _context.ProductReceiptDetails.Add(newDetail);
            await _context.SaveChangesAsync();

            // Cam kết giao dịch an toàn
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Đã lập phiếu bàn giao thành phẩm {ReceiptCode} cho đơn {RequestId}, Sản phẩm {ProductId}, Số lượng {Quantity} {Unit}",
                receiptCode, req.RequestId, req.ProductId, req.Quantity, unit);

            int remainingAfter = availableForHandover - req.Quantity;
            string msg = $"Đã lập và gửi phiếu bàn giao {receiptCode} ({req.Quantity:N0} {unit}) thành công sang Kho! Số lượng còn có thể bàn giao tiếp: {remainingAfter:N0} {unit}.";

            return (true, msg, receiptCode, newReceipt.ProductReceiptId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi ngoại lệ khi tạo phiếu bàn giao cho đơn {RequestId}", req.RequestId);
            return (false, "Lỗi xử lý cơ sở dữ liệu: " + ex.Message, null, 0);
        }
    }

    /// <summary>
    /// Lấy chi tiết một phiếu bàn giao thành phẩm
    /// </summary>
    public async Task<ProductReceipt?> GetHandoverSlipDetailAsync(int receiptId)
    {
        if (receiptId <= 0) return null;

        return await _context.ProductReceipts
            .AsNoTracking()
            .Include(r => r.Requester)
            .Include(r => r.Approver)
            .Include(r => r.ProductionRequest)
            .Include(r => r.Details).ThenInclude(d => d.Product)
            .Include(r => r.Details).ThenInclude(d => d.ProductionLog)
            .FirstOrDefaultAsync(r => r.ProductReceiptId == receiptId);
    }
}
