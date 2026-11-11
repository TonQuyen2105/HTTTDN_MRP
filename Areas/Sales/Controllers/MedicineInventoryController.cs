using httt.Constants;
using httt.Data;
using httt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = "Sales,BanHang")]
    [IgnoreAntiforgeryToken]
    [Route("Sales/MedicineInventory")]
    public class MedicineInventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicineInventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.ProductId)
                .ToListAsync();

            return View("MedicineInventory", products);
        }
    }
}
