using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
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
            imageUrl = i.ImageUrl ?? "",
            description = i.Description ?? "",
            shelfLifeMonths = i.ShelfLifeMonths ?? 0,
            expiredDate = (i.ShelfLifeMonths != null && i.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(i.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
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
                                   ? (r.CreatedDate ?? d.CreatedDate).Value.ToString("dd/MM/yyyy HH:mm:ss")
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
                               status = r.Status ?? "Approve",
                               statusType = (r.Status == "Approve" || r.Status == "Approved" || r.Status == "Đã nhập kho") ? "success" :
                                            (r.Status == "Reject" || r.Status == "Rejected" || r.Status == "Từ chối") ? "danger" : "warning"
                           };

        var receiptDetails = await detailsQuery.ToListAsync();

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
            imageUrl = item.ImageUrl ?? "",
            description = item.Description ?? "",
            shelfLifeMonths = item.ShelfLifeMonths ?? 0,
            expiredDate = (item.ShelfLifeMonths != null && item.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(item.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028",
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
                                   ? (r.CreatedDate ?? d.CreatedDate).Value.ToString("dd/MM/yyyy HH:mm:ss")
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
                               status = r.Status ?? "Approve",
                               statusType = (r.Status == "Approve" || r.Status == "Approved" || r.Status == "Đã nhập kho") ? "success" :
                                            (r.Status == "Reject" || r.Status == "Rejected" || r.Status == "Từ chối") ? "danger" : "warning"
                           };

        return Ok(await detailsQuery.ToListAsync());
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

        var ingredient = new Ingredient
        {
            IngrName = dto.IngrName.Trim(),
            Quantity = dto.Quantity >= 0 ? dto.Quantity : 0,
            Unit = !string.IsNullOrWhiteSpace(dto.Unit) ? dto.Unit.Trim() : "",
            Price = dto.Price >= 0 ? dto.Price : 0,
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
            imageUrl = ingredient.ImageUrl ?? "",
            description = ingredient.Description ?? "",
            shelfLifeMonths = ingredient.ShelfLifeMonths ?? 0,
            expiredDate = (ingredient.ShelfLifeMonths != null && ingredient.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(ingredient.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
        });
    }

    /// <summary>
    /// PUT /api/warehouse/ingredients/{id} : Cập nhật thông tin nguyên vật liệu
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateIngredient(int id, [FromBody] IngredientDto dto)
    {
        var rows = await _context.Ingredients
            .Where(i => i.IngrId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.IngrName, i => !string.IsNullOrWhiteSpace(dto.IngrName) ? dto.IngrName.Trim() : i.IngrName)
                .SetProperty(i => i.Quantity, i => dto.Quantity.HasValue && dto.Quantity.Value >= 0 ? dto.Quantity.Value : i.Quantity)
                .SetProperty(i => i.Unit, i => !string.IsNullOrWhiteSpace(dto.Unit) ? dto.Unit.Trim() : i.Unit)
                .SetProperty(i => i.Price, i => dto.Price.HasValue && dto.Price.Value >= 0 ? dto.Price.Value : i.Price)
                .SetProperty(i => i.ImageUrl, i => dto.ImageUrl != null ? dto.ImageUrl.Trim() : i.ImageUrl)
                .SetProperty(i => i.Description, i => dto.Description != null ? dto.Description.Trim() : i.Description)
                .SetProperty(i => i.ShelfLifeMonths, i => dto.ShelfLifeMonths.HasValue && dto.ShelfLifeMonths.Value >= 0 ? dto.ShelfLifeMonths.Value : i.ShelfLifeMonths));

        if (rows == 0)
        {
            return NotFound(new { message = $"Không tìm thấy nguyên vật liệu có ID {id}." });
        }

        return Ok(new
        {
            success = true,
            message = "Cập nhật nguyên vật liệu thành công.",
            id = id,
            ingrCode = $"NVL{id:D3}"
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
}

