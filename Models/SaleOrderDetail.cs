using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("saleorderdetail", Schema = "public")]
public class SaleOrderDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("saleorderdetailid")]
    public int SaleOrderDetailId { get; set; }

    [Column("orderid")]
    public int OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public virtual SaleOrder? SaleOrder { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unitprice")]
    public decimal UnitPrice { get; set; }

    [Column("totalamount")]
    public decimal TotalAmount { get; set; }
}
