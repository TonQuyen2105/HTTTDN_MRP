using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using httt.Constants;

namespace httt.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity?.Name ?? "Admin";
            ViewBag.FullName = User.FindFirst("FullName")?.Value ?? ViewBag.UserName;
            ViewBag.Role = AppRoles.Admin;
            ViewBag.RoleDisplayName = AppRoles.GetDisplayName(AppRoles.Admin);
            return View();
        }
    }
}
