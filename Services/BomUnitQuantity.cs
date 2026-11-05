namespace httt.Services;

/// <summary>Quy đổi BOM cũ theo mẻ về định mức cho một đơn vị, không sửa dữ liệu gốc.</summary>
public static class BomUnitQuantity
{
    /// <summary>Danh sách các đơn vị tính rời rạc / đếm được cần làm tròn lên (Ceiling)</summary>
    public static readonly HashSet<string> DiscreteUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "cái", "cai", "hộp", "hop", "thùng", "thung", "tờ", "to", "chai", "lọ", "lo", "vỉ", "vi", "ống", "ong", "bao", "viên", "vien", "bọc", "boc", "gói", "goi", "cuộn", "cuon"
    };

    public static decimal PerUnit(decimal? materialQuantity, decimal? originalOutput)
    {
        if (!originalOutput.HasValue || originalOutput <= 0)
            throw new ArgumentException("BOM chưa có sản lượng gốc hợp lệ; hãy sửa BOM trước khi tính.");
        if (!materialQuantity.HasValue || materialQuantity <= 0)
            throw new ArgumentException("BOM chưa có định mức nguyên vật liệu hợp lệ.");
        return materialQuantity.Value / originalOutput.Value;
    }

    public static decimal Required(decimal? materialQuantity, decimal? originalOutput,
        decimal targetQuantity, bool isDiscrete)
    {
        _ = PerUnit(materialQuantity, originalOutput);
        if (targetQuantity <= 0)
            throw new ArgumentException("Số lượng cần sản xuất phải lớn hơn 0.");
        // Nhân trước khi chia để tránh sai số khi định mức đơn vị là số thập phân tuần hoàn.
        var total = materialQuantity!.Value * targetQuantity / originalOutput!.Value;
        return isDiscrete ? Math.Ceiling(total) : Math.Round(total, 4);
    }
}
