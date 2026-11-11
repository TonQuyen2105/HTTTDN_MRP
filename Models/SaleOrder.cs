using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("saleorder", Schema = "public")]
public class SaleOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("orderid")]
    public int OrderId { get; set; }

    [Column("ordercode")]
    public string? OrderCode { get; set; }

    [Column("orderdate")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column("totalamount")]
    public decimal TotalAmount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Navigation property trỏ tới UserAccount (người duyệt đơn hàng).
    /// Hỗ trợ Eager Loading: .Include(x => x.UserAccount) để xử lý triệt để N+1 Query.
    /// </summary>
    [ForeignKey(nameof(ApprovedBy))]
    public virtual UserAccount? UserAccount { get; set; }

    [Column("customerid")]
    public int? CustomerId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}
