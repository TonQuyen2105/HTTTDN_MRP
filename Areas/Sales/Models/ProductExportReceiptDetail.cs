using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productexportreceiptdetail")]
public class ProductExportReceiptDetail
{
    [Column("exportreceiptid")]
    public int Exportreceiptid { get; set; }

    [Column("productid")]
    public int Productid { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? Createddate { get; set; }

    [ForeignKey("Exportreceiptid")]
    public virtual ProductExportReceipt? ProductExportReceipt { get; set; }

    [ForeignKey("Productid")]
    public virtual Product? Product { get; set; }
}
