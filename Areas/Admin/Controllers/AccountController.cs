using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Areas.Admin.Services;
using httt.Constants;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserAccountService _accountService;
    private readonly IPasswordHasher<UserAccount> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext context,
        UserAccountService accountService,
        IPasswordHasher<UserAccount> passwordHasher,
        ILogger<AccountController> logger)
    {
        _context = context;
        _accountService = accountService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // ── GET: /Admin/Account/Login hoặc /Admin/Login ───────────────────────
    [HttpGet]
    [Route("Admin/Account/Login")]
    [Route("Admin/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var currentRole = AppRoles.NormalizeRole(User.FindFirstValue(ClaimTypes.Role));
        // Nếu đã đăng nhập đúng với quyền Admin, chuyển hướng thẳng vào Dashboard
        if (User.Identity?.IsAuthenticated == true && currentRole == AppRoles.Admin)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && AppRoles.CanAccessUrl(User, returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ── POST: /Admin/Account/Login hoặc /Admin/Login ──────────────────────
    [HttpPost]
    [Route("Admin/Account/Login")]
    [Route("Admin/Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        var loweredUsername = username.Trim().ToLowerInvariant();
        var user = await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == loweredUsername);

        if (user is null)
        {
            UserAccountService.BurnPasswordCheck(password);
            ViewBag.Error = "Không tìm thấy tài khoản trong phân hệ này hoặc thông tin đăng nhập không chính xác.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        var normalizedRole = AppRoles.NormalizeRole(user.Role);
        // Nếu tài khoản thuộc Role khác (Sales, Production, Warehouse) đăng nhập vào Admin -> báo không tìm thấy tài khoản
        if (normalizedRole != AppRoles.Admin)
        {
            UserAccountService.BurnPasswordCheck(password);
            ViewBag.Error = "Không tìm thấy tài khoản trong phân hệ này hoặc thông tin đăng nhập không chính xác.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        bool isValidPassword = VerifyAccountPassword(user, password);

        if (!isValidPassword)
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        var isActive = UserAccountRules.StatusKey(user.Status) == UserAccountRules.StatusActive
            || string.Equals(user.Status, "true", StringComparison.OrdinalIgnoreCase);

        if (!isActive)
        {
            ViewBag.Error = "Tài khoản hiện đang bị tạm khóa hoặc chưa được kích hoạt.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        await _accountService.UpgradePasswordIfNeededAsync(user, password);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, AppRoles.Admin),
            new Claim("FullName", user.FullName ?? user.Username),
            new Claim("OriginalRole", user.Role ?? AppRoles.Admin)
        };

        var identity = new ClaimsIdentity(
            claims,
            AppRoles.AdminScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(AppRoles.AdminScheme, principal, authProperties);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && AppRoles.CanAccessUrl(principal, returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    // ── POST: /Admin/Account/Logout hoặc /Admin/Logout ───────────────────
    [HttpPost]
    [Route("Admin/Account/Logout")]
    [Route("Admin/Logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AppRoles.AdminScheme);
        _logger.LogInformation("User logged out of Admin area.");
        return RedirectToAction(nameof(Login));
    }

    // ── GET: /Admin/Account/Logout (Hỗ trợ logout qua link hoặc URL trực tiếp)
    [HttpGet]
    [Route("Admin/Account/Logout")]
    [Route("Admin/Logout")]
    [AllowAnonymous]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(AppRoles.AdminScheme);
        return RedirectToAction(nameof(Login));
    }

    // ── GET: /Admin/Account/Profile ───────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Challenge();

        try
        {
            var account = await _context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (account == null) return NotFound();
            return View(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Admin profile for: {Username}", username);
            TempData["ErrorMessage"] = "Không thể tải thông tin hồ sơ.";
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
    }

    // ── POST: /Admin/Account/UpdateProfile ────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullname, string phone)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Challenge();

        if (string.IsNullOrWhiteSpace(fullname))
        {
            TempData["ErrorMessage"] = "Họ và tên không được để trống.";
            return RedirectToAction(nameof(Profile));
        }

        string? errorMessage = null;

        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var account = await _context.UserAccounts
                        .FirstOrDefaultAsync(u => u.Username == username);

                    if (account == null)
                    {
                        errorMessage = "Không tìm thấy thông tin tài khoản.";
                        return;
                    }

                    account.FullName = fullname.Trim();
                    account.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for: {Username}", username);
            TempData["ErrorMessage"] = "Lỗi khi cập nhật cơ sở dữ liệu. Vui lòng thử lại.";
            return RedirectToAction(nameof(Profile));
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Profile));
        }

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
        return RedirectToAction(nameof(Profile));
    }

    // ── POST: /Admin/Account/ChangePassword ───────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return Challenge();

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin mật khẩu.";
            return RedirectToAction(nameof(Profile));
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
            return RedirectToAction(nameof(Profile));
        }

        var passwordError = UserAccountRules.ValidatePassword(newPassword);
        if (passwordError is not null)
        {
            TempData["ErrorMessage"] = passwordError;
            return RedirectToAction(nameof(Profile));
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = "Mật khẩu mới không được trùng với mật khẩu hiện tại.";
            return RedirectToAction(nameof(Profile));
        }

        string? errorMessage = null;

        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var account = await _context.UserAccounts
                        .FirstOrDefaultAsync(u => u.Username == username);

                    if (account == null)
                    {
                        errorMessage = "Không tìm thấy thông tin tài khoản.";
                        return;
                    }

                    if (!VerifyAccountPassword(account, currentPassword))
                    {
                        errorMessage = "Mật khẩu hiện tại không chính xác.";
                        return;
                    }

                    account.Password = UserAccountService.HashPassword(newPassword);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for Admin: {Username}", username);
            TempData["ErrorMessage"] = "Lỗi khi đổi mật khẩu. Vui lòng thử lại.";
            return RedirectToAction(nameof(Profile));
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Profile));
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Profile));
    }

    private bool VerifyAccountPassword(UserAccount user, string plainPassword)
    {
        if (UserAccountService.VerifyPassword(plainPassword, user.Password))
        {
            return true;
        }

        try
        {
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, plainPassword);
            return verifyResult == PasswordVerificationResult.Success
                || verifyResult == PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch
        {
            return false;
        }
    }
}
