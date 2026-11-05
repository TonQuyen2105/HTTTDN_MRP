using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Areas.Admin.Services;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext context,
        UserAccountService accountService,
        ILogger<AccountController> logger)
    {
        _context = context;
        _accountService = accountService;
        _logger = logger;
    }

    // ── GET: /Admin/Account/Login hoặc /Admin/Login ───────────────────────
    [HttpGet]
    [Route("Admin/Account/Login")]
    [Route("Admin/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Nếu đã đăng nhập với quyền Admin, chuyển hướng thẳng vào Dashboard
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
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

        var normalizedUsername = username.Trim();
        var user = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername.ToLower());

        if (user == null)
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        if (user.Status != "Active" || user.Role != "Admin")
        {
            ViewBag.Error = "Tài khoản không có quyền truy cập phân hệ Admin hoặc đã bị khóa.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        var hasher = new PasswordHasher<UserAccount>();
        var verifyResult = hasher.VerifyHashedPassword(user, user.Password, password);
        var isValidPassword = verifyResult == PasswordVerificationResult.Success || user.Password == password;

        if (!isValidPassword)
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            ViewBag.ErrorMessage = ViewBag.Error;
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    // ── POST: /Admin/Account/Logout hoặc /Admin/Logout ───────────────────
    [HttpPost]
    [Route("Admin/Account/Logout")]
    [Route("Admin/Logout")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
            return Json(new { success = false, message = "Họ và tên không được để trống." });

        try
        {
            var account = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == username);

            if (account == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản." });

            account.FullName = fullname.Trim();
            account.Phone    = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thông tin cá nhân thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for: {Username}", username);
            return Json(new { success = false, message = "Lỗi khi cập nhật cơ sở dữ liệu. Vui lòng thử lại." });
        }
    }
}
