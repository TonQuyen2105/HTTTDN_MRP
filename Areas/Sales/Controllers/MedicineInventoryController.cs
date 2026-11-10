using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using httt.Data;
using httt.Models;

namespace httt.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Route("Sales/MedicineInventory")]
    public class MedicineInventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public MedicineInventoryController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            if (!_cache.TryGetValue("ALL_PRODUCTS_CACHE", out List<Product>? products) || products == null)
            {
                products = await _context.Products
                    .AsNoTracking()
                    .OrderBy(p => p.Productid)
                    .ToListAsync();
                _cache.Set("ALL_PRODUCTS_CACHE", products, TimeSpan.FromMinutes(10));
            }

            return View("~/Areas/Sales/Views/MedicineInventory/MedicineInventory.cshtml", products);
        }

        [HttpGet("GetProductsJson")]
        public async Task<IActionResult> GetProductsJson()
        {
            if (!_cache.TryGetValue("ALL_PRODUCTS_CACHE", out List<Product>? products) || products == null)
            {
                products = await _context.Products
                    .AsNoTracking()
                    .OrderBy(p => p.Productid)
                    .ToListAsync();
                _cache.Set("ALL_PRODUCTS_CACHE", products, TimeSpan.FromMinutes(10));
            }

            return Json(products);
        }
    }
}