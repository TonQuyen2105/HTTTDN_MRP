namespace httt.Models;

public static class ProductionExpiry
{
    public static long Remaining(int produced, IEnumerable<ProductReceiptDetail> details, int logId)
        => Math.Max(0, (long)produced - details.Where(d => d.ProductionLogId == logId).Sum(d => (long)d.Quantity));

    public static DateOnly? Calculate(DateTimeOffset producedAt, int? months)
    {
        if (months is null or <= 0) return null;
        var day = DateOnly.FromDateTime(producedAt.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var maxMonths = (9999 - day.Year) * 12 + 12 - day.Month;
        if (months > maxMonths) throw new ArgumentOutOfRangeException(nameof(months));
        return day.AddMonths(months.Value);
    }
}
