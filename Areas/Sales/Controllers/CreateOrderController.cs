using Microsoft.AspNetCore.Mvc;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    public class CreateOrderController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Sales/Views/CreateOrder/CreateOrder.cshtml");
        }
    }
}