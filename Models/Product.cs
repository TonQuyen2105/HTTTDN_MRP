using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("product", Schema = "public")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required]
    [Column("productname")]
    public string ProductName { get; set; } = string.Empty;

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    public int? ShelfLifeMonths { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    // Required for inventory valuation. The PostgreSQL column is `cost`.
    [Column("cost")]
    public decimal Cost { get; set; }

    [Column("lowstockthreshold")]
    public decimal LowStockThreshold { get; set; }

    [Column("highstockthreshold")]
    public decimal? HighStockThreshold { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Active";
}

