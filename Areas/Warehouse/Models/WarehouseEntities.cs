using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using httt.Models;

namespace httt.Areas.Warehouse.Models;

[Table("importreceipt", Schema = "public")]
public class ImportReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [NotMapped]
    public int Ipreceiptid
    {
        get => IpReceiptId;
        set => IpReceiptId = value;
    }

    [Column("supplierid")]
    public int? SupplierId { get; set; }

    [NotMapped]
    public int? Supplierid
    {
        get => SupplierId;
        set => SupplierId = value;
    }

    [Column("createdby")]
    public int? CreatedBy { get; set; }

    [NotMapped]
    public int? Createdby
    {
        get => CreatedBy;
        set => CreatedBy = value;
    }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [NotMapped]
    public DateTime? Createddate
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [Column("ipreceiptcode")]
    public string? IpReceiptCode { get; set; }

    [NotMapped]
    public string? Ipreceiptcode
    {
        get => IpReceiptCode;
        set => IpReceiptCode = value;
    }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
}

[Table("importreceiptdetail", Schema = "public")]
public class ImportReceiptDetail
{
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [NotMapped]
    public int Ipreceiptid
    {
        get => IpReceiptId;
        set => IpReceiptId = value;
    }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [NotMapped]
    public int Ingrid
    {
        get => IngrId;
        set => IngrId = value;
    }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [NotMapped]
    public DateTime? Createddate
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }

    [NotMapped]
    public DateTime? Expireddate
    {
        get => ExpiredDate;
        set => ExpiredDate = value;
    }

    [ForeignKey(nameof(IpReceiptId))]
    public virtual ImportReceipt? ImportReceipt { get; set; }
}
