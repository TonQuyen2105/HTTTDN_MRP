using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productexportreceipt", Schema = "public")]
public class ProductExportReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("exportreceiptid")]
    public int ExportReceiptId { get; set; }

    [Column("exportreceiptcode")]
    public string ExportReceiptCode { get; set; } = string.Empty;

    [Column("createddate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public virtual UserAccount? ApprovedByUser { get; set; }

    [Column("totalamount")]
    public decimal? TotalAmount { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}
