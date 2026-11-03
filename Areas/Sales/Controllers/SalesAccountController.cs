using Microsoft.AspNetCore.Mvc;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    public class SalesAccountController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Sales/Views/SalesAccount/SalesAccount.cshtml");
        }
    }
}