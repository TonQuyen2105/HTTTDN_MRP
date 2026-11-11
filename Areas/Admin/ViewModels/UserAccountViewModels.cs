using System.ComponentModel.DataAnnotations;
using httt.Areas.Admin.Services;

namespace httt.Areas.Admin.ViewModels;

// ═══════════════════════════════════════════════════════════════════════════
//  Các lớp dưới đây là toàn bộ dữ liệu đi qua ranh giới Server ⇄ Client.
//  TUYỆT ĐỐI không có thuộc tính Password/hash trong chiều Server → Client.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Một dòng trong bảng tài khoản (chỉ chứa dữ liệu hiển thị).</summary>
public sealed record UserListItem(
    int UserId,
    string Username,
    string FullName,
    string? Phone,
    string Role,
    /// <summary>Luôn là một trong: Active, Blocked, Disabled.</summary>
    string Status,
    /// <summary>Là chính tài khoản đang đăng nhập → không được tự khóa/xóa/đổi vai trò.</summary>
    bool IsSelf,
    /// <summary>Là Admin Active duy nhất còn lại → không được khóa/xóa/hạ quyền.</summary>
    bool IsLastActiveAdmin)
{
    public bool IsProtected => IsSelf || IsLastActiveAdmin;
}

public sealed record UserListQuery(string? Search, string? Role, string? Status, int Page = 1, int PageSize = 10);

public sealed class UserListViewModel
{
    public IReadOnlyList<UserListItem> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; } = 1;
    public string Search { get; init; } = string.Empty;
    /// <summary>Role đang lọc (đã chuẩn hóa), rỗng nếu "Tất cả".</summary>
    public string Role { get; init; } = string.Empty;
    /// <summary>Status đang lọc (đã chuẩn hóa), rỗng nếu "Tất cả".</summary>
    public string Status { get; init; } = string.Empty;

    public bool HasFilter => Search.Length > 0 || Role.Length > 0 || Status.Length > 0;
    public int FirstItemIndex => TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItemIndex => TotalItems == 0 ? 0 : FirstItemIndex + Items.Count - 1;
}

// ── Request (Client → Server). Tên thuộc tính = khóa lỗi trả về client (camelCase) ──

public sealed class CreateUserRequest
{
    [Required(ErrorMessage = UserAccountRules.MsgUsername)]
    [RegularExpression(UserAccountRules.UsernameRegex, ErrorMessage = UserAccountRules.MsgUsername)]
    public string? Username { get; set; }

    [StrongPassword]
    public string? Password { get; set; }

    [AccountFullName]
    public string? FullName { get; set; }

    [RegularExpression(UserAccountRules.PhoneRegex, ErrorMessage = UserAccountRules.MsgPhone)]
    public string? Phone { get; set; }

    [AccountRole]
    public string? Role { get; set; }

    [AccountStatus]
    public string? Status { get; set; } = UserAccountRules.StatusActive;
}

public sealed class UpdateUserRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã tài khoản không hợp lệ.")]
    public int Id { get; set; }

    [AccountFullName]
    public string? FullName { get; set; }

    [RegularExpression(UserAccountRules.PhoneRegex, ErrorMessage = UserAccountRules.MsgPhone)]
    public string? Phone { get; set; }

    [AccountRole]
    public string? Role { get; set; }

    [AccountStatus]
    public string? Status { get; set; }
}

public sealed class SetStatusRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã tài khoản không hợp lệ.")]
    public int Id { get; set; }

    [AccountStatus]
    public string? Status { get; set; }
}

public sealed class ResetPasswordRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã tài khoản không hợp lệ.")]
    public int Id { get; set; }

    [StrongPassword]
    public string? NewPassword { get; set; }
}

/// <summary>DTO cho thao tác hàng loạt (khóa/mở khóa/xóa).</summary>
public sealed class BulkActionRequest
{
    [Required(ErrorMessage = "Không có tài khoản nào được chọn.")]
    [MinLength(1, ErrorMessage = "Không có tài khoản nào được chọn.")]
    [MaxLength(UserAccountRules.BulkMaxIds, ErrorMessage = "Mỗi lần chỉ được thao tác tối đa 200 tài khoản.")]
    public List<int>? Ids { get; set; }

    /// <summary>Chỉ dùng cho BulkSetStatus: Active | Blocked | Disabled.</summary>
    public string? Status { get; set; }
}
