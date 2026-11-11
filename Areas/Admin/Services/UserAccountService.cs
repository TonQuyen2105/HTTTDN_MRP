using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using httt.Areas.Admin.ViewModels;
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
/// Mọi thao tác có thể làm mất Admin Active cuối cùng đều chạy trong transaction
/// được tuần tự hóa bằng PostgreSQL advisory lock (xem <see cref="RunGuardedAsync"/>).
/// </summary>
public class UserAccountService
{
    /// <summary>Khóa advisory toàn cục: chỉ 1 thao tác "đụng tới Admin" được chạy tại một thời điểm.</summary>
    private const long AdminGuardLockKey = 704_270_001L;

    private static readonly TimeSpan SessionCacheTtl = TimeSpan.FromSeconds(30);

    private const string MsgLastAdmin =
        "Không thể thực hiện vì hệ thống bắt buộc phải duy trì ít nhất 1 Quản trị viên (Admin) đang hoạt động.";

    private const string MsgSelf =
        "Bạn không thể khóa, xóa hoặc thay đổi vai trò của chính tài khoản đang đăng nhập.";

    private const string MsgNotFound = "Không tìm thấy tài khoản.";

    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserAccountService> _logger;

    // Hash giả dùng để cân bằng thời gian phản hồi khi username không tồn tại (chống dò tài khoản qua timing).
    private static readonly Lazy<string> DummyHash =
        new(() => BCrypt.Net.BCrypt.HashPassword("timing-equalizer-not-a-real-password", workFactor: 12));

    /// <summary>Danh sách role hợp lệ của hệ thống (giữ lại để tương thích mã cũ).</summary>
    public static IReadOnlyList<string> AllowedRoles => UserAccountRules.Roles;

