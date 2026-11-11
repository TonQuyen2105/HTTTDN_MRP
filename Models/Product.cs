using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("product", Schema = "public")]
public class Product
{
    [Column("materialcost", TypeName = "numeric(18,2)")]
    public decimal? MaterialCost { get; set; }

    [NotMapped]
    public decimal? Materialcost
    {
        get => MaterialCost;
        set => MaterialCost = value;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("productid")]
    public int ProductId { get; set; }

    [NotMapped]
    public int Productid
    {
        get => ProductId;
        set => ProductId = value;
    }

    [Required(ErrorMessage = "Tên thành phẩm không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên thành phẩm tối đa 100 ký tự.")]
    [Column("productname")]
    public string ProductName { get; set; } = string.Empty;

    [NotMapped]
    public string Productname
    {
        get => ProductName;
        set => ProductName = value;
    }

    [Column("quantity")]
    public int? Quantity { get; set; }

    [MaxLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    [Column("unit")]
    public string? Unit { get; set; }

    // Required for inventory valuation. The PostgreSQL column is `cost`.
    [Column("cost", TypeName = "numeric(18,2)")]
    public decimal? Cost { get; set; }

    [MaxLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự.")]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [NotMapped]
    public string? Imageurl
    {
        get => ImageUrl;
        set => ImageUrl = value;
    }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn sử dụng phải là số tháng dương.")]
    public int? ShelfLifeMonths { get; set; }

    [NotMapped]
    public int? Shelflifemonths
    {
        get => ShelfLifeMonths;
        set => ShelfLifeMonths = value;
    }

    [Column("lowstockthreshold")]
    public decimal LowStockThreshold { get; set; }

    [Column("highstockthreshold")]
    public decimal? HighStockThreshold { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Active";

    public virtual ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
    public virtual ICollection<ProductExportReceiptDetail> ProductExportReceiptDetails { get; set; } = new List<ProductExportReceiptDetail>();
}
