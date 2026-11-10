using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productexportreceipt")]
public class ProductExportReceipt
{
    [Key]
    [Column("exportreceiptid")]
    public int Exportreceiptid { get; set; }

    [Column("requestedby")]
    public int? Requestedby { get; set; }

    [Column("approvedby")]
    public int? Approvedby { get; set; }

    [Column("createddate")]
    public DateTime? Createddate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("expreceiptcode")]
    public string? Expreceiptcode { get; set; }

    [Column("orderid")]
    public int? Orderid { get; set; }

    [ForeignKey("Orderid")]
    public virtual SaleOrder? SaleOrder { get; set; }

    public virtual ICollection<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = new List<ProductExportReceiptDetail>();
}
