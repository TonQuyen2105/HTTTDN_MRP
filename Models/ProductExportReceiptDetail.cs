using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productexportreceiptdetail", Schema = "public")]
public class ProductExportReceiptDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("exportreceiptdetailid")]
    public int ExportReceiptDetailId { get; set; }

    [Column("exportreceiptid")]
    public int ExportReceiptId { get; set; }

    [ForeignKey(nameof(ExportReceiptId))]
    public virtual ProductExportReceipt? ProductExportReceipt { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unitprice")]
    public decimal UnitPrice { get; set; }
}
