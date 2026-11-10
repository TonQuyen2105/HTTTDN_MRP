using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Models;

namespace httt.Areas.Productions.Controllers;

public partial class HomeController
{
    private static string FormulaKey(Bom b) => string.IsNullOrWhiteSpace(b.BomCode) ? b.BomName : b.BomCode;
    private static string FormulaVersion(IEnumerable<Bom> rows) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows.OrderBy(b => b.BomId)
            .Select(b => new { b.BomId, b.BomCode, b.BomName, b.ProductId, b.IngrId, b.ItemQuantity, b.MaterialQuantity })))));

    public async Task<IActionResult> BomList()
    {
        try
        {
            var rows = await _context.Boms.AsNoTracking().Include(b => b.Product).OrderBy(b => b.BomId).ToListAsync();
            var formulas = rows.GroupBy(b => new { b.ProductId, Code = FormulaKey(b) }).Select(g => new BomFormulaViewModel
            {
                Id = g.First().BomId, Code = g.Key.Code, Name = g.First().BomName,
                ProductId = g.Key.ProductId, ProductName = g.First().Product?.ProductName ?? "—",
                Unit = g.First().Product?.Unit ?? "", MaterialCount = g.Count(), ProductPrice = g.First().Product?.Cost, ShelfLifeMonths = g.First().Product?.ShelfLifeMonths
            }).ToList();
            return View("~/Areas/Productions/Views/BomList.cshtml", formulas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không tải được danh sách BOM");
            Response.StatusCode = 503;
            ViewBag.LoadError = "Không tải được công thức. Vui lòng tải lại trang.";
            return View("~/Areas/Productions/Views/BomList.cshtml", new List<BomFormulaViewModel>());
        }
    }

    private async Task LoadBomCatalogs(BomEditorViewModel model)
    {
        model.Products = await _context.Products.AsNoTracking().OrderBy(p => p.ProductName).ThenBy(p => p.ProductId).ToListAsync();
        model.Ingredients = await _context.Ingredients.AsNoTracking().OrderBy(i => i.IngrName).ThenBy(i => i.IngrId).ToListAsync();
    }

    public async Task<IActionResult> BomCreate()
    {
        var model = new BomEditorViewModel();
        try { await LoadBomCatalogs(model); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không tải được danh mục BOM");
            Response.StatusCode = 503;
            model.Error = "Không tải được danh mục sản phẩm và nguyên liệu. Vui lòng tải lại trang.";
        }
        return View("~/Areas/Productions/Views/BomCreate.cshtml", model);
    }

    public Task<IActionResult> BomDetail(int? id) => LoadBomPage(id, true);

    public Task<IActionResult> BomEdit(int? id) => LoadBomPage(id, false);

    private async Task<IActionResult> LoadBomPage(int? id, bool isReadOnly)
    {
        var model = new BomEditorViewModel { Id = id, IsReadOnly = isReadOnly };
        try
        {
            var anchor = await _context.Boms.AsNoTracking().FirstOrDefaultAsync(b => b.BomId == id);
            if (anchor == null)
            {
                Response.StatusCode = 404;
                model.Error = "Không tìm thấy công thức.";
            }
            else
            {
                var productRows = await _context.Boms.AsNoTracking().Where(b => b.ProductId == anchor.ProductId).OrderBy(b => b.BomId).ToListAsync();
                var rows = productRows.Where(b => FormulaKey(b) == FormulaKey(anchor)).ToList();
                model.Id = rows.First().BomId;
                model.Version = FormulaVersion(rows);
                model.Code = anchor.BomCode ?? "";
                model.Name = anchor.BomName;
                model.ProductId = anchor.ProductId;
                if (rows.Any(b => b.ItemQuantity <= 0 || b.MaterialQuantity <= 0))
                    model.Error = "BOM có định mức không hợp lệ. Cần kiểm tra dữ liệu gốc trước khi quy đổi về 1 thành phẩm.";
                else
                    model.Lines = rows.Select(b => new BomEditorLine { IngredientId = b.IngrId, Quantity = b.MaterialQuantity / b.ItemQuantity }).ToList();
                await LoadBomCatalogs(model);
                model.Description = model.Products.FirstOrDefault(p => p.ProductId == model.ProductId)?.Description;
                model.ShelfLifeMonths = model.Products.FirstOrDefault(p => p.ProductId == model.ProductId)?.ShelfLifeMonths;
                model.ProductPrice = model.Products.FirstOrDefault(p => p.ProductId == model.ProductId)?.Cost;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không tải được công thức {Id}", id);
            Response.StatusCode = 503;
            model.Error = "Không tải được công thức. Vui lòng tải lại trang.";
        }
        return View("~/Areas/Productions/Views/BomEdit.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBom([FromBody] SaveBomRequest? req)
    {
        if (req == null || !ModelState.IsValid || req.ProductId <= 0 || req.Id is <= 0
            || string.IsNullOrWhiteSpace(req.Code) || req.Code.Length > 100
            || string.IsNullOrWhiteSpace(req.Name) || req.Name.Length > 100
            || req.Lines == null || req.Lines.Count == 0 || req.Lines.Count > 200
            || req.Lines.Any(l => l == null || l.IngredientId <= 0 || l.Quantity <= 0 || l.Quantity > 1000000000m))
            return BadRequest(new { success = false, message = "Nhập mã, tên công thức, sản phẩm và ít nhất một nguyên liệu có định mức dương hợp lệ." });
        if (req.Lines.Select(l => l.IngredientId).Distinct().Count() != req.Lines.Count)
            return BadRequest(new { success = false, message = "Mỗi nguyên liệu chỉ được xuất hiện một lần trong công thức." });
        if (req.ProductPrice.HasValue && (req.ProductPrice < 0 || req.ProductPrice > 9999999999999999.99m || decimal.Round(req.ProductPrice.Value, 2) != req.ProductPrice.Value))
            return BadRequest(new { success = false, message = "Giá thành phẩm phải không âm, tối đa 2 chữ số thập phân và nằm trong giới hạn cho phép." });
        if (req.ShelfLifeMonths is <= 0)
            return BadRequest(new { success = false, message = "Thời hạn sử dụng phải là số tháng nguyên lớn hơn 0." });
        req.Code = req.Code.Trim();
        req.Name = req.Name.Trim();
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            // The existing row-per-ingredient schema has no unique formula key.
            // Serialize BOM writes so concurrent creates cannot introduce duplicate formulas.
            await _context.Database.ExecuteSqlRawAsync("LOCK TABLE public.bom IN SHARE ROW EXCLUSIVE MODE");
            var all = await _context.Boms.OrderBy(b => b.BomId).ToListAsync();
            var current = new List<Bom>();
            if (req.Id.HasValue)
            {
                var anchor = all.FirstOrDefault(b => b.BomId == req.Id.Value);
                if (anchor == null) return NotFound(new { success = false, message = "Công thức không còn tồn tại. Vui lòng tải lại danh sách." });
                current = all.Where(b => b.ProductId == anchor.ProductId && FormulaKey(b) == FormulaKey(anchor)).ToList();
                if (req.Version != FormulaVersion(current))
                    return Conflict(new { success = false, message = "Công thức đã được người khác thay đổi. Vui lòng tải lại trang trước khi sửa tiếp." });
                if (req.ProductId != anchor.ProductId)
                    return BadRequest(new { success = false, message = "Không được đổi sản phẩm của công thức đã tạo." });
            }
            var others = all.Except(current).ToList();
            if (others.Any(b => string.Equals(FormulaKey(b), req.Code, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { success = false, message = "Mã công thức đã được sử dụng. Vui lòng chọn mã khác." });
            if (!req.Id.HasValue && others.Any(b => b.ProductId == req.ProductId))
                return Conflict(new { success = false, message = "Sản phẩm đã có công thức. Hãy mở công thức hiện có để chỉnh sửa." });
            var products = await _context.Products.FromSqlInterpolated($"SELECT * FROM public.product WHERE productid = {req.ProductId} FOR UPDATE").ToListAsync();
            var product = products.SingleOrDefault();
            if (product == null || string.IsNullOrWhiteSpace(product.Unit))
                return BadRequest(new { success = false, message = "Sản phẩm không tồn tại hoặc chưa có đơn vị tính." });
            var ids = req.Lines.Select(l => l.IngredientId).ToList();
            if (await _context.Ingredients.CountAsync(i => ids.Contains(i.IngrId) && i.Unit != null && i.Unit.Trim() != "") != ids.Count)
                return BadRequest(new { success = false, message = "Nguyên liệu không tồn tại hoặc chưa có đơn vị tính. Vui lòng kiểm tra danh mục." });
            // A changed price is independent of the BOM material estimate.
            if (req.ProductPrice != req.OriginalProductPrice)
            {
                if (product.Cost != req.OriginalProductPrice)
                    return Conflict(new { success = false, message = "Giá thành phẩm đã được thay đổi ở nơi khác. Vui lòng tải lại trang." });
                product.Cost = req.ProductPrice;
            }
            if (req.ShelfLifeMonths != req.OriginalShelfLifeMonths)
            {
                if (product.ShelfLifeMonths != req.OriginalShelfLifeMonths)
                    return Conflict(new { success = false, message = "Thời hạn sử dụng đã thay đổi ở nơi khác. Vui lòng tải lại trang." });
                product.ShelfLifeMonths = req.ShelfLifeMonths;
            }
            if (req.Description != req.OriginalDescription)
            {
                if (product.Description != req.OriginalDescription)
                    return Conflict(new { success = false, message = "Mô tả sản phẩm đã thay đổi ở nơi khác. Vui lòng tải lại trang." });
                product.Description = req.Description;
            }
            // Keep the first row ID stable for existing links; save the complete group atomically.
            for (int i = 0; i < req.Lines.Count; i++)
            {
                var row = i < current.Count ? current[i] : new Bom();
                row.BomCode = req.Code; row.BomName = req.Name; row.ProductId = req.ProductId;
                row.IngrId = req.Lines[i].IngredientId; row.ItemQuantity = 1; row.MaterialQuantity = req.Lines[i].Quantity;
                if (i >= current.Count) { _context.Boms.Add(row); current.Add(row); }
            }
            _context.Boms.RemoveRange(current.Skip(req.Lines.Count));
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { success = true, message = "Đã lưu đầy đủ công thức cho 1 thành phẩm.", id = current[0].BomId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không lưu được công thức BOM");
            return StatusCode(500, new { success = false, message = "Không lưu được công thức. Vui lòng thử lại; các thay đổi chưa được lưu." });
        }
    }
}
