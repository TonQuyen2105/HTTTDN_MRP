using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using httt.Constants;
using httt.Data;

namespace httt.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
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
                var connString = _context.Database.GetConnectionString();
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                // 1. Lấy danh sách các bảng trong public schema
                var tables = new List<string>();
                await using (var cmd = new NpgsqlCommand(
                    "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;", conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                // 2. Lấy thông tin cột và số dòng của từng bảng
                var tableDetails = new List<object>();
                foreach (var table in tables)
                {
                    long rowCount = 0;
                    await using (var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\";", conn))
                    {
                        var res = await countCmd.ExecuteScalarAsync();
                        if (res != null && res != DBNull.Value)
                        {
                            rowCount = Convert.ToInt64(res);
                        }
                    }

                    var columns = new List<string>();
                    await using (var colCmd = new NpgsqlCommand(
                        $"SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{table}' ORDER BY ordinal_position;", conn))
                    await using (var colReader = await colCmd.ExecuteReaderAsync())
                    {
                        while (await colReader.ReadAsync())
                        {
                            columns.Add($"{colReader.GetString(0)} ({colReader.GetString(1)})");
                        }
                    }

                    // Lấy tối đa 3 dòng mẫu
                    var sampleRows = new List<Dictionary<string, object?>>();
                    await using (var sampleCmd = new NpgsqlCommand($"SELECT * FROM \"{table}\" LIMIT 3;", conn))
                    await using (var sampleReader = await sampleCmd.ExecuteReaderAsync())
                    {
                        while (await sampleReader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < sampleReader.FieldCount; i++)
                            {
                                row[sampleReader.GetName(i)] = sampleReader.IsDBNull(i) ? null : sampleReader.GetValue(i);
                            }
                            sampleRows.Add(row);
                        }
                    }

                    tableDetails.Add(new
                    {
                        ten_bang = table,
                        so_dong = rowCount,
                        cac_cot = columns,
                        du_lieu_mau = sampleRows
                    });
                }

                // 3. Lấy foreign keys
                var foreignKeys = new List<object>();
                await using (var fkCmd = new NpgsqlCommand(@"
                    SELECT
                        tc.table_name, 
                        kcu.column_name, 
                        ccu.table_name AS foreign_table_name,
                        ccu.column_name AS foreign_column_name 
                    FROM 
                        information_schema.table_constraints AS tc 
                        JOIN information_schema.key_column_usage AS kcu
                          ON tc.constraint_name = kcu.constraint_name
                          AND tc.table_schema = kcu.table_schema
                        JOIN information_schema.constraint_column_usage AS ccu
                          ON ccu.constraint_name = tc.constraint_name
                          AND ccu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_schema = 'public'
                    ORDER BY tc.table_name, kcu.column_name;
                ", conn))
                await using (var fkReader = await fkCmd.ExecuteReaderAsync())
                {
                    while (await fkReader.ReadAsync())
                    {
                        foreignKeys.Add(new
                        {
                            bang = fkReader.GetString(0),
                            cot = fkReader.GetString(1),
                            bang_tham_chieu = fkReader.GetString(2),
                            cot_tham_chieu = fkReader.GetString(3)
                        });
                    }
                }

                return Json(new
                {
                    ket_noi = "Thành công 🚀",
                    tong_so_bang = tables.Count,
                    danh_sach_bang = tableDetails,
                    khoa_ngoai = foreignKeys
                });
            }
            catch (Exception ex)
            {
                return Json(new { ket_noi = "Thất bại", loi = ex.Message });
            }
        }
    }
}
