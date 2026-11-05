namespace httt.Areas.Warehouse.Models;

public class IngredientDto
{
    public string IngrName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int? ShelfLifeMonths { get; set; }
}

public class ProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Cost { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int? ShelfLifeMonths { get; set; }
}

public class SupplierDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Mail { get; set; }
}

public class CreateImportReceiptDto
{
    public int SupplierId { get; set; }
    public int? CreatedBy { get; set; }
    public string? IpReceiptCode { get; set; }
    public bool IsDraft { get; set; } = false;
    public List<CreateImportReceiptItemDto> Items { get; set; } = new();
}

public class CreateImportReceiptItemDto
{
    public int IngrId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Cost { get; set; }
}

public class RejectDto
{
    public string? Reason { get; set; }
}

