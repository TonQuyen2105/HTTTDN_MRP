using Microsoft.AspNetCore.Mvc;
using MrpQuanLyThuoc.Api.Models; // Đảm bảo đúng namespace Models của bạn

namespace MrpQuanLyThuoc.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestDbController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestDbController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("check-connection")]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                bool isConnected = await _context.Database.CanConnectAsync();
                if (isConnected)
                {
                    return Ok(new { Message = "Kết nối Supabase (IPv4) THÀNH CÔNG! 🚀" });
                }
                return StatusCode(500, new { Message = "Không thể kết nối đến Database." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}