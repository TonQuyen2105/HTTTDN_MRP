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
            return Redirect("/Sales/Orders");
        }
    }
}
