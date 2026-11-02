using Microsoft.EntityFrameworkCore;
using MrpQuanLyThuoc.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =============================================
// Đăng ký DbContext với PostgreSQL (Supabase)
// Chuỗi kết nối được lấy từ:
//   - Development: dotnet user-secrets (an toàn, không commit lên Git)
//   - Production : biến môi trường hoặc secrets manager
// =============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Controllers (MVC pattern cho Web API)
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.SerializeAsV2 = true;
    });
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controller routes
app.MapControllers();

app.Run();
