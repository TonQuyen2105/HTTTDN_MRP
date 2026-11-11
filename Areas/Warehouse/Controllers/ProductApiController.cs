using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;
using httt.Areas.Warehouse.Services;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/products")]
[Produces("application/json")]
[Tags("2. Thành phẩm (Products)")]
public class ProductApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private static bool _initializedReceiptCosts = false;
    private static readonly string[] ApprovedStatuses = { "Approved", "Approve", "Đã nhập kho" };

    public ProductApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    private sealed record ApprovedReceiptItem(
        int productReceiptId,
        string voucherNo,
        string orderNo,
        string importDate,
        string creatorName,
        string employeeCode,
        string source,
        int quantity,
        string quantityFormatted,
        string unit,
        decimal cost,
        string costFormatted,
        decimal amount,
        string amountFormatted,
        string expiredDate,
        bool isExpired,
        bool isDisposed,
        string status,
        string statusType,
        string note);

    private async Task<List<ApprovedReceiptItem>> GetApprovedReceiptsForProductAsync(Product item)
    {
        string unit = !string.IsNullOrWhiteSpace(item.Unit) ? item.Unit : "Hộp";
        decimal fallbackCost = item.Cost ?? 0;

        var disposedCodes = (await (from ped in _context.ProductExportReceiptDetails.AsNoTracking()
                                    join pe in _context.ProductExportReceipts.AsNoTracking() on ped.ExportReceiptId equals pe.ExportReceiptId
                                    where ped.ProductId == item.ProductId
                                          && pe.ExpReceiptCode != null
                                          && pe.ExpReceiptCode.StartsWith("HSP-")
                                    select pe.ExpReceiptCode!)
                                    .ToListAsync())
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rawDetails = await (from d in _context.ProductReceiptDetails.AsNoTracking()
                               where d.ProductId == item.ProductId
                               join r in _context.ProductReceipts.AsNoTracking() on d.ProductReceiptId equals r.ProductReceiptId
                               where r.Status != null && ApprovedStatuses.Contains(r.Status)
                               join u in _context.UserAccounts.AsNoTracking() on r.RequestedBy equals u.UserID into uJoin
                               from u in uJoin.DefaultIfEmpty()
                               orderby (r.CreatedDate ?? d.CreatedDate) descending, r.ProductReceiptId descending
                               select new
                               {
                                   productReceiptId = r.ProductReceiptId,
                                   voucherNo = !string.IsNullOrWhiteSpace(r.ReceiptCode) ? r.ReceiptCode : $"LNK{r.ProductReceiptId:D5}",
                                   orderNo = !string.IsNullOrWhiteSpace(r.ReceiptCode) ? r.ReceiptCode : $"LNK{r.ProductReceiptId:D5}",
                                   importDate = (r.CreatedDate ?? d.CreatedDate).HasValue
                                       ? (r.CreatedDate ?? d.CreatedDate)!.Value.ToString("dd/MM/yyyy HH:mm:ss")
                                       : "",
                                   creatorName = u != null ? (!string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username) : "Nguyễn Tiến Huy",
                                   employeeCode = u != null ? $"NV{u.UserID:D4}" : "NV0005",
                                   quantity = d.Quantity,
                                   cost = (d.Cost.HasValue && d.Cost.Value > 0) ? d.Cost.Value : fallbackCost,
                                   rawExpiredDate = d.ExpiredDate,
                                   expiredDate = d.ExpiredDate.HasValue
                                       ? d.ExpiredDate.Value.ToString("dd/MM/yyyy")
                                       : "31/12/2028",
                                   note = d.Note ?? ""
                               }).ToListAsync();

        var now = DateTime.UtcNow;
        return rawDetails.Select(r =>
        {
            bool isDisposed = disposedCodes.Contains($"HSP-{r.voucherNo}") || disposedCodes.Contains($"HSP-LNK{r.productReceiptId:D5}");
            bool isExpired = r.rawExpiredDate.HasValue && r.rawExpiredDate.Value <= now;
            return new ApprovedReceiptItem(
                r.productReceiptId,
                r.voucherNo,
                r.orderNo,
                r.importDate,
                r.creatorName,
                r.employeeCode,
                r.voucherNo.StartsWith("HOAN-", StringComparison.OrdinalIgnoreCase) ? "Kho thành phẩm (Hoàn hàng)" : "Phân xưởng sản xuất",
                r.quantity,
                r.quantity.ToString("N0"),
                unit,
                r.cost,
                r.cost.ToString("N0"),
                r.cost * r.quantity,
                (r.cost * r.quantity).ToString("N0"),
                r.expiredDate,
                isExpired,
                isDisposed,
                isDisposed ? "Đã hủy" : "Approved",
                isDisposed ? "danger" : "success",
                r.note);
        }).ToList();
    }

    private async Task EnsureReceiptCostsAsync()
    {
        if (_initializedReceiptCosts) return;
        try
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE productreceiptdetail SET cost = 26000 WHERE productreceiptid = 1 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 28000 WHERE productreceiptid = 2 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 32000 WHERE productreceiptid = 3 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 35000 WHERE productreceiptid = 4 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 29500 WHERE productreceiptid = 5 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 950000 WHERE productreceiptid = 6 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 1000000 WHERE productreceiptid = 7 AND (cost IS NULL OR cost = 0);
                UPDATE productreceiptdetail SET cost = 980000 WHERE productreceiptid = 8 AND (cost IS NULL OR cost = 0);
            ");
            _initializedReceiptCosts = true;
        }
        catch
        {
            // Ignore if column doesn't exist yet or already updated
        }
    }

    /// <summary>
    /// GET /api/warehouse/products : Lấy danh sách thành phẩm (Giá vốn tự động lấy giá cao nhất từ các phiếu nhập thành phẩm)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? keyword = null)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);
        await EnsureReceiptCostsAsync();

        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(kw) 
                                  || (p.Unit != null && p.Unit.ToLower().Contains(kw))
                                  || (p.Description != null && p.Description.ToLower().Contains(kw)));
        }

        var dbItems = await query.OrderBy(p => p.ProductId).ToListAsync();

        // Lấy giá vốn cao nhất từ các phiếu nhập kho thành phẩm đã duyệt (Approved) theo từng sản phẩm (bỏ qua phiếu hoàn kho HOAN-)
        var receiptMaxCosts = await _context.ProductReceiptDetails
            .AsNoTracking()
            .Where(d => d.Cost.HasValue && d.Cost.Value > 0 &&
                        _context.ProductReceipts.Any(r => r.ProductReceiptId == d.ProductReceiptId &&
                                                          r.Status != null && ApprovedStatuses.Contains(r.Status) &&
                                                          (r.ReceiptCode == null || !r.ReceiptCode.StartsWith("HOAN-"))))
            .GroupBy(d => d.ProductId)
            .Select(g => new { ProductId = g.Key, MaxCost = g.Max(d => d.Cost!.Value) })
            .ToDictionaryAsync(g => g.ProductId, g => g.MaxCost);

        var list = dbItems.Select(p =>
        {
            decimal actualMaterialCost = p.MaterialCost.HasValue && p.MaterialCost.Value > 0
                ? p.MaterialCost.Value
                : (receiptMaxCosts.TryGetValue(p.ProductId, out var maxCost) && maxCost > 0 ? maxCost : 0);

            decimal actualCost = p.Cost ?? 0;

            return new
            {
                id = p.ProductId,
                productId = p.ProductId,
                productCode = $"SP{p.ProductId:D3}",
                productName = p.ProductName,
                quantity = p.Quantity ?? 0,
                quantityFormatted = (p.Quantity ?? 0).ToString("N0"),
                unit = !string.IsNullOrWhiteSpace(p.Unit) ? p.Unit : "Hộp",
                cost = actualCost, // Giá bán
                costFormatted = actualCost.ToString("N0"),
                materialCost = actualMaterialCost, // Giá vốn
                materialCostFormatted = actualMaterialCost.ToString("N0"),
                imageUrl = p.ImageUrl ?? "",
                description = p.Description ?? "",
                shelfLifeMonths = p.ShelfLifeMonths ?? 0,
                expiredDate = (p.ShelfLifeMonths != null && p.ShelfLifeMonths > 0)
                    ? DateTime.Now.AddMonths(p.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                    : "31/12/2028"
            };
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/products/{id} : Xem chi tiết 1 thành phẩm kèm các phiếu nhập kho thành phẩm
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        await SaleOrderCancellationSync.SyncAsync(_context);
        await EnsureReceiptCostsAsync();

        var item = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (item == null)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        // Lấy danh sách chi tiết các phiếu nhập kho thành phẩm có chứa sản phẩm này (chỉ hiển thị phiếu có status Approved)
        var receiptDetails = await GetApprovedReceiptsForProductAsync(item);

        // Giá vốn của sản phẩm sẽ lấy giá cao nhất từ các phiếu nhập thành phẩm, hoặc item.MaterialCost
        decimal maxReceiptCost = receiptDetails.Where(r => r.cost > 0).Select(r => r.cost).DefaultIfEmpty(item.MaterialCost ?? 0).Max();
        decimal actualMaterialCost = item.MaterialCost.HasValue && item.MaterialCost.Value > 0 ? item.MaterialCost.Value : maxReceiptCost;
        decimal sellingPrice = item.Cost ?? 0;

        // Hạn sử dụng lấy từ phiếu nhập gần nhất (hoặc tính theo shelfLifeMonths)
        string effectiveExpiredDate = receiptDetails.FirstOrDefault()?.expiredDate ?? (
            (item.ShelfLifeMonths != null && item.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(item.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
        );

        return Ok(new
        {
            id = item.ProductId,
            productId = item.ProductId,
            productCode = $"SP{item.ProductId:D3}",
            productName = item.ProductName,
            quantity = item.Quantity ?? 0,
            quantityFormatted = (item.Quantity ?? 0).ToString("N0"),
            unit = !string.IsNullOrWhiteSpace(item.Unit) ? item.Unit : "Hộp",
            cost = sellingPrice, // Giá bán
            costFormatted = sellingPrice.ToString("N0"),
            materialCost = actualMaterialCost, // Giá vốn
            materialCostFormatted = actualMaterialCost.ToString("N0"),
            imageUrl = item.ImageUrl ?? "",
            description = item.Description ?? "",
            shelfLifeMonths = item.ShelfLifeMonths ?? 0,
            expiredDate = effectiveExpiredDate,
            importReceipts = receiptDetails,
            productReceipts = receiptDetails
        });
    }

    /// <summary>
    /// GET /api/warehouse/products/{id}/product-receipts : Danh sách lệnh nhập kho thành phẩm của sản phẩm (chỉ phiếu Approved)
    /// </summary>
    [HttpGet("{id:int}/product-receipts")]
    public async Task<IActionResult> GetProductReceipts(int id)
    {
        await EnsureReceiptCostsAsync();

        var item = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (item == null)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        var list = await GetApprovedReceiptsForProductAsync(item);
        return Ok(list);
    }

    /// <summary>
    /// POST /api/warehouse/products : Thêm mới thành phẩm (Mở khóa cho phép người dùng chỉ định Đơn vị tính)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductName))
        {
            return BadRequest(new { message = "Tên thành phẩm không được để trống." });
        }

        var product = new Product
        {
            ProductName = dto.ProductName.Trim(),
            Quantity = dto.Quantity >= 0 ? dto.Quantity : 0,
            Unit = !string.IsNullOrWhiteSpace(dto.Unit) ? dto.Unit.Trim() : "Hộp",
            Cost = dto.Cost >= 0 ? dto.Cost : 0, // Giá bán
            MaterialCost = 0, // Giá vốn khóa khi thêm mới, tự động cập nhật từ phiếu nhập thành phẩm
            ImageUrl = dto.ImageUrl?.Trim(),
            Description = dto.Description?.Trim(),
            ShelfLifeMonths = dto.ShelfLifeMonths >= 0 ? dto.ShelfLifeMonths : 24
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, new
        {
            id = product.ProductId,
            productId = product.ProductId,
            productCode = $"SP{product.ProductId:D3}",
            productName = product.ProductName,
            quantity = product.Quantity ?? 0,
            quantityFormatted = (product.Quantity ?? 0).ToString("N0"),
            unit = product.Unit,
            cost = product.Cost ?? 0,
            costFormatted = (product.Cost ?? 0).ToString("N0"),
            materialCost = product.MaterialCost ?? 0,
            materialCostFormatted = (product.MaterialCost ?? 0).ToString("N0"),
            imageUrl = product.ImageUrl ?? "",
            description = product.Description ?? "",
            shelfLifeMonths = product.ShelfLifeMonths ?? 0,
            expiredDate = (product.ShelfLifeMonths != null && product.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(product.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
        });
    }

    /// <summary>
    /// PUT /api/warehouse/products/{id} : Cập nhật thông tin thành phẩm (Khóa giá và HSD, giá vốn tự động lấy từ phiếu nhập cao nhất)
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
    {
        var existing = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (existing == null)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        // Lấy giá vốn cao nhất từ các phiếu nhập thành phẩm đã duyệt (Approved) của sản phẩm này (bỏ qua phiếu hoàn kho HOAN-)
        var maxReceiptCost = await _context.ProductReceiptDetails
            .Where(d => d.ProductId == id && d.Cost.HasValue && d.Cost.Value > 0 &&
                        _context.ProductReceipts.Any(r => r.ProductReceiptId == d.ProductReceiptId &&
                                                          r.Status != null && ApprovedStatuses.Contains(r.Status) &&
                                                          (r.ReceiptCode == null || !r.ReceiptCode.StartsWith("HOAN-"))))
            .Select(d => (decimal?)d.Cost!.Value)
            .MaxAsync() ?? 0;

        if (!string.IsNullOrWhiteSpace(dto.ProductName)) existing.ProductName = dto.ProductName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Unit)) existing.Unit = dto.Unit.Trim();
        if (dto.ImageUrl != null) existing.ImageUrl = dto.ImageUrl.Trim();
        if (dto.Description != null) existing.Description = dto.Description.Trim();
        if (dto.ShelfLifeMonths.HasValue && dto.ShelfLifeMonths.Value >= 0) existing.ShelfLifeMonths = dto.ShelfLifeMonths.Value;

        if (dto.Cost.HasValue && dto.Cost.Value >= 0) existing.Cost = dto.Cost.Value; // Giá bán
        if (dto.MaterialCost.HasValue && dto.MaterialCost.Value >= 0) existing.MaterialCost = dto.MaterialCost.Value; // Giá vốn

        // Nếu sản phẩm đã có phiếu nhập thành phẩm, giá vốn luôn đồng bộ bằng giá cao nhất từ các phiếu nhập
        if (maxReceiptCost > 0)
        {
            existing.MaterialCost = maxReceiptCost;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật thành phẩm thành công.",
            id = id,
            productCode = $"SP{id:D3}",
            cost = existing.Cost ?? 0,
            costFormatted = (existing.Cost ?? 0).ToString("N0"),
            materialCost = existing.MaterialCost ?? 0,
            materialCostFormatted = (existing.MaterialCost ?? 0).ToString("N0")
        });
    }

    /// <summary>
    /// DELETE /api/warehouse/products/{id} : Xóa thành phẩm
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        bool hasBom = await _context.Boms.AsNoTracking().AnyAsync(b => b.ProductId == id);
        bool hasProductReceipt = await _context.ProductReceiptDetails.AsNoTracking().AnyAsync(d => d.ProductId == id);
        bool hasExportDetail = await _context.ProductExportReceiptDetails.AsNoTracking().AnyAsync(d => d.ProductId == id);

        if (hasBom || hasProductReceipt || hasExportDetail)
        {
            return BadRequest(new { message = "Không thể xóa thành phẩm đã phát sinh lệnh sản xuất, nhập kho hoặc xuất kho." });
        }

        var rows = await _context.Products.Where(p => p.ProductId == id).ExecuteDeleteAsync();
        if (rows == 0)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        return Ok(new { success = true, message = $"Đã xóa thành phẩm SP{id:D3} thành công." });
    }

    /// <summary>
    /// POST /api/warehouse/products/{id}/dispose-expired-batch/{receiptId} : Hủy lô sản phẩm hết hạn sử dụng
    /// </summary>
    [HttpPost("{id:int}/dispose-expired-batch/{receiptId:int}")]
    public async Task<IActionResult> DisposeExpiredProductBatch(int id, int receiptId)
    {
        var detail = await _context.ProductReceiptDetails
            .FirstOrDefaultAsync(d => d.ProductId == id && d.ProductReceiptId == receiptId);
        if (detail == null)
        {
            return NotFound(new { message = $"Không tìm thấy thông tin lô nhập của thành phẩm ID {id} từ lệnh LNK{receiptId:D5}." });
        }

        var receipt = await _context.ProductReceipts.FirstOrDefaultAsync(r => r.ProductReceiptId == receiptId);
        if (receipt == null || string.IsNullOrWhiteSpace(receipt.Status) || !ApprovedStatuses.Contains(receipt.Status))
        {
            return BadRequest(new { message = "Chỉ được phép hủy lô thành phẩm thuộc phiếu nhập kho đã duyệt (Approved)." });
        }

        var voucherCode = !string.IsNullOrWhiteSpace(receipt.ReceiptCode) ? receipt.ReceiptCode : $"LNK{receiptId:D5}";
        var disposalVoucherCode = $"HSP-{voucherCode}";

        bool alreadyDisposed = await (from ped in _context.ProductExportReceiptDetails.AsNoTracking()
                                      join pe in _context.ProductExportReceipts.AsNoTracking() on ped.ExportReceiptId equals pe.ExportReceiptId
                                      where ped.ProductId == id
                                            && (pe.ExpReceiptCode == disposalVoucherCode || pe.ExpReceiptCode == $"HSP-LNK{receiptId:D5}")
                                      select ped.ExportReceiptId).AnyAsync();
        if (alreadyDisposed)
        {
            return BadRequest(new { message = "Lô thành phẩm này đã được xử lý hủy trước đó." });
        }

        var now = DateTime.UtcNow;
        if (!detail.ExpiredDate.HasValue || detail.ExpiredDate.Value > now)
        {
            return BadRequest(new { message = "Lô thành phẩm này chưa hết hạn sử dụng. Chỉ được phép hủy các lô đã hết hạn." });
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (product == null)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        int disposedQty = detail.Quantity;
        product.Quantity = Math.Max(0, (product.Quantity ?? 0) - disposedQty);

        string unitLabel = !string.IsNullOrWhiteSpace(product.Unit) ? product.Unit : "Hộp";
        int nextExpId = (await _context.ProductExportReceipts.MaxAsync(e => (int?)e.ExportReceiptId) ?? 0) + 1;

        // Lưu vào bảng productexportreceipt & productexportreceiptdetail có sẵn mà không thêm cột vào bảng
        var disposalReceipt = new ProductExportReceipt
        {
            ExportReceiptId = nextExpId,
            OrderId = null,
            ExpReceiptCode = disposalVoucherCode,
            Status = "Approved",
            CreatedDate = now
        };
        _context.ProductExportReceipts.Add(disposalReceipt);

        decimal unitCost = (detail.Cost.HasValue && detail.Cost.Value > 0)
            ? detail.Cost.Value
            : (product.MaterialCost ?? product.Cost ?? 0);

        var disposalDetail = new ProductExportReceiptDetail
        {
            ExportReceiptId = nextExpId,
            ProductId = id,
            Quantity = disposedQty,
            Price = unitCost,
            Status = "Đã hủy",
            Note = $"Hủy sản phẩm hết hạn từ phiếu {voucherCode}",
            CreatedDate = now
        };
        _context.ProductExportReceiptDetails.Add(disposalDetail);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Đã hủy thành công {disposedQty:N0} {unitLabel} thành phẩm hết hạn sử dụng.",
            disposedQty = disposedQty,
            remainingStock = product.Quantity
        });
    }
}
