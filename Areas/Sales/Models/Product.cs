using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models
{
    [Table("product", Schema = "public")]
    public class Product
    {
        [Key]
        [Column("productid")]
        public int Productid { get; set; }

        [Column("productname")]
        [Required]
        public string Productname { get; set; } = string.Empty;

        [Column("quantity")]
        public int? Quantity { get; set; }

        [Column("unit")]
        public string? Unit { get; set; }

        [Column("cost")]
        public decimal? Cost { get; set; }

        [Column("imageurl")]
        public string? Imageurl { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("shelflifemonths")]
        public int? Shelflifemonths { get; set; }

        [Column("materialcost")]
        public decimal? Materialcost { get; set; }

        [InverseProperty("Product")]
        public virtual ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();

        [InverseProperty("Product")]
        public virtual ICollection<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = new List<ProductExportReceiptDetail>();
    }
}