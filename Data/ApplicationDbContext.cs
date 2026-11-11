using Microsoft.EntityFrameworkCore;
using httt.Models;

namespace httt.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts { get; set; } = null!;
    public DbSet<SaleOrder> SaleOrders { get; set; } = null!;
    public DbSet<SaleOrderDetail> SaleOrderDetails { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<ImportReceipt> ImportReceipts { get; set; } = null!;
    public DbSet<ImportReceiptDetail> ImportReceiptDetails { get; set; } = null!;
    public DbSet<ProductExportReceipt> ProductExportReceipts { get; set; } = null!;
    public DbSet<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Khóa chính phức hợp (orderid, productid) cho bảng saleorderdetail
        modelBuilder.Entity<SaleOrderDetail>(entity =>
        {
            entity.HasKey(e => new { e.Orderid, e.Productid });

            entity.HasOne(e => e.SaleOrder)
                  .WithMany(o => o.SaleOrderDetails)
                  .HasForeignKey(e => e.Orderid)
                  .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình rõ ràng trỏ tới p.SaleOrderDetails để ngăn EF Core sinh cột Productid1
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.SaleOrderDetails)
                  .HasForeignKey(e => e.Productid)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Khóa chính phức hợp (ipreceiptid, ingrid) cho bảng importreceiptdetail
        modelBuilder.Entity<ImportReceiptDetail>(entity =>
        {
            entity.HasKey(e => new { e.Ipreceiptid, e.Ingrid });

            entity.HasOne(e => e.ImportReceipt)
                  .WithMany(r => r.ImportReceiptDetails)
                  .HasForeignKey(e => e.Ipreceiptid)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Khóa chính phức hợp (exportreceiptid, productid) cho bảng productexportreceiptdetail
        modelBuilder.Entity<ProductExportReceiptDetail>(entity =>
        {
            entity.HasKey(e => new { e.Exportreceiptid, e.Productid });

            entity.HasOne(e => e.ProductExportReceipt)
                  .WithMany(r => r.ProductExportReceiptDetails)
                  .HasForeignKey(e => e.Exportreceiptid)
                  .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình rõ ràng trỏ tới p.ProductExportReceiptDetails
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.ProductExportReceiptDetails)
                  .HasForeignKey(e => e.Productid)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Quan hệ SaleOrder -> ProductExportReceipt (orderid nullable)
        modelBuilder.Entity<ProductExportReceipt>(entity =>
        {
            entity.HasOne(e => e.SaleOrder)
                  .WithMany(o => o.ProductExportReceipts)
                  .HasForeignKey(e => e.Orderid)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình Unique Index cho username của bảng useraccount
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("useraccount_username_key");
        });
    }
}