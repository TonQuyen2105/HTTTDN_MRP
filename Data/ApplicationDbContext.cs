using Microsoft.EntityFrameworkCore;
using httt.Models;
using httt.Areas.Warehouse.Models;

namespace httt.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Ingredient> Ingredients { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Bom> Boms { get; set; } = null!;
    public DbSet<ProductionRequest> ProductionRequests { get; set; } = null!;
    public DbSet<ImportReceipt> ImportReceipts { get; set; } = null!;
    public DbSet<ImportReceiptDetail> ImportReceiptDetails { get; set; } = null!;
    public DbSet<MaterialReceipt> MaterialReceipts { get; set; } = null!;
    public DbSet<MaterialReceiptDetail> MaterialReceiptDetails { get; set; } = null!;
    public DbSet<ProductReceipt> ProductReceipts { get; set; } = null!;
    public DbSet<ProductReceiptDetail> ProductReceiptDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình bảng useraccount
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("useraccount_username_key");

            entity.Property(u => u.Status)
                  .HasDefaultValue("Active");
        });

        // Khóa chính phức hợp (Composite Primary Keys) cho các bảng chi tiết phiếu kho
        modelBuilder.Entity<ImportReceiptDetail>()
            .HasKey(d => new { d.IpReceiptId, d.IngrId });

        modelBuilder.Entity<MaterialReceiptDetail>()
            .HasKey(d => new { d.ExpReceiptId, d.IngrId });

        modelBuilder.Entity<ProductReceiptDetail>()
            .HasKey(d => new { d.ProductReceiptId, d.ProductId });
    }
}
