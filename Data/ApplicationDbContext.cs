using Microsoft.EntityFrameworkCore;
using httt.Models;

namespace httt.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ── Core ─────────────────────────────────────────────────────────────
    public DbSet<UserAccount> UserAccounts { get; set; } = null!;

    // ── MRP ──────────────────────────────────────────────────────────────
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Ingredient> Ingredients { get; set; } = null!;
    public DbSet<ProductionRequest> ProductionRequests { get; set; } = null!;
    public DbSet<ProductReceipt> ProductReceipts { get; set; } = null!;
    public DbSet<ProductReceiptDetail> ProductReceiptDetails { get; set; } = null!;
    public DbSet<WipInventory> WipInventories { get; set; } = null!;
    public DbSet<SaleOrder> SaleOrders { get; set; } = null!;
    public DbSet<SaleOrderDetail> SaleOrderDetails { get; set; } = null!;
    public DbSet<ProductExportReceipt> ProductExportReceipts { get; set; } = null!;
    public DbSet<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("useraccount", "public");
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("ix_useraccount_username");

            entity.Property(u => u.Role)
                  .HasDefaultValue("Employee");

            entity.Property(u => u.Status)
                  .HasDefaultValue("Active");

            entity.HasIndex(u => new { u.Role, u.Status })
                  .HasDatabaseName("ix_useraccount_role_status");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("product", "public");
            entity.HasKey(p => p.ProductId);
            entity.Property(p => p.Quantity).HasColumnType("integer");
            entity.Property(p => p.Cost).HasColumnType("numeric(18,2)");
            entity.Property(p => p.LowStockThreshold).HasColumnType("numeric(18,2)");
            entity.Property(p => p.HighStockThreshold).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredient", "public");
            entity.HasKey(i => i.IngredientId);
            entity.Property(i => i.Quantity).HasColumnType("numeric(18,2)");
            entity.Property(i => i.Price).HasColumnType("numeric(18,2)");
            entity.Property(i => i.LowStockThreshold).HasColumnType("numeric(18,2)");
            entity.Property(i => i.HighStockThreshold).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<ProductionRequest>(entity =>
        {
            entity.ToTable("productionrequest", "public");
            entity.HasKey(r => r.RequestId);
            entity.HasIndex(r => r.Status).HasDatabaseName("ix_productionrequest_status");
            entity.Property(r => r.QuantityRequired).HasColumnType("numeric(18,2)");
            entity.Property(r => r.CreatedDate).HasColumnType("timestamp with time zone");

            entity.HasOne(r => r.AssignedUser)
                  .WithMany()
                  .HasForeignKey(r => r.AssignedTo)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Product)
                  .WithMany()
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductReceipt>(entity =>
        {
            entity.ToTable("productreceipt", "public");
            entity.HasKey(r => r.ReceiptId);
            entity.Property(r => r.TotalAmount).HasColumnType("numeric(18,2)");
            entity.HasIndex(r => r.CreatedDate).HasDatabaseName("ix_productreceipt_createddate");
        });

        modelBuilder.Entity<ProductReceiptDetail>(entity =>
        {
            entity.ToTable("productreceiptdetail", "public");
            entity.HasKey(d => d.ReceiptDetailId);
            entity.Property(d => d.Quantity).HasColumnType("integer");
            entity.Property(d => d.UnitPrice).HasColumnType("numeric(18,2)");

            entity.HasOne(d => d.ProductReceipt)
                  .WithMany()
                  .HasForeignKey(d => d.ReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WipInventory>(entity =>
        {
            entity.ToTable("wip_inventory", "public");
            entity.HasKey(w => w.WipId);
            entity.Property(w => w.Quantity).HasColumnType("numeric(18,2)");
            entity.Property(w => w.UpdatedAt).HasColumnType("timestamp with time zone");

            entity.HasOne(w => w.Product)
                  .WithMany()
                  .HasForeignKey(w => w.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleOrder>(entity =>
        {
            entity.ToTable("saleorder", "public");
            entity.HasKey(o => o.OrderId);
            entity.HasIndex(o => o.OrderDate).HasDatabaseName("ix_saleorder_orderdate");
            entity.HasIndex(o => o.Status).HasDatabaseName("ix_saleorder_status");
            entity.Property(o => o.TotalAmount).HasColumnType("numeric(18,2)");

            entity.HasOne(o => o.UserAccount)
                  .WithMany()
                  .HasForeignKey(o => o.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleOrderDetail>(entity =>
        {
            entity.ToTable("saleorderdetail", "public");
            entity.HasKey(d => d.SaleOrderDetailId);
            entity.Property(d => d.Quantity).HasColumnType("integer");
            entity.Property(d => d.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Property(d => d.TotalAmount).HasColumnType("numeric(18,2)");

            entity.HasOne(d => d.SaleOrder)
                  .WithMany()
                  .HasForeignKey(d => d.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductExportReceipt>(entity =>
        {
            entity.ToTable("productexportreceipt", "public");
            entity.HasKey(r => r.ExportReceiptId);
            entity.Property(r => r.TotalAmount).HasColumnType("numeric(18,2)");
            entity.Property(r => r.CreatedDate).HasColumnType("timestamp with time zone");

            entity.HasOne(r => r.ApprovedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductExportReceiptDetail>(entity =>
        {
            entity.ToTable("productexportreceiptdetail", "public");
            entity.HasKey(d => d.ExportReceiptDetailId);
            entity.Property(d => d.Quantity).HasColumnType("integer");
            entity.Property(d => d.UnitPrice).HasColumnType("numeric(18,2)");

            entity.HasOne(d => d.ProductExportReceipt)
                  .WithMany()
                  .HasForeignKey(d => d.ExportReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
