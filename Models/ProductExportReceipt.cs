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

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public virtual UserAccount? ApprovedByUser { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [Column("expreceiptcode")]
    public string? ExpReceiptCode { get; set; }

    [NotMapped]
    public string ExportReceiptCode
    {
        get => ExpReceiptCode ?? string.Empty;
        set => ExpReceiptCode = value;
    }

    [Column("orderid")]
    public int? OrderId { get; set; }

    [NotMapped]
    public decimal? TotalAmount { get; set; }

    [NotMapped]
    public string? Notes { get; set; }
}
