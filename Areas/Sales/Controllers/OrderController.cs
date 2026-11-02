using Microsoft.AspNetCore.Mvc;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Sales/Views/Order/Orders.cshtml");
        }
    }
}