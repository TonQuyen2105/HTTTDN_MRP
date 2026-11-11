using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace httt.Models;

/// <summary>
/// Các hằng số chuẩn hóa cho trạng thái tài khoản
/// </summary>
public static class UserAccountStatus
{
    public const string Active   = "Active";
    public const string Inactive = "Inactive";
    public const string Disabled = "Disabled";
    public const string Blocked  = "Blocked";
}

/// <summary>
/// Các vai trò người dùng trong hệ thống
/// </summary>
public static class UserAccountRoles
{
    public const string Admin      = "Admin";
    public const string Manager    = "Manager";
    public const string Employee   = "Employee";
    public const string Production = "Production";
    public const string Warehouse  = "Warehouse";
    public const string Sales      = "Sales";
    public const string Kho        = "Kho";
    public const string BanHang    = "BanHang";
}

[Table("useraccount", Schema = "public")]
public class UserAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("userid")]
    public int UserId { get; set; }

    [NotMapped]
    public int UserID
    {
        get => UserId;
        set => UserId = value;
    }

    [Required]
    [MaxLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    [JsonIgnore] // Ngăn chặn rủi ro lộ hash mật khẩu khi serialize JSON ra ngoài API/Client
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("fullname")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("role")]
    public string Role { get; set; } = UserAccountRoles.Employee;

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = UserAccountStatus.Active;

    [NotMapped]
    public bool IsActive => !string.Equals(Status, UserAccountStatus.Inactive, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, UserAccountStatus.Disabled, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, UserAccountStatus.Blocked, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, "false", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, "0", StringComparison.OrdinalIgnoreCase);
}