    public UserAccountService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<UserAccountService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PASSWORD
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Hash mật khẩu dạng plain-text bằng BCrypt (work factor 12). KHÔNG trim mật khẩu.</summary>
    public static string HashPassword(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

    /// <summary>
    /// Kiểm tra mật khẩu nhập vào khớp với hash trong DB (BCrypt).
    /// Vẫn nhận diện dữ liệu legacy để <see cref="UpgradePasswordIfNeededAsync"/> băm lại sau khi đăng nhập.
    /// </summary>
    public static bool VerifyPassword(string plainPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        // Chặn input khổng lồ trước khi tốn CPU cho BCrypt.
        if (plainPassword.Length > 256)
            return false;

        if (storedHash.StartsWith("$2", StringComparison.Ordinal))
        {
            try { return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash); }
            catch { return false; }
        }

        // ── Legacy / Identity PasswordHasher (sẽ được nâng cấp lên BCrypt ngay khi đăng nhập thành công) ──
        if (IsDpApiProtectedValue(storedHash))
        {
            try
            {
                var identityHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UserAccount>();
                var verifyRes = identityHasher.VerifyHashedPassword(new UserAccount(), storedHash, plainPassword);
                if (verifyRes == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success ||
                    verifyRes == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }
            catch
            {
                // Fallback to DPAPI check below
            }

            return TryDecryptDpApiPassword(storedHash, out var decrypted) && ConstantTimeEquals(plainPassword, decrypted);
        }

        return ConstantTimeEquals(plainPassword, storedHash);
    }

    /// <summary>
    /// Tốn thời gian tương đương một lần verify thật. Gọi khi username không tồn tại
    /// để kẻ tấn công không phân biệt được "sai username" với "sai mật khẩu" qua thời gian phản hồi.
    /// </summary>
    public static bool BurnPasswordCheck(string plainPassword)
    {
        VerifyPassword(plainPassword, DummyHash.Value);
        return false;
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var x = Encoding.UTF8.GetBytes(a);
        var y = Encoding.UTF8.GetBytes(b);
        return x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
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

    public async Task UpgradePasswordIfNeededAsync(UserAccount user, string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(user.Password))
            return;

        if (user.Password.StartsWith("$2", StringComparison.Ordinal))
            return;

        var isLegacyPlainText = ConstantTimeEquals(plainPassword, user.Password);
        var isLegacyDpApi = IsDpApiProtectedValue(user.Password);

        if (!isLegacyPlainText && !isLegacyDpApi)
            return;

        var passwordToStore = plainPassword;
        if (isLegacyDpApi)
        {
            if (!TryDecryptDpApiPassword(user.Password, out var decrypted)) return;
            passwordToStore = decrypted;
        }

        try
        {
            var trackedUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (trackedUser is null || trackedUser.Password != user.Password)
                return;

            trackedUser.Password = HashPassword(passwordToStore);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Không làm gián đoạn đăng nhập nếu việc nâng cấp hash gặp sự cố.
            _logger.LogWarning(ex, "Could not upgrade legacy password hash for user {UserId}.", user.UserId);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SESSION VALIDATION (dùng bởi Cookie OnValidatePrincipal)
    // ══════════════════════════════════════════════════════════════════════

    private static string SessionKey(int userId) => $"admin:session:{userId}";

    /// <summary>
    /// Phiên đăng nhập còn hợp lệ khi tài khoản VẪN tồn tại và Active trong DB.
    /// Hỗ trợ cả Admin lẫn các Role khác (Sales, Warehouse, Production, Manager...) trong hệ thống.
    /// </summary>
    public async Task<bool> IsActiveUserSessionAsync(int userId, string? requiredRole = null)
    {
        var result = await _cache.GetOrCreateAsync(SessionKey(userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SessionCacheTtl;
            var user = await _context.UserAccounts.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Role, u.Status })
                .FirstOrDefaultAsync();

            if (user is null) return (Exists: false, Role: string.Empty, IsActive: false);

            var isActive = user.Status == "Active" 
                        || user.Status == "true" 
                        || user.Status == "1" 
                        || EF.Functions.ILike(user.Status, "Active");

            return (Exists: true, user.Role, IsActive: isActive);
        });

        if (!result.Exists || !result.IsActive)
            return false;

        if (!string.IsNullOrWhiteSpace(requiredRole))
        {
            return string.Equals(result.Role, requiredRole, StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    httt.Constants.AppRoles.NormalizeRole(result.Role),
                    httt.Constants.AppRoles.NormalizeRole(requiredRole),
                    StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    /// <summary>Giữ tương thích ngược cho các nơi đang gọi IsActiveAdminSessionAsync.</summary>
    public Task<bool> IsActiveAdminSessionAsync(int userId) => IsActiveUserSessionAsync(userId, UserAccountRules.AdminRole);

    private void InvalidateSessions(IEnumerable<int> userIds)
    {
        foreach (var id in userIds) _cache.Remove(SessionKey(id));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  READ (luôn AsNoTracking + projection → hash không bao giờ được load lên)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<UserListViewModel> GetPagedAsync(
        UserListQuery query, int? currentUserId, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 5, 100);
        var search = query.Search?.Trim();
        if (search is { Length: > 100 }) search = search[..100];

        var role = UserAccountRules.NormalizeRole(query.Role);
        var status = UserAccountRules.NormalizeStatus(query.Status);

        IQueryable<UserAccount> q = _context.UserAccounts.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            // Escape % _ \ để ký tự người dùng nhập không bị hiểu là wildcard.
            var pattern = $"%{UserAccountRules.EscapeLike(search)}%";
            q = q.Where(u =>
                EF.Functions.ILike(u.Username, pattern, "\\") ||
                EF.Functions.ILike(u.FullName, pattern, "\\") ||
                (u.Phone != null && EF.Functions.ILike(u.Phone, pattern, "\\")));
        }

        if (role is not null)
            q = q.Where(u => u.Role == role);

        q = status switch
        {
            UserAccountRules.StatusActive => q.Where(u => u.Status == UserAccountRules.StatusActive),
            UserAccountRules.StatusBlocked => q.Where(u => u.Status == UserAccountRules.StatusBlocked),
            // Disabled gồm cả giá trị legacy (Inactive, true, 1...) để khớp với StatusKey().
            UserAccountRules.StatusDisabled => q.Where(u =>
                u.Status != UserAccountRules.StatusActive && u.Status != UserAccountRules.StatusBlocked),
            _ => q
        };

        var totalItems = await q.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);

        var rows = await q
            .OrderBy(u => u.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.UserId, u.Username, u.FullName, u.Phone, u.Role, u.Status })
            .ToListAsync(ct);

        var activeAdmins = await CountActiveAdminsAsync(ct);

        var items = rows.Select(r =>
        {
            var key = UserAccountRules.StatusKey(r.Status);
            var isActiveAdmin = r.Role == UserAccountRules.AdminRole && key == UserAccountRules.StatusActive;
            return new UserListItem(
                r.UserId, r.Username, r.FullName, r.Phone, r.Role, key,
                IsSelf: currentUserId == r.UserId,
                IsLastActiveAdmin: isActiveAdmin && activeAdmins <= 1);
        }).ToList();

        return new UserListViewModel
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Search = search ?? string.Empty,
            Role = role ?? string.Empty,
            Status = status ?? string.Empty
        };
    }

