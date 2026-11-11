namespace httt.Models;

public static class HandoverStatus
{
    public static bool IsRejected(string? status) =>
        string.Equals(status?.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status?.Trim(), "Reject", StringComparison.OrdinalIgnoreCase);

    // A rejected receipt remains in history but no longer reserves finished goods.
    public static IEnumerable<ProductReceipt> ForAllocation(IEnumerable<ProductReceipt> receipts)
        => receipts.Where(r => !IsRejected(r.Status));
}
