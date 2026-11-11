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

    [Required]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Column("ingrid")]
    public int? IngrId { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [Column("quantity", TypeName = "numeric")]
    public decimal? Quantity { get; set; }

    [Column("lastupdated")]
    public DateTime? LastUpdated { get; set; }

    [Column("stage")]
    public string? Stage { get; set; }

    [Column("updatedat")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    [ForeignKey("IngrId")]
    public Ingredient? Ingredient { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
