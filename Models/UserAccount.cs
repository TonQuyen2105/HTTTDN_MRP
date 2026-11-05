using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("useraccount")]
public class UserAccount
{
    [Key]
    [Column("userid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserID { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MaxLength(255, ErrorMessage = "Mật khẩu tối đa 255 ký tự.")]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    [Column("fullname")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(20, ErrorMessage = "Vai trò tối đa 20 ký tự.")]
    [Column("role")]
    public string? Role { get; set; } = "Employee";

    [MaxLength(20, ErrorMessage = "Trạng thái tối đa 20 ký tự.")]
    [Column("status")]
    public string? Status { get; set; } = "Active";
}
