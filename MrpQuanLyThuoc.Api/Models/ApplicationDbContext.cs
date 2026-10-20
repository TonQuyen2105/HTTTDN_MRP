using Microsoft.EntityFrameworkCore;
using MrpQuanLyThuoc.Api.Models;

namespace MrpQuanLyThuoc.Api.Models;

/// <summary>
/// DbContext chính của ứng dụng, quản lý kết nối và ánh xạ các Entity sang bảng trong PostgreSQL.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // =============================================
    // DbSets — mỗi DbSet tương ứng với một bảng
    // =============================================

    /// <summary>
    /// Bảng tài khoản người dùng.
    /// </summary>
    public DbSet<UserAccount> UserAccounts { get; set; } = null!;

    // =============================================
    // Model Configuration
    // =============================================

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

