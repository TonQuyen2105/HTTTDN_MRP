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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình bảng UserAccounts
        modelBuilder.Entity<UserAccount>(entity =>
        {
            // Đảm bảo Username là duy nhất (unique index)
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("ix_user_accounts_username");

            // Giá trị mặc định cho Role
            entity.Property(u => u.Role)
                  .HasDefaultValue("User");

            // Giá trị mặc định cho Status
            entity.Property(u => u.Status)
                  .HasDefaultValue(true);
        });
    }
}

