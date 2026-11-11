using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("ingredient", Schema = "public")]
public class Ingredient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ingrid")]
    public int IngrId { get; set; }

    [NotMapped]
    public int IngredientId
    {
        get => IngrId;
        set => IngrId = value;
    }

    [Required(ErrorMessage = "Tên nguyên vật liệu không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên nguyên vật liệu tối đa 100 ký tự.")]
    [Column("ingrname")]
    public string IngrName { get; set; } = string.Empty;

    [NotMapped]
    public string IngredientName
    {
        get => IngrName;
        set => IngrName = value;
    }

    [Column("quantity", TypeName = "numeric")]
    public decimal? Quantity { get; set; }

    [MaxLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    [Column("unit")]
    public string? Unit { get; set; }

    // Required for inventory valuation. The PostgreSQL column is `price`.
    [Column("price", TypeName = "numeric(18,2)")]
    public decimal? Price { get; set; }

    [Column("cost", TypeName = "numeric(18,2)")]
    public decimal? Cost { get; set; }

    [MaxLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự.")]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn sử dụng phải là số tháng dương.")]
    public int? ShelfLifeMonths { get; set; }

    [Column("lowstockthreshold")]
    public decimal LowStockThreshold { get; set; }

    [Column("highstockthreshold")]
    public decimal? HighStockThreshold { get; set; }
}

