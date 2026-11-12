using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace httt.Areas.Admin.Services;

/// <summary>
/// NGUỒN SỰ THẬT DUY NHẤT cho các quy tắc validate tài khoản.
/// Được dùng bởi: DataAnnotations (ModelState phía server), Service (defense-in-depth)
/// và Razor (sinh thuộc tính pattern/minlength/maxlength + thông báo lỗi cho client).
/// </summary>
public static class UserAccountRules
{
    // ── Giới hạn ──────────────────────────────────────────────────────────
    public const int UsernameMin = 3;
    public const int UsernameMax = 50;
    public const int FullNameMin = 3;
    public const int FullNameMax = 100;
    public const int MinPasswordLength = 8;
    public const int PasswordMin = MinPasswordLength;
    /// <summary>BCrypt chỉ băm tối đa 72 byte đầu, vượt quá sẽ bị cắt âm thầm nên phải chặn.</summary>
    public const int PasswordMaxBytes = 72;
    public const int BulkMaxIds = 200;

    // ── Pattern (HTML pattern tự neo ^…$ nên dùng bản không neo cho client) ─
    public const string UsernamePattern = @"\S{3,50}";
    public const string PhonePattern = @"(84|0[35789])[0-9]{8}";
    public const string PasswordPattern = @".{8,72}";

    // Server dùng \z (không phải $) để không chấp nhận ký tự xuống dòng ở cuối chuỗi.
    public const string UsernameRegex = @"^\S{3,50}\z";
    public const string PhoneRegex = @"^(84|0[35789])[0-9]{8}\z";

    // ── Thông báo (dùng chung cho cả server lẫn client) ───────────────────
    public const string MsgUsername = "Tên đăng nhập gồm 3–50 ký tự và không được chứa khoảng trắng.";
    public const string MsgPassword = "Mật khẩu phải có từ 8 đến 72 ký tự và không được chỉ gồm khoảng trắng.";
    public const string MsgFullName = "Họ và tên phải có từ 3 đến 100 ký tự.";
    public const string MsgPhone = "Số điện thoại không hợp lệ (ví dụ: 0912345678).";
    public const string MsgRole = "Vui lòng chọn vai trò hợp lệ.";
    public const string MsgStatus = "Vui lòng chọn trạng thái hợp lệ.";
    public const string MsgRequired = "Trường này là bắt buộc.";

    // ── Danh mục hợp lệ ───────────────────────────────────────────────────
    public const string AdminRole = "Admin";
    public const string SalesRole = "Sales";
    public const string WarehouseRole = "Warehouse";
    public const string ProductionRole = "Production";

    public const string StatusActive = "Active";
    public const string StatusBlocked = "Blocked";
    public const string StatusDisabled = "Disabled";

    public static readonly string[] Roles =
    [
        "Admin", "Sales", "Warehouse", "Production"
    ];

    public static readonly string[] Statuses = [StatusActive, StatusBlocked, StatusDisabled];

    private static readonly Regex UsernameRx =
        new(UsernameRegex, RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    private static readonly Regex PhoneRx =
        new(PhoneRegex, RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    // ── Chuẩn hóa ─────────────────────────────────────────────────────────

    /// <summary>Trả về role đúng chữ hoa/thường chuẩn, hoặc null nếu không hợp lệ.</summary>
    public static string? NormalizeRole(string? role)
    {
        var value = role?.Trim();
        if (string.IsNullOrEmpty(value)) return null;

        var matched = Roles.FirstOrDefault(r => r.Equals(value, StringComparison.OrdinalIgnoreCase));
        if (matched != null) return matched;

        // Tự động ánh xạ các alias cũ về đúng 4 vai trò chuẩn
        if (value.Equals("Kho", StringComparison.OrdinalIgnoreCase)) return WarehouseRole;
        if (value.Equals("BanHang", StringComparison.OrdinalIgnoreCase) || value.Equals("Bán hàng", StringComparison.OrdinalIgnoreCase)) return SalesRole;
        if (value.Equals("Manager", StringComparison.OrdinalIgnoreCase) || value.Equals("Employee", StringComparison.OrdinalIgnoreCase) || value.Equals("SanXuat", StringComparison.OrdinalIgnoreCase)) return ProductionRole;

        return null;
    }

    /// <summary>Trả về status chuẩn (Active/Blocked/Disabled), hoặc null nếu không hợp lệ.</summary>
    public static string? NormalizeStatus(string? status)
    {
        var value = status?.Trim();
        return string.IsNullOrEmpty(value)
            ? null
            : Statuses.FirstOrDefault(s => s.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Quy mọi giá trị trong DB về 3 trạng thái chuẩn. Chỉ "Active" (đúng như Login yêu cầu) mới là Active;
    /// giá trị legacy (Inactive, true, 1, null...) được coi là Disabled.
    /// </summary>
    public static string StatusKey(string? rawStatus)
    {
        if (string.Equals(rawStatus?.Trim(), StatusActive, StringComparison.OrdinalIgnoreCase)) return StatusActive;
        if (string.Equals(rawStatus?.Trim(), StatusBlocked, StringComparison.OrdinalIgnoreCase)) return StatusBlocked;
        return StatusDisabled;
    }

    // ── Validate ──────────────────────────────────────────────────────────

    public static bool IsValidUsername(string? username) => username is not null && UsernameRx.IsMatch(username);

    public static bool IsValidFullName(string? fullName)
    {
        var len = fullName?.Trim().Length ?? 0;
        return len >= FullNameMin && len <= FullNameMax;
    }

    public static bool IsValidPhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) || PhoneRx.IsMatch(phone.Trim());

    /// <summary>
    /// Mật khẩu KHÔNG được Trim (nếu trim lúc lưu mà không trim lúc đăng nhập thì user sẽ không vào được).
    /// Trả về thông báo lỗi, hoặc null nếu hợp lệ.
    /// </summary>
    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return MsgPassword;
        if (password.Length < PasswordMin) return MsgPassword;
        if (Encoding.UTF8.GetByteCount(password) > PasswordMaxBytes) return MsgPassword;
        return null;
    }

    /// <summary>Escape ký tự đặc biệt của LIKE/ILIKE (dùng escape character là '\').</summary>
    public static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

// ── DataAnnotations dùng lại đúng các quy tắc ở trên ─────────────────────

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => UserAccountRules.ValidatePassword(value as string) is null;

    public override string FormatErrorMessage(string name) => UserAccountRules.MsgPassword;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AccountRoleAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => UserAccountRules.NormalizeRole(value as string) is not null;

    public override string FormatErrorMessage(string name) => UserAccountRules.MsgRole;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AccountStatusAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => UserAccountRules.NormalizeStatus(value as string) is not null;

    public override string FormatErrorMessage(string name) => UserAccountRules.MsgStatus;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AccountFullNameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => UserAccountRules.IsValidFullName(value as string);

    public override string FormatErrorMessage(string name) => UserAccountRules.MsgFullName;
}
