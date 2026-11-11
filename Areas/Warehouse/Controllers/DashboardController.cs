using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using httt.Constants;

namespace httt.Areas.Warehouse.Controllers;

[Area("Warehouse")]
[Route("Warehouse/[controller]")]
[Authorize(Roles = "Warehouse,Kho")]
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
