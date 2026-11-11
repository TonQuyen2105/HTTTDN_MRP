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

    [Column("createdby")]
    public int? CreatedBy { get; set; }

    [MaxLength(150)]
    [Column("customername")]
    public string? CustomerName { get; set; }

    [MaxLength(20)]
    [Column("customerphone")]
    public string? CustomerPhone { get; set; }

    [MaxLength(255)]
    [Column("shippingaddress")]
    public string? ShippingAddress { get; set; }

    [Column("totalamount")]
    public decimal? TotalAmount { get; set; }

    [Column("orderdate")]
    public DateTime? OrderDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [MaxLength(50)]
    [Column("ordercode")]
    public string? OrderCode { get; set; }

    [NotMapped]
    public int? ApprovedBy { get; set; }

    [NotMapped]
    public virtual UserAccount? UserAccount { get; set; }

    [NotMapped]
    public int? CustomerId { get; set; }

    [NotMapped]
    public string? Notes { get; set; }
}
