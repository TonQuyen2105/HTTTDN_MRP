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

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("ix_useraccount_username");

            entity.Property(u => u.Role)
                  .HasDefaultValue("User");

            entity.Property(u => u.Status)
                  .HasDefaultValue("Active");
        });
    }
}
