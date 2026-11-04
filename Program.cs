using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<MaterialRequirementService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Route cho phân hệ Productions với đầy đủ các dạng URL
app.MapAreaControllerRoute(
    name: "productions_full",
    areaName: "Productions",
    pattern: "Productions/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapAreaControllerRoute(
    name: "productions_short",
    areaName: "Productions",
    pattern: "Productions/{action=Index}/{id?}",
    defaults: new { controller = "Home" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route mặc định điều hướng vào Productions / Home
app.MapAreaControllerRoute(
    name: "productions_root",
    areaName: "Productions",
    pattern: "{action=Index}/{id?}",
    defaults: new { controller = "Home" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
