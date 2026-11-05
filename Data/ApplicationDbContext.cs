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
    public DbSet<WipInventory> WipInventories { get; set; } = null!;
    public DbSet<SaleOrder> SaleOrders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(entity =>
        {
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

        // Explicit table mappings for MRP entities
        modelBuilder.Entity<Product>().ToTable("product", "public");
        modelBuilder.Entity<Ingredient>().ToTable("ingredient", "public");
        modelBuilder.Entity<ProductionRequest>(entity =>
        {
            entity.ToTable("productionrequest", "public");
            entity.HasIndex(r => r.Status).HasDatabaseName("ix_productionrequest_status");
        });
        modelBuilder.Entity<WipInventory>().ToTable("wip_inventory", "public");
        modelBuilder.Entity<SaleOrder>(entity =>
        {
            entity.ToTable("saleorder", "public");
            entity.HasIndex(o => o.OrderDate).HasDatabaseName("ix_saleorder_orderdate");
            entity.HasIndex(o => o.Status).HasDatabaseName("ix_saleorder_status");
        });
    }
}
