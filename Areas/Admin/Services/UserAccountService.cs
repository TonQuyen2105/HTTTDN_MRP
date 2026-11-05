using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Services;

/// <summary>
/// Service xử lý toàn bộ logic nghiệp vụ tài khoản trong Admin area.
/// Tách biệt khỏi Controller để dễ unit-test và tái sử dụng.
/// </summary>
public class UserAccountService
{
    private readonly ApplicationDbContext _context;

    // Danh sách role hợp lệ của hệ thống
    public static readonly string[] AllowedRoles =
    [
        "Admin", "Manager", "Employee", "Production", "Warehouse", "Sales", "Kho", "BanHang"
    ];

    public UserAccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Password Hashing ──────────────────────────────────────────────────

    /// <summary>Hash mật khẩu dạng plain-text bằng BCrypt (work factor 12).</summary>
    public static string HashPassword(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

    private static bool IsValidPassword(string password) => password.Length >= 8;

    /// <summary>
    /// Kiểm tra mật khẩu nhập vào khớp với hash trong DB.
    /// Hỗ trợ BCrypt, plain-text legacy, và Windows DPAPI-protected values.
    /// </summary>
    public static bool VerifyPassword(string plainPassword, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (storedHash.StartsWith("$2", StringComparison.Ordinal))
        {
            try { return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash); }
            catch { return false; }
        }

        if (plainPassword == storedHash)
            return true;

        if (IsDpApiProtectedValue(storedHash))
        {
            return TryDecryptDpApiPassword(storedHash, out var decrypted) &&
                   string.Equals(plainPassword, decrypted, StringComparison.Ordinal);
        }

        return false;
    }

    public static bool IsDpApiProtectedValue(string storedHash)
        => !string.IsNullOrWhiteSpace(storedHash)
           && storedHash.StartsWith("AQAAAA", StringComparison.OrdinalIgnoreCase);

    public static bool TryDecryptDpApiPassword(string storedHash, out string decryptedValue)
    {
        decryptedValue = string.Empty;

        if (!OperatingSystem.IsWindows() || !IsDpApiProtectedValue(storedHash))
            return false;

        try
        {
            var protectedBytes = Convert.FromBase64String(storedHash);
            var unprotected = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            decryptedValue = Encoding.UTF8.GetString(unprotected);
            return !string.IsNullOrEmpty(decryptedValue);
        }
        catch
        {
            return false;
        }
    }

    // ── CRUD ─────────────────────────────────────────────────────────────

    /// <summary>Tạo tài khoản mới, hash mật khẩu tự động.</summary>
    public async Task<(bool Success, string Message)> CreateAsync(
        string username, string plainPassword, string fullName, string? phone, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Tên đăng nhập không được để trống.");
        if (string.IsNullOrWhiteSpace(fullName))
            return (false, "Họ tên không được để trống.");
        if (string.IsNullOrWhiteSpace(plainPassword) || !IsValidPassword(plainPassword.Trim()))
            return (false, "Mật khẩu phải có ít nhất 8 ký tự.");
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        var exists = await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.Username == username.Trim());

        if (exists)
            return (false, "Tên đăng nhập đã tồn tại.");

        var user = new UserAccount
        {
            Username  = username.Trim(),
            Password  = HashPassword(plainPassword.Trim()),
            FullName  = fullName.Trim(),
            Phone     = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Role      = role.Trim(),
            Status    = "Active"
        };

