using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productionrequest", Schema = "public")]
public class ProductionRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Column("requestcode")]
    public string? RequestCode { get; set; }

    [Column("quantityrequired")]
    public decimal QuantityRequired { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("createddate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("assignedto")]
    public int? AssignedTo { get; set; }

    /// <summary>
    /// Navigation property trỏ tới UserAccount (người được giao xử lý phiếu).
    /// </summary>
    [ForeignKey(nameof(AssignedTo))]
    public virtual UserAccount? AssignedUser { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}
