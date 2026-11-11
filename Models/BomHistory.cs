using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json;
namespace httt.Models;

[Table("bom_history", Schema = "public")]
public class BomHistory
{
    [Key, Column("historyid")] public long HistoryId { get; set; }
    [Column("operationkey")] public Guid OperationKey { get; set; }
    [Column("productid")] public int ProductId { get; set; }
    [Column("bomid")] public int BomId { get; set; }
    [Column("changedby")] public int ChangedBy { get; set; }
    [Column("changedbyname")] public string ChangedByName { get; set; } = "";
    [Column("changedat")] public DateTimeOffset ChangedAt { get; set; }
    [Column("reason")] public string Reason { get; set; } = "";
    [Column("beforedata", TypeName = "jsonb")] public string? BeforeData { get; set; }
    [Column("afterdata", TypeName = "jsonb")] public string AfterData { get; set; } = "{}";
    [NotMapped] public BomSnapshot? Before => BeforeData == null ? null : JsonSerializer.Deserialize<BomSnapshot>(BeforeData);
    [NotMapped] public BomSnapshot After => JsonSerializer.Deserialize<BomSnapshot>(AfterData)!;
    [NotMapped] public List<BomChange> Changes => BomSnapshot.Compare(Before, After);
}

public record BomMaterialSnapshot(int IngredientId, string Name, string Unit, decimal Quantity, decimal? Cost);
public record BomChange(string Field, string Before, string After, string Kind);
public class BomSnapshot
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? ShelfLifeMonths { get; set; }
    public decimal? MaterialCost { get; set; }
    public List<BomMaterialSnapshot> Materials { get; set; } = new();

    public static BomSnapshot Capture(IEnumerable<Bom> rows, Product product, IEnumerable<Ingredient> ingredients)
    {
        var list = rows.ToList();
        var catalog = ingredients.ToDictionary(i => i.IngrId);
        return new BomSnapshot {
            Code = list.FirstOrDefault()?.BomCode ?? "", Name = list.FirstOrDefault()?.BomName ?? "",
            Description = product.Description, Price = product.Cost, ShelfLifeMonths = product.ShelfLifeMonths,
            MaterialCost = product.MaterialCost,
            Materials = list.OrderBy(b => b.IngrId).Select(b => {
                catalog.TryGetValue(b.IngrId, out var i);
                return new BomMaterialSnapshot(b.IngrId, i?.IngrName ?? $"NVL-{b.IngrId}", i?.Unit ?? "",
                    b.ItemQuantity > 0 ? b.MaterialQuantity / b.ItemQuantity : b.MaterialQuantity, i?.Cost);
            }).ToList()
        };
    }
    public static bool MaterialsChanged(BomSnapshot before, BomSnapshot after) =>
        !before.Materials.OrderBy(m => m.IngredientId).Select(m => (m.IngredientId, m.Quantity))
            .SequenceEqual(after.Materials.OrderBy(m => m.IngredientId).Select(m => (m.IngredientId, m.Quantity)));

    public static List<BomChange> Compare(BomSnapshot? before, BomSnapshot after)
    {
        var changes = new List<BomChange>();
        void Add(string label, string? oldValue, string? newValue) {
            if (oldValue != newValue) changes.Add(new(label, oldValue ?? "—", newValue ?? "—", "Cập nhật"));
        }
        Add("Mã BOM", before?.Code, after.Code);
        Add("Tên công thức", before?.Name, after.Name);
        Add("Đơn giá thành phẩm", before == null ? null : BomEditorViewModel.Money(before.Price), BomEditorViewModel.Money(after.Price));
        Add("Thời hạn sử dụng", before?.ShelfLifeMonths?.ToString(), after.ShelfLifeMonths?.ToString());
        Add("Mô tả sản phẩm", before?.Description, after.Description);
        Add("Tổng tiền NVL", before == null ? null : BomEditorViewModel.Money(before.MaterialCost), BomEditorViewModel.Money(after.MaterialCost));
        var oldLines = before?.Materials ?? new();
        foreach (var id in oldLines.Select(m => m.IngredientId).Union(after.Materials.Select(m => m.IngredientId)).OrderBy(id => id)) {
            var old = oldLines.FirstOrDefault(m => m.IngredientId == id);
            var next = after.Materials.FirstOrDefault(m => m.IngredientId == id);
            string Qty(BomMaterialSnapshot? m) => m == null ? "—" : m.Quantity.ToString("0.############################", CultureInfo.GetCultureInfo("vi-VN")) + " " + m.Unit;
            if (old == null || next == null || old.Quantity != next.Quantity || old.Unit != next.Unit)
                changes.Add(new($"{next?.Name ?? old!.Name} (NVL-{id})", Qty(old), Qty(next), old == null ? "Thêm mới" : next == null ? "Loại bỏ" : "Đổi định mức"));
            if (old != null && next != null && old.Cost != next.Cost)
                Add($"Đơn giá NVL-{id}", BomEditorViewModel.Money(old.Cost), BomEditorViewModel.Money(next.Cost));
        }
        return changes;
    }
}
