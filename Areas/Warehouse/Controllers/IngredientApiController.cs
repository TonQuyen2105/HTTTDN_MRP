using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Authorize(Roles = "Warehouse,Kho")]
[IgnoreAntiforgeryToken]
[Route("api/warehouse/ingredients")]
[Produces("application/json")]
[Tags("1. Nguyên vật liệu (Ingredients)")]
public class IngredientApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public IngredientApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/ingredients : Lấy danh sách nguyên vật liệu
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetIngredients([FromQuery] string? keyword = null)
    {
        var query = _context.Ingredients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(i => i.IngrName.ToLower().Contains(kw) 
                                  || (i.Unit != null && i.Unit.ToLower().Contains(kw))
                                  || (i.Description != null && i.Description.ToLower().Contains(kw)));
        }

        var dbItems = await query.OrderBy(i => i.IngrId).ToListAsync();
        var ingrIdsWithReceipts = (await _context.ImportReceiptDetails
            .AsNoTracking()
            .Select(d => d.IngrId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var list = dbItems.Select(i => new
        {
            id = i.IngrId,
            ingrId = i.IngrId,
            ingrCode = $"NVL{i.IngrId:D3}",
            ingrName = i.IngrName,
            quantity = i.Quantity ?? 0,
            quantityFormatted = (i.Quantity ?? 0).ToString("N2"),
            unit = i.Unit ?? "kg",
            price = i.Price ?? 0,
            priceFormatted = (i.Price ?? 0).ToString("N0"),
            cost = i.Cost ?? 0,
            costFormatted = (i.Cost ?? 0).ToString("N0"),
            imageUrl = i.ImageUrl ?? "",
            description = i.Description ?? "",
            shelfLifeMonths = i.ShelfLifeMonths ?? 0,
            expiredDate = (i.ShelfLifeMonths != null && i.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(i.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028",
            hasImportReceipts = ingrIdsWithReceipts.Contains(i.IngrId)
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/ingredients/{id} : Xem chi tiết 1 nguyên vật liệu và lịch sử các phiếu nhập
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetIngredientById(int id)
    {
        var item = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngrId == id);
        if (item == null)
        {
            return NotFound(new { message = $"Không tìm thấy nguyên vật liệu có ID {id}." });
        }

        // Lấy danh sách chi tiết các phiếu nhập có chứa nguyên vật liệu này
        var detailsQuery = from d in _context.ImportReceiptDetails.AsNoTracking()
                           where d.IngrId == id
                           join r in _context.ImportReceipts.AsNoTracking() on d.IpReceiptId equals r.IpReceiptId
                           join s in _context.Suppliers.AsNoTracking() on r.SupplierId equals s.SupplierId into supJoin
                           from s in supJoin.DefaultIfEmpty()
                           orderby (r.CreatedDate ?? d.CreatedDate) descending, r.IpReceiptId descending
                           select new
                           {
                               ipReceiptId = r.IpReceiptId,
                               voucherNo = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}",
                               orderNo = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}",
                               importDate = (r.CreatedDate ?? d.CreatedDate).HasValue
                                   ? (r.CreatedDate ?? d.CreatedDate)!.Value.ToString("dd/MM/yyyy HH:mm:ss")
                                   : "",
                               supplierId = r.SupplierId,
                               supplierCode = s != null ? $"NCC{s.SupplierId:D4}" : "",
                               supplierName = s != null ? s.SupplierName : "Chưa xác định",
                               quantity = d.Quantity,
                               quantityFormatted = d.Quantity.ToString("N2"),
                               unit = item.Unit ?? "kg",
                               cost = d.Cost ?? item.Price ?? 0,
                               costFormatted = (d.Cost ?? item.Price ?? 0).ToString("N0"),
                               amount = (d.Cost ?? item.Price ?? 0) * d.Quantity,
                               amountFormatted = ((d.Cost ?? item.Price ?? 0) * d.Quantity).ToString("N0"),
                               expiredDate = d.ExpiredDate.HasValue
                                   ? d.ExpiredDate.Value.ToString("dd/MM/yyyy")
                                   : "31/12/2028",
                               rawExpiredDate = d.ExpiredDate,
                               status = r.Status ?? "Approve",
                               statusType = (r.Status == "Approve" || r.Status == "Approved" || r.Status == "Đã nhập kho") ? "success" :
                                            (r.Status == "Reject" || r.Status == "Rejected" || r.Status == "Từ chối") ? "danger" : "warning"
                           };

        var disposedCodes = await _context.MaterialReceipts.AsNoTracking()
            .Where(m => m.ExpReceiptCode != null && m.ExpReceiptCode.StartsWith("HMY-"))
            .Select(m => m.ExpReceiptCode!)
            .ToListAsync();

        var rawReceiptDetails = await detailsQuery.ToListAsync();
        var now = DateTime.UtcNow;
        var receiptDetails = rawReceiptDetails.Select(d =>
        {
            var vCode = d.voucherNo;
            bool isDisposed = disposedCodes.Contains($"HMY-{vCode}") || disposedCodes.Contains($"HMY-PNK{d.ipReceiptId:D5}");
            bool isExpired = d.rawExpiredDate.HasValue && d.rawExpiredDate.Value <= now;
            return new
            {
                d.ipReceiptId,
                d.voucherNo,
                d.orderNo,
                d.importDate,
                d.supplierId,
                d.supplierCode,
                d.supplierName,
                d.quantity,
                d.quantityFormatted,
                d.unit,
                d.cost,
                d.costFormatted,
                d.amount,
                d.amountFormatted,
                d.expiredDate,
                isExpired = isExpired,
                isDisposed = isDisposed,
                status = isDisposed ? "Đã hủy" : d.status,
                statusType = isDisposed ? "danger" : d.statusType
            };
        }).ToList();

        return Ok(new
        {
            id = item.IngrId,
            ingrId = item.IngrId,
            ingrCode = $"NVL{item.IngrId:D3}",
            ingrName = item.IngrName,
            quantity = item.Quantity ?? 0,
            quantityFormatted = (item.Quantity ?? 0).ToString("N2"),
            unit = item.Unit ?? "kg",
            price = item.Price ?? 0,
            priceFormatted = (item.Price ?? 0).ToString("N0"),
            cost = item.Cost ?? 0,
            costFormatted = (item.Cost ?? 0).ToString("N0"),
            imageUrl = item.ImageUrl ?? "",
            description = item.Description ?? "",
            shelfLifeMonths = item.ShelfLifeMonths ?? 0,
            expiredDate = (item.ShelfLifeMonths != null && item.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(item.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028",
            hasImportReceipts = receiptDetails.Count > 0,
            importReceipts = receiptDetails
        });
    }

    /// <summary>
    /// GET /api/warehouse/ingredients/{id}/import-receipts : Danh sách phiếu nhập của nguyên vật liệu
    /// </summary>
    [HttpGet("{id:int}/import-receipts")]
    public async Task<IActionResult> GetIngredientImportReceipts(int id)
    {
        var exists = await _context.Ingredients.AnyAsync(i => i.IngrId == id);
        if (!exists)
        {
            return NotFound(new { message = $"Không tìm thấy nguyên vật liệu có ID {id}." });
        }

        var detailsQuery = from d in _context.ImportReceiptDetails.AsNoTracking()
                           where d.IngrId == id
                           join r in _context.ImportReceipts.AsNoTracking() on d.IpReceiptId equals r.IpReceiptId
                           join s in _context.Suppliers.AsNoTracking() on r.SupplierId equals s.SupplierId into supJoin
                           from s in supJoin.DefaultIfEmpty()
                           orderby (r.CreatedDate ?? d.CreatedDate) descending, r.IpReceiptId descending
                           select new
                           {
                               ipReceiptId = r.IpReceiptId,
                               voucherNo = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}",
                               orderNo = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}",
                               importDate = (r.CreatedDate ?? d.CreatedDate).HasValue
                                   ? (r.CreatedDate ?? d.CreatedDate)!.Value.ToString("dd/MM/yyyy HH:mm:ss")
                                   : "",
                               supplierId = r.SupplierId,
                               supplierCode = s != null ? $"NCC{s.SupplierId:D4}" : "",
                               supplierName = s != null ? s.SupplierName : "Chưa xác định",
                               quantity = d.Quantity,
                               quantityFormatted = d.Quantity.ToString("N2"),
                               cost = d.Cost ?? 0,
                               costFormatted = (d.Cost ?? 0).ToString("N0"),
                               amount = (d.Cost ?? 0) * d.Quantity,
                               amountFormatted = ((d.Cost ?? 0) * d.Quantity).ToString("N0"),
                               expiredDate = d.ExpiredDate.HasValue
                                   ? d.ExpiredDate.Value.ToString("dd/MM/yyyy")
                                   : "31/12/2028",
                               rawExpiredDate = d.ExpiredDate,
                               status = r.Status ?? "Approve",
                               statusType = (r.Status == "Approve" || r.Status == "Approved" || r.Status == "Đã nhập kho") ? "success" :
                                            (r.Status == "Reject" || r.Status == "Rejected" || r.Status == "Từ chối") ? "danger" : "warning"
                           };

        var disposedCodes = await _context.MaterialReceipts.AsNoTracking()
            .Where(m => m.ExpReceiptCode != null && m.ExpReceiptCode.StartsWith("HMY-"))
            .Select(m => m.ExpReceiptCode!)
            .ToListAsync();

        var rawList = await detailsQuery.ToListAsync();
        var nowTime = DateTime.UtcNow;
        var resultList = rawList.Select(d =>
        {
            var vCode = d.voucherNo;
            bool isDisposed = disposedCodes.Contains($"HMY-{vCode}") || disposedCodes.Contains($"HMY-PNK{d.ipReceiptId:D5}");
            bool isExpired = d.rawExpiredDate.HasValue && d.rawExpiredDate.Value <= nowTime;
            return new
            {
                d.ipReceiptId,
                d.voucherNo,
                d.orderNo,
                d.importDate,
                d.supplierId,
                d.supplierCode,
                d.supplierName,
                d.quantity,
                d.quantityFormatted,
                d.cost,
                d.costFormatted,
                d.amount,
                d.amountFormatted,
                d.expiredDate,
                isExpired = isExpired,
                isDisposed = isDisposed,
                status = isDisposed ? "Đã hủy" : d.status,
                statusType = isDisposed ? "danger" : d.statusType
            };
        }).ToList();

        return Ok(resultList);
    }

    /// <summary>
    /// POST /api/warehouse/ingredients : Thêm mới nguyên vật liệu
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateIngredient([FromBody] IngredientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IngrName))
        {
            return BadRequest(new { message = "Tên nguyên vật liệu không được để trống." });
        }

        if (string.IsNullOrWhiteSpace(dto.Unit))
        {
            return BadRequest(new { message = "Đơn vị tính (ĐVT) không được để trống." });
        }

        string cleanUnit = dto.Unit.Trim();
        if (cleanUnit.Length > 20)
        {
            return BadRequest(new { message = "Đơn vị tính (ĐVT) tối đa 20 ký tự." });
        }

        // Ràng buộc: đơn giá không được bằng 0
        if (!dto.Price.HasValue || dto.Price.Value <= 0)
        {
            return BadRequest(new { message = "Đơn giá nguyên vật liệu không được bằng 0. Vui lòng nhập đơn giá hợp lệ lớn hơn 0." });
        }

        var ingredient = new Ingredient
        {
            IngrName = dto.IngrName.Trim(),
            Quantity = dto.Quantity >= 0 ? dto.Quantity : 0,
            Unit = cleanUnit,
            Price = dto.Price.Value,
            Cost = dto.Cost.HasValue && dto.Cost.Value >= 0 ? dto.Cost.Value : 0,
            ImageUrl = dto.ImageUrl?.Trim(),
            Description = dto.Description?.Trim(),
            ShelfLifeMonths = dto.ShelfLifeMonths >= 0 ? dto.ShelfLifeMonths : 24
        };

        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetIngredientById), new { id = ingredient.IngrId }, new
        {
            id = ingredient.IngrId,
            ingrId = ingredient.IngrId,
            ingrCode = $"NVL{ingredient.IngrId:D3}",
            ingrName = ingredient.IngrName,
            quantity = ingredient.Quantity ?? 0,
            quantityFormatted = (ingredient.Quantity ?? 0).ToString("N2"),
            unit = ingredient.Unit,
            price = ingredient.Price ?? 0,
            priceFormatted = (ingredient.Price ?? 0).ToString("N0"),
            cost = ingredient.Cost ?? 0,
            costFormatted = (ingredient.Cost ?? 0).ToString("N0"),
            imageUrl = ingredient.ImageUrl ?? "",
            description = ingredient.Description ?? "",
            shelfLifeMonths = ingredient.ShelfLifeMonths ?? 0,
            expiredDate = (ingredient.ShelfLifeMonths != null && ingredient.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(ingredient.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028",
            hasImportReceipts = false
        });
    }

    /// <summary>
    /// PUT /api/warehouse/ingredients/{id} : Cập nhật thông tin nguyên vật liệu
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateIngredient(int id, [FromBody] IngredientDto dto)
    {
        var existing = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngrId == id);
        if (existing == null)
        {
            return NotFound(new { message = $"Không tìm thấy nguyên vật liệu có ID {id}." });
        }

        if (dto.IngrName != null && string.IsNullOrWhiteSpace(dto.IngrName))
        {
            return BadRequest(new { message = "Tên nguyên vật liệu không được để trống." });
        }

        if (dto.Price.HasValue && dto.Price.Value <= 0)
        {
            return BadRequest(new { message = "Đơn giá nguyên vật liệu không được bằng 0. Vui lòng nhập đơn giá hợp lệ lớn hơn 0." });
        }

        bool hasImportReceipt = await _context.ImportReceiptDetails.AsNoTracking().AnyAsync(d => d.IngrId == id);
        if (dto.Unit != null)
        {
            string incomingUnit = dto.Unit.Trim();
            string currentUnit = (existing.Unit ?? "").Trim();

            if (!string.Equals(incomingUnit, currentUnit, StringComparison.Ordinal))
            {
                if (hasImportReceipt)
                {
                    return BadRequest(new { message = $"Nguyên vật liệu NVL{id:D3} đã có phiếu nhập kho nên không được sửa đơn vị tính." });
                }
                if (string.IsNullOrWhiteSpace(incomingUnit))
                {
                    return BadRequest(new { message = "Đơn vị tính (ĐVT) không được để trống." });
                }
                if (incomingUnit.Length > 20)
                {
                    return BadRequest(new { message = "Đơn vị tính (ĐVT) tối đa 20 ký tự." });
                }
                existing.Unit = incomingUnit;
            }
            else if (!hasImportReceipt && string.IsNullOrWhiteSpace(incomingUnit))
            {
                return BadRequest(new { message = "Đơn vị tính (ĐVT) không được để trống." });
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.IngrName)) existing.IngrName = dto.IngrName.Trim();
        if (dto.Quantity.HasValue && dto.Quantity.Value >= 0) existing.Quantity = dto.Quantity.Value;
        if (dto.Price.HasValue && dto.Price.Value > 0) existing.Price = dto.Price.Value;
        if (dto.Cost.HasValue && dto.Cost.Value >= 0) existing.Cost = dto.Cost.Value;
        if (dto.ImageUrl != null) existing.ImageUrl = dto.ImageUrl.Trim();
        if (dto.Description != null) existing.Description = dto.Description.Trim();
        if (dto.ShelfLifeMonths.HasValue && dto.ShelfLifeMonths.Value >= 0) existing.ShelfLifeMonths = dto.ShelfLifeMonths.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật nguyên vật liệu thành công.",
            id = id,
            ingrCode = $"NVL{id:D3}",
            unit = existing.Unit ?? "",
            hasImportReceipts = hasImportReceipt
        });
    }

    /// <summary>
    /// DELETE /api/warehouse/ingredients/{id} : Xóa nguyên vật liệu
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteIngredient(int id)
    {
        bool hasBom = await _context.Boms.AsNoTracking().AnyAsync(b => b.IngrId == id);
        bool hasImportDetail = await _context.ImportReceiptDetails.AsNoTracking().AnyAsync(d => d.IngrId == id);
        bool hasMaterialDetail = await _context.MaterialReceiptDetails.AsNoTracking().AnyAsync(d => d.IngrId == id);

        if (hasBom || hasImportDetail || hasMaterialDetail)
        {
            return BadRequest(new { message = "Không thể xóa nguyên vật liệu đã phát sinh giao dịch nhập kho, xuất kho hoặc định mức BOM." });
        }

        var rows = await _context.Ingredients.Where(i => i.IngrId == id).ExecuteDeleteAsync();
        if (rows == 0)
        {
            return NotFound(new { message = $"Không tìm thấy nguyên vật liệu có ID {id}." });
        }

        return Ok(new { success = true, message = $"Đã xóa nguyên vật liệu NVL{id:D3} thành công." });
    }

    /// <summary>
    /// POST /api/warehouse/ingredients/{id}/dispose-expired-batch/{receiptId} : Hủy lô nguyên vật liệu hết hạn sử dụng
    /// </summary>
    [HttpPost("{id:int}/dispose-expired-batch/{receiptId:int}")]
    public async Task<IActionResult> DisposeExpiredBatch(int id, int receiptId)
    {
        var detail = await _context.ImportReceiptDetails.FirstOrDefaultAsync(d => d.IngrId == id && d.IpReceiptId == receiptId);
        if (detail == null)
        {
            return NotFound(new { message = $"Không tìm thấy thông tin lô nhập của nguyên vật liệu ID {id} từ phiếu PNK{receiptId:D5}." });
        }

        var receipt = await _context.ImportReceipts.FirstOrDefaultAsync(r => r.IpReceiptId == receiptId);
        var voucherCode = receipt?.IpReceiptCode ?? $"PNK{receiptId:D5}";
        var disposalVoucherCode = $"HMY-{voucherCode}";

        bool alreadyDisposed = await _context.MaterialReceipts.AnyAsync(m => m.ExpReceiptCode == disposalVoucherCode || m.ExpReceiptCode == $"HMY-PNK{receiptId:D5}");
        if (alreadyDisposed)
        {
            return BadRequest(new { message = "Lô nguyên vật liệu này đã được xử lý hủy trước đó." });
        }

        var now = DateTime.UtcNow;
        if (!detail.ExpiredDate.HasValue || detail.ExpiredDate.Value > now)
        {
            return BadRequest(new { message = "Lô nguyên vật liệu này chưa hết hạn sử dụng. Chỉ được phép hủy các lô đã hết hạn." });
        }

        var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngrId == id);
        if (ingredient == null)
        {
            return NotFound(new { message = $"Không tìm thấy nguyên vật liệu có ID {id}." });
        }

        var disposedQty = detail.Quantity;
        ingredient.Quantity = Math.Max(0m, (ingredient.Quantity ?? 0m) - disposedQty);

        // Lưu vào bảng materialreceipt & materialreceiptdetail có sẵn mà không thêm cột vào bảng
        var disposalReceipt = new MaterialReceipt
        {
            ExpReceiptCode = disposalVoucherCode,
            Status = "Approved",
            CreatedDate = now
        };
        _context.MaterialReceipts.Add(disposalReceipt);
        await _context.SaveChangesAsync();

        var disposalDetail = new MaterialReceiptDetail
        {
            ExpReceiptId = disposalReceipt.ExpReceiptId,
            IngrId = id,
            Quantity = disposedQty,
            UnitCost = detail.Cost ?? ingredient.Price ?? 0,
            Status = "Đã hủy",
            Note = $"Hủy nguyên vật liệu hết hạn từ phiếu {voucherCode}",
            CreatedDate = now,
            ExportDate = now
        };
        _context.MaterialReceiptDetails.Add(disposalDetail);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Đã hủy thành công {disposedQty:N2} {ingredient.Unit} nguyên vật liệu hết hạn sử dụng.",
            disposedQty = disposedQty,
            remainingStock = ingredient.Quantity
        });
    }
}

