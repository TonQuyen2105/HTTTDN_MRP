using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models
{
    [Table("saleorderdetail", Schema = "public")]
    public class SaleOrderDetail
    {
        [Column("orderid")]
        public int Orderid { get; set; }

        [Column("productid")]
        public int Productid { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unitprice")]
        public decimal? Unitprice { get; set; }

        // Khóa ngoại trỏ đến SaleOrder
        [ForeignKey("Orderid")]
        public virtual SaleOrder? SaleOrder { get; set; }

        // Khóa ngoại trỏ đến Product
        [ForeignKey("Productid")]
        public virtual Product? Product { get; set; }
    }
}