using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using httt.Models;

namespace httt.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Tự động điều hướng trang chủ (http://localhost:5147) vào thẳng giao diện Kho hàng
        return Redirect("/Areas/Warehouse/Views/Index.html");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