    private Task<int> CountActiveAdminsAsync(CancellationToken ct = default)
        => _context.UserAccounts.AsNoTracking().CountAsync(u =>
            u.Role == UserAccountRules.AdminRole && u.Status == UserAccountRules.StatusActive, ct);

    /// <summary>
    /// True nếu việc làm các tài khoản <paramref name="targetIds"/> mất tư cách "Admin Active"
    /// sẽ khiến hệ thống không còn Admin Active nào. PHẢI gọi bên trong <see cref="RunGuardedAsync"/>.
    /// </summary>
    private async Task<bool> WouldRemoveLastActiveAdminAsync(int[] targetIds)
    {
        var hitsActiveAdmin = await _context.UserAccounts.AsNoTracking().AnyAsync(u =>
            targetIds.Contains(u.UserId) &&
            u.Role == UserAccountRules.AdminRole &&
            u.Status == UserAccountRules.StatusActive);

        if (!hitsActiveAdmin) return false;

        var remaining = await _context.UserAccounts.AsNoTracking().CountAsync(u =>
            !targetIds.Contains(u.UserId) &&
            u.Role == UserAccountRules.AdminRole &&
            u.Status == UserAccountRules.StatusActive);

        return remaining < 1;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TRANSACTION + ADVISORY LOCK
    // ══════════════════════════════════════════════════════════════════════

    private static string? GetSqlState(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is PostgresException pg) return pg.SqlState;
        return null;
    }

    /// <summary>
    /// Chạy <paramref name="work"/> trong 1 transaction và giữ <c>pg_advisory_xact_lock</c> cho tới khi commit/rollback.
    /// Nhờ vậy "đếm Admin Active còn lại → cập nhật" là nguyên tử: hai request song song không thể
    /// cùng vượt qua kiểm tra rồi cùng vô hiệu hóa/xóa hai Admin cuối cùng.
    /// Lock theo transaction nên an toàn với Supabase pooler (port 6543, transaction mode).
    /// </summary>
    private async Task<ServiceResult> RunGuardedAsync(Func<Task<ServiceResult>> work)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Nếu strategy retry, bỏ hết entity đang track từ lần chạy trước.
            _context.ChangeTracker.Clear();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", AdminGuardLockKey);

                var result = await work();

                if (result.Success) await tx.CommitAsync();
                else await tx.RollbackAsync();

