using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using httt.Constants;

namespace httt.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Warehouse}")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity?.Name ?? "Thủ kho";
            ViewBag.FullName = User.FindFirst("FullName")?.Value ?? ViewBag.UserName;
            ViewBag.Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? AppRoles.Warehouse;
            ViewBag.RoleDisplayName = AppRoles.GetDisplayName(ViewBag.Role);
            return View();
        }
    }
}
