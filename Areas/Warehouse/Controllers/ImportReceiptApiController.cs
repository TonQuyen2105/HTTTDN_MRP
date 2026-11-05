using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/import-receipts")]
[Produces("application/json")]
[Tags("4. Nhập kho mua hàng (Import Receipts)")]
public class ImportReceiptApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ImportReceiptApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/import-receipts : Lấy danh sách phiếu nhập mua hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetImportReceipts()
    {
        var receipts = await _context.ImportReceipts.AsNoTracking().OrderByDescending(r => r.CreatedDate).ToListAsync();
        var suppliers = await _context.Suppliers.AsNoTracking().ToDictionaryAsync(s => s.SupplierId, s => s.SupplierName);

        var list = receipts.Select(r => new
        {
            voucherNo = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}",
            orderNo = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}",
            ipReceiptId = r.IpReceiptId,
            voucherDate = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            supplierId = r.SupplierId,
            supplierName = (r.SupplierId.HasValue && suppliers.ContainsKey(r.SupplierId.Value)) ? suppliers[r.SupplierId.Value] : "N/A",
            supplierCode = r.SupplierId.HasValue ? $"NCC{r.SupplierId.Value:D4}" : "",
            status = r.Status ?? "Chờ nhập kho",
            statusType = (r.Status == "Đã nhập kho" || r.Status == "Approved") ? "success" : "warning",
            creatorName = "Nguyễn Tiến Huy"
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/import-receipts/{id} : Xem chi tiết phiếu nhập
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetImportReceiptById(int id)
    {
        var receipt = await _context.ImportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu nhập có ID {id}." });
        }

        var supplier = receipt.SupplierId.HasValue 
            ? await _context.Suppliers.FindAsync(receipt.SupplierId.Value) 
            : null;

        var rawDetails = await _context.ImportReceiptDetails.Where(d => d.IpReceiptId == id).ToListAsync();
        var ingrIds = rawDetails.Select(d => d.IngrId).Distinct().ToList();
        var ingrs = await _context.Ingredients.Where(i => ingrIds.Contains(i.IngrId)).ToDictionaryAsync(i => i.IngrId);

        var items = rawDetails.Select(d =>
        {
            ingrs.TryGetValue(d.IngrId, out var ingr);
            var price = d.Cost ?? ingr?.Price ?? 0;
            return new
            {
                ingrId = d.IngrId,
                itemCode = $"NVL{d.IngrId:D3}",
                itemName = ingr?.IngrName ?? "N/A",
                unit = ingr?.Unit ?? "kg",
                quantity = d.Quantity,
                price = price,
                amount = d.Quantity * price,
                expiryDate = d.ExpiredDate.HasValue ? d.ExpiredDate.Value.ToString("dd/MM/yyyy") : ""
            };
        }).ToList();

        return Ok(new
        {
            ipReceiptId = receipt.IpReceiptId,
            voucherNo = receipt.IpReceiptCode ?? $"PNK{receipt.IpReceiptId:D5}",
            voucherDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            supplierId = receipt.SupplierId,
            supplierCode = supplier != null ? $"NCC{supplier.SupplierId:D4}" : "",
            supplierName = supplier?.SupplierName ?? "",
            status = receipt.Status ?? "Chờ nhập kho",
            employeeCode = "NV0005",
            employeeName = "Nguyễn Tiến Huy",
            items
        });
    }

    /// <summary>
    /// POST /api/warehouse/import-receipts : Tạo phiếu nhập mua hàng
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateImportReceipt([FromBody] CreateImportReceiptDto dto)
    {
        if (dto.SupplierId <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn nhà cung cấp hợp lệ." });
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Phiếu nhập phải có ít nhất 1 dòng nguyên vật liệu." });
        }

        var receipt = new ImportReceipt
        {
            SupplierId = dto.SupplierId,
            CreatedBy = dto.CreatedBy ?? 1,
            CreatedDate = DateTime.UtcNow,
            IpReceiptCode = dto.IpReceiptCode,
            Status = dto.IsDraft ? "Chờ nhập kho" : "Chờ duyệt"
        };

        _context.ImportReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(receipt.IpReceiptCode))
        {
            receipt.IpReceiptCode = $"PNK{receipt.IpReceiptId:D5}";
        }

        foreach (var it in dto.Items)
        {
            var detail = new ImportReceiptDetail
            {
                IpReceiptId = receipt.IpReceiptId,
                IngrId = it.IngrId,
                Quantity = it.Quantity > 0 ? it.Quantity : 1,
                Cost = it.Cost,
                CreatedDate = DateTime.UtcNow
            };
            _context.ImportReceiptDetails.Add(detail);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetImportReceiptById), new { id = receipt.IpReceiptId }, new
        {
            success = true,
            message = "Tạo phiếu nhập thành công.",
            ipReceiptId = receipt.IpReceiptId,
            voucherNo = receipt.IpReceiptCode
        });
    }

    /// <summary>
    /// PUT /api/warehouse/import-receipts/{id}/approve : Duyệt phiếu nhập (Cộng tồn kho NVL & cập nhật hạn dùng)
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveImportReceipt(int id)
    {
        var receipt = await _context.ImportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu nhập có ID {id}." });
        }

        if (receipt.Status == "Đã nhập kho" || receipt.Status == "Approved")
        {
            return BadRequest(new { message = "Phiếu nhập này đã được duyệt trước đó." });
        }

        var details = await _context.ImportReceiptDetails.Where(d => d.IpReceiptId == id).ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var d in details)
        {
            var ingr = await _context.Ingredients.FindAsync(d.IngrId);
            if (ingr != null)
            {
                ingr.Quantity = (ingr.Quantity ?? 0) + d.Quantity;
                int months = ingr.ShelfLifeMonths.HasValue && ingr.ShelfLifeMonths.Value > 0 ? ingr.ShelfLifeMonths.Value : 12;
                d.ExpiredDate = now.AddMonths(months);
            }
        }

        receipt.Status = "Đã nhập kho";
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Duyệt phiếu nhập {receipt.IpReceiptCode} thành công. Đã cộng tồn kho và cập nhật hạn sử dụng."
        });
    }
}

