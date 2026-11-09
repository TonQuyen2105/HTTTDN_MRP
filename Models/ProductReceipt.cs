using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productreceipt", Schema = "public")]
public class ProductReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("receiptid")]
    public int ReceiptId { get; set; }

    [Column("receiptcode")]
    public string ReceiptCode { get; set; } = string.Empty;

    [Column("createddate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("supplierid")]
    public int? SupplierId { get; set; }

    [Column("totalamount")]
    public decimal? TotalAmount { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}
