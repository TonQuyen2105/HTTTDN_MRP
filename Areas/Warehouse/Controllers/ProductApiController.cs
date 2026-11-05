using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/products")]
[Produces("application/json")]
[Tags("2. Thành phẩm (Products)")]
public class ProductApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/products : Lấy danh sách thành phẩm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? keyword = null)
    {
        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(kw) 
                                  || (p.Unit != null && p.Unit.ToLower().Contains(kw))
                                  || (p.Description != null && p.Description.ToLower().Contains(kw)));
        }

        var dbItems = await query.OrderBy(p => p.ProductId).ToListAsync();

        var list = dbItems.Select(p => new
        {
            id = p.ProductId,
            productId = p.ProductId,
            productCode = $"SP{p.ProductId:D3}",
            productName = p.ProductName,
            quantity = p.Quantity ?? 0,
            quantityFormatted = (p.Quantity ?? 0).ToString("N0"),
            unit = p.Unit ?? "Hộp",
            cost = p.Cost ?? 0,
            costFormatted = (p.Cost ?? 0).ToString("N0"),
            imageUrl = p.ImageUrl ?? "",
            description = p.Description ?? "",
            shelfLifeMonths = p.ShelfLifeMonths ?? 0,
            expiredDate = (p.ShelfLifeMonths != null && p.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(p.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/products/{id} : Xem chi tiết 1 thành phẩm
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var item = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (item == null)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        return Ok(new
        {
            id = item.ProductId,
            productId = item.ProductId,
            productCode = $"SP{item.ProductId:D3}",
            productName = item.ProductName,
            quantity = item.Quantity ?? 0,
            quantityFormatted = (item.Quantity ?? 0).ToString("N0"),
            unit = item.Unit ?? "Hộp",
            cost = item.Cost ?? 0,
            costFormatted = (item.Cost ?? 0).ToString("N0"),
            imageUrl = item.ImageUrl ?? "",
            description = item.Description ?? "",
            shelfLifeMonths = item.ShelfLifeMonths ?? 0,
            expiredDate = (item.ShelfLifeMonths != null && item.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(item.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
        });
    }

    /// <summary>
    /// POST /api/warehouse/products : Thêm mới thành phẩm
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
            Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Hộp" : dto.Unit.Trim(),
            Cost = dto.Cost >= 0 ? dto.Cost : 0,
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
            imageUrl = product.ImageUrl ?? "",
            description = product.Description ?? "",
            shelfLifeMonths = product.ShelfLifeMonths ?? 0,
            expiredDate = (product.ShelfLifeMonths != null && product.ShelfLifeMonths > 0)
                ? DateTime.Now.AddMonths(product.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                : "31/12/2028"
        });
    }

    /// <summary>
    /// PUT /api/warehouse/products/{id} : Cập nhật thông tin thành phẩm
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
    {
        var rows = await _context.Products
            .Where(p => p.ProductId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.ProductName, p => !string.IsNullOrWhiteSpace(dto.ProductName) ? dto.ProductName.Trim() : p.ProductName)
                .SetProperty(p => p.Quantity, p => dto.Quantity.HasValue && dto.Quantity.Value >= 0 ? dto.Quantity.Value : p.Quantity)
                .SetProperty(p => p.Unit, p => !string.IsNullOrWhiteSpace(dto.Unit) ? dto.Unit.Trim() : p.Unit)
                .SetProperty(p => p.Cost, p => dto.Cost.HasValue && dto.Cost.Value >= 0 ? dto.Cost.Value : p.Cost)
                .SetProperty(p => p.ImageUrl, p => dto.ImageUrl != null ? dto.ImageUrl.Trim() : p.ImageUrl)
                .SetProperty(p => p.Description, p => dto.Description != null ? dto.Description.Trim() : p.Description)
                .SetProperty(p => p.ShelfLifeMonths, p => dto.ShelfLifeMonths.HasValue && dto.ShelfLifeMonths.Value >= 0 ? dto.ShelfLifeMonths.Value : p.ShelfLifeMonths));

        if (rows == 0)
        {
            return NotFound(new { message = $"Không tìm thấy thành phẩm có ID {id}." });
        }

        return Ok(new
        {
            success = true,
            message = "Cập nhật thành phẩm thành công.",
            id = id,
            productCode = $"SP{id:D3}"
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
}

