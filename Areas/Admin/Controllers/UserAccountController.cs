using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;

namespace httt.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class UserAccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserAccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchString, string? roleFilter, string? statusFilter)
    {
        var usersQuery = _context.UserAccounts.AsNoTracking().AsQueryable();

        try
        {
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim();
                usersQuery = usersQuery.Where(u =>
                    u.Username.Contains(keyword) ||
                    u.FullName.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter) && !roleFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var isActive = statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase);
                usersQuery = usersQuery.Where(u =>
                    (isActive && (u.Status == "Active" || u.Status == "true" || u.Status == "1")) ||
                    (!isActive && (u.Status == "Inactive" || u.Status == "false" || u.Status == "0")));
            }

            var users = await usersQuery
                .OrderBy(u => u.UserId)
                .ToListAsync();

            return View(users);
        }
        catch
        {
            return View(new List<UserAccount>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Username,Password,FullName,Phone,Role,Status")] UserAccount user)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            user.Password = "Default@123";
        }

        if (string.IsNullOrWhiteSpace(user.Role))
        {
            user.Role = "Employee";
        }

        if (string.IsNullOrWhiteSpace(user.Username))
        {
            return BadRequest("Username is required.");
        }

        user.Status = "Active";

        try
        {
            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();
        }
        catch
        {
            return StatusCode(500, "Database unavailable. Please ensure PostgreSQL is running.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Username,Password,FullName,Phone,Role,Status")] UserAccount? user)
    {
        try
        {
            if (user == null)
            {
                return BadRequest("User data is required.");
            }

            var existingUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            existingUser.Username = string.IsNullOrWhiteSpace(user.Username) ? existingUser.Username : user.Username.Trim();
            existingUser.FullName = string.IsNullOrWhiteSpace(user.FullName) ? existingUser.FullName : user.FullName.Trim();
            existingUser.Phone = string.IsNullOrWhiteSpace(user.Phone) ? existingUser.Phone : user.Phone.Trim();
            existingUser.Role = string.IsNullOrWhiteSpace(user.Role) ? existingUser.Role : user.Role.Trim();
            existingUser.Status = string.IsNullOrWhiteSpace(user.Status) ? existingUser.Status : user.Status.Trim();

            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                existingUser.Password = user.Password.Trim();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return StatusCode(500, "Database unavailable. Please ensure PostgreSQL is running.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = user.IsActive ? "Inactive" : "Active";
            await _context.SaveChangesAsync();

            return Json(new { success = true, status = user.Status });
        }
        catch
        {
            return Json(new { success = false, message = "Database unavailable. Please ensure PostgreSQL is running." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            _context.UserAccounts.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch
        {
            return Json(new { success = false, message = "Database unavailable. Please ensure PostgreSQL is running." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccount(string? username, string? fullname, string? phone, string? role, string? newpassword)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(fullname))
        {
            return BadRequest("Full name is required.");
        }

        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound();
        }

        var normalizedRole = role?.Trim();
        var allowedRoles = new[] { "Admin", "Manager", "Employee" };
        if (string.IsNullOrWhiteSpace(normalizedRole) || !allowedRoles.Contains(normalizedRole, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid role value.");
        }

        try
        {
            user.FullName = fullname.Trim();
            user.Phone = string.IsNullOrWhiteSpace(phone) ? user.Phone : phone.Trim();
            user.Role = normalizedRole;

            if (!string.IsNullOrWhiteSpace(newpassword))
            {
                user.Password = newpassword.Trim();
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật tài khoản thành công." });
        }
        catch
        {
            return Json(new { success = false, message = "Database unavailable. Please ensure PostgreSQL is running." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(int id, string role)
    {
        var allowedRoles = new[] { "Admin", "Manager", "Employee" };
        if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid role value.");
        }

        try
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            user.Role = role;
            await _context.SaveChangesAsync();

            return Json(new { success = true, role = user.Role });
        }
        catch
        {
            return Json(new { success = false, message = "Database unavailable. Please ensure PostgreSQL is running." });
        }
    }
}
