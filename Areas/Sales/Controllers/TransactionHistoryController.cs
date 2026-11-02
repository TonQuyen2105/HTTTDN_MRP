using Microsoft.AspNetCore.Mvc;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    public class TransactionHistoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Sales/Views/TransactionHistory/TransactionHistory.cshtml");
        }
    }
}