        _context.UserAccounts.Add(user);
        await _context.SaveChangesAsync();
        return (true, "Tạo tài khoản thành công.");
    }

    /// <summary>Cập nhật thông tin + tuỳ chọn đặt lại mật khẩu cho một tài khoản.</summary>
    public async Task<(bool Success, string Message)> EditAsync(
        int userId, string fullName, string? phone, string role, string? newPlainPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (false, "Họ tên không được để trống.");
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return (false, "Không tìm thấy tài khoản.");

        user.FullName = fullName.Trim();
        user.Phone    = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.Role     = role.Trim();

        if (!string.IsNullOrWhiteSpace(newPlainPassword) && !IsValidPassword(newPlainPassword.Trim()))
            return (false, "Mật khẩu mới phải có ít nhất 8 ký tự.");
        if (!string.IsNullOrWhiteSpace(newPlainPassword))
            user.Password = HashPassword(newPlainPassword.Trim());

        await _context.SaveChangesAsync();
        return (true, "Cập nhật tài khoản thành công.");
    }

    /// <summary>
    /// Cập nhật tài khoản bằng username (dùng cho EditAccount action — tìm theo Username thay vì Id).
    /// </summary>
    public async Task<(bool Success, string Message)> EditByUsernameAsync(
        string username, string fullName, string? phone, string role, string? newPlainPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (false, "Họ tên không được để trống.");
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return (false, "Không tìm thấy tài khoản.");

        user.FullName = fullName.Trim();
        user.Phone    = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.Role     = role.Trim();

        if (!string.IsNullOrWhiteSpace(newPlainPassword) && !IsValidPassword(newPlainPassword.Trim()))
            return (false, "Mật khẩu mới phải có ít nhất 8 ký tự.");
        if (!string.IsNullOrWhiteSpace(newPlainPassword))
            user.Password = HashPassword(newPlainPassword.Trim());

        await _context.SaveChangesAsync();
        return (true, "Cập nhật tài khoản thành công.");
    }

    // ── Status Toggle (with Transaction) ─────────────────────────────────

    /// <summary>Chuyển đổi trạng thái Active ↔ Inactive của một tài khoản (có Transaction).</summary>
    public async Task<(bool Success, string NewStatus, string Message)> ToggleStatusAsync(int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return (false, string.Empty, "Không tìm thấy tài khoản.");

            user.Status = user.IsActive ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, user.Status, "Cập nhật trạng thái thành công.");
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, string.Empty, "Lỗi cơ sở dữ liệu khi cập nhật trạng thái.");
        }
    }

    /// <summary>Chỉ cập nhật Role của một tài khoản (có Transaction).</summary>
    public async Task<(bool Success, string Message)> UpdateRoleAsync(int userId, string role)
    {
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            user.Role = role.Trim();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Cập nhật vai trò thành công.");
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, "Lỗi cơ sở dữ liệu khi cập nhật vai trò.");
        }
    }

    // ── Delete ────────────────────────────────────────────────────────────

    /// <summary>Xóa tài khoản (có Transaction).</summary>
    public async Task<(bool Success, string Message)> DeleteAsync(int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            _context.UserAccounts.Remove(user);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Đã xóa tài khoản.");
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, "Lỗi cơ sở dữ liệu khi xóa tài khoản.");
        }
    }

    // ── Bulk Operations (with Transaction) ───────────────────────────────

    /// <summary>Khóa/Mở khóa hàng loạt tài khoản theo danh sách Id.</summary>
    public async Task<(bool Success, int AffectedRows, string Message)> BulkToggleStatusAsync(
        IEnumerable<int> userIds, string targetStatus)
    {
        if (targetStatus != "Active" && targetStatus != "Inactive")
            return (false, 0, "Trạng thái không hợp lệ.");

        try
        {
            var ids = userIds.Where(id => id > 0).Distinct().Take(500).ToArray();
            if (ids.Length == 0) return (false, 0, "Không có mã tài khoản hợp lệ.");

            var affected = await _context.UserAccounts.Where(u => ids.Contains(u.UserId))
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.Status, targetStatus));
            return (true, affected, $"Đã cập nhật {affected} tài khoản sang {targetStatus}.");
        }
        catch
        {
            return (false, 0, "Lỗi cơ sở dữ liệu khi cập nhật hàng loạt.");
        }
    }

    /// <summary>Xóa hàng loạt tài khoản theo danh sách Id.</summary>
    public async Task<(bool Success, int AffectedRows, string Message)> BulkDeleteAsync(
        IEnumerable<int> userIds)
    {
        try
        {
            var ids = userIds.Where(id => id > 0).Distinct().Take(500).ToArray();
            if (ids.Length == 0) return (false, 0, "Không có mã tài khoản hợp lệ.");

            var affected = await _context.UserAccounts.Where(u => ids.Contains(u.UserId)).ExecuteDeleteAsync();
            return (true, affected, $"Đã xóa {affected} tài khoản.");
        }
        catch
        {
            return (false, 0, "Lỗi cơ sở dữ liệu khi xóa hàng loạt.");
        }
    }

    // ── Password Upgrade on Login ─────────────────────────────────────────

    /// <summary>
    /// Nâng cấp plain-text password → BCrypt hash sau khi đăng nhập thành công.
    /// Chỉ chạy khi mật khẩu hiện tại chưa phải BCrypt.
    /// </summary>
    public async Task UpgradePasswordIfNeededAsync(UserAccount user, string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(user.Password))
            return;

        if (user.Password.StartsWith("$2", StringComparison.Ordinal))
            return;

        var isLegacyPlainText = plainPassword == user.Password;
        var isLegacyDpApi = IsDpApiProtectedValue(user.Password);

        if (!isLegacyPlainText && !isLegacyDpApi)
            return;

        string decryptedPlainText = string.Empty;
        if (isLegacyDpApi && !TryDecryptDpApiPassword(user.Password, out decryptedPlainText))
            return;

        var passwordToStore = isLegacyDpApi ? decryptedPlainText : plainPassword;
        // The caller reads with AsNoTracking. Load the tracked row before saving,
        // otherwise SaveChangesAsync would silently persist nothing.
        try
        {
            var trackedUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (trackedUser is null || trackedUser.Password != user.Password)
                return;

            trackedUser.Password = HashPassword(passwordToStore);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Không làm gián đoạn quá trình đăng nhập nếu việc nâng cấp hash gặp sự cố
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPlainPassword)
    {
        if (string.IsNullOrWhiteSpace(newPlainPassword) || !IsValidPassword(newPlainPassword.Trim()))
            return (false, "Mật khẩu mới không được để trống.");

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return (false, "Không tìm thấy tài khoản.");

        user.Password = HashPassword(newPlainPassword.Trim());
        await _context.SaveChangesAsync();
        return (true, "Đặt lại mật khẩu thành công.");
    }
}

