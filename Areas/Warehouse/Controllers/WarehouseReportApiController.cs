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

    /// <summary>
    /// GET /api/warehouse/reports/period-stats : Báo cáo thống kê số lượng tồn, xuất kho, lưu kho theo tháng, quý, năm về vật liệu và sản phẩm
    /// </summary>
    [HttpGet("period-stats")]
    public async Task<IActionResult> GetPeriodStats(
        [FromQuery] string periodType = "month",
        [FromQuery] int? year = null,
        [FromQuery] int? periodValue = null,
        [FromQuery] string itemType = "all",
        [FromQuery] string? search = null)
    {
        var currentYear = year ?? DateTime.Now.Year;
        DateTime startDate;
        DateTime endDate;
        string periodTitle;

        periodType = (periodType ?? "month").ToLower();
        int pVal;

        if (periodType == "quarter")
        {
            pVal = periodValue ?? ((DateTime.Now.Month - 1) / 3 + 1);
            if (pVal < 1 || pVal > 4) pVal = 1;
            int startMonth = (pVal - 1) * 3 + 1;
            int endMonth = startMonth + 2;
            startDate = new DateTime(currentYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            endDate = new DateTime(currentYear, endMonth, DateTime.DaysInMonth(currentYear, endMonth), 23, 59, 59, DateTimeKind.Utc);
            periodTitle = $"Quý {pVal}/{currentYear}";
        }
        else if (periodType == "year")
        {
            pVal = currentYear;
            startDate = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            endDate = new DateTime(currentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            periodTitle = $"Năm {currentYear}";
        }
        else // "month"
        {
            pVal = periodValue ?? DateTime.Now.Month;
            if (pVal < 1 || pVal > 12) pVal = 1;
            startDate = new DateTime(currentYear, pVal, 1, 0, 0, 0, DateTimeKind.Utc);
            endDate = new DateTime(currentYear, pVal, DateTime.DaysInMonth(currentYear, pVal), 23, 59, 59, DateTimeKind.Utc);
            periodTitle = $"Tháng {pVal:D2}/{currentYear}";
        }

        // Lấy danh sách NVL và Sản phẩm
        var ingrs = await _context.Ingredients.AsNoTracking().ToListAsync();
        var prods = await _context.Products.AsNoTracking().ToListAsync();
        var wips = await _context.WipInventories.AsNoTracking().ToListAsync();

        // 1. Giao dịch Nhập kho NVL
        var importReceipts = await _context.ImportReceipts.AsNoTracking()
            .Where(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate)
            .ToListAsync();
        var importReceiptIds = importReceipts.Select(r => r.IpReceiptId).ToList();
        var importDetails = await _context.ImportReceiptDetails.AsNoTracking()
            .Where(d => importReceiptIds.Contains(d.IpReceiptId))
            .ToListAsync();

        // 2. Giao dịch Xuất kho NVL
        var materialReceipts = await _context.MaterialReceipts.AsNoTracking()
            .Where(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate)
            .ToListAsync();
        var materialReceiptIds = materialReceipts.Select(r => r.ExpReceiptId).ToList();
        var materialDetails = await _context.MaterialReceiptDetails.AsNoTracking()
            .Where(d => materialReceiptIds.Contains(d.ExpReceiptId))
            .ToListAsync();

        // 3. Giao dịch Nhập kho Thành phẩm (Lưu kho từ sản xuất)
        var productReceipts = await _context.ProductReceipts.AsNoTracking()
            .Where(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate)
            .ToListAsync();
        var productReceiptIds = productReceipts.Select(r => r.ProductReceiptId).ToList();
        var productReceiptDetails = await _context.ProductReceiptDetails.AsNoTracking()
            .Where(d => productReceiptIds.Contains(d.ProductReceiptId))
            .ToListAsync();

        // 4. Giao dịch Xuất kho Thành phẩm (Xuất bán)
        var exportReceipts = await _context.ProductExportReceipts.AsNoTracking()
            .Where(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate)
            .ToListAsync();
        var exportReceiptIds = exportReceipts.Select(r => r.ExportReceiptId).ToList();
        var exportDetails = await _context.ProductExportReceiptDetails.AsNoTracking()
            .Where(d => exportReceiptIds.Contains(d.ExportReceiptId))
            .ToListAsync();

        // Gom nhóm theo IngrId & ProductId
        var ingrInGroup = importDetails.GroupBy(d => d.IngrId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var ingrOutGroup = materialDetails.GroupBy(d => d.IngrId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var ingrWipGroup = wips.Where(w => w.IngrId.HasValue).GroupBy(w => w.IngrId!.Value).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

        var prodInGroup = productReceiptDetails.GroupBy(d => d.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.Quantity));
        var prodOutGroup = exportDetails.GroupBy(d => d.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.Quantity));
        var prodWipGroup = wips.Where(w => w.ProductId.HasValue).GroupBy(w => w.ProductId!.Value).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

        // Xây dựng danh sách items
        var allItems = new List<object>();

        // Duyệt nguyên vật liệu
        if (itemType != "product")
        {
            foreach (var ingr in ingrs)
            {
                decimal curStock = ingr.Quantity ?? 0;
                decimal inQ = ingrInGroup.GetValueOrDefault(ingr.IngrId, 0);
                decimal outQ = ingrOutGroup.GetValueOrDefault(ingr.IngrId, 0);
                decimal wipQ = ingrWipGroup.GetValueOrDefault(ingr.IngrId, 0);
                decimal openStock = Math.Max(0, curStock - inQ + outQ);
                decimal unitPrice = ingr.Price ?? 0;
                decimal totalVal = curStock * unitPrice;
                string status = curStock == 0 ? "danger" : (curStock <= 100 ? "warning" : "safe");
                string statusLabel = curStock == 0 ? "Hết hàng" : (curStock <= 100 ? "Sắp hết" : "An toàn");

                allItems.Add(new
                {
                    id = ingr.IngrId,
                    code = $"NVL{ingr.IngrId:D3}",
                    name = ingr.IngrName,
                    type = "ingredient",
                    typeLabel = "Nguyên vật liệu",
                    unit = ingr.Unit ?? "Kg",
                    openingStock = openStock,
                    inQty = inQ,
                    outQty = outQ,
                    wipQty = wipQ,
                    closingStock = curStock,
                    unitPrice = unitPrice,
                    totalValue = totalVal,
                    stockStatus = status,
                    stockStatusLabel = statusLabel
                });
            }
        }

        // Duyệt thành phẩm
        if (itemType != "ingredient")
        {
            foreach (var prod in prods)
            {
                decimal curStock = (decimal)(prod.Quantity ?? 0);
                decimal inQ = prodInGroup.GetValueOrDefault(prod.ProductId, 0);
                decimal outQ = prodOutGroup.GetValueOrDefault(prod.ProductId, 0);
                decimal wipQ = prodWipGroup.GetValueOrDefault(prod.ProductId, 0);
                decimal openStock = Math.Max(0, curStock - inQ + outQ);
                decimal unitPrice = prod.Cost ?? 0;
                decimal totalVal = curStock * unitPrice;
                string status = curStock == 0 ? "danger" : (curStock <= 50 ? "warning" : "safe");
                string statusLabel = curStock == 0 ? "Hết hàng" : (curStock <= 50 ? "Sắp hết" : "An toàn");

                allItems.Add(new
                {
                    id = prod.ProductId,
                    code = $"SP{prod.ProductId:D3}",
                    name = prod.ProductName,
                    type = "product",
                    typeLabel = "Thành phẩm",
                    unit = prod.Unit ?? "Hộp",
                    openingStock = openStock,
                    inQty = inQ,
                    outQty = outQ,
                    wipQty = wipQ,
                    closingStock = curStock,
                    unitPrice = unitPrice,
                    totalValue = totalVal,
                    stockStatus = status,
                    stockStatusLabel = statusLabel
                });
            }
        }

        // Lọc tìm kiếm nếu có
        var filteredList = allItems;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            filteredList = allItems.Where(it =>
            {
                dynamic d = it;
                string c = d.code;
                string n = d.name;
                return c.ToLower().Contains(kw) || n.ToLower().Contains(kw);
            }).ToList();
        }

        // Biểu đồ xu hướng (chartData)
        var chartData = new List<object>();
        if (periodType == "year")
        {
            // 12 tháng
            for (int m = 1; m <= 12; m++)
            {
                var mStart = new DateTime(currentYear, m, 1, 0, 0, 0, DateTimeKind.Utc);
                var mEnd = new DateTime(currentYear, m, DateTime.DaysInMonth(currentYear, m), 23, 59, 59, DateTimeKind.Utc);
                
                var inReceiptIds = importReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.IpReceiptId).ToList();
                var inSumNVL = importDetails.Where(d => inReceiptIds.Contains(d.IpReceiptId)).Sum(d => d.Quantity);
                
                var inProdIds = productReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.ProductReceiptId).ToList();
                var inSumProd = productReceiptDetails.Where(d => inProdIds.Contains(d.ProductReceiptId)).Sum(d => (decimal)d.Quantity);

                var outMatIds = materialReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.ExpReceiptId).ToList();
                var outSumNVL = materialDetails.Where(d => outMatIds.Contains(d.ExpReceiptId)).Sum(d => d.Quantity);

                var outProdIds = exportReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.ExportReceiptId).ToList();
                var outSumProd = exportDetails.Where(d => outProdIds.Contains(d.ExportReceiptId)).Sum(d => (decimal)d.Quantity);

                chartData.Add(new
                {
                    label = $"T{m}",
                    inQty = inSumNVL + inSumProd,
                    outQty = outSumNVL + outSumProd
                });
            }
        }
        else if (periodType == "quarter")
        {
            int startMonth = (pVal - 1) * 3 + 1;
            for (int m = startMonth; m <= startMonth + 2; m++)
            {
                var mStart = new DateTime(currentYear, m, 1, 0, 0, 0, DateTimeKind.Utc);
                var mEnd = new DateTime(currentYear, m, DateTime.DaysInMonth(currentYear, m), 23, 59, 59, DateTimeKind.Utc);

                var inReceiptIds = importReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.IpReceiptId).ToList();
                var inSumNVL = importDetails.Where(d => inReceiptIds.Contains(d.IpReceiptId)).Sum(d => d.Quantity);
                
                var inProdIds = productReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.ProductReceiptId).ToList();
                var inSumProd = productReceiptDetails.Where(d => inProdIds.Contains(d.ProductReceiptId)).Sum(d => (decimal)d.Quantity);

                var outMatIds = materialReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.ExpReceiptId).ToList();
                var outSumNVL = materialDetails.Where(d => outMatIds.Contains(d.ExpReceiptId)).Sum(d => d.Quantity);

                var outProdIds = exportReceipts.Where(r => r.CreatedDate >= mStart && r.CreatedDate <= mEnd).Select(r => r.ExportReceiptId).ToList();
                var outSumProd = exportDetails.Where(d => outProdIds.Contains(d.ExportReceiptId)).Sum(d => (decimal)d.Quantity);

                chartData.Add(new
                {
                    label = $"Tháng {m}",
                    inQty = inSumNVL + inSumProd,
                    outQty = outSumNVL + outSumProd
                });
            }
        }
        else // month
        {
            // 4 tuần
            int days = DateTime.DaysInMonth(currentYear, pVal);
            int[][] weekRanges = new int[][]
            {
                new int[] { 1, 7 },
                new int[] { 8, 14 },
                new int[] { 15, 21 },
                new int[] { 22, days }
            };

            for (int w = 0; w < weekRanges.Length; w++)
            {
                var wStart = new DateTime(currentYear, pVal, weekRanges[w][0], 0, 0, 0, DateTimeKind.Utc);
                var wEnd = new DateTime(currentYear, pVal, weekRanges[w][1], 23, 59, 59, DateTimeKind.Utc);

                var inReceiptIds = importReceipts.Where(r => r.CreatedDate >= wStart && r.CreatedDate <= wEnd).Select(r => r.IpReceiptId).ToList();
                var inSumNVL = importDetails.Where(d => inReceiptIds.Contains(d.IpReceiptId)).Sum(d => d.Quantity);
                
                var inProdIds = productReceipts.Where(r => r.CreatedDate >= wStart && r.CreatedDate <= wEnd).Select(r => r.ProductReceiptId).ToList();
                var inSumProd = productReceiptDetails.Where(d => inProdIds.Contains(d.ProductReceiptId)).Sum(d => (decimal)d.Quantity);

                var outMatIds = materialReceipts.Where(r => r.CreatedDate >= wStart && r.CreatedDate <= wEnd).Select(r => r.ExpReceiptId).ToList();
                var outSumNVL = materialDetails.Where(d => outMatIds.Contains(d.ExpReceiptId)).Sum(d => d.Quantity);

                var outProdIds = exportReceipts.Where(r => r.CreatedDate >= wStart && r.CreatedDate <= wEnd).Select(r => r.ExportReceiptId).ToList();
                var outSumProd = exportDetails.Where(d => outProdIds.Contains(d.ExportReceiptId)).Sum(d => (decimal)d.Quantity);

                chartData.Add(new
                {
                    label = $"Tuần {w + 1}",
                    inQty = inSumNVL + inSumProd,
                    outQty = outSumNVL + outSumProd
                });
            }
        }

        // Tổng kết KPI
        decimal totalStockQty = 0;
        decimal totalInQty = 0;
        decimal totalOutQty = 0;
        decimal totalWipQty = 0;
        decimal totalStockValue = 0;
        int lowStockCount = 0;
        int ingrCount = 0;
        int prodCount = 0;

        foreach (dynamic it in filteredList)
        {
            totalStockQty += (decimal)it.closingStock;
            totalInQty += (decimal)it.inQty;
            totalOutQty += (decimal)it.outQty;
            totalWipQty += (decimal)it.wipQty;
            totalStockValue += (decimal)it.totalValue;
            if (it.stockStatus != "safe") lowStockCount++;
            if (it.type == "ingredient") ingrCount++;
            if (it.type == "product") prodCount++;
        }

        return Ok(new
        {
            period = new
            {
                type = periodType,
                year = currentYear,
                value = pVal,
                title = periodTitle,
                startDate = startDate.ToString("dd/MM/yyyy"),
                endDate = endDate.ToString("dd/MM/yyyy")
            },
            summary = new
            {
                totalItems = filteredList.Count,
                totalStockQty,
                totalInQty,
                totalOutQty,
                totalWipQty,
                totalStockValue,
                lowStockCount,
                ingredientCount = ingrCount,
                productCount = prodCount
            },
            chartData,
            items = filteredList,
            availableYears = new List<int> { currentYear, currentYear - 1, currentYear - 2 }
        });
    }
}

