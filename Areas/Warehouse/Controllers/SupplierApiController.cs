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
    private static bool _initializedSupplierImportCost = false;

    public SupplierApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task EnsureSupplierImportCostColumnAsync()
    {
        if (_initializedSupplierImportCost) return;
        try
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE supplier ADD COLUMN IF NOT EXISTS import_cost NUMERIC(5,2) DEFAULT 0;
                UPDATE supplier SET import_cost = 8.5 WHERE supplierid = 46 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 10.0 WHERE supplierid = 47 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 5.0 WHERE supplierid = 48 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 7.5 WHERE supplierid = 49 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 12.0 WHERE supplierid = 50 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 15.0 WHERE supplierid = 51 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 6.0 WHERE supplierid = 52 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 18.0 WHERE supplierid = 53 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 9.0 WHERE supplierid = 54 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 11.5 WHERE supplierid = 55 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 14.0 WHERE supplierid = 57 AND (import_cost IS NULL OR import_cost = 0);
                UPDATE supplier SET import_cost = 16.5 WHERE supplierid = 58 AND (import_cost IS NULL OR import_cost = 0);
            ");
            _initializedSupplierImportCost = true;
        }
        catch
        {
            // Ignore if error or already updated
        }
    }

    /// <summary>
    /// GET /api/warehouse/suppliers : Lấy danh sách nhà cung cấp
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        await EnsureSupplierImportCostColumnAsync();

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
            mail = s.Mail ?? "",
            importCost = s.ImportCost ?? 0,
            importCostFormatted = $"{s.ImportCost ?? 0:N1}%"
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/suppliers/{id} : Chi tiết nhà cung cấp theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSupplierById(int id)
    {
        await EnsureSupplierImportCostColumnAsync();

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
            mail = s.Mail ?? "",
            importCost = s.ImportCost ?? 0,
            importCostFormatted = $"{s.ImportCost ?? 0:N1}%"
        });
    }

    /// <summary>
    /// POST /api/warehouse/suppliers : Thêm mới nhà cung cấp
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] SupplierDto dto)
    {
        await EnsureSupplierImportCostColumnAsync();

        if (string.IsNullOrWhiteSpace(dto.SupplierName))
        {
            return BadRequest(new { message = "Tên nhà cung cấp không được để trống." });
        }

        string cleanName = dto.SupplierName.Trim();

        // 1. Ràng buộc kiểm tra nhà cung cấp đã tồn tại theo tên (không phân biệt hoa thường)
        var existingByName = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierName.ToLower() == cleanName.ToLower());
        if (existingByName != null)
        {
            return BadRequest(new
            {
                message = $"Nhà cung cấp '{cleanName}' đã tồn tại trong hệ thống (Mã: NCC{existingByName.SupplierId:D4}). Không thể thêm mới."
            });
        }

        // 2. Kiểm tra trùng số điện thoại (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            string cleanPhone = dto.Phone.Trim();
            var existingByPhone = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Phone != null && s.Phone.Trim() == cleanPhone);
            if (existingByPhone != null)
            {
                return BadRequest(new
                {
                    message = $"Số điện thoại '{cleanPhone}' đã được đăng ký cho nhà cung cấp '{existingByPhone.SupplierName}' (Mã: NCC{existingByPhone.SupplierId:D4})."
                });
            }
        }

        // 3. Kiểm tra trùng email (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(dto.Mail))
        {
            string cleanMail = dto.Mail.Trim().ToLower();
            var existingByMail = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Mail != null && s.Mail.Trim().ToLower() == cleanMail);
            if (existingByMail != null)
            {
                return BadRequest(new
                {
                    message = $"Email '{dto.Mail.Trim()}' đã được sử dụng bởi nhà cung cấp '{existingByMail.SupplierName}' (Mã: NCC{existingByMail.SupplierId:D4})."
                });
            }
        }

        // 4. Ràng buộc trường phí nhập: từ 0 đến 100% và tối đa 1 chữ số thập phân
        if (dto.ImportCost.HasValue)
        {
            if (dto.ImportCost.Value < 0 || dto.ImportCost.Value > 100)
            {
                return BadRequest(new { message = "Tỷ lệ chi phí nhập phải nằm trong khoảng từ 0% đến 100%." });
            }

            decimal rounded = Math.Round(dto.ImportCost.Value, 1, MidpointRounding.AwayFromZero);
            if (dto.ImportCost.Value != rounded)
            {
                return BadRequest(new { message = "Tỷ lệ chi phí nhập chỉ được có tối đa 1 chữ số thập phân (Ví dụ: 8.5%)." });
            }
        }

        var supplier = new Supplier
        {
            SupplierName = cleanName,
            Phone = dto.Phone?.Trim() ?? "",
            Address = dto.Address?.Trim() ?? "",
            Mail = dto.Mail?.Trim() ?? "",
            ImportCost = (dto.ImportCost.HasValue && dto.ImportCost.Value >= 0) ? Math.Round(dto.ImportCost.Value, 1, MidpointRounding.AwayFromZero) : 0
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
            mail = supplier.Mail ?? "",
            importCost = supplier.ImportCost ?? 0,
            importCostFormatted = $"{supplier.ImportCost ?? 0:N1}%"
        });
    }

    /// <summary>
    /// PUT /api/warehouse/suppliers/{id} : Cập nhật thông tin nhà cung cấp
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierDto dto)
    {
        await EnsureSupplierImportCostColumnAsync();

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == id);
        if (supplier == null)
        {
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp có ID {id}." });
        }

        if (string.IsNullOrWhiteSpace(dto.SupplierName))
        {
            return BadRequest(new { message = "Tên nhà cung cấp không được để trống." });
        }

        string cleanName = dto.SupplierName.Trim();

        // 1. Ràng buộc kiểm tra trùng tên nhà cung cấp khác
        var existingByName = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId != id && s.SupplierName.ToLower() == cleanName.ToLower());
        if (existingByName != null)
        {
            return BadRequest(new
            {
                message = $"Tên nhà cung cấp '{cleanName}' đã tồn tại trong hệ thống (Mã: NCC{existingByName.SupplierId:D4}). Vui lòng chọn tên khác."
            });
        }

        // 2. Kiểm tra trùng số điện thoại với nhà cung cấp khác
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            string cleanPhone = dto.Phone.Trim();
            var existingByPhone = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierId != id && s.Phone != null && s.Phone.Trim() == cleanPhone);
            if (existingByPhone != null)
            {
                return BadRequest(new
                {
                    message = $"Số điện thoại '{cleanPhone}' đã được đăng ký cho nhà cung cấp '{existingByPhone.SupplierName}' (Mã: NCC{existingByPhone.SupplierId:D4})."
                });
            }
        }

        // 3. Kiểm tra trùng email với nhà cung cấp khác
        if (!string.IsNullOrWhiteSpace(dto.Mail))
        {
            string cleanMail = dto.Mail.Trim().ToLower();
            var existingByMail = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierId != id && s.Mail != null && s.Mail.Trim().ToLower() == cleanMail);
            if (existingByMail != null)
            {
                return BadRequest(new
                {
                    message = $"Email '{dto.Mail.Trim()}' đã được sử dụng bởi nhà cung cấp '{existingByMail.SupplierName}' (Mã: NCC{existingByMail.SupplierId:D4})."
                });
            }
        }

        // 4. Ràng buộc trường phí nhập: từ 0 đến 100% và tối đa 1 chữ số thập phân
        if (dto.ImportCost.HasValue)
        {
            if (dto.ImportCost.Value < 0 || dto.ImportCost.Value > 100)
            {
                return BadRequest(new { message = "Tỷ lệ chi phí nhập phải nằm trong khoảng từ 0% đến 100%." });
            }

            decimal rounded = Math.Round(dto.ImportCost.Value, 1, MidpointRounding.AwayFromZero);
            if (dto.ImportCost.Value != rounded)
            {
                return BadRequest(new { message = "Tỷ lệ chi phí nhập chỉ được có tối đa 1 chữ số thập phân (Ví dụ: 8.5%)." });
            }
        }

        supplier.SupplierName = cleanName;
        supplier.Phone = dto.Phone?.Trim() ?? "";
        supplier.Address = dto.Address?.Trim() ?? "";
        supplier.Mail = dto.Mail?.Trim() ?? "";
        if (dto.ImportCost.HasValue && dto.ImportCost.Value >= 0)
        {
            supplier.ImportCost = Math.Round(dto.ImportCost.Value, 1, MidpointRounding.AwayFromZero);
        }

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
                mail = supplier.Mail ?? "",
                importCost = supplier.ImportCost ?? 0,
                importCostFormatted = $"{supplier.ImportCost ?? 0:N1}%"
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
