using Microsoft.AspNetCore.Mvc;

namespace httt.Areas.Warehouse.Controllers;

[Area("Warehouse")]
[Route("Warehouse/[controller]")]
public class DashboardController : Controller
{
    [HttpGet("/Warehouse")]
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return Redirect("/Areas/Warehouse/Views/Index.html");
    }
}
