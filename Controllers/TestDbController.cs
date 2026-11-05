using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Areas.Warehouse.Models;

namespace httt.Controllers
{
    public class TestDbController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestDbController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("test-db")]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                bool isConnected = await _context.Database.CanConnectAsync();
                if (isConnected)
                {
                    return Content("Kết nối Supabase (Dự án httt) THÀNH CÔNG! 🚀");
                }
                return Content("Không thể kết nối đến Database.");
            }
            catch (Exception ex)
            {
                return Content("Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpPost("api/test/seed-material-issue")]
        [HttpGet("api/test/seed-material-issue")]
        public async Task<IActionResult> SeedMaterialIssue()
        {
            try
            {
                // Lấy 1 User/Manager hợp lệ từ Database
                var manager = await _context.UserAccounts.FirstOrDefaultAsync();
                int managerId = manager?.UserID ?? 5; // Hoặc user hiện có

                // 1. Tạo 1 ProductionRequest mới
                var now = DateTime.UtcNow;
                var prodRequest = new ProductionRequest
                {
                    ManagerId = managerId,
                    ProductId = 29, // Viên nén Paracetamol 500mg
                    QuantityRequired = 1000,
                    Status = "In Progress",
                    CreatedDate = now,
                    RequestCode = $"LSX{now:yyyyMMddHHmmss}"
                };
                _context.ProductionRequests.Add(prodRequest);
                await _context.SaveChangesAsync();

                // 2. Tạo 1 MaterialReceipt chờ xuất kho (Status = "Chờ xuất kho" / "Pending")
                var matReceipt = new MaterialReceipt
                {
                    RequestId = prodRequest.RequestId,
                    RequestedBy = managerId,
                    CreatedDate = now,
                    Status = "Chờ xuất kho",
                    ExpReceiptCode = $"PXK{now:MMddHHmm}"
                };
                _context.MaterialReceipts.Add(matReceipt);
                await _context.SaveChangesAsync();

                // 3. Tạo các dòng chi tiết MaterialReceiptDetail
                var detail1 = new MaterialReceiptDetail
                {
                    ExpReceiptId = matReceipt.ExpReceiptId,
                    IngrId = 41, // Hoạt chất Paracetamol
                    Quantity = 25.0m,
                    Status = "Pending",
                    Note = $"Xuất nguyên liệu Paracetamol cho lệnh sản xuất {prodRequest.RequestCode} - Phân xưởng Viên nén GMP",
                    CreatedDate = now,
                    UnitCost = 185000m
                };

                var detail2 = new MaterialReceiptDetail
                {
                    ExpReceiptId = matReceipt.ExpReceiptId,
                    IngrId = 46, // Tá dược độn Microcrystalline Cellulose
                    Quantity = 15.0m,
                    Status = "Pending",
                    Note = $"Xuất tá dược độn cho lệnh sản xuất {prodRequest.RequestCode} - Phân xưởng Viên nén GMP",
                    CreatedDate = now,
                    UnitCost = 95000m
                };

                _context.MaterialReceiptDetails.AddRange(detail1, detail2);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Đã tạo thành công 1 ProductionRequest và 1 MaterialReceipt kèm chi tiết để test duyệt phiếu xuất!",
                    productionRequest = new
                    {
                        requestId = prodRequest.RequestId,
                        requestCode = prodRequest.RequestCode,
                        productId = prodRequest.ProductId,
                        quantityRequired = prodRequest.QuantityRequired,
                        status = prodRequest.Status
                    },
                    materialReceipt = new
                    {
                        expReceiptId = matReceipt.ExpReceiptId,
                        orderNo = matReceipt.ExpReceiptCode,
                        status = matReceipt.Status,
                        requestId = matReceipt.RequestId,
                        itemsCount = 2,
                        items = new[]
                        {
                            new { ingrId = detail1.IngrId, quantity = detail1.Quantity, note = detail1.Note },
                            new { ingrId = detail2.IngrId, quantity = detail2.Quantity, note = detail2.Note }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}