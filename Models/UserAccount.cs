using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("useraccount")]
public class UserAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("userid")]
    public int Userid { get; set; }

    [Required]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Column("fullname")]
    public string Fullname { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("role")]
    public string? Role { get; set; } = "Employee";

    [Column("status")]
    public string? Status { get; set; } = "Active";
}