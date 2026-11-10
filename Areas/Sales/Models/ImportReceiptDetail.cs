using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("importreceiptdetail")]
public class ImportReceiptDetail
{
    [Column("ipreceiptid")]
    public int Ipreceiptid { get; set; }

    [Column("ingrid")]
    public int Ingrid { get; set; }

    [Column("quantity")]
    public decimal? Quantity { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("createddate")]
    public DateTime? Createddate { get; set; }

    [Column("expireddate")]
    public DateTime? Expireddate { get; set; }

    [ForeignKey("Ipreceiptid")]
    public virtual ImportReceipt? ImportReceipt { get; set; }
}
