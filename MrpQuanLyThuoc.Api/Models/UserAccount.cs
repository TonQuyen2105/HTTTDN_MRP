using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MrpQuanLyThuoc.Api.Models;

/// <summary>
/// Đại diện cho một tài khoản người dùng trong hệ thống.
/// </summary>
[Table("user_accounts")]
public class UserAccount
{
    /// <summary>
    /// Khóa chính, tự động tăng.
    /// </summary>
    [Key]
    [Column("user_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserID { get; set; }

    /// <summary>
    /// Tên đăng nhập, bắt buộc và duy nhất.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu (lưu dưới dạng hash, KHÔNG lưu plaintext).
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Họ và tên đầy đủ của người dùng.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Số điện thoại (không bắt buộc).
    /// </summary>
    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Vai trò của người dùng trong hệ thống (ví dụ: "Admin", "User", "Pharmacist").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("role")]
    public string Role { get; set; } = "User";

    /// <summary>
    /// Trạng thái tài khoản: true = đang hoạt động, false = đã vô hiệu hóa.
    /// </summary>
    [Column("status")]
    public bool Status { get; set; } = true;
}

