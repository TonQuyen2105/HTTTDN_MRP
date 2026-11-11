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

        // ── GET: /Account/Login -> Điều hướng tự động về trang đăng nhập của Area tương ứng ──
        [HttpGet("/Account/Login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null, string? portal = null)
        {
            if (!string.IsNullOrWhiteSpace(portal))
            {
                var p = portal.Trim();
                if (p.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Login", "Account", new { area = "Admin", returnUrl });
                if (p.Equals("Sales", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(LoginSales), new { returnUrl });
                if (p.Equals("Warehouse", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(LoginWarehouse), new { returnUrl });
                if (p.Equals("Production", StringComparison.OrdinalIgnoreCase) || p.Equals("Productions", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(LoginProduction), new { returnUrl });
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                if (returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Login", "Account", new { area = "Admin", returnUrl });
                if (returnUrl.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(LoginSales), new { returnUrl });
                if (returnUrl.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                    returnUrl.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(LoginWarehouse), new { returnUrl });
                if (returnUrl.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(LoginProduction), new { returnUrl });
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                return Redirect(AppRoles.GetDefaultUrlForRole(role));
            }

            return RedirectToAction(nameof(LoginProduction), new { returnUrl });
        }

        // ── 2. PHÂN HỆ BÁN HÀNG (/Sales/Account/Login hoặc /Account/LoginSales) ──────────────
        [HttpGet("/Sales/Account/Login")]
        [HttpGet("/Sales/Login")]
        [HttpGet("/Account/LoginSales")]
        [AllowAnonymous]
        public IActionResult LoginSales(string? returnUrl = null)
        {
            var currentRole = AppRoles.NormalizeRole(User?.FindFirst(ClaimTypes.Role)?.Value);
            if (User?.Identity?.IsAuthenticated == true && currentRole == AppRoles.Sales)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && AppRoles.CanAccessUrl(User, returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return Redirect("/Sales/Orders");
            }

            return View("LoginSales", new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost("/Sales/Account/Login")]
        [HttpPost("/Sales/Login")]
        [HttpPost("/Account/LoginSales")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginSales(LoginViewModel model)
        {
            return await ProcessPortalLoginAsync(
                model,
                expectedRole: AppRoles.Sales,
                viewName: "LoginSales",
                portalDisplayName: "Phân hệ Bán hàng & Phân phối",
                defaultRedirectUrl: "/Sales/Orders");
        }

        // ── 3. PHÂN HỆ SẢN XUẤT & MRP (/Productions/Account/Login hoặc /Account/LoginProduction) ──
        [HttpGet("/Productions/Account/Login")]
        [HttpGet("/Productions/Login")]
        [HttpGet("/Account/LoginProduction")]
        [AllowAnonymous]
        public IActionResult LoginProduction(string? returnUrl = null)
        {
            var currentRole = AppRoles.NormalizeRole(User?.FindFirst(ClaimTypes.Role)?.Value);
            if (User?.Identity?.IsAuthenticated == true && currentRole == AppRoles.Production)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && AppRoles.CanAccessUrl(User, returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return Redirect("/Productions/Home/Index");
            }

            return View("LoginProduction", new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost("/Productions/Account/Login")]
        [HttpPost("/Productions/Login")]
        [HttpPost("/Account/LoginProduction")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginProduction(LoginViewModel model)
        {
            return await ProcessPortalLoginAsync(
                model,
                expectedRole: AppRoles.Production,
                viewName: "LoginProduction",
                portalDisplayName: "Phân hệ Sản xuất & MRP",
                defaultRedirectUrl: "/Productions/Home/Index");
        }

        // ── 4. PHÂN HỆ KHO CHÍNH (/Warehouse/Account/Login hoặc /Account/LoginWarehouse) ────
        [HttpGet("/Warehouse/Account/Login")]
        [HttpGet("/Warehouse/Login")]
        [HttpGet("/Account/LoginWarehouse")]
        [AllowAnonymous]
        public IActionResult LoginWarehouse(string? returnUrl = null)
        {
            var currentRole = AppRoles.NormalizeRole(User?.FindFirst(ClaimTypes.Role)?.Value);
            if (User?.Identity?.IsAuthenticated == true && currentRole == AppRoles.Warehouse)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && AppRoles.CanAccessUrl(User, returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return Redirect("/Warehouse/Dashboard");
            }

            return View("LoginWarehouse", new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost("/Warehouse/Account/Login")]
        [HttpPost("/Warehouse/Login")]
        [HttpPost("/Account/LoginWarehouse")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWarehouse(LoginViewModel model)
        {
            return await ProcessPortalLoginAsync(
                model,
                expectedRole: AppRoles.Warehouse,
                viewName: "LoginWarehouse",
                portalDisplayName: "Phân hệ Kho dược phẩm",
                defaultRedirectUrl: "/Warehouse/Dashboard");
        }

        // ── XỬ LÝ XÁC THỰC CHUNG CHO TỪNG TRANG ĐĂNG NHẬP PHÂN HỆ ─────────────────────────
        private async Task<IActionResult> ProcessPortalLoginAsync(
            LoginViewModel model,
            string expectedRole,
            string viewName,
            string portalDisplayName,
            string defaultRedirectUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(viewName, model);
            }

            var cleanUsername = model.Username.Trim().ToLowerInvariant();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimitKey = $"login_fail_{ipAddress}_{cleanUsername}";

            // 1. Kiểm tra giới hạn thử đăng nhập thất bại
            if (_cache.TryGetValue(rateLimitKey, out int failedCount) && failedCount >= MaxFailedAttempts)
            {
                _logger.LogWarning("Đăng nhập bị tạm chặn do thử thất bại quá nhiều lần: IP={IP}, User={User}", ipAddress, cleanUsername);
                ModelState.AddModelError(string.Empty, "Bạn đã thử đăng nhập thất bại quá nhiều lần. Vui lòng đợi 5 phút trước khi thử lại.");
                return View(viewName, model);
            }

            // 2. Tra cứu tài khoản từ CSDL
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername);

            const string accountNotFoundError = "Không tìm thấy tài khoản trong phân hệ này hoặc thông tin đăng nhập không chính xác.";
            const string genericError = "Tên đăng nhập hoặc mật khẩu không chính xác.";

            if (user == null)
            {
                RecordFailedAttempt(rateLimitKey);
                ModelState.AddModelError(string.Empty, accountNotFoundError);
                return View(viewName, model);
            }

            // 3. Kiểm tra đúng vai trò của phân hệ đăng nhập (nếu tài khoản thuộc phân hệ khác -> báo không tìm thấy tài khoản)
            var normalizedRole = AppRoles.NormalizeRole(user.Role);
            if (!string.Equals(normalizedRole, expectedRole, StringComparison.OrdinalIgnoreCase))
            {
                RecordFailedAttempt(rateLimitKey);
                ModelState.AddModelError(string.Empty, accountNotFoundError);
                return View(viewName, model);
            }

            // 4. Kiểm tra trạng thái tài khoản
            var isActive = UserAccountRules.StatusKey(user.Status) == UserAccountRules.StatusActive ||
                           string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(user.Status, "true", StringComparison.OrdinalIgnoreCase);

            if (!isActive)
            {
                RecordFailedAttempt(rateLimitKey);
                _logger.LogWarning("Đăng nhập thất bại do tài khoản không còn hiệu lực: UserID={UserID}, Username={Username}, Status={Status}", user.UserID, user.Username, user.Status);
                ModelState.AddModelError(string.Empty, "Tài khoản hiện đang bị tạm khóa hoặc chưa được kích hoạt.");
                return View(viewName, model);
            }

            // 5. Kiểm tra mật khẩu bằng BCrypt (UserAccountService) hoặc PasswordHasher chuẩn
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
                return View(viewName, model);
            }

            // 6. Đăng nhập thành công -> Xóa bộ đếm thất bại
            _cache.Remove(rateLimitKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, normalizedRole),
                new Claim("FullName", user.FullName ?? user.Username),
                new Claim("OriginalRole", user.Role ?? normalizedRole)
            };

            if (!string.IsNullOrWhiteSpace(user.Role) &&
                !string.Equals(user.Role.Trim(), normalizedRole, StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Trim()));
            }

            var portalScheme = AppRoles.GetSchemeForRole(normalizedRole);
            var claimsIdentity = new ClaimsIdentity(claims, portalScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                portalScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Đăng nhập thành công vào {Portal} (Scheme={Scheme}): UserID={UserID}, Username={Username}, Role={Role}", portalDisplayName, portalScheme, user.UserID, user.Username, normalizedRole);

            if (!string.IsNullOrEmpty(model.ReturnUrl) &&
                Url.IsLocalUrl(model.ReturnUrl) &&
                AppRoles.CanAccessUrl(new ClaimsPrincipal(claimsIdentity), model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect(defaultRedirectUrl);
        }

        // ── ĐĂNG XUẤT THEO PHÂN HỆ (CHỈ XÓA COOKIE CỦA PHÂN HỆ ĐÓ, GIỮ NGUYÊN CÁC TAB CÒN LẠI) ────
        [HttpPost("/Account/Logout")]
        [HttpPost("/Sales/Account/Logout")]
        [HttpPost("/Productions/Account/Logout")]
        [HttpPost("/Warehouse/Account/Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? area = null)
        {
            var (portalScheme, redirectLoginUrl) = ResolveLogoutTarget(area);
            var username = User?.Identity?.Name ?? "unknown";
            await HttpContext.SignOutAsync(portalScheme);
            _logger.LogInformation("Người dùng {Username} đã đăng xuất khỏi {Scheme} -> chuyển tới {RedirectUrl}", username, portalScheme, redirectLoginUrl);
            return Redirect(redirectLoginUrl);
        }

        [HttpGet("/Account/Logout")]
        [HttpGet("/Account/LogoutGet")]
        [HttpGet("/Sales/Account/Logout")]
        [HttpGet("/Productions/Account/Logout")]
        [HttpGet("/Warehouse/Account/Logout")]
        [AllowAnonymous]
        public async Task<IActionResult> LogoutGet(string? area = null)
        {
            var (portalScheme, redirectLoginUrl) = ResolveLogoutTarget(area);
            await HttpContext.SignOutAsync(portalScheme);
            return Redirect(redirectLoginUrl);
        }

        private (string Scheme, string LoginUrl) ResolveLogoutTarget(string? area)
        {
            // 1. Ưu tiên tham số area hoặc đường dẫn URL trực tiếp (/Sales/Account/Logout, v.v.)
            var requestPath = HttpContext.Request.Path.Value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(area))
            {
                if (area.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.AdminScheme, "/Admin/Account/Login");
                if (area.Equals("Sales", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.SalesScheme, "/Sales/Account/Login");
                if (area.Equals("Warehouse", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.WarehouseScheme, "/Warehouse/Account/Login");
                if (area.Equals("Productions", StringComparison.OrdinalIgnoreCase) || area.Equals("Production", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.ProductionScheme, "/Productions/Account/Login");
            }

            if (requestPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                return (AppRoles.AdminScheme, "/Admin/Account/Login");
            if (requestPath.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase))
                return (AppRoles.SalesScheme, "/Sales/Account/Login");
            if (requestPath.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase))
                return (AppRoles.WarehouseScheme, "/Warehouse/Account/Login");
            if (requestPath.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase))
                return (AppRoles.ProductionScheme, "/Productions/Account/Login");

            // 2. Kiểm tra Referer header xem người dùng vừa bấm Đăng xuất từ Area nào
            var referer = HttpContext.Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refUri))
            {
                var refPath = refUri.AbsolutePath;
                if (refPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.AdminScheme, "/Admin/Account/Login");
                if (refPath.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.SalesScheme, "/Sales/Account/Login");
                if (refPath.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                    refPath.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.WarehouseScheme, "/Warehouse/Account/Login");
                if (refPath.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase))
                    return (AppRoles.ProductionScheme, "/Productions/Account/Login");
            }

            // 3. Cuối cùng dựa vào Role hiện tại trong Cookie
            var role = User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(role))
            {
                return (AppRoles.GetSchemeForRole(role), AppRoles.GetLoginUrlForRole(role));
            }

            return (AppRoles.ProductionScheme, "/Productions/Account/Login");
        }

        [HttpGet("/Account/AccessDenied")]
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
