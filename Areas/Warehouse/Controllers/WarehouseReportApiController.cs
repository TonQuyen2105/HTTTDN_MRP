using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/reports")]
[Produces("application/json")]
[Tags("8. Báo cáo & Thẻ kho (Warehouse Reports)")]
public class WarehouseReportApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WarehouseReportApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/reports/wip-inventory : Xem Tồn kho tại xưởng (Read-only)
    /// </summary>
    [HttpGet("wip-inventory")]
    public async Task<IActionResult> GetWipInventory()
    {
        var dbList = await _context.WipInventories.AsNoTracking().OrderBy(w => w.WipId).ToListAsync();

        var list = dbList.Select(w => new
        {
            w.WipId,
            w.RequestId,
            w.IngrId,
            w.ProductId,
            quantity = w.Quantity ?? 0,
            lastUpdated = w.LastUpdated.HasValue ? w.LastUpdated.Value.ToString("dd/MM/yyyy HH:mm:ss") : ""
        }).ToList();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/warehouse/reports/low-stock : Lấy danh sách NVL và Thành phẩm sắp hết
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] decimal threshold = 100)
    {
        var dbIngrs = await _context.Ingredients
            .AsNoTracking()
            .Where(i => (i.Quantity ?? 0) <= threshold)
            .ToListAsync();

        var dbProds = await _context.Products
            .AsNoTracking()
            .Where(p => (p.Quantity ?? 0) <= threshold)
            .ToListAsync();

        var lowIngredients = dbIngrs.Select(i => new
        {
            type = "ingredient",
            code = $"NVL{i.IngrId:D3}",
            name = i.IngrName,
            quantity = i.Quantity ?? 0,
            unit = i.Unit,
            threshold
        }).ToList();

        var lowProducts = dbProds.Select(p => new
        {
            type = "product",
            code = $"SP{p.ProductId:D3}",
            name = p.ProductName,
            quantity = p.Quantity ?? 0,
            unit = p.Unit,
            threshold
        }).ToList();

        return Ok(new
        {
            totalLowStockCount = lowIngredients.Count + lowProducts.Count,
            threshold,
            ingredients = lowIngredients,
            products = lowProducts
        });
    }

    /// <summary>
    /// GET /api/warehouse/reports/inventory-summary : Báo cáo Nhập - Xuất - Tồn tổng hợp
    /// </summary>
    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        var ingrs = await _context.Ingredients.AsNoTracking().Select(i => new { i.Quantity, i.Price }).ToListAsync();
        var prods = await _context.Products.AsNoTracking().Select(p => new { p.Quantity, p.Cost }).ToListAsync();
        var totalSupplierCount = await _context.Suppliers.CountAsync();

        var totalIngredientValue = ingrs.Sum(i => (i.Quantity ?? 0) * (i.Price ?? 0));
        var totalProductValue = prods.Sum(p => (decimal)(p.Quantity ?? 0) * (p.Cost ?? 0));

        var pendingImports = await _context.ImportReceipts
            .CountAsync(r => r.Status == "Pending" || r.Status == "Chờ nhập kho");

        var pendingMaterialIssues = await _context.MaterialReceipts
            .CountAsync(m => m.Status == "Pending" || m.Status == "Chờ xuất kho");

        return Ok(new
        {
            summaryDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
            counts = new
            {
                totalIngredients = ingrs.Count,
                totalProducts = prods.Count,
                totalSuppliers = totalSupplierCount,
                pendingImports,
                pendingMaterialIssues
            },
            valuation = new
            {
                totalIngredientValue,
                totalProductValue,
                grandTotal = totalIngredientValue + totalProductValue
            }
        });
    }

    /// <summary>
    /// GET /api/warehouse/reports/stock-card?itemId={id}&itemType={ingredient|product} : Thẻ kho chi tiết
    /// </summary>
    [HttpGet("stock-card")]
    public async Task<IActionResult> GetStockCard([FromQuery] int itemId, [FromQuery] string itemType = "ingredient")
    {
        if (itemType.ToLower() == "ingredient")
        {
            var ingr = await _context.Ingredients.FindAsync(itemId);
            if (ingr == null) return NotFound(new { message = "Không tìm thấy nguyên vật liệu." });

            var importDetails = await _context.ImportReceiptDetails.Where(d => d.IngrId == itemId).ToListAsync();
            var importReceiptIds = importDetails.Select(d => d.IpReceiptId).Distinct().ToList();
            var importReceipts = await _context.ImportReceipts.Where(r => importReceiptIds.Contains(r.IpReceiptId)).ToDictionaryAsync(r => r.IpReceiptId);

            var importHistories = importDetails.Select(d =>
            {
                importReceipts.TryGetValue(d.IpReceiptId, out var r);
                return new
                {
                    transDate = r?.CreatedDate,
                    voucherNo = r?.IpReceiptCode ?? $"PNK{d.IpReceiptId:D5}",
                    transType = "Nhập kho mua hàng",
                    inQty = d.Quantity,
                    outQty = 0m,
                    unit = ingr.Unit,
                    cost = d.Cost ?? 0,
                    status = r?.Status
                };
            }).ToList();

            var exportDetails = await _context.MaterialReceiptDetails.Where(d => d.IngrId == itemId).ToListAsync();
            var exportReceiptIds = exportDetails.Select(d => d.ExpReceiptId).Distinct().ToList();
            var exportReceipts = await _context.MaterialReceipts.Where(r => exportReceiptIds.Contains(r.ExpReceiptId)).ToDictionaryAsync(r => r.ExpReceiptId);

            var exportHistories = exportDetails.Select(d =>
            {
                exportReceipts.TryGetValue(d.ExpReceiptId, out var r);
                return new
                {
                    transDate = r?.CreatedDate,
                    voucherNo = r?.ExpReceiptCode ?? $"PXK{d.ExpReceiptId:D5}",
                    transType = "Xuất kho sản xuất",
                    inQty = 0m,
                    outQty = d.Quantity,
                    unit = ingr.Unit,
                    cost = d.UnitCost ?? 0,
                    status = r?.Status
                };
            }).ToList();

            var merged = importHistories.Concat(exportHistories)
                .OrderBy(t => t.transDate)
                .ToList();

            return Ok(new
            {
                itemCode = $"NVL{ingr.IngrId:D3}",
                itemName = ingr.IngrName,
                currentStock = ingr.Quantity ?? 0,
                unit = ingr.Unit,
                transactions = merged
            });
        }
        else
        {
            var prod = await _context.Products.FindAsync(itemId);
            if (prod == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

            var receiptDetails = await _context.ProductReceiptDetails.Where(d => d.ProductId == itemId).ToListAsync();
            var receiptIds = receiptDetails.Select(d => d.ProductReceiptId).Distinct().ToList();
            var productReceipts = await _context.ProductReceipts.Where(r => receiptIds.Contains(r.ProductReceiptId)).ToDictionaryAsync(r => r.ProductReceiptId);

            var receiptHistories = receiptDetails.Select(d =>
            {
                productReceipts.TryGetValue(d.ProductReceiptId, out var r);
                return new
                {
                    transDate = r?.CreatedDate,
                    voucherNo = r?.ReceiptCode ?? $"PNK_TP{d.ProductReceiptId:D4}",
                    transType = "Nhập thành phẩm từ xưởng",
                    inQty = (decimal)d.Quantity,
                    outQty = 0m,
                    unit = prod.Unit,
                    status = r?.Status
                };
            }).ToList();

            var exportDetails = await _context.ProductExportReceiptDetails.Where(d => d.ProductId == itemId).ToListAsync();
            var exportIds = exportDetails.Select(d => d.ExportReceiptId).Distinct().ToList();
            var exportReceipts = await _context.ProductExportReceipts.Where(r => exportIds.Contains(r.ExportReceiptId)).ToDictionaryAsync(r => r.ExportReceiptId);

            var exportHistories = exportDetails.Select(d =>
            {
                exportReceipts.TryGetValue(d.ExportReceiptId, out var r);
                return new
                {
                    transDate = r?.CreatedDate,
                    voucherNo = r?.ExpReceiptCode ?? $"PXK_BH{d.ExportReceiptId:D4}",
                    transType = "Xuất bán hàng",
                    inQty = 0m,
                    outQty = (decimal)d.Quantity,
                    unit = prod.Unit,
                    status = r?.Status
                };
            }).ToList();

            var merged = receiptHistories.Concat(exportHistories)
                .OrderBy(t => t.transDate)
                .ToList();

            return Ok(new
            {
                itemCode = $"SP{prod.ProductId:D3}",
                itemName = prod.ProductName,
                currentStock = prod.Quantity ?? 0,
                unit = prod.Unit,
                transactions = merged
            });
        }
    }
}

