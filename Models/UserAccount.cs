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

    [Required]
    [MaxLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("fullname")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(20)]
    [Column("role")]
    public string? Role { get; set; } = "User";

    [MaxLength(20)]
    [Column("status")]
    public string? Status { get; set; } = "Active";
}
