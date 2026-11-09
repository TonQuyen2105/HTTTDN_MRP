using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Services;

/// <summary>
/// DTO phục vụ thêm mới hàng loạt chi tiết/tài khoản
/// </summary>
public class UserAccountCreateDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "Employee";
}

/// <summary>
/// Service xử lý toàn bộ logic nghiệp vụ tài khoản trong Admin area.
/// Tách biệt khỏi Controller để dễ unit-test và tối ưu hóa hiệu năng mạng với Supabase.
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

    private static bool IsValidPassword(string password) => !string.IsNullOrWhiteSpace(password) && password.Trim().Length >= 12;

    /// <summary>
    /// Kiểm tra mật khẩu nhập vào khớp với hash trong DB.
    /// Sử dụng BCrypt.Net.BCrypt.Verify, đồng thời hỗ trợ dữ liệu legacy nếu có.
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

        // Fallback kiểm tra an toàn cho mật khẩu cũ để hệ thống tự động băm lại khi login
        if (plainPassword == storedHash)
            return true;

        if (IsDpApiProtectedValue(storedHash))
        {
            return TryDecryptDpApiPassword(storedHash, out var decrypted) &&
                   string.Equals(plainPassword, decrypted, StringComparison.Ordinal);
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra xem tài khoản này có phải là Admin đang Active cuối cùng của hệ thống hay không.
    /// Ngăn chặn việc vô hiệu hóa, giáng chức hoặc xóa Admin cuối cùng.
    /// </summary>
    public async Task<bool> IsLastActiveAdminAsync(int userId)
    {
        var targetUser = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (targetUser == null || targetUser.Role != "Admin" || !targetUser.IsActive)
            return false;

        var activeAdminCount = await _context.UserAccounts
            .AsNoTracking()
            .CountAsync(u => u.Role == "Admin" && (u.Status == "Active" || u.Status == "true" || u.Status == "1"));

        return activeAdminCount <= 1;
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

    // ── TIÊU CHÍ 1: READ-ONLY QUERIES VỚI .AsNoTracking() ─────────────────

    /// <summary>Lấy thông tin tài khoản theo Id chỉ để đọc/xem.</summary>
    public async Task<UserAccount?> GetByIdAsync(int userId)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>Lấy thông tin tài khoản theo Username chỉ để đọc/xem.</summary>
    public async Task<UserAccount?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim();
        return await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, normalized));
    }

    /// <summary>Lấy danh sách toàn bộ tài khoản chỉ để đọc.</summary>
    public async Task<List<UserAccount>> GetAllAsync()
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .OrderBy(u => u.UserId)
            .ToListAsync();
    }

    // ── TIÊU CHÍ 2: XỬ LÝ N+1 QUERY BẰNG .Include(x => x.UserAccount) ─────

    /// <summary>
    /// Lấy danh sách các yêu cầu sản xuất kèm thông tin người tạo (UserAccount) và sản phẩm (Product).
    /// Triệt tiêu hoàn toàn lỗi N+1 Query qua kết nối mạng Supabase.
    /// </summary>
    public async Task<List<ProductionRequest>> GetProductionRequestsWithUserAsync(int take = 50)
    {
        return await _context.ProductionRequests
            .AsNoTracking()
            .Include(r => r.AssignedUser)
            .Include(r => r.Product)
            .OrderByDescending(r => r.RequestId)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách đơn hàng kèm người duyệt (UserAccount).
    /// Triệt tiêu hoàn toàn lỗi N+1 Query.
    /// </summary>
    public async Task<List<SaleOrder>> GetSaleOrdersWithUserAsync(int take = 50)
    {
        return await _context.SaleOrders
            .AsNoTracking()
            .Include(o => o.UserAccount)
            .OrderByDescending(o => o.OrderId)
            .Take(take)
            .ToListAsync();
    }

    // ── CRUD CƠ BẢN ──────────────────────────────────────────────────────

    /// <summary>Tạo tài khoản mới, hash mật khẩu tự động.</summary>
    public async Task<(bool Success, string Message)> CreateAsync(
        string username, string plainPassword, string fullName, string? phone, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Tên đăng nhập không được để trống.");
        if (string.IsNullOrWhiteSpace(fullName))
            return (false, "Họ tên không được để trống.");
        if (string.IsNullOrWhiteSpace(plainPassword))
            return (false, "Mật khẩu không được để trống.");
        if (!IsValidPassword(plainPassword.Trim()))
            return (false, "Mật khẩu phải có ít nhất 12 ký tự.");
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        // Read-only kiểm tra tồn tại với .AsNoTracking()
        var exists = await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => EF.Functions.ILike(u.Username, username.Trim()));

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

        // Kiểm tra không được giáng chức Admin cuối cùng
        if (user.Role == "Admin" && !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (await IsLastActiveAdminAsync(userId))
            {
                return (false, "Không thể thay đổi vai trò của quản trị viên (Admin) duy nhất còn lại trong hệ thống.");
            }
        }

        user.FullName = fullName.Trim();
        user.Phone    = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.Role     = role.Trim();

        if (!string.IsNullOrWhiteSpace(newPlainPassword))
        {
            if (!IsValidPassword(newPlainPassword.Trim()))
                return (false, "Mật khẩu mới phải có ít nhất 12 ký tự.");
            user.Password = HashPassword(newPlainPassword.Trim());
        }

        await _context.SaveChangesAsync();
        return (true, "Cập nhật tài khoản thành công.");
    }

    /// <summary>Cập nhật tài khoản bằng username.</summary>
    public async Task<(bool Success, string Message)> EditByUsernameAsync(
        string username, string fullName, string? phone, string role, string? newPlainPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (false, "Họ tên không được để trống.");
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return (false, "Không tìm thấy tài khoản.");

        // Kiểm tra không được giáng chức Admin cuối cùng
        if (user.Role == "Admin" && !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (await IsLastActiveAdminAsync(user.UserId))
            {
                return (false, "Không thể thay đổi vai trò của quản trị viên (Admin) duy nhất còn lại trong hệ thống.");
            }
        }

        user.FullName = fullName.Trim();
        user.Phone    = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.Role     = role.Trim();

        if (!string.IsNullOrWhiteSpace(newPlainPassword))
        {
            if (!IsValidPassword(newPlainPassword.Trim()))
                return (false, "Mật khẩu mới phải có ít nhất 12 ký tự.");
            user.Password = HashPassword(newPlainPassword.Trim());
        }

        await _context.SaveChangesAsync();
        return (true, "Cập nhật tài khoản thành công.");
    }

    // ── STATUS TOGGLE & ROLE UPDATE ──────────────────────────────────────

    public async Task<(bool Success, string NewStatus, string Message)> ToggleStatusAsync(int userId)
    {
        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return (false, string.Empty, "Không tìm thấy tài khoản.");

        // Nếu đang Active và là Admin, kiểm tra có phải Admin Active cuối cùng không
        if (user.IsActive && user.Role == "Admin")
        {
            if (await IsLastActiveAdminAsync(userId))
            {
                return (false, string.Empty, "Không thể vô hiệu hóa quản trị viên (Admin) duy nhất còn lại trong hệ thống.");
            }
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trackedUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
                if (trackedUser == null) return (false, string.Empty, "Không tìm thấy tài khoản.");

                trackedUser.Status = trackedUser.IsActive ? "Inactive" : "Active";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, trackedUser.Status, "Cập nhật trạng thái thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, string.Empty, "Lỗi cơ sở dữ liệu khi cập nhật trạng thái.");
            }
        });
    }

    public async Task<(bool Success, string Message)> UpdateRoleAsync(int userId, string role)
    {
        if (!AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return (false, "Vai trò không hợp lệ.");

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return (false, "Không tìm thấy tài khoản.");

        // Không được giáng chức Admin cuối cùng
        if (user.Role == "Admin" && !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (await IsLastActiveAdminAsync(userId))
            {
                return (false, "Không thể thay đổi vai trò của quản trị viên (Admin) duy nhất còn lại trong hệ thống.");
            }
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trackedUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
                if (trackedUser == null) return (false, "Không tìm thấy tài khoản.");

                trackedUser.Role = role.Trim();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Cập nhật vai trò thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Lỗi cơ sở dữ liệu khi cập nhật vai trò.");
            }
        });
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int userId)
    {
        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return (false, "Không tìm thấy tài khoản.");

        if (user.Role == "Admin" && await IsLastActiveAdminAsync(userId))
        {
            return (false, "Không thể xóa quản trị viên (Admin) duy nhất còn lại trong hệ thống.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trackedUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
                if (trackedUser == null) return (false, "Không tìm thấy tài khoản.");

                _context.UserAccounts.Remove(trackedUser);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Đã xóa tài khoản.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "Lỗi cơ sở dữ liệu khi xóa tài khoản.");
            }
        });
    }

    // ── TIÊU CHÍ 3: GỘP SAVECHANGES RA NGOÀI VÒNG LẶP (BATCHING) ─────────

    /// <summary>
    /// Thêm mới nhiều tài khoản/chi tiết hàng loạt.
    /// TUYỆT ĐỐI KHÔNG gọi SaveChangesAsync trong vòng lặp.
    /// Gom toàn bộ vào List và gọi AddRangeAsync + SaveChangesAsync đúng 1 LẦN DUY NHẤT ở ngoài cùng.
    /// </summary>
    public async Task<(bool Success, int InsertedCount, string Message)> BulkCreateAccountsAsync(
        IEnumerable<UserAccountCreateDto> accountDtos)
    {
        var dtoList = accountDtos?.ToList();
        if (dtoList == null || dtoList.Count == 0)
            return (false, 0, "Danh sách tài khoản trống.");

        // Lấy danh sách username hiện có (Read-only với .AsNoTracking) để kiểm tra trùng lặp
        var existingUsernames = (await _context.UserAccounts
            .AsNoTracking()
            .Select(u => u.Username)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var entitiesToAdd = new List<UserAccount>();

        foreach (var dto in dtoList)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.FullName))
                continue;

            var normalizedUsername = dto.Username.Trim();
            if (existingUsernames.Contains(normalizedUsername))
                continue; // bỏ qua tài khoản đã tồn tại

            var role = AllowedRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase) ? dto.Role.Trim() : "Employee";
            
            // TUYỆT ĐỐI KHÔNG dùng fallback "Default@123". Bắt buộc mật khẩu hợp lệ tối thiểu 12 ký tự
            if (string.IsNullOrWhiteSpace(dto.Password) || !IsValidPassword(dto.Password.Trim()))
                continue;

            entitiesToAdd.Add(new UserAccount
            {
                Username = normalizedUsername,
                Password = HashPassword(dto.Password.Trim()),
                FullName = dto.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                Role = role,
                Status = "Active"
            });

            existingUsernames.Add(normalizedUsername);
        }

        if (entitiesToAdd.Count == 0)
            return (false, 0, "Không có tài khoản hợp lệ nào để thêm mới.");

        try
        {
            await _context.UserAccounts.AddRangeAsync(entitiesToAdd);

            // GỌI ĐÚNG 1 LẦN DUY NHẤT Ở NGOÀI CÙNG SAU VÒNG LẶP
            await _context.SaveChangesAsync();

            return (true, entitiesToAdd.Count, $"Đã thêm mới thành công {entitiesToAdd.Count} tài khoản.");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Lỗi khi thêm hàng loạt tài khoản: {ex.Message}");
        }
    }

    /// <summary>
    /// Duyệt/Kích hoạt danh sách nhiều tài khoản.
    /// Vòng lặp chỉ cập nhật trạng thái trong memory tracker.
    /// GỌI SaveChangesAsync đúng 1 LẦN DUY NHẤT ở ngoài cùng sau khi vòng lặp kết thúc.
    /// </summary>
    public async Task<(bool Success, int ApprovedCount, string Message)> ApproveMultipleAccountsAsync(
        IEnumerable<int> userIds)
    {
        var ids = userIds?.Where(id => id > 0).Distinct().ToList();
        if (ids == null || ids.Count == 0)
            return (false, 0, "Không có tài khoản nào được chọn.");

        try
        {
            var accountsToApprove = await _context.UserAccounts
                .Where(u => ids.Contains(u.UserId))
                .ToListAsync();

            if (accountsToApprove.Count == 0)
                return (false, 0, "Không tìm thấy tài khoản cần duyệt.");

            // Cập nhật trạng thái trong vòng lặp - TUYỆT ĐỐI KHÔNG gọi SaveChangesAsync ở đây
            foreach (var account in accountsToApprove)
            {
                account.Status = "Active";
            }

            // GỌI ĐÚNG 1 LẦN DUY NHẤT Ở NGOÀI CÙNG
            var affected = await _context.SaveChangesAsync();

            return (true, accountsToApprove.Count, $"Đã duyệt thành công {accountsToApprove.Count} tài khoản.");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Lỗi khi duyệt tài khoản: {ex.Message}");
        }
    }

    /// <summary>
    /// Duyệt/Phê duyệt hàng loạt phiếu yêu cầu sản xuất.
    /// Gom SaveChangesAsync đúng 1 LẦN DUY NHẤT ở ngoài cùng để chống nghẽn mạng Supabase.
    /// </summary>
    public async Task<(bool Success, int ApprovedCount, string Message)> ApproveProductionRequestsAsync(
        IEnumerable<int> requestIds, int approvedByUserId)
    {
        var ids = requestIds?.Where(id => id > 0).Distinct().ToList();
        if (ids == null || ids.Count == 0)
            return (false, 0, "Không có phiếu nào được chọn.");

        try
        {
            var requests = await _context.ProductionRequests
                .Where(r => ids.Contains(r.RequestId) && r.Status == "Pending")
                .ToListAsync();

            if (requests.Count == 0)
                return (false, 0, "Không có phiếu chờ xử lý hợp lệ.");

            // Lặp qua danh sách chỉ cập nhật trạng thái
            foreach (var req in requests)
            {
                req.Status = "Processing";
            }

            // GỌI ĐÚNG 1 LẦN DUY NHẤT Ở NGOÀI CÙNG
            await _context.SaveChangesAsync();

            return (true, requests.Count, $"Đã duyệt thành công {requests.Count} phiếu yêu cầu sản xuất.");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Lỗi khi duyệt phiếu yêu cầu sản xuất: {ex.Message}");
        }
    }

    // ── BULK OPERATIONS DÙNG DIRECT SQL (Tối ưu cao nhất khi có thể) ─────

    public async Task<(bool Success, int AffectedRows, string Message)> BulkToggleStatusAsync(
        IEnumerable<int> userIds, string targetStatus)
    {
        if (targetStatus != "Active" && targetStatus != "Inactive")
            return (false, 0, "Trạng thái không hợp lệ.");

        try
        {
            var ids = userIds.Where(id => id > 0).Distinct().Take(500).ToArray();
            if (ids.Length == 0) return (false, 0, "Không có mã tài khoản hợp lệ.");

            // Nếu đang vô hiệu hóa (Inactive), kiểm tra không được khóa hết toàn bộ Admin Active
            if (targetStatus == "Inactive")
            {
                var totalActiveAdmins = await _context.UserAccounts
                    .AsNoTracking()
                    .CountAsync(u => u.Role == "Admin" && (u.Status == "Active" || u.Status == "true" || u.Status == "1"));

                var targetedActiveAdmins = await _context.UserAccounts
                    .AsNoTracking()
                    .CountAsync(u => ids.Contains(u.UserId) && u.Role == "Admin" && (u.Status == "Active" || u.Status == "true" || u.Status == "1"));

                if (totalActiveAdmins - targetedActiveAdmins < 1)
                {
                    return (false, 0, "Không thể vô hiệu hóa vì hệ thống bắt buộc phải duy trì ít nhất 1 Quản trị viên (Admin) hoạt động.");
                }
            }

            var affected = await _context.UserAccounts.Where(u => ids.Contains(u.UserId))
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.Status, targetStatus));
            return (true, affected, $"Đã cập nhật {affected} tài khoản sang {targetStatus}.");
        }
        catch
        {
            return (false, 0, "Lỗi cơ sở dữ liệu khi cập nhật hàng loạt.");
        }
    }

    public async Task<(bool Success, int AffectedRows, string Message)> BulkDeleteAsync(
        IEnumerable<int> userIds)
    {
        try
        {
            var ids = userIds.Where(id => id > 0).Distinct().Take(500).ToArray();
            if (ids.Length == 0) return (false, 0, "Không có mã tài khoản hợp lệ.");

            // Kiểm tra không được xóa hết toàn bộ Admin Active
            var totalActiveAdmins = await _context.UserAccounts
                .AsNoTracking()
                .CountAsync(u => u.Role == "Admin" && (u.Status == "Active" || u.Status == "true" || u.Status == "1"));

            var targetedActiveAdmins = await _context.UserAccounts
                .AsNoTracking()
                .CountAsync(u => ids.Contains(u.UserId) && u.Role == "Admin" && (u.Status == "Active" || u.Status == "true" || u.Status == "1"));

            if (totalActiveAdmins > 0 && totalActiveAdmins - targetedActiveAdmins < 1)
            {
                return (false, 0, "Không thể xóa vì hệ thống bắt buộc phải duy trì ít nhất 1 Quản trị viên (Admin) hoạt động.");
            }

            var affected = await _context.UserAccounts.Where(u => ids.Contains(u.UserId)).ExecuteDeleteAsync();
            return (true, affected, $"Đã xóa {affected} tài khoản.");
        }
        catch
        {
            return (false, 0, "Lỗi cơ sở dữ liệu khi xóa hàng loạt.");
        }
    }

    // ── PASSWORD UPGRADE ON LOGIN ─────────────────────────────────────────

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
