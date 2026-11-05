using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using httt.Areas.Admin.Services;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserAccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserAccountService _accountService;
    private readonly ILogger<UserAccountController> _logger;
    private readonly IMemoryCache _cache;

    public UserAccountController(
        ApplicationDbContext context,
        UserAccountService accountService,
        ILogger<UserAccountController> logger,
        IMemoryCache cache)
    {
        _context        = context;
        _accountService = accountService;
        _logger         = logger;
        _cache          = cache;
    }

    // ── GET: /Admin/UserAccount/Index ─────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(string? searchString, string? roleFilter, string? statusFilter)
    {
        var normalizedSearch = searchString?.Trim();
        var normalizedRole = roleFilter?.Trim();
        var normalizedStatus = statusFilter?.Trim();
        var cacheKey = $"admin:user-list:{normalizedSearch ?? ""}|{normalizedRole ?? ""}|{normalizedStatus ?? ""}";

        if (_cache.TryGetValue(cacheKey, out List<UserAccount>? cachedUsers))
        {
            return View(cachedUsers ?? new List<UserAccount>());
        }

        var query = _context.UserAccounts.AsNoTracking().AsQueryable();

        try
        {
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var kw = normalizedSearch.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(kw) || u.FullName.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(normalizedRole) && !normalizedRole.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.Role == normalizedRole);
            }

            if (!string.IsNullOrWhiteSpace(normalizedStatus) && !normalizedStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                bool isActive = normalizedStatus.Equals("Active", StringComparison.OrdinalIgnoreCase);
                query = query.Where(u =>
                    (isActive  && (u.Status == "Active" || u.Status == "true"  || u.Status == "1")) ||
                    (!isActive && (u.Status == "Inactive" || u.Status == "false" || u.Status == "0")));
            }

            var users = await query.OrderBy(u => u.UserId).ToListAsync();
            _cache.Set(cacheKey, users, TimeSpan.FromMinutes(2));
            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying UserAccounts in Admin area.");
            TempData["ErrorMessage"] = "Không thể tải danh sách tài khoản từ cơ sở dữ liệu.";
            return View(new List<UserAccount>());
        }
    }

    // ── POST: Create ──────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string username, string password, string fullname, string? phone, string role)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (string.IsNullOrWhiteSpace(fullname))
        {
            if (isAjax) return BadRequest(new { success = false, message = "Họ tên không được để trống." });
            TempData["ErrorMessage"] = "Họ tên không được để trống.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var (success, message) = await _accountService.CreateAsync(
                username, password, fullname, phone, role);

            if (!success)
            {
                if (isAjax) return BadRequest(new { success = false, message });
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            _cache.Remove("admin:user-list:");
            if (isAjax) return Json(new { success = true, message });
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", username);
            var errMsg = "Lỗi hệ thống khi tạo tài khoản. Vui lòng thử lại.";
            if (isAjax) return StatusCode(500, new { success = false, message = errMsg });
            TempData["ErrorMessage"] = errMsg;
            return RedirectToAction(nameof(Index));
        }
    }

    // ── POST: Edit (by UserId) ────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string fullname, string? phone, string role, string? newpassword)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (string.IsNullOrWhiteSpace(fullname))
        {
            if (isAjax) return BadRequest(new { success = false, message = "Họ tên không được để trống." });
            TempData["ErrorMessage"] = "Họ tên không được để trống.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var (success, message) = await _accountService.EditAsync(id, fullname, phone, role, newpassword);
            if (!success)
            {
                if (isAjax) return BadRequest(new { success = false, message });
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            _cache.Remove("admin:user-list:");
            if (isAjax) return Json(new { success = true, message });
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing user ID: {UserId}", id);
            var errMsg = "Lỗi hệ thống khi cập nhật tài khoản.";
            if (isAjax) return StatusCode(500, new { success = false, message = errMsg });
            TempData["ErrorMessage"] = errMsg;
            return RedirectToAction(nameof(Index));
        }
    }

    // ── POST: EditAccount (by Username — dùng từ modal AJAX) ─────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccount(
        string? username, string? fullname, string? phone, string? role, string? newpassword)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { success = false, message = "Username là bắt buộc." });
        if (string.IsNullOrWhiteSpace(fullname))
            return BadRequest(new { success = false, message = "Họ tên là bắt buộc." });
        if (string.IsNullOrWhiteSpace(role))
            return BadRequest(new { success = false, message = "Vai trò là bắt buộc." });

        try
        {
            var (success, message) = await _accountService.EditByUsernameAsync(
                username, fullname!, phone, role!, newpassword);

            if (success)
            {
                _cache.Remove("admin:user-list:");
            }

            return Json(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user via EditAccount: {Username}", username);
            return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật tài khoản." });
        }
    }

    // ── POST: ToggleStatus ────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (id <= 0)
            return BadRequest(new { success = false, message = "Mã tài khoản không hợp lệ." });

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id.ToString())
            return BadRequest(new { success = false, message = "Bạn không thể tự khóa tài khoản Admin đang đăng nhập." });

        try
        {
            var (success, newStatus, message) = await _accountService.ToggleStatusAsync(id);
            if (success)
            {
                _cache.Remove("admin:user-list:");
            }
            return Json(new { success, status = newStatus, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling status for user ID: {UserId}", id);
            return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật trạng thái." });
        }
    }

    // ── POST: UpdateRole ──────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(int id, string role)
    {
        try
        {
            var (success, message) = await _accountService.UpdateRoleAsync(id, role);
            if (success)
            {
                _cache.Remove("admin:user-list:");
            }
            return Json(new { success, role, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for user ID: {UserId}", id);
            return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật vai trò." });
        }
    }

    // ── POST: Delete ──────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return BadRequest(new { success = false, message = "Mã tài khoản không hợp lệ." });

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id.ToString())
            return BadRequest(new { success = false, message = "Bạn không thể tự xóa tài khoản Admin đang đăng nhập." });

        try
        {
            var (success, message) = await _accountService.DeleteAsync(id);
            if (success)
            {
                _cache.Remove("admin:user-list:");
            }
            return Json(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user ID: {UserId}", id);
            return Json(new { success = false, message = "Lỗi hệ thống khi xóa tài khoản." });
        }
    }

    // ── POST: BulkToggleStatus ────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkToggleStatus([FromBody] BulkActionRequest request)
    {
        if (request?.Ids == null || !request.Ids.Any())
            return BadRequest(new { success = false, message = "Không có tài khoản nào được chọn." });

        if (int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var currentUserId))
            request.Ids.RemoveAll(id => id == currentUserId);

        if (!request.Ids.Any())
            return BadRequest(new { success = false, message = "Không thể tự khóa tài khoản Admin đang đăng nhập." });

        var targetStatus = request.Action?.Equals("activate", StringComparison.OrdinalIgnoreCase) == true
            ? "Active" : "Inactive";

        try
        {
            var (success, affected, message) = await _accountService.BulkToggleStatusAsync(request.Ids, targetStatus);
            if (success)
            {
                _cache.Remove("admin:user-list:");
            }
            return Json(new { success, affected, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk toggling status.");
            return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật hàng loạt." });
        }
    }

    // ── POST: BulkDelete ──────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete([FromBody] BulkActionRequest request)
    {
        if (request?.Ids == null || !request.Ids.Any())
            return BadRequest(new { success = false, message = "Không có tài khoản nào được chọn." });

        if (int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var currentUserId))
            request.Ids.RemoveAll(id => id == currentUserId);

        if (!request.Ids.Any())
            return BadRequest(new { success = false, message = "Không thể tự xóa tài khoản Admin đang đăng nhập." });

        try
        {
            var (success, affected, message) = await _accountService.BulkDeleteAsync(request.Ids);
            if (success)
            {
                _cache.Remove("admin:user-list:");
            }
            return Json(new { success, affected, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting users.");
            return Json(new { success = false, message = "Lỗi hệ thống khi xóa hàng loạt." });
        }
    }
}

/// <summary>DTO cho Bulk Actions từ AJAX.</summary>
public class BulkActionRequest
{
    public List<int>? Ids { get; set; }
    public string? Action { get; set; } // "activate" | "deactivate"
}