                return result;
            }
            catch (Exception ex) when (GetSqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ServiceError.Conflict,
                    "Tài khoản đã phát sinh dữ liệu nghiệp vụ (đơn hàng, yêu cầu sản xuất...) nên không thể xóa. " +
                    "Hãy chuyển sang trạng thái Blocked hoặc Disabled.");
            }
            catch (Exception ex) when (GetSqlState(ex) == PostgresErrorCodes.UniqueViolation)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ServiceError.Conflict, "Dữ liệu bị trùng lặp.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CREATE
    // ══════════════════════════════════════════════════════════════════════

    public async Task<ServiceResult> CreateAsync(CreateUserRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (!UserAccountRules.IsValidUsername(username))
            return ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgUsername, "username");

        var passwordError = UserAccountRules.ValidatePassword(request.Password);
        if (passwordError is not null)
            return ServiceResult.Fail(ServiceError.Validation, passwordError, "password");

        if (!TryNormalizeProfile(request.FullName, request.Phone, request.Role, request.Status ?? UserAccountRules.StatusActive,
                out var profile, out var error))
            return error!;

        var lowered = username.ToLowerInvariant();
        var exists = await _context.UserAccounts.AsNoTracking()
            .AnyAsync(u => u.Username.ToLower() == lowered);

        if (exists)
            return ServiceResult.Fail(ServiceError.Conflict, "Tên đăng nhập đã tồn tại.", "username");

        var user = new UserAccount
        {
            Username = username,
            Password = HashPassword(request.Password!),
            FullName = profile.FullName,
            Phone = profile.Phone,
            Role = profile.Role,
            Status = profile.Status
        };

        try
        {
            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Tạo tài khoản thành công.");
        }
        catch (DbUpdateException ex) when (GetSqlState(ex) == PostgresErrorCodes.UniqueViolation)
        {
            // Hai request tạo cùng username đồng thời: unique index ix_useraccount_username chặn request thứ hai.
            _context.Entry(user).State = EntityState.Detached;
            return ServiceResult.Fail(ServiceError.Conflict, "Tên đăng nhập đã tồn tại.", "username");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  UPDATE / STATUS / DELETE / RESET PASSWORD (đều qua RunGuardedAsync)
    // ══════════════════════════════════════════════════════════════════════

    private sealed record Profile(string FullName, string? Phone, string Role, string Status);

    private static bool TryNormalizeProfile(
        string? fullName, string? phone, string? role, string? status,
        out Profile profile, out ServiceResult? error)
    {
        profile = new Profile(string.Empty, null, string.Empty, string.Empty);

        if (!UserAccountRules.IsValidFullName(fullName))
        { error = ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgFullName, "fullName"); return false; }

        if (!UserAccountRules.IsValidPhone(phone))
        { error = ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgPhone, "phone"); return false; }

        var normalizedRole = UserAccountRules.NormalizeRole(role);
        if (normalizedRole is null)
        { error = ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgRole, "role"); return false; }

        var normalizedStatus = UserAccountRules.NormalizeStatus(status);
        if (normalizedStatus is null)
        { error = ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgStatus, "status"); return false; }

        profile = new Profile(
            fullName!.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            normalizedRole,
            normalizedStatus);
        error = null;
        return true;
    }

    public async Task<ServiceResult> UpdateAsync(UpdateUserRequest request, int? actorUserId)
    {
        if (!TryNormalizeProfile(request.FullName, request.Phone, request.Role, request.Status,
                out var profile, out var error))
            return error!;

        var result = await RunGuardedAsync(async () =>
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == request.Id);
            if (user is null) return ServiceResult.Fail(ServiceError.NotFound, MsgNotFound);

            var wasActiveAdmin = user.Role == UserAccountRules.AdminRole
                                 && UserAccountRules.StatusKey(user.Status) == UserAccountRules.StatusActive;
            var willBeActiveAdmin = profile.Role == UserAccountRules.AdminRole
                                    && profile.Status == UserAccountRules.StatusActive;

            if (actorUserId == user.UserId && wasActiveAdmin && !willBeActiveAdmin)
                return ServiceResult.Fail(ServiceError.Forbidden, MsgSelf);

            if (wasActiveAdmin && !willBeActiveAdmin && await WouldRemoveLastActiveAdminAsync([user.UserId]))
                return ServiceResult.Fail(ServiceError.Conflict, MsgLastAdmin);

            user.FullName = profile.FullName;
            user.Phone = profile.Phone;
            user.Role = profile.Role;
            user.Status = profile.Status;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cập nhật tài khoản thành công.");
        });

        if (result.Success) InvalidateSessions([request.Id]);
        return result;
    }

    public async Task<ServiceResult> SetStatusAsync(int userId, string? status, int? actorUserId)
    {
        var target = UserAccountRules.NormalizeStatus(status);
        if (target is null)
            return ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgStatus, "status");

        if (actorUserId == userId && target != UserAccountRules.StatusActive)
            return ServiceResult.Fail(ServiceError.Forbidden, MsgSelf);

        var result = await RunGuardedAsync(async () =>
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return ServiceResult.Fail(ServiceError.NotFound, MsgNotFound);

            var wasActiveAdmin = user.Role == UserAccountRules.AdminRole
                                 && UserAccountRules.StatusKey(user.Status) == UserAccountRules.StatusActive;

            if (wasActiveAdmin && target != UserAccountRules.StatusActive
                && await WouldRemoveLastActiveAdminAsync([userId]))
                return ServiceResult.Fail(ServiceError.Conflict, MsgLastAdmin);

            user.Status = target;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cập nhật trạng thái thành công.", value: target);
        });

        if (result.Success) InvalidateSessions([userId]);
        return result;
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int? actorUserId)
    {
        if (actorUserId == userId)
            return ServiceResult.Fail(ServiceError.Forbidden, MsgSelf);

        var result = await RunGuardedAsync(async () =>
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return ServiceResult.Fail(ServiceError.NotFound, MsgNotFound);

            if (await WouldRemoveLastActiveAdminAsync([userId]))
                return ServiceResult.Fail(ServiceError.Conflict, MsgLastAdmin);

            _context.UserAccounts.Remove(user);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã xóa tài khoản.");
        });

        if (result.Success) InvalidateSessions([userId]);
        return result;
    }

    public async Task<ServiceResult> ResetPasswordAsync(int userId, string? newPlainPassword)
    {
        var passwordError = UserAccountRules.ValidatePassword(newPlainPassword);
        if (passwordError is not null)
            return ServiceResult.Fail(ServiceError.Validation, passwordError, "newPassword");

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return ServiceResult.Fail(ServiceError.NotFound, MsgNotFound);

        user.Password = HashPassword(newPlainPassword!);
        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Đặt lại mật khẩu thành công.");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BULK (1 câu SQL duy nhất, nguyên tử, trong cùng transaction + lock)
    // ══════════════════════════════════════════════════════════════════════

    private static bool TryNormalizeIds(IEnumerable<int>? ids, out int[] result, out ServiceResult? error)
    {
        result = ids?.Where(id => id > 0).Distinct().ToArray() ?? [];
        if (result.Length == 0)
        { error = ServiceResult.Fail(ServiceError.Validation, "Không có tài khoản nào được chọn."); return false; }

        if (result.Length > UserAccountRules.BulkMaxIds)
        { error = ServiceResult.Fail(ServiceError.Validation, "Mỗi lần chỉ được thao tác tối đa 200 tài khoản."); return false; }

        error = null;
        return true;
    }

    public async Task<ServiceResult> BulkSetStatusAsync(IEnumerable<int>? userIds, string? status, int? actorUserId)
    {
        var target = UserAccountRules.NormalizeStatus(status);
        if (target is null)
            return ServiceResult.Fail(ServiceError.Validation, UserAccountRules.MsgStatus, "status");

        if (!TryNormalizeIds(userIds, out var ids, out var error)) return error!;

        if (target != UserAccountRules.StatusActive && actorUserId is int self && ids.Contains(self))
            return ServiceResult.Fail(ServiceError.Forbidden, MsgSelf + " Hãy bỏ chọn tài khoản của bạn.");

        var result = await RunGuardedAsync(async () =>
        {
            if (target != UserAccountRules.StatusActive && await WouldRemoveLastActiveAdminAsync(ids))
                return ServiceResult.Fail(ServiceError.Conflict, MsgLastAdmin);

            var affected = await _context.UserAccounts
                .Where(u => ids.Contains(u.UserId))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, target));

            return ServiceResult.Ok($"Đã cập nhật {affected} tài khoản sang trạng thái {target}.", target, affected);
        });

        if (result.Success) InvalidateSessions(ids);
        return result;
    }

    public async Task<ServiceResult> BulkDeleteAsync(IEnumerable<int>? userIds, int? actorUserId)
    {
        if (!TryNormalizeIds(userIds, out var ids, out var error)) return error!;

        if (actorUserId is int self && ids.Contains(self))
            return ServiceResult.Fail(ServiceError.Forbidden, MsgSelf + " Hãy bỏ chọn tài khoản của bạn.");

        var result = await RunGuardedAsync(async () =>
        {
            if (await WouldRemoveLastActiveAdminAsync(ids))
                return ServiceResult.Fail(ServiceError.Conflict, MsgLastAdmin);

            var affected = await _context.UserAccounts
                .Where(u => ids.Contains(u.UserId))
                .ExecuteDeleteAsync();

            return ServiceResult.Ok($"Đã xóa {affected} tài khoản.", affected: affected);
        });

        if (result.Success) InvalidateSessions(ids);
        return result;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CÁC HÀM KHÁC (giữ nguyên hành vi, không thuộc luồng quản lý tài khoản)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Thêm hàng loạt tài khoản: SaveChangesAsync đúng 1 lần ở ngoài vòng lặp.</summary>
    public async Task<(bool Success, int InsertedCount, string Message)> BulkCreateAccountsAsync(
        IEnumerable<UserAccountCreateDto> accountDtos)
    {
        var dtoList = accountDtos?.ToList();
        if (dtoList == null || dtoList.Count == 0)
            return (false, 0, "Danh sách tài khoản trống.");

        var existingUsernames = (await _context.UserAccounts
            .AsNoTracking()
            .Select(u => u.Username)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var entitiesToAdd = new List<UserAccount>();

        foreach (var dto in dtoList)
        {
            var username = dto.Username?.Trim() ?? string.Empty;
            if (!UserAccountRules.IsValidUsername(username) || !UserAccountRules.IsValidFullName(dto.FullName))
                continue;

            if (existingUsernames.Contains(username) || !UserAccountRules.IsValidPhone(dto.Phone))
                continue;

            // Không có mật khẩu mặc định: bắt buộc mật khẩu hợp lệ truyền vào.
            if (UserAccountRules.ValidatePassword(dto.Password) is not null)
                continue;

            entitiesToAdd.Add(new UserAccount
            {
                Username = username,
                Password = HashPassword(dto.Password),
                FullName = dto.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                Role = UserAccountRules.NormalizeRole(dto.Role) ?? "Employee",
                Status = UserAccountRules.StatusActive
            });

            existingUsernames.Add(username);
        }

        if (entitiesToAdd.Count == 0)
            return (false, 0, "Không có tài khoản hợp lệ nào để thêm mới.");

        try
        {
            await _context.UserAccounts.AddRangeAsync(entitiesToAdd);
            await _context.SaveChangesAsync();
            return (true, entitiesToAdd.Count, $"Đã thêm mới thành công {entitiesToAdd.Count} tài khoản.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk create accounts failed.");
            return (false, 0, "Lỗi khi thêm hàng loạt tài khoản.");
        }
    }

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

    public async Task<List<SaleOrder>> GetSaleOrdersWithUserAsync(int take = 50)
    {
        return await _context.SaleOrders
            .AsNoTracking()
            .Include(o => o.UserAccount)
            .OrderByDescending(o => o.OrderId)
            .Take(take)
            .ToListAsync();
    }

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

            foreach (var req in requests)
            {
                req.Status = "Processing";
            }

            await _context.SaveChangesAsync();

            return (true, requests.Count, $"Đã duyệt thành công {requests.Count} phiếu yêu cầu sản xuất.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Approve production requests failed.");
            return (false, 0, "Lỗi khi duyệt phiếu yêu cầu sản xuất.");
        }
    }
}
