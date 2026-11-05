using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productionrequest", Schema = "public")]
public class ProductionRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("requestdate")]
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}

