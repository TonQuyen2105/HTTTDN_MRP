using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productexportreceiptdetail", Schema = "public")]
public class ProductExportReceiptDetail
{
    [NotMapped]
    public int ExportReceiptDetailId { get; set; }

    [Column("exportreceiptid")]
    public int ExportReceiptId { get; set; }

    [NotMapped]
    public int Exportreceiptid
    {
        get => ExportReceiptId;
        set => ExportReceiptId = value;
    }

    [ForeignKey(nameof(ExportReceiptId))]
    public virtual ProductExportReceipt? ProductExportReceipt { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [NotMapped]
    public int Productid
    {
        get => ProductId;
        set => ProductId = value;
    }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [NotMapped]
    public decimal UnitPrice
    {
        get => Price ?? 0m;
        set => Price = value;
    }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [NotMapped]
    public DateTime? Createddate
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }
}
