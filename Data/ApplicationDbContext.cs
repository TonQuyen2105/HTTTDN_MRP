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
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Ingredient> Ingredients { get; set; } = null!;
    public DbSet<Bom> Boms { get; set; } = null!;
    public DbSet<ProductionRequest> ProductionRequests { get; set; } = null!;
    public DbSet<MaterialReceipt> MaterialReceipts { get; set; } = null!;
    public DbSet<MaterialReceiptDetail> MaterialReceiptDetails { get; set; } = null!;
    public DbSet<ProductReceipt> ProductReceipts { get; set; } = null!;
    public DbSet<ProductReceiptDetail> ProductReceiptDetails { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<ProductionLog> ProductionLogs { get; set; } = null!;
    public DbSet<WipInventory> WipInventories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Always address the real tables, regardless of pooled connection search_path
        // or session-local temporary tables with the same names. This changes SQL
        // mapping only; it does not create or alter database objects.
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<ProductionLog>(entity =>
        {
            entity.HasIndex(l => l.OperationKey).IsUnique();
            entity.HasOne(l => l.ProductionRequest).WithMany().HasForeignKey(l => l.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.Creator).WithMany().HasForeignKey(l => l.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // Khóa chính và quan hệ cho chi tiết xuất kho vật tư
        modelBuilder.Entity<MaterialReceiptDetail>(entity =>
        {
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

        // Khóa chính và quan hệ cho chi tiết nhập kho thành phẩm
        modelBuilder.Entity<ProductReceiptDetail>(entity =>
        {
            entity.HasKey(d => new { d.ProductReceiptId, d.ProductId });

            entity.HasOne<ProductReceipt>()
                .WithMany(m => m.Details)
                .HasForeignKey(d => d.ProductReceiptId);

            entity.Property(d => d.Note)
                .HasMaxLength(255);

            entity.Property(d => d.Status)
                .HasMaxLength(50);
        });

        // Định mức BOM
        modelBuilder.Entity<Bom>(entity =>
        {
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

        // Nguyên vật liệu
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(i => i.IngrName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(i => i.Quantity)
                .HasColumnType("numeric");

            entity.Property(i => i.Price)
                .HasPrecision(18, 2);

            entity.Property(i => i.Unit)
                .HasMaxLength(20);

            entity.Property(i => i.ImageUrl)
                .HasMaxLength(255);
        });

        // Thành phẩm
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.ProductName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Cost)
                .HasPrecision(18, 2);

            entity.Property(p => p.Unit)
                .HasMaxLength(20);

            entity.Property(p => p.ImageUrl)
                .HasMaxLength(255);
        });

        // Yêu cầu sản xuất
        modelBuilder.Entity<ProductionRequest>(entity =>
        {
            entity.Property(r => r.Status)
                .HasMaxLength(50);
        });

        // Phiếu xuất kho vật tư
        modelBuilder.Entity<MaterialReceipt>(entity =>
        {
            entity.Property(m => m.Status)
                .HasMaxLength(50);

            entity.Property(m => m.ExpReceiptCode)
                .HasMaxLength(50);
        });

        // Phiếu nhập kho thành phẩm
        modelBuilder.Entity<ProductReceipt>(entity =>
        {
            entity.Property(p => p.Status)
                .HasMaxLength(50);
        });

        // Nhà cung cấp
        modelBuilder.Entity<Supplier>(entity =>
        {
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

        // Tài khoản người dùng
        modelBuilder.Entity<UserAccount>(entity =>
        {
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
                .HasMaxLength(20);

            entity.Property(u => u.Status)
                .HasMaxLength(20);
        });

        // Bảng kho sản xuất (WIP Inventory)
        modelBuilder.Entity<WipInventory>(entity =>
        {
            entity.ToTable("wip_inventory");
            entity.HasKey(w => w.WipId);
            entity.Property(w => w.WipId).HasColumnName("wipid");
            entity.Property(w => w.RequestId).HasColumnName("requestid").IsRequired();
            entity.Property(w => w.IngrId).HasColumnName("ingrid");
            entity.Property(w => w.ProductId).HasColumnName("productid");
            entity.Property(w => w.Quantity).HasColumnName("quantity").HasColumnType("numeric");
            entity.Property(w => w.LastUpdated).HasColumnName("lastupdated");

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
    }
}
