using Microsoft.AspNetCore.Mvc;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    public class MedicineInventoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Sales/Views/MedicineInventory/MedicineInventory.cshtml");
        }
    }
}