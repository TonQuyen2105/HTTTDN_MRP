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

    // ── Core & MRP ───────────────────────────────────────────────────────
    public DbSet<UserAccount> UserAccounts { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Ingredient> Ingredients { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Bom> Boms { get; set; } = null!;
    public DbSet<ProductionRequest> ProductionRequests { get; set; } = null!;
    public DbSet<ProductionLog> ProductionLogs { get; set; } = null!;
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

        // Always address the real tables, regardless of pooled connection search_path
        // or session-local temporary tables with the same names.
        modelBuilder.HasDefaultSchema("public");

        // Tài khoản người dùng
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("useraccount", "public");
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("ix_useraccount_username");

            entity.Property(u => u.Username)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(u => u.FullName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(u => u.Password)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(u => u.Phone)
                  .HasMaxLength(20);

            entity.Property(u => u.Role)
                  .HasMaxLength(20)
                  .HasDefaultValue("Employee");

            entity.Property(u => u.Status)
                  .HasMaxLength(20)
                  .HasDefaultValue("Active");

            entity.HasIndex(u => new { u.Role, u.Status })
                  .HasDatabaseName("ix_useraccount_role_status");
        });

        // Thành phẩm
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("product", "public");
            entity.HasKey(p => p.ProductId);

            entity.Property(p => p.ProductName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(p => p.Quantity)
                  .HasColumnType("integer");

            entity.Property(p => p.Cost)
                  .HasColumnType("numeric(18,2)")
                  .HasPrecision(18, 2);

            entity.Property(p => p.MaterialCost)
                  .HasColumnType("numeric(18,2)")
                  .HasPrecision(18, 2);

            entity.Property(p => p.Unit)
                  .HasMaxLength(20);

            entity.Property(p => p.ImageUrl)
                  .HasMaxLength(255);

            entity.Property(p => p.LowStockThreshold)
                  .HasColumnType("numeric(18,2)");

            entity.Property(p => p.HighStockThreshold)
                  .HasColumnType("numeric(18,2)");
        });

        // Nguyên vật liệu
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredient", "public");
            entity.HasKey(i => i.IngrId);

            entity.Property(i => i.IngrName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(i => i.Quantity)
                  .HasColumnType("numeric");

            entity.Property(i => i.Price)
                  .HasColumnType("numeric(18,2)")
                  .HasPrecision(18, 2);

            entity.Property(i => i.Cost)
                  .HasColumnType("numeric(18,2)")
                  .HasPrecision(18, 2);

            entity.Property(i => i.Unit)
                  .HasMaxLength(20);

            entity.Property(i => i.ImageUrl)
                  .HasMaxLength(255);

            entity.Property(i => i.LowStockThreshold)
                  .HasColumnType("numeric(18,2)");

            entity.Property(i => i.HighStockThreshold)
                  .HasColumnType("numeric(18,2)");
        });

        // Định mức BOM
        modelBuilder.Entity<Bom>(entity =>
        {
            entity.ToTable("bom", "public");
            entity.HasKey(b => b.BomId);

            entity.Property(b => b.BomName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(b => b.BomCode)
                  .HasMaxLength(100);

            entity.Property(b => b.MaterialQuantity)
                  .HasColumnType("numeric")
                  .IsRequired();

            entity.Property(b => b.ItemQuantity)
                  .IsRequired();
        });

        // Yêu cầu sản xuất
        modelBuilder.Entity<ProductionRequest>(entity =>
        {
            entity.ToTable("productionrequest", "public");
            entity.HasKey(r => r.RequestId);
            entity.HasIndex(r => r.Status)
                  .HasDatabaseName("ix_productionrequest_status");

            entity.Property(r => r.Status)
                  .HasMaxLength(50);

            entity.HasOne(r => r.AssignedUser)
                  .WithMany()
                  .HasForeignKey(r => r.AssignedTo)
                  .HasConstraintName("fk_productionrequest_assignedto")
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.Manager)
                  .WithMany()
                  .HasForeignKey(r => r.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Product)
                  .WithMany()
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Nhật ký sản xuất
        modelBuilder.Entity<ProductionLog>(entity =>
        {
            entity.ToTable("production_log", "public");
            entity.HasKey(l => l.LogId);
            entity.HasIndex(l => l.OperationKey).IsUnique();
            entity.HasOne(l => l.ProductionRequest).WithMany().HasForeignKey(l => l.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.Creator).WithMany().HasForeignKey(l => l.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // Phiếu nhập mua hàng (ImportReceipt & ImportReceiptDetail)
        modelBuilder.Entity<ImportReceipt>(entity =>
        {
            entity.ToTable("importreceipt", "public");
            entity.HasKey(r => r.IpReceiptId);
            entity.Property(r => r.Status).HasMaxLength(50);
            entity.HasOne(r => r.Supplier)
                  .WithMany(s => s.ImportReceipts)
                  .HasForeignKey(r => r.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ImportReceiptDetail>(entity =>
        {
            entity.ToTable("importreceiptdetail", "public");
            entity.HasKey(d => new { d.IpReceiptId, d.IngrId });
            entity.HasOne(d => d.ImportReceipt)
                  .WithMany(r => r.ImportReceiptDetails)
                  .HasForeignKey(d => d.IpReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Phiếu xuất kho vật tư
        modelBuilder.Entity<MaterialReceipt>(entity =>
        {
            entity.ToTable("materialreceipt", "public");
            entity.HasKey(m => m.ExpReceiptId);

            entity.Property(m => m.Status)
                  .HasMaxLength(50);

            entity.Property(m => m.ExpReceiptCode)
                  .HasMaxLength(50);
        });

        // Khóa chính và quan hệ cho chi tiết xuất kho vật tư
        modelBuilder.Entity<MaterialReceiptDetail>(entity =>
        {
            entity.ToTable("materialreceiptdetail", "public");
            entity.HasKey(d => new { d.ExpReceiptId, d.IngrId });

            entity.HasOne<MaterialReceipt>()
                  .WithMany(m => m.Details)
                  .HasForeignKey(d => d.ExpReceiptId);

            entity.Property(d => d.Quantity)
                  .HasPrecision(18, 4);

            entity.Property(d => d.UnitCost)
                  .HasColumnType("numeric");

            entity.Property(d => d.Note)
                  .HasMaxLength(255);

            entity.Property(d => d.Status)
                  .HasMaxLength(50);
        });

        // Phiếu nhập kho thành phẩm
        modelBuilder.Entity<ProductReceipt>(entity =>
        {
            entity.ToTable("productreceipt", "public");
            entity.HasKey(r => r.ProductReceiptId);

            entity.Property(r => r.Status)
                  .HasMaxLength(50);

            entity.Property(r => r.TotalAmount)
                  .HasColumnType("numeric(18,2)");

            entity.HasIndex(r => r.CreatedDate)
                  .HasDatabaseName("ix_productreceipt_createddate");
        });

        // Khóa chính và quan hệ cho chi tiết nhập kho thành phẩm
        modelBuilder.Entity<ProductReceiptDetail>(entity =>
        {
            entity.ToTable("productreceiptdetail", "public");
            entity.HasKey(d => new { d.ProductReceiptId, d.ProductId });

            entity.Property(d => d.Quantity)
                  .HasColumnType("integer");

            entity.Property(d => d.Cost)
                  .HasColumnType("numeric(18,2)");

            entity.Property(d => d.Note)
                  .HasMaxLength(255);

            entity.Property(d => d.Status)
                  .HasMaxLength(50);

            entity.HasOne(d => d.ProductReceipt)
                  .WithMany(m => m.Details)
                  .HasForeignKey(d => d.ProductReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Nhà cung cấp
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("supplier", "public");
            entity.HasKey(s => s.SupplierId);

            entity.Property(s => s.SupplierName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(s => s.Phone)
                  .HasMaxLength(20);

            entity.Property(s => s.Address)
                  .HasMaxLength(255);

            entity.Property(s => s.Mail)
                  .HasMaxLength(100);
        });

        // Bảng kho sản xuất (WIP Inventory)
        modelBuilder.Entity<WipInventory>(entity =>
        {
            entity.ToTable("wip_inventory", "public");
            entity.HasKey(w => w.WipId);
            entity.Property(w => w.WipId).HasColumnName("wipid");
            entity.Property(w => w.RequestId).HasColumnName("requestid").IsRequired();
            entity.Property(w => w.IngrId).HasColumnName("ingrid");
            entity.Property(w => w.ProductId).HasColumnName("productid");
            entity.Property(w => w.Quantity).HasColumnName("quantity").HasColumnType("numeric");
            entity.Property(w => w.LastUpdated).HasColumnName("lastupdated");
            entity.Property(w => w.UpdatedAt).HasColumnType("timestamp with time zone");

            entity.HasOne(w => w.ProductionRequest)
                  .WithMany()
                  .HasForeignKey(w => w.RequestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.Ingredient)
                  .WithMany()
                  .HasForeignKey(w => w.IngrId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.Product)
                  .WithMany()
                  .HasForeignKey(w => w.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Đơn hàng bán
        modelBuilder.Entity<SaleOrder>(entity =>
        {
            entity.ToTable("saleorder", "public");
            entity.HasKey(o => o.OrderId);
            entity.HasIndex(o => o.OrderDate).HasDatabaseName("ix_saleorder_orderdate");
            entity.HasIndex(o => o.Status).HasDatabaseName("ix_saleorder_status");
            entity.Property(o => o.TotalAmount).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<SaleOrderDetail>(entity =>
        {
            entity.ToTable("saleorderdetail", "public");
            entity.HasKey(d => new { d.OrderId, d.ProductId });
            entity.Property(d => d.Quantity).HasColumnType("integer");
            entity.Property(d => d.UnitPrice).HasColumnType("numeric(18,2)");

            entity.HasOne(d => d.SaleOrder)
                  .WithMany(o => o.SaleOrderDetails)
                  .HasForeignKey(d => d.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.SaleOrderDetails)
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Phiếu xuất kho thành phẩm
        modelBuilder.Entity<ProductExportReceipt>(entity =>
        {
            entity.ToTable("productexportreceipt", "public");
            entity.HasKey(r => r.ExportReceiptId);

            entity.HasOne(r => r.ApprovedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.SaleOrder)
                  .WithMany(o => o.ProductExportReceipts)
                  .HasForeignKey(r => r.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductExportReceiptDetail>(entity =>
        {
            entity.ToTable("productexportreceiptdetail", "public");
            entity.HasKey(d => new { d.ExportReceiptId, d.ProductId });
            entity.Property(d => d.Quantity).HasColumnType("integer");
            entity.Property(d => d.Price).HasColumnType("numeric(18,2)");

            entity.HasOne(d => d.ProductExportReceipt)
                  .WithMany(r => r.ProductExportReceiptDetails)
                  .HasForeignKey(d => d.ExportReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.ProductExportReceiptDetails)
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
