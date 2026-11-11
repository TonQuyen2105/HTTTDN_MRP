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
            return Redirect("/Areas/Warehouse/Views/Index.html");
        }
    }
}
