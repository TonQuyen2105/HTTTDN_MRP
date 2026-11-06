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
    public DbSet<ProductExportReceipt> ProductExportReceipts { get; set; } = null!;
    public DbSet<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = null!;
    public DbSet<SaleOrder> SaleOrders { get; set; } = null!;
    public DbSet<SaleOrderDetail> SaleOrderDetails { get; set; } = null!;
    public DbSet<WipInventory> WipInventories { get; set; } = null!;

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

        // Cấu hình bảng productionrequest và foreign key assignedto
        modelBuilder.Entity<ProductionRequest>(entity =>
        {
            entity.HasOne(p => p.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(p => p.AssignedTo)
                  .HasConstraintName("fk_productionrequest_assignedto")
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Manager)
                  .WithMany()
                  .HasForeignKey(p => p.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Product)
                  .WithMany()
                  .HasForeignKey(p => p.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Khóa chính phức hợp (Composite Primary Keys) cho các bảng chi tiết
        modelBuilder.Entity<ImportReceiptDetail>()
            .HasKey(d => new { d.IpReceiptId, d.IngrId });

        modelBuilder.Entity<MaterialReceiptDetail>()
            .HasKey(d => new { d.ExpReceiptId, d.IngrId });

        modelBuilder.Entity<ProductReceiptDetail>()
            .HasKey(d => new { d.ProductReceiptId, d.ProductId });

        modelBuilder.Entity<ProductExportReceiptDetail>()
            .HasKey(d => new { d.ExportReceiptId, d.ProductId });

        modelBuilder.Entity<SaleOrderDetail>()
            .HasKey(d => new { d.OrderId, d.ProductId });
    }
}
