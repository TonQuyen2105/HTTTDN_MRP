using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("saleorderdetail", Schema = "public")]
public class SaleOrderDetail
{
    [NotMapped]
    public int SaleOrderDetailId { get; set; }

    [Column("orderid")]
    public int OrderId { get; set; }

    [NotMapped]
    public int Orderid
    {
        get => OrderId;
        set => OrderId = value;
    }

    [ForeignKey(nameof(OrderId))]
    public virtual SaleOrder? SaleOrder { get; set; }

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

    [Column("unitprice")]
    public decimal? UnitPrice { get; set; }

    [NotMapped]
    public decimal? Unitprice
    {
        get => UnitPrice;
        set => UnitPrice = value;
    }

    [NotMapped]
    public decimal TotalAmount { get; set; }
}
