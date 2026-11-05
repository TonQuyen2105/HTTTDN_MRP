using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using httt.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connStr);
dataSourceBuilder.ConnectionStringBuilder.Pooling = true;
dataSourceBuilder.ConnectionStringBuilder.NoResetOnClose = true;
dataSourceBuilder.ConnectionStringBuilder.Multiplexing = false;
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorCodesToAdd: null)));
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MRP Warehouse & Production Web API",
        Version = "v1",
        Description = "Hệ thống Web API quản lý kho nguyên vật liệu, thành phẩm, nhà cung cấp và báo cáo tồn kho MRP."
    });
    // Chỉ hiển thị các controller API có [ApiController], ẩn các Controller View MVC thông thường
    c.DocInclusionPredicate((docName, apiDesc) =>
        apiDesc.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MRP Warehouse Web API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "MRP Warehouse API Documentation";
});

app.UseStaticFiles();

// Cho phép phục vụ trực tiếp các file HTML, CSS, JS trong thư mục Areas (VD: /Areas/Warehouse/Views/Index.html)
var areasPath = Path.Combine(builder.Environment.ContentRootPath, "Areas");
if (Directory.Exists(areasPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(areasPath),
        RequestPath = "/Areas",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    });
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.Run();
