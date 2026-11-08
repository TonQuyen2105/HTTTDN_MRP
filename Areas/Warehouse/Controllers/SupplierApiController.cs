using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/suppliers")]
[Produces("application/json")]
[Tags("3. Nhà cung cấp (Suppliers)")]
public class SupplierApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SupplierApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/suppliers : Lấy danh sách nhà cung cấp
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var dbItems = await _context.Suppliers.AsNoTracking().OrderBy(s => s.SupplierId).ToListAsync();

        var list = dbItems.Select(s => new
        {
            id = s.SupplierId,
            supplierId = s.SupplierId,
            code = $"NCC{s.SupplierId:D4}",
            name = s.SupplierName,
            supplierName = s.SupplierName,
            phone = s.Phone ?? "",
            address = s.Address ?? "",
            mail = s.Mail ?? ""
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/suppliers/{id} : Chi tiết nhà cung cấp theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSupplierById(int id)
    {
        var s = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.SupplierId == id);
        if (s == null)
        {
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp có ID {id}." });
        }

        return Ok(new
        {
            id = s.SupplierId,
            supplierId = s.SupplierId,
            code = $"NCC{s.SupplierId:D4}",
            name = s.SupplierName,
            supplierName = s.SupplierName,
            phone = s.Phone ?? "",
            address = s.Address ?? "",
            mail = s.Mail ?? ""
        });
    }

    /// <summary>
    /// POST /api/warehouse/suppliers : Thêm mới nhà cung cấp
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] SupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierName))
        {
            return BadRequest(new { message = "Tên nhà cung cấp không được để trống." });
        }

        var supplier = new Supplier
        {
            SupplierName = dto.SupplierName.Trim(),
            Phone = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            Mail = dto.Mail?.Trim()
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.SupplierId }, new
        {
            id = supplier.SupplierId,
            supplierId = supplier.SupplierId,
            code = $"NCC{supplier.SupplierId:D4}",
            name = supplier.SupplierName,
            supplierName = supplier.SupplierName,
            phone = supplier.Phone ?? "",
            address = supplier.Address ?? "",
            mail = supplier.Mail ?? ""
        });
    }

    /// <summary>
    /// PUT /api/warehouse/suppliers/{id} : Cập nhật thông tin nhà cung cấp
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierDto dto)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == id);
        if (supplier == null)
        {
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp có ID {id}." });
        }

        if (string.IsNullOrWhiteSpace(dto.SupplierName))
        {
            return BadRequest(new { message = "Tên nhà cung cấp không được để trống." });
        }

        supplier.SupplierName = dto.SupplierName.Trim();
        supplier.Phone = dto.Phone?.Trim() ?? "";
        supplier.Address = dto.Address?.Trim() ?? "";
        supplier.Mail = dto.Mail?.Trim() ?? "";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật nhà cung cấp thành công.",
            supplier = new
            {
                id = supplier.SupplierId,
                supplierId = supplier.SupplierId,
                code = $"NCC{supplier.SupplierId:D4}",
                name = supplier.SupplierName,
                supplierName = supplier.SupplierName,
                phone = supplier.Phone ?? "",
                address = supplier.Address ?? "",
                mail = supplier.Mail ?? ""
            }
        });
    }

    /// <summary>
    /// DELETE /api/warehouse/suppliers/{id} : Xóa nhà cung cấp
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == id);
        if (supplier == null)
        {
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp có ID {id}." });
        }

        // Kiểm tra xem nhà cung cấp có liên kết với chứng từ nhập kho nào không
        bool hasReceipts = await _context.ImportReceipts.AnyAsync(r => r.SupplierId == id);
        if (hasReceipts)
        {
            return BadRequest(new { message = $"Không thể xóa nhà cung cấp '{supplier.SupplierName}' vì đã phát sinh chứng từ nhập kho trong hệ thống." });
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Đã xóa thành công nhà cung cấp '{supplier.SupplierName}'." });
    }
}
