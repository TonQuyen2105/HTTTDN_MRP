using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("saleorder")]
public class SaleOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("orderid")]
    public int Orderid { get; set; }

    [Column("createdby")]
    public int? Createdby { get; set; }

    [Column("customername")]
    public string? Customername { get; set; }

    [Column("customerphone")]
    public string? Customerphone { get; set; }

    [Column("shippingaddress")]
    public string? Shippingaddress { get; set; }

    [Column("totalamount")]
    public decimal? Totalamount { get; set; }

    [Column("orderdate")]
    public DateTime? Orderdate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("ordercode")]
    public string? Ordercode { get; set; }

    [ForeignKey("Createdby")]
    public virtual UserAccount? UserAccount { get; set; }

    public virtual ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
    public virtual ICollection<ProductExportReceipt> ProductExportReceipts { get; set; } = new List<ProductExportReceipt>();
}