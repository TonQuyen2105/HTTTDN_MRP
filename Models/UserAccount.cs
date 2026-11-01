using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("useraccount", Schema = "public")]
public class UserAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("userid")]
    public int UserId { get; set; }

    [Required]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Column("fullname")]
    public string FullName { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Required]
    [Column("role")]
    public string Role { get; set; } = "User";

    [Column("status")]
    public string Status { get; set; } = "Active";

    [NotMapped]
    public bool IsActive => !string.Equals(Status, "Inactive", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, "false", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, "0", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Status, "Disabled", StringComparison.OrdinalIgnoreCase);
}
