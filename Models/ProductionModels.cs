using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("bom")]
public class Bom
{
    [Key]
    [Column("bomid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BomId { get; set; }

    [Required(ErrorMessage = "Tên định mức BOM không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên định mức BOM tối đa 100 ký tự.")]
    [Column("bomname")]
    public string BomName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Mã BOM tối đa 100 ký tự.")]
    [Column("bomcode")]
    public string? BomCode { get; set; }

    [Required(ErrorMessage = "Sản phẩm không được để trống.")]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Nguyên vật liệu không được để trống.")]
    [Column("ingrid")]
    public int IngrId { get; set; }

    [Required(ErrorMessage = "Quy cách sản phẩm không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quy cách sản phẩm phải lớn hơn 0.")]
    [Column("itemquantity")]
    public int ItemQuantity { get; set; }

    [Required(ErrorMessage = "Định mức nguyên vật liệu không được để trống.")]
    [Range(0.000001, double.MaxValue, ErrorMessage = "Định mức nguyên vật liệu phải lớn hơn 0.")]
    [Column("materialquantity", TypeName = "numeric")]
    public decimal MaterialQuantity { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("IngrId")]
    public Ingredient? Ingredient { get; set; }
}

[Table("materialreceipt")]
public class MaterialReceipt
{
    [Key]
    [Column("expreceiptid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExpReceiptId { get; set; }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [MaxLength(50, ErrorMessage = "Trạng thái tối đa 50 ký tự.")]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [MaxLength(50, ErrorMessage = "Mã phiếu xuất kho tối đa 50 ký tự.")]
    [Column("expreceiptcode")]
    public string? ExpReceiptCode { get; set; }

    [Column("requestid")]
    public int? RequestId { get; set; }

    [NotMapped]
    public string? ProductionOrderCode { get; set; }

    [NotMapped]
    public string? BomCode { get; set; }

    [NotMapped]
    public string? BomName { get; set; }

    [NotMapped]
    public DateTime? RequiredDate { get; set; }

    [NotMapped]
    public string? Note { get; set; }

    [ForeignKey("RequestedBy")]
    public UserAccount? Requester { get; set; }

    [ForeignKey("ApprovedBy")]
    public UserAccount? Approver { get; set; }

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    public ICollection<MaterialReceiptDetail> Details { get; set; } = new List<MaterialReceiptDetail>();

    [NotMapped]
    public string DisplayCode => !string.IsNullOrWhiteSpace(ExpReceiptCode) ? ExpReceiptCode : $"YCXVT-{ExpReceiptId}";
}

[Table("materialreceiptdetail")]
public class MaterialReceiptDetail
{
    [Column("expreceiptid")]
    public int ExpReceiptId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [Required(ErrorMessage = "Số lượng không được để trống.")]
    [Column("quantity", TypeName = "numeric(18,4)")]
    public decimal Quantity { get; set; }

    [MaxLength(50, ErrorMessage = "Trạng thái tối đa 50 ký tự.")]
    [Column("status")]
    public string? Status { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("exportdate")]
    public DateTime? ExportDate { get; set; }

    [Column("unitcost", TypeName = "numeric")]
    public decimal? UnitCost { get; set; }

    [NotMapped]
    public decimal? BomNorm { get; set; }

    [NotMapped]
    public string? Unit { get; set; }

    [NotMapped]
    public decimal? RequiredQuantity { get; set; }

    [NotMapped]
    public decimal? AvailableQuantity { get; set; }

    [NotMapped]
    public decimal? ShortageQuantity { get; set; }

    [ForeignKey("IngrId")]
    public Ingredient? Ingredient { get; set; }
}

[Table("supplier")]
public class Supplier
{
    [Key]
    [Column("supplierid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Tên nhà cung cấp không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên nhà cung cấp tối đa 100 ký tự.")]
    [Column("suppliername")]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    [Column("mail")]
    public string? Mail { get; set; }
}

public class MarkProducedGoodsRequest
{
    public Guid OperationKey { get; set; }
    [Required(ErrorMessage = "Mã đơn hàng không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã đơn hàng không hợp lệ.")]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Số lượng ghi nhận không được để trống.")]
    [Range(0, 100000000, ErrorMessage = "Số lượng ghi nhận phải từ 0 trở lên và không vượt quá 100.000.000.")]
    public int ProducedQuantity { get; set; }

    public int? QuantityToProduce { get; set; }

    public bool IsCompleted { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    public string? Note { get; set; }
}

public class CreateDrugOrderRequest
{
    [Required(ErrorMessage = "Sản phẩm không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã sản phẩm không hợp lệ.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Số lượng không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng yêu cầu phải lớn hơn 0.")]
    public int QuantityRequired { get; set; }

    public int? ManagerId { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    public string? Note { get; set; }
}

public class AcceptWarehouseOrderRequest
{
    [Required(ErrorMessage = "Mã yêu cầu không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã yêu cầu không hợp lệ.")]
    public int RequestId { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    public string? Note { get; set; }
}

public class ExportToWipRequest
{
    [Required(ErrorMessage = "Mã phiếu không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã phiếu không hợp lệ.")]
    public int ExpReceiptId { get; set; }
}

public class ReplenishIngredientRequest
{
    [Required(ErrorMessage = "Mã nguyên vật liệu không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã nguyên vật liệu không hợp lệ.")]
    public int IngrId { get; set; }

    [Required(ErrorMessage = "Số lượng nhập không được để trống.")]
    [Range(0.0001, 100000000, ErrorMessage = "Số lượng nhập phải lớn hơn 0.")]
    public decimal Quantity { get; set; }
}

public class ReplenishAllForReceiptRequest
{
    [Required(ErrorMessage = "Mã phiếu không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã phiếu không hợp lệ.")]
    public int ExpReceiptId { get; set; }
}
