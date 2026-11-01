using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = new List<UserAccount>();

        try
        {
            users = await _context.UserAccounts
                .AsNoTracking()
                .OrderBy(u => u.UserId)
                .ToListAsync();
        }
        catch
        {
            users = new List<UserAccount>();
        }

        var totalAccounts = users.Count;
        var adminCount = users.Count(u => u.Role == "Admin");
        var managerCount = users.Count(u => u.Role == "Manager");
        var employeeCount = users.Count(u => u.Role == "Employee");
        var activeCount = users.Count(u => u.IsActive);

        ViewBag.TotalAccounts = totalAccounts;
        ViewBag.AdminCount = adminCount;
        ViewBag.ManagerCount = managerCount;
        ViewBag.EmployeeCount = employeeCount;
        ViewBag.ActiveCount = activeCount;

        var roleLabels = new[] { "Admin", "Manager", "Employee" };
        var roleValues = new[] { adminCount, managerCount, employeeCount };

        ViewBag.RoleLabels = JsonSerializer.Serialize(roleLabels);
        ViewBag.RoleValues = JsonSerializer.Serialize(roleValues);
        ViewBag.UserList = users.Take(5).ToList();

        return View();
    }
}
