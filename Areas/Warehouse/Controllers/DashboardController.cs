using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;

namespace httt.Areas.Warehouse.Controllers;

[Area("Warehouse")]
[Route("Warehouse/[controller]")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Warehouse")]
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return Redirect("/Areas/Warehouse/Views/Index.html");
    }

    [HttpGet("DatabaseSummary")]
    public async Task<IActionResult> GetDatabaseSummary()
    {
        var canConnect = await _context.Database.CanConnectAsync();
        if (!canConnect)
        {
            return StatusCode(500, new { connected = false, message = "Không thể kết nối tới Supabase." });
        }

        return Ok(new
        {
            connected = true,
            supabaseProject = "https://uhxginhmlnrikfyineei.supabase.co",
            counts = new
            {
                users = await _context.UserAccounts.CountAsync(),
                suppliers = await _context.Suppliers.CountAsync(),
                ingredients = await _context.Ingredients.CountAsync(),
                products = await _context.Products.CountAsync(),
                boms = await _context.Boms.CountAsync(),
                productionRequests = await _context.ProductionRequests.CountAsync(),
                importReceipts = await _context.ImportReceipts.CountAsync(),
                materialReceipts = await _context.MaterialReceipts.CountAsync(),
                productReceipts = await _context.ProductReceipts.CountAsync()
            },
            ingredients = await _context.Ingredients.OrderBy(i => i.IngrId).Take(20).ToListAsync(),
            products = await _context.Products.OrderBy(p => p.ProductId).Take(20).ToListAsync(),
            suppliers = await _context.Suppliers.OrderBy(s => s.SupplierId).Take(20).ToListAsync(),
            importReceipts = await _context.ImportReceipts.OrderByDescending(r => r.CreatedDate).Take(20).ToListAsync(),
            materialReceipts = await _context.MaterialReceipts.OrderByDescending(r => r.CreatedDate).Take(20).ToListAsync(),
            productReceipts = await _context.ProductReceipts.OrderByDescending(r => r.CreatedDate).Take(20).ToListAsync()
        });
    }
}
