using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using httt.Areas.Admin.Services;
using httt.Constants;
using httt.Data;
using httt.Models;

namespace httt.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<UserAccount> _passwordHasher;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountController> _logger;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(5);

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<UserAccount> passwordHasher,
            IMemoryCache cache,
            ILogger<AccountController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập thì chuyển ngay về trang chủ theo vai trò
            if (User?.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && AppRoles.CanAccessUrl(User, returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return Redirect(AppRoles.GetDefaultUrlForRole(role));
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cleanUsername = model.Username.Trim().ToLowerInvariant();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimitKey = $"login_fail_{ipAddress}_{cleanUsername}";

            // 1. Kiểm tra giới hạn thử đăng nhập thất bại
            if (_cache.TryGetValue(rateLimitKey, out int failedCount) && failedCount >= MaxFailedAttempts)
            {
                _logger.LogWarning("Đăng nhập bị tạm chặn do thử thất bại quá nhiều lần: IP={IP}, User={User}", ipAddress, cleanUsername);
                ModelState.AddModelError(string.Empty, "Bạn đã thử đăng nhập thất bại quá nhiều lần. Vui lòng đợi 5 phút trước khi thử lại.");
                return View(model);
            }

            // 2. Tra cứu tài khoản từ CSDL
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername);

            // Thông báo lỗi chung, không tiết lộ tài khoản có tồn tại hay không
            const string genericError = "Tên đăng nhập hoặc mật khẩu không chính xác.";

            if (user == null)
            {
                RecordFailedAttempt(rateLimitKey);
                ModelState.AddModelError(string.Empty, genericError);
                return View(model);
            }

            // 3. Kiểm tra trạng thái tài khoản
            var isActive = string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(user.Status, "true", StringComparison.OrdinalIgnoreCase);

            if (!isActive)
            {
                RecordFailedAttempt(rateLimitKey);
                _logger.LogWarning("Đăng nhập thất bại do tài khoản không còn hiệu lực: UserID={UserID}, Username={Username}, Status={Status}", user.UserID, user.Username, user.Status);
                ModelState.AddModelError(string.Empty, "Tài khoản hiện đang bị tạm khóa hoặc chưa được kích hoạt.");
                return View(model);
            }

            // 4. Kiểm tra mật khẩu bằng PasswordHasher chuẩn hoặc BCrypt (UserAccountService)
            bool passwordValid = false;
            if (UserAccountService.VerifyPassword(model.Password, user.Password))
            {
                passwordValid = true;
            }
            else
            {
                try
                {
                    var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
                    if (verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        passwordValid = true;

                        // Tự động nâng cấp hash nếu thuật toán có phiên bản mới hơn
                        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            user.Password = _passwordHasher.HashPassword(user, model.Password);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Mật khẩu của tài khoản {Username} có định dạng không tương thích với PasswordHasher.", user.Username);
                    passwordValid = false;
                }
            }

            if (!passwordValid)
            {
                RecordFailedAttempt(rateLimitKey);
                _logger.LogWarning("Đăng nhập thất bại (sai mật khẩu): UserID={UserID}, Username={Username}", user.UserID, user.Username);
                ModelState.AddModelError(string.Empty, genericError);
                return View(model);
            }

            // 5. Đăng nhập thành công -> Xóa bộ đếm thất bại
            _cache.Remove(rateLimitKey);

            var normalizedRole = AppRoles.NormalizeRole(user.Role);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, normalizedRole),
                new Claim("FullName", user.FullName ?? user.Username),
                new Claim("OriginalRole", user.Role ?? normalizedRole)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe 
                    ? DateTimeOffset.UtcNow.AddDays(7) 
                    : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Đăng nhập thành công: UserID={UserID}, Username={Username}, Role={Role}", user.UserID, user.Username, normalizedRole);

            // 6. Điều hướng an toàn
            if (!string.IsNullOrEmpty(model.ReturnUrl) && 
                Url.IsLocalUrl(model.ReturnUrl) && 
                AppRoles.CanAccessUrl(new ClaimsPrincipal(claimsIdentity), model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect(AppRoles.GetDefaultUrlForRole(normalizedRole));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User?.Identity?.Name ?? "unknown";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Người dùng {Username} đã đăng xuất", username);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.UserRole = User?.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.RoleDisplayName = AppRoles.GetDisplayName(ViewBag.UserRole);
            return View();
        }

        private void RecordFailedAttempt(string key)
        {
            var attempts = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = LockoutWindow;
                return 0;
            });

            _cache.Set(key, attempts + 1, LockoutWindow);
        }
    }
}
