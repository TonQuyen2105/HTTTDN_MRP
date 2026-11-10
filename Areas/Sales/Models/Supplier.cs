using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("supplier")]
public class Supplier
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("supplierid")]
    public int Supplierid { get; set; }

    [Column("suppliername")]
    public string? Suppliername { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("mail")]
    public string? Mail { get; set; }
}
