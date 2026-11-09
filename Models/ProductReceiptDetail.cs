using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productreceiptdetail", Schema = "public")]
public class ProductReceiptDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("receiptdetailid")]
    public int ReceiptDetailId { get; set; }

    [Column("receiptid")]
    public int ReceiptId { get; set; }

    [ForeignKey(nameof(ReceiptId))]
    public virtual ProductReceipt? ProductReceipt { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unitprice")]
    public decimal? UnitPrice { get; set; }
}
