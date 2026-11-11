using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Services;

public static class SaleOrderCancellationSync
{
    private static readonly SemaphoreSlim _syncLock = new(1, 1);

    private static readonly string[] CancelStatusValues =
    {
        "cancel", "cancelled", "canceled", "hủy", "đã hủy"
    };

    private static readonly HashSet<string> CancelStatuses = new(CancelStatusValues, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> RejectedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "rejected", "reject", "từ chối"
    };

    private static readonly HashSet<string> ApprovedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "approved", "approve", "đã xuất kho", "completed"
    };

    private sealed record CancelledExportCandidate(
        int ExportReceiptId,
        string? ExpReceiptCode,
        string? Status,
        int? ApprovedBy,
        string? OrderCode
    );

    public static bool IsCancelStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && CancelStatuses.Contains(status.Trim());

    public static bool IsRejectedStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && RejectedStatuses.Contains(status.Trim());

    public static bool IsApprovedStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && ApprovedStatuses.Contains(status.Trim());

    public static bool IsPendingStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
           || status.Trim().Equals("pending", StringComparison.OrdinalIgnoreCase)
           || status.Trim().Equals("chờ xuất kho", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Tạo mã phiếu nhập hoàn kho duy nhất cho từng phiếu xuất đã duyệt để tránh hoàn trùng.
    /// </summary>
    public static string BuildRefundReceiptCode(int exportReceiptId, string? expReceiptCode)
    {
        var code = !string.IsNullOrWhiteSpace(expReceiptCode)
            ? expReceiptCode.Trim()
            : $"PX{exportReceiptId}";
        return $"HOAN-{code}";
    }

    public static async Task<HashSet<int>> GetCancelledOrderIdsAsync(ApplicationDbContext context)
    {
        var ids = await context.SaleOrders
            .AsNoTracking()
            .Where(o => o.Status != null && CancelStatusValues.Contains(o.Status.Trim().ToLower()))
            .Select(o => o.OrderId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    private static async Task<List<CancelledExportCandidate>> QueryCancelledOrderExportsAsync(ApplicationDbContext context)
    {
        return await (
            from e in context.ProductExportReceipts.AsNoTracking()
            join s in context.SaleOrders.AsNoTracking() on e.OrderId equals s.OrderId
            where (e.ExpReceiptCode == null || !EF.Functions.ILike(e.ExpReceiptCode, "HSP-%"))
                  && s.Status != null
                  && CancelStatusValues.Contains(s.Status.Trim().ToLower())
            select new CancelledExportCandidate(
                e.ExportReceiptId,
                e.ExpReceiptCode,
                e.Status,
                e.ApprovedBy,
                s.OrderCode))
            .ToListAsync();
    }

    private static async Task<HashSet<string>> GetExistingRefundCodesAsync(
        ApplicationDbContext context,
        IEnumerable<string> candidateRefundCodes)
    {
        var codeList = candidateRefundCodes.Distinct().ToList();
        if (codeList.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var existing = await context.ProductReceipts
            .AsNoTracking()
            .Where(pr => pr.ReceiptCode != null && codeList.Contains(pr.ReceiptCode))
            .Select(pr => pr.ReceiptCode!)
            .ToListAsync();

        return existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool WasApprovedForExport(string? status, int? approvedBy)
        => IsApprovedStatus(status) || (IsCancelStatus(status) && approvedBy.HasValue);

    private static List<CancelledExportCandidate> FilterPendingWork(
        List<CancelledExportCandidate> rows,
        HashSet<string> existingRefundCodes)
    {
        var work = new List<CancelledExportCandidate>();
        foreach (var r in rows)
        {
            if (IsRejectedStatus(r.Status)) continue;

            if (WasApprovedForExport(r.Status, r.ApprovedBy))
            {
                var refundCode = BuildRefundReceiptCode(r.ExportReceiptId, r.ExpReceiptCode);
                bool needsStatusRestore = IsCancelStatus(r.Status) && r.ApprovedBy.HasValue;
                if (needsStatusRestore || !existingRefundCodes.Contains(refundCode))
                {
                    work.Add(r);
                }
            }
            else if (!IsCancelStatus(r.Status))
            {
                // Pending -> cần chuyển sang Cancel
                work.Add(r);
            }
        }
        return work;
    }

    /// <summary>
    /// Đồng bộ các phiếu xuất kho thành phẩm gắn với đơn hàng (saleorder) ở trạng thái Cancel:
    /// - Pending -> chuyển status phiếu xuất (và chi tiết) sang Cancel.
    /// - Approved (kể cả trường hợp bị ghi đè Cancelled dù đã có ApprovedBy trừ kho) -> giữ nguyên/khôi phục status Approved,
    ///   hoàn lại số lượng đã xuất vào bảng product, đồng thời tạo phiếu nhập (ProductReceipt mã HOAN-...) ghi nhận loại giao dịch hoàn kho.
    /// - Rejected -> giữ nguyên status phiếu, không xử lý.
    /// </summary>
    public static async Task<(int PendingCancelled, int ApprovedRefunded)> SyncAsync(ApplicationDbContext context)
    {
        var preRows = await QueryCancelledOrderExportsAsync(context);
        if (preRows.Count == 0)
        {
            return (0, 0);
        }

        var preRefundCodes = preRows
            .Where(r => WasApprovedForExport(r.Status, r.ApprovedBy))
            .Select(r => BuildRefundReceiptCode(r.ExportReceiptId, r.ExpReceiptCode));
        var preExistingRefunds = await GetExistingRefundCodesAsync(context, preRefundCodes);

        if (FilterPendingWork(preRows, preExistingRefunds).Count == 0)
        {
            return (0, 0);
        }

        await _syncLock.WaitAsync();
        try
        {
            int pendingCancelled = 0;
            int approvedRefunded = 0;

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                context.ChangeTracker.Clear();
                pendingCancelled = 0;
                approvedRefunded = 0;

                await using var tx = await context.Database.BeginTransactionAsync();

                // Khóa mức dòng trong Postgres trên các phiếu xuất gắn đơn hàng Cancel để đảm bảo an toàn đồng thời
                await context.Database.ExecuteSqlRawAsync(@"
                    SELECT 1
                    FROM productexportreceipt e
                    INNER JOIN saleorder s ON e.orderid = s.orderid
                    WHERE (e.expreceiptcode IS NULL OR e.expreceiptcode NOT ILIKE 'HSP-%')
                      AND LOWER(BTRIM(COALESCE(s.status, ''))) IN ('cancel', 'cancelled', 'canceled', 'hủy', 'đã hủy')
                    FOR UPDATE OF e");

                var rows = await QueryCancelledOrderExportsAsync(context);
                var candidateRefundCodes = rows
                    .Where(r => WasApprovedForExport(r.Status, r.ApprovedBy))
                    .Select(r => BuildRefundReceiptCode(r.ExportReceiptId, r.ExpReceiptCode));
                var existingRefundCodes = await GetExistingRefundCodesAsync(context, candidateRefundCodes);

                var work = FilterPendingWork(rows, existingRefundCodes);
                if (work.Count == 0)
                {
                    await tx.CommitAsync();
                    return;
                }

                var workById = work.ToDictionary(w => w.ExportReceiptId);
                var workIds = workById.Keys.ToList();

                // Query đơn bảng để EF Core track chính xác các entity cần cập nhật
                var receipts = await context.ProductExportReceipts
                    .Where(e => workIds.Contains(e.ExportReceiptId))
                    .ToListAsync();

                var details = await context.ProductExportReceiptDetails
                    .Where(d => workIds.Contains(d.ExportReceiptId))
                    .ToListAsync();

                var approvedProductIds = receipts
                    .Where(r => WasApprovedForExport(r.Status, r.ApprovedBy))
                    .SelectMany(r => details.Where(d => d.ExportReceiptId == r.ExportReceiptId).Select(d => d.ProductId))
                    .Distinct()
                    .ToList();

                var productsById = approvedProductIds.Count > 0
                    ? await context.Products
                        .Where(p => approvedProductIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId)
                    : new Dictionary<int, Product>();

                var now = DateTime.UtcNow;
                var newRefundEntries = new List<(ProductReceipt Header, ProductExportReceipt SourceExport, string ExportCode, string? OrderCode, List<ProductExportReceiptDetail> SourceDetails)>();

                foreach (var receipt in receipts)
                {
                    if (IsRejectedStatus(receipt.Status)) continue;

                    var receiptDetails = details.Where(d => d.ExportReceiptId == receipt.ExportReceiptId).ToList();

                    if (WasApprovedForExport(receipt.Status, receipt.ApprovedBy))
                    {
                        // Giữ nguyên (hoặc khôi phục nếu bị bên ngoài ghi đè) trạng thái Approved
                        receipt.Status = "Approved";
                        foreach (var d in receiptDetails)
                        {
                            d.Status = "Approved";
                        }

                        var refundCode = BuildRefundReceiptCode(receipt.ExportReceiptId, receipt.ExpReceiptCode);
                        if (!existingRefundCodes.Contains(refundCode))
                        {
                            foreach (var d in receiptDetails)
                            {
                                if (productsById.TryGetValue(d.ProductId, out var prod))
                                {
                                    prod.Quantity = (prod.Quantity ?? 0) + d.Quantity;
                                }
                            }

                            var exportCode = !string.IsNullOrWhiteSpace(receipt.ExpReceiptCode)
                                ? receipt.ExpReceiptCode.Trim()
                                : $"PX{receipt.ExportReceiptId:D4}";
                            workById.TryGetValue(receipt.ExportReceiptId, out var info);

                            var refundReceipt = new ProductReceipt
                            {
                                ReceiptCode = refundCode,
                                Status = "Approved",
                                ApprovedBy = receipt.ApprovedBy,
                                RequestedBy = receipt.RequestedBy,
                                RequestId = null,
                                CreatedDate = now
                            };

                            context.ProductReceipts.Add(refundReceipt);
                            existingRefundCodes.Add(refundCode);
                            newRefundEntries.Add((refundReceipt, receipt, exportCode, info?.OrderCode, receiptDetails));
                            approvedRefunded++;
                        }
                    }
                    else if (!IsCancelStatus(receipt.Status))
                    {
                        foreach (var d in receiptDetails)
                        {
                            d.Status = "Cancel";
                        }

                        receipt.Status = "Cancel";
                        pendingCancelled++;
                    }
                }

                // Lưu bước 1 để lấy identity ProductReceiptId sinh bởi Postgres
                await context.SaveChangesAsync();

                if (newRefundEntries.Count > 0)
                {
                    foreach (var entry in newRefundEntries)
                    {
                        var orderInfo = !string.IsNullOrWhiteSpace(entry.OrderCode) ? $" (đơn {entry.OrderCode} hủy)" : " (đơn hàng hủy)";
                        var rawNote = $"Hoàn lại số lượng đã xuất từ phiếu {entry.ExportCode}{orderInfo}";
                        var note = rawNote.Length > 255 ? rawNote[..255] : rawNote;

                        foreach (var d in entry.SourceDetails)
                        {
                            productsById.TryGetValue(d.ProductId, out var prod);
                            int shelfMonths = (prod?.ShelfLifeMonths.HasValue == true && prod.ShelfLifeMonths.Value > 0)
                                ? prod.ShelfLifeMonths.Value
                                : 24;
                            decimal? unitCost = (prod?.MaterialCost.HasValue == true && prod.MaterialCost.Value > 0)
                                ? prod.MaterialCost.Value
                                : prod?.Cost;

                            context.ProductReceiptDetails.Add(new ProductReceiptDetail
                            {
                                ProductReceiptId = entry.Header.ProductReceiptId,
                                ProductId = d.ProductId,
                                Quantity = d.Quantity,
                                Status = "Approved",
                                Note = note,
                                CreatedDate = now,
                                ExpiredDate = now.AddMonths(shelfMonths),
                                Cost = unitCost
                            });
                        }
                    }

                    await context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                context.ChangeTracker.Clear();
            });

            return (pendingCancelled, approvedRefunded);
        }
        finally
        {
            _syncLock.Release();
        }
    }
}
