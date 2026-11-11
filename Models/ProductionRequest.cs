using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace httt.Models;

[Table("productionrequest", Schema = "public")]
public class ProductionRequest
{
    [Column("assignedto")]
    public int? AssignedTo { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Người quản lý không được để trống.")]
    [Column("managerid")]
    public int ManagerId { get; set; }

    [Required(ErrorMessage = "Sản phẩm không được để trống.")]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Số lượng yêu cầu không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng yêu cầu phải lớn hơn 0.")]
    [Column("quantityrequired")]
    public int QuantityRequired { get; set; }

    [MaxLength(50, ErrorMessage = "Trạng thái tối đa 50 ký tự.")]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("requestcode")]
    public string? RequestCode { get; set; }

    [NotMapped]
    public string? Notes { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ManagerId")]
    public virtual UserAccount? Manager { get; set; }

    /// <summary>
    /// Navigation property trỏ tới UserAccount (người được giao xử lý phiếu).
    /// </summary>
    [ForeignKey(nameof(AssignedTo))]
    public virtual UserAccount? AssignedUser { get; set; }

    [NotMapped]
    [JsonIgnore]
    public virtual UserAccount? AssignedToUser
    {
        get => AssignedUser;
        set => AssignedUser = value;
    }

    [NotMapped]
    public int ProducedQuantity { get; set; }

    [NotMapped]
    public string DisplayCode
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RequestCode))
            {
                return $"YCSX-{RequestId}";
            }
            if (RequestCode.StartsWith("LSX-", StringComparison.OrdinalIgnoreCase))
            {
                return "YCSX-" + RequestCode.Substring(4);
            }
            if (RequestCode.StartsWith("LSX", StringComparison.OrdinalIgnoreCase))
            {
                return "YCSX-" + RequestCode.Substring(3);
            }
            return RequestCode;
        }
    }

    [NotMapped]
    public string RequestDisplayCode => DisplayCode;
}
