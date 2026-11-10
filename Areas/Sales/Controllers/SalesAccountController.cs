using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using httt.Data;
using httt.Models;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Route("Sales/SalesAccount")]
    public class SalesAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public SalesAccountController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            if (!_cache.TryGetValue("CURRENT_SALES_USER", out UserAccount? user) || user == null)
            {
                user = await _context.UserAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync() ?? new UserAccount
                    {
                        Username = "sales_user",
                        Fullname = "Nhân viên Bán hàng",
                        Role = "Sales",
                        Status = "Hoạt động",
                        Phone = "0987654321"
                    };
                _cache.Set("CURRENT_SALES_USER", user, TimeSpan.FromHours(1));
            }

            return View("~/Areas/Sales/Views/SalesAccount/SalesAccount.cshtml", user);
        }
    }
}
