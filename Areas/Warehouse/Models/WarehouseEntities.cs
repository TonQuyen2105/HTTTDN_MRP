using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Areas.Warehouse.Models;

[Table("importreceipt", Schema = "public")]
public class ImportReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [Column("supplierid")]
    public int? SupplierId { get; set; }

    [Column("createdby")]
    public int? CreatedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("ipreceiptcode")]
    public string? IpReceiptCode { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }
}

[Table("importreceiptdetail", Schema = "public")]
public class ImportReceiptDetail
{
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }
}
