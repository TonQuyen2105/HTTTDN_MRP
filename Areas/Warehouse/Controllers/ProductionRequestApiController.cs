using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[IgnoreAntiforgeryToken]
[Route("api/warehouse/production-requests")]
[Route("api/production-requests")]
public class ProductionRequestApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductionRequestApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/production-requests : Lấy danh sách lệnh sản xuất
    /// Hỗ trợ tìm kiếm từ khóa, lọc theo status, assignedTo (người được phân công), productId
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProductionRequests(
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] int? assignedTo = null,
        [FromQuery] int? productId = null)
    {
        var query = _context.ProductionRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
        {
            query = query.Where(r => r.Status == status);
        }

        if (productId.HasValue && productId.Value > 0)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (assignedTo.HasValue)
        {
            // Nếu assignedTo == 0: lọc các lệnh chưa được phân công
            if (assignedTo.Value == 0)
            {
                query = query.Where(r => r.AssignedTo == null);
            }
            else
            {
                query = query.Where(r => r.AssignedTo == assignedTo.Value);
            }
        }

        var list = await query
            .OrderByDescending(r => r.CreatedDate)
            .ThenByDescending(r => r.RequestId)
            .ToListAsync();

        var prodIds = list.Select(r => r.ProductId).Distinct().ToList();
        var prods = await _context.Products.AsNoTracking()
            .Where(p => prodIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var managerIds = list.Select(r => r.ManagerId).Distinct().ToList();
        var assignedIds = list.Where(r => r.AssignedTo.HasValue).Select(r => r.AssignedTo!.Value).Distinct().ToList();
        var allUserIds = managerIds.Concat(assignedIds).Distinct().ToList();

        var users = await _context.UserAccounts.AsNoTracking()
            .Where(u => allUserIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        var result = list.Select(r =>
        {
            prods.TryGetValue(r.ProductId, out var prod);
            users.TryGetValue(r.ManagerId, out var mgr);

            UserAccount? assignedUser = null;
            if (r.AssignedTo.HasValue)
            {
                users.TryGetValue(r.AssignedTo.Value, out assignedUser);
            }

            string orderNo = !string.IsNullOrWhiteSpace(r.RequestCode) ? r.RequestCode : $"LSX{r.RequestId:D5}";

            return new
            {
                requestId = r.RequestId,
                requestCode = orderNo,
                orderNo,
                productId = r.ProductId,
                productCode = prod != null ? $"SP{prod.ProductId:D3}" : $"SP{r.ProductId:D3}",
                productName = prod?.ProductName ?? $"Sản phẩm #{r.ProductId}",
                unit = prod?.Unit ?? "Hộp",
                quantityRequired = r.QuantityRequired,
                status = r.Status ?? "Pending",
                createdDate = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                orderDate = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
                
                // Thông tin người tạo / quản lý
                managerId = r.ManagerId,
                managerName = mgr != null ? (!string.IsNullOrWhiteSpace(mgr.FullName) ? mgr.FullName : mgr.Username) : "Quản lý sản xuất",
                managerEmployeeCode = mgr != null ? $"NV{mgr.UserID:D4}" : $"NV{r.ManagerId:D4}",

                // Thông tin người được phân công (assignedto)
                assignedTo = r.AssignedTo,
                assignedToName = assignedUser != null ? (!string.IsNullOrWhiteSpace(assignedUser.FullName) ? assignedUser.FullName : assignedUser.Username) : null,
                assignedToEmployeeCode = assignedUser != null ? $"NV{assignedUser.UserID:D4}" : null,
                assignedToRole = assignedUser?.Role,
                assignedToPhone = assignedUser?.Phone,
                isAssigned = r.AssignedTo.HasValue
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            result = result.Where(r =>
                r.requestCode.ToLower().Contains(kw) ||
                r.productName.ToLower().Contains(kw) ||
                r.productCode.ToLower().Contains(kw) ||
                r.managerName.ToLower().Contains(kw) ||
                (r.assignedToName != null && r.assignedToName.ToLower().Contains(kw))
            ).ToList();
        }

        return Ok(result);
    }

    /// <summary>
    /// GET /api/production-requests/{id} : Lấy thông tin chi tiết một lệnh sản xuất
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductionRequestById(int id)
    {
        var r = await _context.ProductionRequests.FindAsync(id);
        if (r == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh sản xuất có ID {id}." });
        }

        var prod = await _context.Products.FindAsync(r.ProductId);
        var mgr = await _context.UserAccounts.FindAsync(r.ManagerId);

        UserAccount? assignedUser = null;
        if (r.AssignedTo.HasValue)
        {
            assignedUser = await _context.UserAccounts.FindAsync(r.AssignedTo.Value);
        }

        // Lấy định mức BOM của sản phẩm này (nếu có)
        var bomList = await (from b in _context.Boms.AsNoTracking()
                             where b.ProductId == r.ProductId
                             join ingr in _context.Ingredients.AsNoTracking() on b.IngrId equals ingr.IngrId
                             select new
                             {
                                 b.BomId,
                                 b.BomName,
                                 b.IngrId,
                                 ingrCode = $"NVL{ingr.IngrId:D3}",
                                 ingr.IngrName,
                                 unit = ingr.Unit ?? "kg",
                                 b.ItemQuantity,
                                 b.MaterialQuantity,
                                 requiredMaterial = b.MaterialQuantity * (r.QuantityRequired > 0 ? r.QuantityRequired : 1)
                             }).ToListAsync();

        // Lấy danh sách phiếu xuất NVL liên kết với lệnh này
        var materialReceipts = await _context.MaterialReceipts.AsNoTracking()
            .Where(m => m.RequestId == id)
            .Select(m => new
            {
                m.ExpReceiptId,
                expReceiptCode = m.ExpReceiptCode ?? $"PXK{m.ExpReceiptId:D5}",
                m.Status,
                createdDate = m.CreatedDate.HasValue ? m.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : ""
            }).ToListAsync();

        // Lấy danh sách phiếu nhập thành phẩm liên kết với lệnh này
        var productReceipts = await _context.ProductReceipts.AsNoTracking()
            .Where(p => p.RequestId == id)
            .Select(p => new
            {
                p.ProductReceiptId,
                receiptCode = p.ReceiptCode ?? $"PNK_TP{p.ProductReceiptId:D4}",
                p.Status,
                createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : ""
            }).ToListAsync();

        string orderNo = !string.IsNullOrWhiteSpace(r.RequestCode) ? r.RequestCode : $"LSX{r.RequestId:D5}";

        return Ok(new
        {
            requestId = r.RequestId,
            requestCode = orderNo,
            orderNo,
            productId = r.ProductId,
            productCode = prod != null ? $"SP{prod.ProductId:D3}" : $"SP{r.ProductId:D3}",
            productName = prod?.ProductName ?? $"Sản phẩm #{r.ProductId}",
            unit = prod?.Unit ?? "Hộp",
            quantityRequired = r.QuantityRequired,
            status = r.Status ?? "Pending",
            createdDate = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            orderDate = r.CreatedDate.HasValue ? r.CreatedDate.Value.ToString("dd/MM/yyyy") : "",

            // Quản lý tạo lệnh
            managerId = r.ManagerId,
            managerName = mgr != null ? (!string.IsNullOrWhiteSpace(mgr.FullName) ? mgr.FullName : mgr.Username) : "Quản lý sản xuất",
            managerEmployeeCode = mgr != null ? $"NV{mgr.UserID:D4}" : $"NV{r.ManagerId:D4}",

            // Người được phân công (assignedto)
            assignedTo = r.AssignedTo,
            assignedToName = assignedUser != null ? (!string.IsNullOrWhiteSpace(assignedUser.FullName) ? assignedUser.FullName : assignedUser.Username) : null,
            assignedToEmployeeCode = assignedUser != null ? $"NV{assignedUser.UserID:D4}" : null,
            assignedToRole = assignedUser?.Role,
            assignedToPhone = assignedUser?.Phone,
            isAssigned = r.AssignedTo.HasValue,

            // Dữ liệu mở rộng liên kết
            bom = bomList,
            materialReceipts,
            productReceipts
        });
    }

    /// <summary>
    /// POST /api/production-requests : Tạo mới lệnh sản xuất (hỗ trợ yêu cầu nhiều sản phẩm cùng lúc)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProductionRequest([FromBody] CreateProductionRequestDto dto)
    {
        var items = new List<CreateProductionRequestItemDto>();
        if (dto.Items != null && dto.Items.Count > 0)
        {
            items.AddRange(dto.Items.Where(it => it.ProductId > 0 && it.QuantityRequired > 0));
            if (items.Count == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất một sản phẩm với số lượng hợp lệ (> 0)." });
            }
        }
        else if (dto.ProductId.HasValue && dto.ProductId.Value > 0)
        {
            if (!dto.QuantityRequired.HasValue || dto.QuantityRequired.Value <= 0)
            {
                return BadRequest(new { message = "Số lượng sản xuất phải lớn hơn 0." });
            }
            items.Add(new CreateProductionRequestItemDto
            {
                ProductId = dto.ProductId.Value,
                QuantityRequired = dto.QuantityRequired.Value
            });
        }
        else
        {
            return BadRequest(new { message = "Vui lòng chọn sản phẩm cần sản xuất." });
        }

        // Lấy ManagerId mặc định nếu chưa truyền
        int managerId = dto.ManagerId ?? 5;
        var manager = await _context.UserAccounts.FindAsync(managerId);
        if (manager == null)
        {
            var firstUser = await _context.UserAccounts.FirstOrDefaultAsync();
            if (firstUser != null) managerId = firstUser.UserID;
        }

        var now = DateTime.UtcNow;
        string baseCode = !string.IsNullOrWhiteSpace(dto.RequestCode)
            ? dto.RequestCode.Trim()
            : $"LSX{now:yyyyMMddHHmmss}";

        var createdRequests = new List<ProductionRequest>();
        foreach (var it in items)
        {
            var product = await _context.Products.FindAsync(it.ProductId);
            if (product == null) continue;

            var newReq = new ProductionRequest
            {
                ProductId = it.ProductId,
                QuantityRequired = it.QuantityRequired,
                ManagerId = managerId,
                Status = "Pending", // Luôn luôn mặc định là Pending khi tạo mới
                CreatedDate = now,
                RequestCode = baseCode,
                AssignedTo = null // Không phân công trước, chờ nhân viên tiếp nhận
            };

            _context.ProductionRequests.Add(newReq);
            createdRequests.Add(newReq);
        }

        await _context.SaveChangesAsync();

        var firstCreated = createdRequests.FirstOrDefault();
        return Ok(new
        {
            success = true,
            message = $"Tạo yêu cầu sản xuất {baseCode} cho {createdRequests.Count} sản phẩm thành công.",
            requestCode = baseCode,
            count = createdRequests.Count,
            requestId = firstCreated?.RequestId ?? 0,
            requestIds = createdRequests.Select(r => r.RequestId).ToList()
        });
    }

    /// <summary>
    /// PUT /api/production-requests/{id} : Cập nhật thông tin lệnh sản xuất (chỉ cho phép khi status == 'Pending')
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProductionRequest(int id, [FromBody] UpdateProductionRequestDto dto)
    {
        var req = await _context.ProductionRequests.FindAsync(id);
        if (req == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh sản xuất có ID {id}." });
        }

        // Chỉ status Pending mới được chỉnh sửa và thay đổi phân công
        if (req.Status != "Pending")
        {
            return BadRequest(new { message = $"Lệnh sản xuất đang ở trạng thái '{req.Status}', các thông tin đã bị khóa không được chỉnh sửa cũng như thay đổi phân công. Chỉ lệnh ở trạng thái 'Pending' mới được chỉnh sửa." });
        }

        if (dto.ProductId.HasValue && dto.ProductId.Value > 0)
        {
            var prodExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId.Value);
            if (!prodExists) return BadRequest(new { message = $"Sản phẩm ID {dto.ProductId} không tồn tại." });
            req.ProductId = dto.ProductId.Value;
        }

        if (dto.QuantityRequired.HasValue && dto.QuantityRequired.Value > 0)
        {
            req.QuantityRequired = dto.QuantityRequired.Value;
        }

        // Trạng thái trong trang chỉnh sửa yêu cầu luôn luôn là pending
        req.Status = "Pending";

        // Cập nhật người được phân công (assignedTo)
        if (dto.AssignedTo.HasValue)
        {
            if (dto.AssignedTo.Value > 0)
            {
                var userExists = await _context.UserAccounts.AnyAsync(u => u.UserId == dto.AssignedTo.Value);
                if (!userExists) return BadRequest(new { message = $"Người dùng có ID {dto.AssignedTo.Value} không tồn tại." });
                req.AssignedTo = dto.AssignedTo.Value;
            }
            else
            {
                // Truyền 0 hoặc âm -> Hủy phân công
                req.AssignedTo = null;
            }
        }
        else if (dto.ClearAssignedTo == true)
        {
            req.AssignedTo = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật lệnh sản xuất thành công.",
            requestId = req.RequestId,
            requestCode = req.RequestCode,
            status = req.Status,
            quantityRequired = req.QuantityRequired,
            assignedTo = req.AssignedTo
        });
    }

    /// <summary>
    /// PATCH/PUT /api/production-requests/{id}/assign : Phân công người thực hiện cho lệnh sản xuất (chỉ khi status == 'Pending')
    /// </summary>
    [HttpPatch("{id:int}/assign")]
    [HttpPut("{id:int}/assign")]
    public async Task<IActionResult> AssignProductionRequest(int id, [FromBody] AssignProductionRequestDto dto)
    {
        var req = await _context.ProductionRequests.FindAsync(id);
        if (req == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh sản xuất có ID {id}." });
        }

        // Chỉ status Pending mới được thay đổi phân công
        if (req.Status != "Pending")
        {
            return BadRequest(new { message = $"Lệnh sản xuất đang ở trạng thái '{req.Status}', không thể thay đổi phân công. Chỉ lệnh ở trạng thái 'Pending' mới được thay đổi phân công." });
        }

        UserAccount? assignedUser = null;
        if (dto.AssignedTo.HasValue && dto.AssignedTo.Value > 0)
        {
            assignedUser = await _context.UserAccounts.FindAsync(dto.AssignedTo.Value);
            if (assignedUser == null)
            {
                return BadRequest(new { message = $"Người dùng có ID {dto.AssignedTo.Value} không tồn tại." });
            }

            req.AssignedTo = dto.AssignedTo.Value;
            // Lưu ý: Phân công xong trạng thái vẫn giữ nguyên là Pending!
        }
        else
        {
            // Bỏ phân công
            req.AssignedTo = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = req.AssignedTo.HasValue
                ? $"Đã phân công thực hiện lệnh sản xuất cho '{assignedUser?.FullName ?? assignedUser?.Username}'."
                : "Đã hủy phân công thực hiện cho lệnh sản xuất.",
            requestId = req.RequestId,
            requestCode = req.RequestCode,
            status = req.Status,
            assignedTo = req.AssignedTo,
            assignedToName = assignedUser != null ? (!string.IsNullOrWhiteSpace(assignedUser.FullName) ? assignedUser.FullName : assignedUser.Username) : null,
            assignedToEmployeeCode = assignedUser != null ? $"NV{assignedUser.UserID:D4}" : null,
            assignedToRole = assignedUser?.Role
        });
    }

    /// <summary>
    /// POST /api/warehouse/production-requests/{id}/receive : Nhân viên tiếp nhận yêu cầu sản xuất
    /// </summary>
    [HttpPost("{id:int}/receive")]
    [HttpPatch("{id:int}/receive")]
    public async Task<IActionResult> ReceiveProductionRequest(int id, [FromBody] ReceiveProductionRequestDto? dto = null)
    {
        var req = await _context.ProductionRequests.FindAsync(id);
        if (req == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh sản xuất có ID {id}." });
        }

        // Người tiếp nhận: lấy từ dto.ReceiverId, nếu không có lấy mặc định 9 (DS. Phan Thanh Hà) hoặc user đầu tiên
        int receiverId = dto?.ReceiverId ?? 9;
        var user = await _context.UserAccounts.FindAsync(receiverId);
        if (user == null)
        {
            user = await _context.UserAccounts.FirstOrDefaultAsync();
            if (user != null) receiverId = user.UserID;
        }

        req.AssignedTo = receiverId;
        req.Status = "In Production";

        // Đồng thời tiếp nhận các sản phẩm cùng chung mã lệnh RequestCode (nếu có)
        if (!string.IsNullOrWhiteSpace(req.RequestCode))
        {
            var related = await _context.ProductionRequests
                .Where(r => r.RequestCode == req.RequestCode && r.RequestId != id && r.Status == "Pending")
                .ToListAsync();
            foreach (var r in related)
            {
                r.AssignedTo = receiverId;
                r.Status = "In Production";
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Nhân viên {user?.FullName ?? user?.Username} đã tiếp nhận yêu cầu sản xuất '{req.RequestCode}' thành công!",
            requestId = req.RequestId,
            requestCode = req.RequestCode,
            status = req.Status,
            assignedTo = req.AssignedTo,
            assignedToName = user?.FullName ?? user?.Username,
            assignedToEmployeeCode = user != null ? $"NV{user.UserID:D4}" : null
        });
    }

    /// <summary>
    /// PATCH /api/production-requests/{id}/status : Nhân viên tiếp nhận yêu cầu hoặc cập nhật trạng thái tiến độ
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateProductionRequestStatus(int id, [FromBody] UpdateProductionRequestStatusDto dto)
    {
        var req = await _context.ProductionRequests.FindAsync(id);
        if (req == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh sản xuất có ID {id}." });
        }

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { message = "Vui lòng cung cấp trạng thái mới." });
        }

        if (dto.Status == "In Production")
        {
            if (dto.ReceiverId.HasValue && dto.ReceiverId.Value > 0)
            {
                req.AssignedTo = dto.ReceiverId.Value;
            }
            else if (!req.AssignedTo.HasValue)
            {
                req.AssignedTo = 9; // Mặc định người tiếp nhận
            }
        }

        req.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = req.Status == "In Production"
                ? $"Nhân viên đã tiếp nhận yêu cầu sản xuất '{req.RequestCode}'. Trạng thái đã chuyển sang 'In Production'."
                : $"Đã cập nhật trạng thái lệnh sản xuất '{req.RequestCode}' thành '{req.Status}'.",
            requestId = req.RequestId,
            requestCode = req.RequestCode,
            status = req.Status
        });
    }

    /// <summary>
    /// GET /api/production-requests/assignable-users : Lấy danh sách nhân viên để phân công thực hiện
    /// </summary>
    [HttpGet("assignable-users")]
    public async Task<IActionResult> GetAssignableUsers()
    {
        var users = await _context.UserAccounts.AsNoTracking()
            .Where(u => u.Status == "Active" || u.Status == null)
            .OrderBy(u => u.UserId)
            .Select(u => new
            {
                userId = u.UserId,
                employeeCode = $"NV{u.UserId:D4}",
                fullName = u.FullName,
                username = u.Username,
                role = u.Role ?? "Nhân viên",
                phone = u.Phone ?? ""
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// DELETE /api/production-requests/{id} : Xóa lệnh sản xuất (chỉ xóa khi chưa phát sinh phiếu xuất/nhập)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProductionRequest(int id)
    {
        var req = await _context.ProductionRequests.FindAsync(id);
        if (req == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh sản xuất có ID {id}." });
        }

        // Chỉ được xóa khi lệnh còn ở trạng thái Pending
        if (req.Status != "Pending")
        {
            return BadRequest(new { message = $"Lệnh sản xuất đang ở trạng thái '{req.Status}'. Chỉ có thể xóa lệnh khi còn ở trạng thái 'Pending'." });
        }

        var hasMat = await _context.MaterialReceipts.AnyAsync(m => m.RequestId == id);
        var hasProd = await _context.ProductReceipts.AnyAsync(p => p.RequestId == id);
        if (hasMat || hasProd)
        {
            return BadRequest(new { message = "Không thể xóa lệnh sản xuất đã phát sinh phiếu xuất kho hoặc phiếu nhập kho liên kết." });
        }

        _context.ProductionRequests.Remove(req);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa thành công lệnh sản xuất '{req.RequestCode ?? $"LSX{req.RequestId:D5}"}'." });
    }
}

public class UpdateProductionRequestStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int? ReceiverId { get; set; }
}

public class ReceiveProductionRequestDto
{
    public int? ReceiverId { get; set; }
}

public class CreateProductionRequestItemDto
{
    public int ProductId { get; set; }
    public int QuantityRequired { get; set; }
}

public class CreateProductionRequestDto
{
    public int? ProductId { get; set; }
    public int? QuantityRequired { get; set; }
    public int? ManagerId { get; set; }
    public string? RequestCode { get; set; }
    public string? Status { get; set; }
    public int? AssignedTo { get; set; }
    public List<CreateProductionRequestItemDto>? Items { get; set; }
}

public class UpdateProductionRequestDto
{
    public int? ProductId { get; set; }
    public int? QuantityRequired { get; set; }
    public string? Status { get; set; }
    public int? AssignedTo { get; set; }
    public bool? ClearAssignedTo { get; set; }
}

public class AssignProductionRequestDto
{
    public int? AssignedTo { get; set; }
}

