namespace httt.Models;

public class ProductionStatisticsViewModel
{
    public ProductionRequest Request { get; set; } = null!;
    public List<ProductionLog> Logs { get; set; } = new();
    public long AccumulatedQuantity { get; set; }
    public long LoggedQuantity => Logs.Sum(l => (long)l.ProducedQuantity);
    public long LegacyQuantity => Math.Max(0, AccumulatedQuantity - LoggedQuantity);
    public long TotalProduced => LoggedQuantity + LegacyQuantity;
    public long Remaining => Math.Max(0, Request.QuantityRequired - TotalProduced);
    public DateTimeOffset? LatestProduction => Logs.Count == 0 ? null : Logs.Max(l => l.ProducedAt);
    public static string DisplayTime(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm") ?? "—";
}
