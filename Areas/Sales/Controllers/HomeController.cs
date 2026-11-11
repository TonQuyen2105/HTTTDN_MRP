using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using httt.Constants;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Sales}")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity?.Name ?? "Nhân viên Bán hàng";
            ViewBag.FullName = User.FindFirst("FullName")?.Value ?? ViewBag.UserName;
            ViewBag.Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? AppRoles.Sales;
            ViewBag.RoleDisplayName = AppRoles.GetDisplayName(ViewBag.Role);
            return View();
        }
    }
}
