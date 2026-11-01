using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;

namespace httt.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Challenge();
        }

        var account = await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (account == null)
        {
            return NotFound();
        }

        return View(account);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullname, string phone)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Challenge();
        }

        var account = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == username);

        if (account == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(fullname))
        {
            TempData["ErrorMessage"] = "Họ và tên không được để trống.";
            return RedirectToAction(nameof(Profile));
        }

        account.FullName = fullname.Trim();
        account.Phone = string.IsNullOrWhiteSpace(phone) ? account.Phone : phone.Trim();

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công.";

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.ErrorMessage = "Tên đăng nhập và mật khẩu là bắt buộc.";
            return View();
        }

        var user = await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.ErrorMessage = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên.";
            return View();
        }

        if (user.Role != "Admin" && user.Role != "Manager")
        {
            ViewBag.ErrorMessage = "Tài khoản của bạn không có quyền truy cập quản trị.";
            return View();
        }

        if (user.Password != password)
        {
            ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
