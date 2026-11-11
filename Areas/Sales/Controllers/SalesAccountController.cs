using System.Security.Claims;
using httt.Constants;
using httt.Data;
using httt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "Sales,BanHang")]
    [Route("Sales/SalesAccount")]
    public class SalesAccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesAccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                UserAccount? user = null;
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out var userId))
                {
                    user = await _context.UserAccounts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);
                }

                if (user == null)
                {
                    var username = User.Identity?.Name;
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        user = await _context.UserAccounts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
                    }
                }

                if (user == null)
                {
                    user = await _context.UserAccounts
                        .AsNoTracking()
                        .OrderBy(u => u.UserId)
                        .FirstOrDefaultAsync();
                }

                user ??= new UserAccount
                {
                    Username = User.Identity?.Name ?? "sales_user",
                    FullName = User.FindFirstValue("FullName") ?? "Nhân viên bán hàng",
                    Role = User.FindFirstValue(ClaimTypes.Role) ?? "Sales",
                    Status = "Hoạt động",
                    Phone = "0987654321"
                };

                return View("SalesAccount", user);
            }
            catch
            {
                var fallback = new UserAccount
                {
                    Username = User.Identity?.Name ?? "sales_user",
                    FullName = User.FindFirstValue("FullName") ?? "Nhân viên bán hàng",
                    Role = User.FindFirstValue(ClaimTypes.Role) ?? "Sales",
                    Status = "Hoạt động",
                    Phone = "0987654321"
                };

                return View("SalesAccount", fallback);
            }
        }
    }
}
