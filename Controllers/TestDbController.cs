
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;

namespace httt.Controllers
{
    [Route("[controller]")]
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
                bool isConnected =
                    await _context.Database.CanConnectAsync();

                if (isConnected)
                {
                    return Content(
                        "Kết nối Supabase (Dự án httt) THÀNH CÔNG!"
                    );
                }

                return Content("Không thể kết nối đến Database.");
            }
            catch (Exception ex)
            {
                return Content("Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpGet("check-orders")]
        public async Task<IActionResult> CheckOrders()
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT orderid, ordercode, status, customername 
                    FROM saleorder 
                    ORDER BY orderid DESC 
                    LIMIT 20;
                ";
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new List<object>();
                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        orderid = reader.GetInt32(0),
                        ordercode = reader.IsDBNull(1) ? null : reader.GetString(1),
                        status = reader.IsDBNull(2) ? null : reader.GetString(2),
                        customername = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
                return Json(results);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        [HttpGet("schema")]
        public async Task<IActionResult> Schema()
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT table_name, column_name, data_type, is_nullable
                    FROM information_schema.columns 
                    WHERE table_schema = 'public'
                    ORDER BY table_name, ordinal_position;
                ";
                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<string>();
                while (await reader.ReadAsync())
                {
                    list.Add($"{reader.GetString(0)}.{reader.GetString(1)} ({reader.GetString(2)}, nullable: {reader.GetString(3)})");
                }
                return Json(list);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }
    }
}
