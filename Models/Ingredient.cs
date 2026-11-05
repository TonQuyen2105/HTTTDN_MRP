using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("ingredient", Schema = "public")]
public class Ingredient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ingredientid")]
    public int IngredientId { get; set; }

    [Required]
    [Column("ingredientname")]
    public string IngredientName { get; set; } = string.Empty;

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    // Required for inventory valuation. The PostgreSQL column is `price`.
    [Column("price")]
    public decimal Price { get; set; }

    [Column("lowstockthreshold")]
    public decimal LowStockThreshold { get; set; }

    [Column("highstockthreshold")]
    public decimal? HighStockThreshold { get; set; }
}

