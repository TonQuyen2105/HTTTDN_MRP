using System.Collections.Generic;
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

    [NotMapped]
    public int Exportreceiptid
    {
        get => ExportReceiptId;
        set => ExportReceiptId = value;
    }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [NotMapped]
    public int? Requestedby
    {
        get => RequestedBy;
        set => RequestedBy = value;
    }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [NotMapped]
    public int? Approvedby
    {
        get => ApprovedBy;
        set => ApprovedBy = value;
    }

    [ForeignKey(nameof(ApprovedBy))]
    public virtual UserAccount? ApprovedByUser { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public DateTime? Createddate
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [Column("expreceiptcode")]
    public string? ExpReceiptCode { get; set; }

    [NotMapped]
    public string? Expreceiptcode
    {
        get => ExpReceiptCode;
        set => ExpReceiptCode = value;
    }

    [NotMapped]
    public string ExportReceiptCode
    {
        get => ExpReceiptCode ?? string.Empty;
        set => ExpReceiptCode = value;
    }

    [Column("orderid")]
    public int? OrderId { get; set; }

    [NotMapped]
    public int? Orderid
    {
        get => OrderId;
        set => OrderId = value;
    }

    [ForeignKey(nameof(OrderId))]
    public virtual SaleOrder? SaleOrder { get; set; }

    [NotMapped]
    public decimal? TotalAmount { get; set; }

    [NotMapped]
    public string? Notes { get; set; }

    public virtual ICollection<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = new List<ProductExportReceiptDetail>();
}
