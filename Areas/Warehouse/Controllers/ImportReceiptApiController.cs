using System.Security.Claims;
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

    private int? GetCurrentLoggedInUserId()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawId, out var uid) && uid > 0 ? uid : null;
    }

    private static (string Code, string Name) ResolveEmployeeDisplay(UserAccount? user, UserAccount? fallbackUser = null)
    {
        var target = user ?? fallbackUser;
        if (target == null) return ("", "");
        var name = !string.IsNullOrWhiteSpace(target.FullName) ? target.FullName : target.Username;
        return ($"NV{target.UserId:D4}", name);
    }

    /// <summary>
    /// GET /api/warehouse/import-receipts : Lấy danh sách phiếu nhập mua hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetImportReceipts([FromQuery] string? keyword = null)
    {
        // Tự động chuẩn hóa các bản ghi cũ sang Approve / Reject (không lưu tiếng Việt vào DB)
        var nonStandard = await _context.ImportReceipts
            .Where(r => r.Status != "Approve" && r.Status != "Reject")
            .ToListAsync();
        if (nonStandard.Any())
        {
            foreach (var r in nonStandard)
            {
                r.Status = (r.Status == "Từ chối" || r.Status == "Rejected" || r.Status == "Reject") ? "Reject" : "Approve";
            }
            await _context.SaveChangesAsync();
        }

        var receipts = await _context.ImportReceipts.AsNoTracking().OrderByDescending(r => r.CreatedDate).ToListAsync();
        var suppliers = await _context.Suppliers.AsNoTracking().ToDictionaryAsync(s => s.SupplierId, s => s.SupplierName);
        var users = await _context.UserAccounts.AsNoTracking().ToDictionaryAsync(u => u.UserId);
        var currentUserId = GetCurrentLoggedInUserId();
        UserAccount? loggedInUser = (currentUserId.HasValue && users.TryGetValue(currentUserId.Value, out var curU)) ? curU : null;

        var list = receipts.Select(r =>
        {
            suppliers.TryGetValue(r.SupplierId ?? 0, out var supName);
            string supCode = r.SupplierId.HasValue ? $"NCC{r.SupplierId.Value:D4}" : "NCC0001";
            string code = r.IpReceiptCode ?? $"PNK{r.IpReceiptId:D5}";
            string st = (r.Status == "Reject") ? "Reject" : "Approve";
            string stType = (st == "Reject") ? "danger" : "success";
            UserAccount? creator = (r.CreatedBy.HasValue && users.TryGetValue(r.CreatedBy.Value, out var crU)) ? crU : null;
            var (empCode, empName) = ResolveEmployeeDisplay(creator, loggedInUser);

            return new
            {
                id = r.IpReceiptId,
                ipReceiptId = r.IpReceiptId,
                voucherNo = code,
                orderNo = code,
                voucherDate = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                supplierId = r.SupplierId,
                supplierName = supName ?? "N/A",
                supplierCode = supCode,
                warehouse = "KHO HƯNG YÊN - HÀNG TƯƠNG LAI",
                status = st,
                orderStatus = st,
                statusType = stType,
                createdBy = creator?.UserId ?? r.CreatedBy ?? loggedInUser?.UserId,
                creatorName = empName,
                employeeName = empName,
                employeeCode = empCode
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            list = list.Where(r =>
                r.voucherNo.ToLower().Contains(kw) ||
                r.supplierName.ToLower().Contains(kw) ||
                r.supplierCode.ToLower().Contains(kw) ||
                r.warehouse.ToLower().Contains(kw) ||
                r.status.ToLower().Contains(kw) ||
                r.creatorName.ToLower().Contains(kw) ||
                r.employeeCode.ToLower().Contains(kw)
            ).ToList();
        }

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

        var items = rawDetails.Select((d, idx) =>
        {
            ingrs.TryGetValue(d.IngrId, out var ingr);
            var price = d.Cost ?? ingr?.Price ?? 0;
            string expStr = d.ExpiredDate.HasValue
                ? d.ExpiredDate.Value.ToString("dd/MM/yyyy")
                : ((ingr != null && ingr.ShelfLifeMonths.HasValue)
                    ? (receipt.CreatedDate ?? DateTime.UtcNow).AddMonths(ingr.ShelfLifeMonths.Value).ToString("dd/MM/yyyy")
                    : "31/12/2026");

            return new
            {
                id = idx + 1,
                ingrId = d.IngrId,
                itemCode = $"NVL{d.IngrId:D3}",
                itemName = ingr?.IngrName ?? "N/A",
                unit = ingr?.Unit ?? "kg",
                quantity = d.Quantity,
                reqQty = d.Quantity,
                actualQty = d.Quantity,
                price = price,
                unitCost = price,
                amount = d.Quantity * price,
                stockQty = ingr?.Quantity ?? 0,
                expiryDate = expStr
            };
        }).ToList();

        string st = (receipt.Status == "Reject") ? "Reject" : "Approve";
        string stType = (st == "Reject") ? "danger" : "success";

        UserAccount? creator = receipt.CreatedBy.HasValue
            ? await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == receipt.CreatedBy.Value)
            : null;
        UserAccount? loggedInUser = null;
        if (creator == null)
        {
            var currentUserId = GetCurrentLoggedInUserId();
            if (currentUserId.HasValue)
            {
                loggedInUser = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == currentUserId.Value);
            }
        }
        var (empCode, empName) = ResolveEmployeeDisplay(creator, loggedInUser);

        return Ok(new
        {
            id = receipt.IpReceiptId,
            ipReceiptId = receipt.IpReceiptId,
            voucherNo = receipt.IpReceiptCode ?? $"PNK{receipt.IpReceiptId:D5}",
            orderNo = receipt.IpReceiptCode ?? $"PNK{receipt.IpReceiptId:D5}",
            voucherType = "inbound",
            docType = "inbound",
            voucherDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            supplierId = receipt.SupplierId,
            supplierCode = supplier != null ? $"NCC{supplier.SupplierId:D4}" : "NCC0001",
            supplierName = supplier?.SupplierName ?? "N/A",
            warehouse = "KHO HƯNG YÊN - HÀNG TƯƠNG LAI",
            status = st,
            orderStatus = st,
            statusType = stType,
            createdBy = creator?.UserId ?? loggedInUser?.UserId ?? receipt.CreatedBy,
            employeeCode = empCode,
            employeeName = empName,
            creatorName = empName,
            totalQuantity = items.Sum(i => i.quantity),
            totalAmount = items.Sum(i => i.amount),
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

        // Kiểm tra trùng lặp nguyên vật liệu trong cùng một phiếu nhập kho
        var duplicateGroup = dto.Items.GroupBy(i => i.IngrId).FirstOrDefault(g => g.Count() > 1);
        if (duplicateGroup != null)
        {
            var dupIngr = await _context.Ingredients.AsNoTracking().FirstOrDefaultAsync(i => i.IngrId == duplicateGroup.Key);
            var dupLabel = dupIngr != null ? $"{dupIngr.IngrName} (NVL{dupIngr.IngrId:D3})" : $"NVL{duplicateGroup.Key:D3}";
            return BadRequest(new
            {
                success = false,
                message = $"Nguyên vật liệu '{dupLabel}' đã được thêm trước đó trong phiếu nhập. Không được thêm nguyên vật liệu trùng lặp."
            });
        }

        string finalStatus = (dto.Status == "Reject" || dto.IsReject) ? "Reject" : "Approve";

        int? currentUserId = GetCurrentLoggedInUserId();
        UserAccount? creatorUser = currentUserId.HasValue
            ? await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == currentUserId.Value)
            : null;
        if (creatorUser == null && dto.CreatedBy.HasValue && dto.CreatedBy.Value > 0)
        {
            creatorUser = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == dto.CreatedBy.Value);
        }
        if (creatorUser == null)
        {
            creatorUser = await _context.UserAccounts.AsNoTracking()
                .Where(u => u.Role == "Warehouse" || u.Role == "Kho")
                .OrderBy(u => u.UserId)
                .FirstOrDefaultAsync()
                ?? await _context.UserAccounts.AsNoTracking().OrderBy(u => u.UserId).FirstOrDefaultAsync();
        }
        int? createdBy = creatorUser?.UserId;
        var (createdEmpCode, createdEmpName) = ResolveEmployeeDisplay(creatorUser);

        var receipt = new ImportReceipt
        {
            SupplierId = dto.SupplierId,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IpReceiptCode = dto.IpReceiptCode,
            Status = finalStatus
        };

        _context.ImportReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(receipt.IpReceiptCode))
        {
            receipt.IpReceiptCode = $"PNK{receipt.IpReceiptId:D5}";
        }

        var sup = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == dto.SupplierId);
        decimal importCostPct = sup?.ImportCost ?? 0;

        var now = DateTime.UtcNow;
        foreach (var it in dto.Items)
        {
            // Giới hạn tối đa 6 chữ số cho số lượng nhập (tối đa 999,999)
            decimal validQty = Math.Min(Math.Max(it.Quantity, 1), 999999m);

            var ingr = await _context.Ingredients.FindAsync(it.IngrId);

            DateTime? expDate = null;
            if (finalStatus == "Approve" && ingr != null)
            {
                ingr.Quantity = (ingr.Quantity ?? 0) + validQty;
                int months = (ingr.ShelfLifeMonths.HasValue && ingr.ShelfLifeMonths.Value > 0) ? ingr.ShelfLifeMonths.Value : 12;
                expDate = now.AddMonths(months);
            }

            // Đơn giá: nếu client không truyền giá hoặc = 0, tính theo công thức:
            // Đơn giá nguyên liệu + (đơn giá nguyên liệu * import_cost của NCC)
            decimal itemCost = (it.Cost.HasValue && it.Cost.Value > 0)
                ? it.Cost.Value
                : ((ingr?.Price ?? 0) + ((ingr?.Price ?? 0) * importCostPct / 100m));

            var detail = new ImportReceiptDetail
            {
                IpReceiptId = receipt.IpReceiptId,
                IngrId = it.IngrId,
                Quantity = validQty,
                Cost = itemCost,
                ExpiredDate = expDate,
                CreatedDate = now
            };
            _context.ImportReceiptDetails.Add(detail);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetImportReceiptById), new { id = receipt.IpReceiptId }, new
        {
            success = true,
            message = finalStatus == "Approve" 
                ? "Lập phiếu nhập kho thành công. Đã cập nhật tồn kho và hạn sử dụng (Trạng thái: Approve)."
                : "Đã lưu phiếu vào CSDL với trạng thái Reject.",
            status = finalStatus,
            ipReceiptId = receipt.IpReceiptId,
            voucherNo = receipt.IpReceiptCode,
            createdBy = receipt.CreatedBy,
            employeeCode = createdEmpCode,
            employeeName = createdEmpName,
            creatorName = createdEmpName
        });
    }

    /// <summary>
    /// PUT /api/warehouse/import-receipts/{id}/approve : Duyệt nhập kho (Cộng tồn kho NVL & cập nhật hạn dùng)
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveImportReceipt(int id)
    {
        var receipt = await _context.ImportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu nhập có ID {id}." });
        }

        if (receipt.Status == "Approve")
        {
            return BadRequest(new { message = "Phiếu nhập này đã ở trạng thái Approve." });
        }

        var details = await _context.ImportReceiptDetails.Where(d => d.IpReceiptId == id).ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var d in details)
        {
            var ingr = await _context.Ingredients.FindAsync(d.IngrId);
            if (ingr != null)
            {
                ingr.Quantity = (ingr.Quantity ?? 0) + d.Quantity;
                int months = (ingr.ShelfLifeMonths.HasValue && ingr.ShelfLifeMonths.Value > 0) ? ingr.ShelfLifeMonths.Value : 12;
                d.ExpiredDate = now.AddMonths(months);
            }
        }

        string code = receipt.IpReceiptCode ?? $"PNK{receipt.IpReceiptId:D5}";
        receipt.Status = "Approve";
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            status = "Approve",
            message = $"Nhập kho phiếu {code} thành công. Đã cộng tồn kho và cập nhật hạn sử dụng (Trạng thái: Approve)."
        });
    }

    /// <summary>
    /// PUT /api/warehouse/import-receipts/{id}/reject : Từ chối phiếu nhập kho NVL (Status = Reject)
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectImportReceipt(int id, [FromBody] RejectDto? dto)
    {
        var receipt = await _context.ImportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy phiếu nhập có ID {id}." });
        }

        if (receipt.Status == "Reject")
        {
            return BadRequest(new { message = "Phiếu nhập này đã ở trạng thái Reject." });
        }

        string code = receipt.IpReceiptCode ?? $"PNK{receipt.IpReceiptId:D5}";
        receipt.Status = "Reject";
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            status = "Reject",
            message = $"Đã từ chối nhập kho cho phiếu {code} (Trạng thái: Reject)."
        });
    }
}

