using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

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

    /// <summary>
    /// GET /api/warehouse/product-exports/pending : Danh sách lệnh xuất hàng từ bộ phận Bán hàng chờ duyệt
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingProductExports()
    {
        var list = await _context.ProductExportReceipts
            .AsNoTracking()
            .Where(e => e.Status == "Pending" || e.Status == "Chờ xuất kho")
            .OrderByDescending(e => e.CreatedDate)
            .ToListAsync();

        var result = list.Select(e => new
        {
            e.ExportReceiptId,
            expReceiptCode = e.ExpReceiptCode ?? $"PXK_BH{e.ExportReceiptId:D4}",
            createdDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            status = e.Status,
            orderId = e.OrderId
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/warehouse/product-exports/{id} : Xem chi tiết đơn xuất bán
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductExportById(int id)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products.Where(p => prodIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

        var items = details.Select(d =>
        {
            prods.TryGetValue(d.ProductId, out var prod);
            return new
            {
                productId = d.ProductId,
                productCode = $"SP{d.ProductId:D3}",
                productName = prod?.ProductName ?? "N/A",
                unit = prod?.Unit ?? "Hộp",
                quantity = d.Quantity,
                price = d.Price ?? prod?.Cost ?? 0,
                status = d.Status,
                note = d.Note
            };
        }).ToList();

        return Ok(new
        {
            receipt.ExportReceiptId,
            expReceiptCode = receipt.ExpReceiptCode ?? $"PXK_BH{receipt.ExportReceiptId:D4}",
            receipt.CreatedDate,
            receipt.Status,
            receipt.OrderId,
            items
        });
    }

    /// <summary>
    /// PUT /api/warehouse/product-exports/{id}/approve : Duyệt xuất hàng -> Trừ tồn kho thành phẩm gốc
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveProductExport(int id)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        if (receipt.Status == "Approved" || receipt.Status == "Đã xuất kho")
        {
            return BadRequest(new { message = "Lệnh xuất hàng này đã được duyệt xuất kho trước đó." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();

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
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Duyệt xuất hàng {receipt.ExpReceiptCode} thành công. Đã trừ tồn kho thành phẩm." });
    }

    /// <summary>
    /// PUT /api/warehouse/product-exports/{id}/reject : Từ chối xuất hàng
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectProductExport(int id, [FromBody] RejectDto? dto)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        receipt.Status = "Rejected";
        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        foreach (var d in details)
        {
            d.Status = "Rejected";
            if (!string.IsNullOrWhiteSpace(dto?.Reason)) d.Note = dto.Reason;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = $"Đã từ chối lệnh xuất hàng {receipt.ExpReceiptCode}." });
    }
}

