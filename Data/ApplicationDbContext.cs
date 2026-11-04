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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Khóa chính và quan hệ cho chi tiết xuất kho vật tư
        modelBuilder.Entity<MaterialReceiptDetail>()
            .HasKey(d => new { d.ExpReceiptId, d.IngrId });

        modelBuilder.Entity<MaterialReceiptDetail>()
            .HasOne<MaterialReceipt>()
            .WithMany(m => m.Details)
            .HasForeignKey(d => d.ExpReceiptId);

        // Khóa chính và quan hệ cho chi tiết nhập kho thành phẩm
        modelBuilder.Entity<ProductReceiptDetail>()
            .HasKey(d => new { d.ProductReceiptId, d.ProductId });

        modelBuilder.Entity<ProductReceiptDetail>()
            .HasOne<ProductReceipt>()
            .WithMany(m => m.Details)
            .HasForeignKey(d => d.ProductReceiptId);
    }
}
