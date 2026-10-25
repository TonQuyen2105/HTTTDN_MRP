using Microsoft.AspNetCore.Mvc;
using httt.Data;

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
    }
}