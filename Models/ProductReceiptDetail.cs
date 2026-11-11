using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("productreceiptdetail", Schema = "public")]
public class ProductReceiptDetail
{
    [NotMapped]
    public int ReceiptDetailId { get; set; }

    [Column("productionlogid")]
    public int? ProductionLogId { get; set; }

    [ForeignKey(nameof(ProductionLogId))]
    public ProductionLog? ProductionLog { get; set; }

    [Column("productreceiptid")]
    public int ProductReceiptId { get; set; }

    [NotMapped]
    public int ReceiptId
    {
        get => ProductReceiptId;
        set => ProductReceiptId = value;
    }

    [ForeignKey(nameof(ProductReceiptId))]
    public virtual ProductReceipt? ProductReceipt { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [Required(ErrorMessage = "Số lượng không được để trống.")]
    [Range(0, 100000000, ErrorMessage = "Số lượng phải từ 0 trở lên.")]
    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [NotMapped]
    public decimal? UnitPrice
    {
        get => Cost;
        set => Cost = value;
    }

    [MaxLength(50, ErrorMessage = "Trạng thái tối đa 50 ký tự.")]
    [Column("status")]
    public string? Status { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }
}
