using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using httt.Constants;
using httt.Models;

namespace httt.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var targetUrl = AppRoles.GetDefaultUrlForRole(role);
            return Redirect(targetUrl);
        }

        return RedirectToAction("Login", "Account");
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
