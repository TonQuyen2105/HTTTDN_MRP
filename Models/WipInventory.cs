using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("wip_inventory", Schema = "public")]
public class WipInventory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("wipid")]
    public int WipId { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("stage")]
    public string? Stage { get; set; }

    [Column("updatedat")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
