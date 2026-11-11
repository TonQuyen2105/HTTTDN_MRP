using System.ComponentModel.DataAnnotations;
namespace httt.Models;

public class BomFormulaViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Unit { get; set; } = "";
    public int MaterialCount { get; set; }
    public int? ShelfLifeMonths { get; set; }
    public decimal? ProductPrice { get; set; }
}
public class BomEditorViewModel
{
    public bool IsReadOnly { get; set; }
    public string? Description { get; set; }
    public int? ShelfLifeMonths { get; set; }
    public decimal? ProductPrice { get; set; }
    public decimal? MaterialTotal => Lines.Count == 0 || Lines.Any(l => Ingredients.All(i => i.IngrId != l.IngredientId || !i.Cost.HasValue || i.Cost < 0))
        ? null : Lines.Sum(l => Math.Round(l.Quantity * Ingredients.First(i => i.IngrId == l.IngredientId).Cost!.Value, 2, MidpointRounding.AwayFromZero));
    public static string Money(decimal? value) => value.HasValue ? value.Value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " đ" : "Chưa có giá";
    public int? Id { get; set; }
    public string Version { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int ProductId { get; set; }
    public List<BomEditorLine> Lines { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<Ingredient> Ingredients { get; set; } = new();
    public string? Error { get; set; }
}
public class BomEditorLine
{
    [Range(1, int.MaxValue)]
    public int IngredientId { get; set; }
    [Range(typeof(decimal), "0", "1000000000", MinimumIsExclusive = true, ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Định mức phải lớn hơn 0 và không vượt quá 1.000.000.000.")]
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public decimal Quantity { get; set; }
}
public class SaveBomRequest
{
    public string? Description { get; set; }
    public string? OriginalDescription { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn sử dụng phải là số tháng nguyên lớn hơn 0.")]
    public int? ShelfLifeMonths { get; set; }
    public int? OriginalShelfLifeMonths { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public decimal? ProductPrice { get; set; }
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public decimal? OriginalProductPrice { get; set; }
    public int? Id { get; set; }
    public string? Version { get; set; }
    [Required, MaxLength(100)]
    public string Code { get; set; } = "";
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
    [Required, MinLength(1), MaxLength(200)]
    public List<BomEditorLine> Lines { get; set; } = new();
}
