using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("saleorder", Schema = "public")]
public class SaleOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("orderid")]
    public int OrderId { get; set; }

    [Column("orderdate")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column("totalamount")]
    public decimal TotalAmount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("customerid")]
    public int? CustomerId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}

