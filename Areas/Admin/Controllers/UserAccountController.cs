using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using httt.Areas.Admin.Services;
using httt.Areas.Admin.ViewModels;

namespace httt.Areas.Admin.Controllers;

/// <summary>
/// Quản lý tài khoản (Admin). Controller chỉ làm 3 việc: kiểm tra ModelState, gọi Service, định dạng phản hồi.
/// Toàn bộ nghiệp vụ (Admin cuối cùng, hash mật khẩu, transaction) nằm ở <see cref="UserAccountService"/>.
///
/// Hợp đồng JSON cho mọi endpoint ghi:
///   thành công: 200 { success: true,  message, value?, affected }
///   thất bại  : 4xx { success: false, message, errors?: { fieldCamelCase: "thông báo" } }
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserAccountController : Controller
{
    private readonly UserAccountService _accountService;
    private readonly ILogger<UserAccountController> _logger;

    public UserAccountController(UserAccountService accountService, ILogger<UserAccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    // ══════════════════════════════════════════════════════════════════════
    //  GET: Index  (trang đầy đủ, hoặc partial bảng khi gọi bằng AJAX)
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1)
    {
        // Cùng một URL trả về 2 dạng (đầy đủ/partial) → bắt buộc Vary + no-store để nút Back
        // của trình duyệt không bao giờ hiển thị nhầm mảnh HTML partial.
        Response.Headers.Vary = "X-Requested-With";
        Response.Headers.CacheControl = "no-store";

        try
        {
            var model = await _accountService.GetPagedAsync(
                new UserListQuery(search, role, status, page), CurrentUserId, HttpContext.RequestAborted);

            return IsAjax ? PartialView("_UserTable", model) : View(model);
        }
        catch (OperationCanceledException)
        {
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying UserAccounts in Admin area.");

            if (IsAjax)
                return StatusCode(500, new { success = false, message = "Không thể tải danh sách tài khoản." });

            TempData["ErrorMessage"] = "Không thể tải danh sách tài khoản từ cơ sở dữ liệu.";
            return View(new UserListViewModel());
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  POST: các thao tác ghi (AJAX/JSON)
    // ══════════════════════════════════════════════════════════════════════

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateUserRequest? request) =>
        ExecuteAsync(request, "create user", async r =>
        {
            var result = await _accountService.CreateAsync(r);
            if (result.Success) _logger.LogInformation("Admin {Actor} created account {Username}.", CurrentUserId, r.Username);
            return result;
        });

    [HttpPost]
    public Task<IActionResult> Update([FromBody] UpdateUserRequest? request) =>
        ExecuteAsync(request, "update user", async r =>
        {
            var result = await _accountService.UpdateAsync(r, CurrentUserId);
            if (result.Success) _logger.LogInformation("Admin {Actor} updated account {UserId} (role={Role}, status={Status}).", CurrentUserId, r.Id, r.Role, r.Status);
            return result;
        });

    [HttpPost]
    public Task<IActionResult> SetStatus([FromBody] SetStatusRequest? request) =>
        ExecuteAsync(request, "set status", async r =>
        {
            var result = await _accountService.SetStatusAsync(r.Id, r.Status, CurrentUserId);
            if (result.Success) _logger.LogInformation("Admin {Actor} set account {UserId} status to {Status}.", CurrentUserId, r.Id, result.Value);
            return result;
        });

    [HttpPost]
    public Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest? request) =>
        ExecuteAsync(request, "reset password", async r =>
        {
            var result = await _accountService.ResetPasswordAsync(r.Id, r.NewPassword);
            if (result.Success) _logger.LogInformation("Admin {Actor} reset password of account {UserId}.", CurrentUserId, r.Id);
            return result;
        });

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return BadRequest(new { success = false, message = "Mã tài khoản không hợp lệ." });

        try
        {
            var result = await _accountService.DeleteAsync(id, CurrentUserId);
            if (result.Success) _logger.LogInformation("Admin {Actor} deleted account {UserId}.", CurrentUserId, id);
            return ToJson(result);
        }
        catch (Exception ex)
        {
            return ServerError(ex, "delete user", id);
        }
    }

    [HttpPost]
    public Task<IActionResult> BulkSetStatus([FromBody] BulkActionRequest? request) =>
        ExecuteAsync(request, "bulk set status", async r =>
        {
            var result = await _accountService.BulkSetStatusAsync(r.Ids, r.Status, CurrentUserId);
            if (result.Success) _logger.LogInformation("Admin {Actor} bulk set status {Status} on {Count} accounts.", CurrentUserId, r.Status, result.Affected);
            return result;
        });

    [HttpPost]
    public Task<IActionResult> BulkDelete([FromBody] BulkActionRequest? request) =>
        ExecuteAsync(request, "bulk delete", async r =>
        {
            var result = await _accountService.BulkDeleteAsync(r.Ids, CurrentUserId);
            if (result.Success) _logger.LogInformation("Admin {Actor} bulk deleted {Count} accounts.", CurrentUserId, result.Affected);
            return result;
        });

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Kiểm tra ModelState → chạy handler → map kết quả/ngoại lệ về JSON chuẩn.</summary>
    private async Task<IActionResult> ExecuteAsync<T>(T? request, string operation, Func<T, Task<ServiceResult>> handler)
        where T : class
    {
        if (request is null)
            return BadRequest(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });

        if (!ModelState.IsValid)
            return ValidationFailed();

        try
        {
            return ToJson(await handler(request));
        }
        catch (Exception ex)
        {
            return ServerError(ex, operation);
        }
    }

    /// <summary>Trả lỗi ModelState theo từng field (khóa camelCase) để client gắn lại đúng ô nhập.</summary>
    private IActionResult ValidationFailed()
    {
        var errors = ModelState
            .Where(kv => kv.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kv => ToCamelCase(kv.Key),
                kv => string.IsNullOrWhiteSpace(kv.Value!.Errors[0].ErrorMessage)
                    ? "Giá trị không hợp lệ."
                    : kv.Value.Errors[0].ErrorMessage);

        var message = errors.Count > 0 ? errors.Values.First() : "Dữ liệu gửi lên không hợp lệ.";
        return BadRequest(new { success = false, message, errors });
    }

    private IActionResult ToJson(ServiceResult result)
    {
        if (result.Success)
            return Json(new { success = true, message = result.Message, value = result.Value, affected = result.Affected });

        var status = result.Error switch
        {
            ServiceError.Validation => StatusCodes.Status400BadRequest,
            ServiceError.NotFound => StatusCodes.Status404NotFound,
            ServiceError.Forbidden => StatusCodes.Status403Forbidden,
            ServiceError.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        object? errors = result.Field is null
            ? null
            : new Dictionary<string, string> { [result.Field] = result.Message };

        return StatusCode(status, new { success = false, message = result.Message, errors });
    }

    // Không bao giờ trả ex.Message ra client (có thể lộ cấu trúc DB / chuỗi kết nối).
    private IActionResult ServerError(Exception ex, string operation, object? target = null)
    {
        _logger.LogError(ex, "Unhandled error during '{Operation}' (target: {Target}).", operation, target);
        return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại sau." });
    }

    private static string ToCamelCase(string key) =>
        string.IsNullOrEmpty(key) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}
