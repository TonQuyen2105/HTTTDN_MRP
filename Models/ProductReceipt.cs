using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productreceipt", Schema = "public")]
public class ProductReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("productreceiptid")]
    public int ProductReceiptId { get; set; }

    [NotMapped]
    public int ReceiptId
    {
        get => ProductReceiptId;
        set => ProductReceiptId = value;
    }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50, ErrorMessage = "Trạng thái tối đa 50 ký tự.")]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("requestid")]
    public int? RequestId { get; set; }

    [Column("receiptcode")]
    public string? ReceiptCode { get; set; }

    [Column("supplierid")]
    public int? SupplierId { get; set; }

    [Column("totalamount")]
    public decimal? TotalAmount { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [ForeignKey("RequestedBy")]
    public UserAccount? Requester { get; set; }

    [ForeignKey("ApprovedBy")]
    public UserAccount? Approver { get; set; }

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    public ICollection<ProductReceiptDetail> Details { get; set; } = new List<ProductReceiptDetail>();

    [NotMapped]
    public string DisplayCode => !string.IsNullOrWhiteSpace(ReceiptCode) ? ReceiptCode : $"BG-{ProductReceiptId}";
}
