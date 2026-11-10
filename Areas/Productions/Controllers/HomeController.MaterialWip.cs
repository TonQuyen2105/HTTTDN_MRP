using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace httt.Areas.Productions.Controllers;

public partial class HomeController
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetMaterialWipForRequest(int requestId)
    {
        if (requestId <= 0) return BadRequest(new { message = "Chọn yêu cầu sản xuất hợp lệ." });
        try
        {
            if (!await _context.ProductionRequests.AsNoTracking().AnyAsync(r => r.RequestId == requestId))
                return NotFound(new { message = "Không tìm thấy yêu cầu sản xuất." });
            var items = await _context.WipInventories.AsNoTracking()
                .Where(w => w.RequestId == requestId && w.IngrId.HasValue && w.ProductId == null)
                .GroupBy(w => w.IngrId!.Value)
                .Select(g => new { ingredientId = g.Key, quantity = g.Sum(w => w.Quantity ?? 0m) })
                .ToListAsync();
            return Ok(new { requestId, items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không đọc được WIP cho đơn {RequestId}", requestId);
            return StatusCode(503, new { message = "Không tải được tồn WIP. Vui lòng thử lại." });
        }
    }
}
