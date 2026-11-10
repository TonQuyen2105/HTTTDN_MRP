using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("product")]
public class Product
{
    [Key]
    [Column("productid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên thành phẩm không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên thành phẩm tối đa 100 ký tự.")]
    [Column("productname")]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int? Quantity { get; set; }

    [MaxLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    [Column("unit")]
    public string? Unit { get; set; }

    [Column("cost", TypeName = "numeric(18,2)")]
    public decimal? Cost { get; set; }

    [MaxLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự.")]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn sử dụng phải là số tháng dương.")]
    public int? ShelfLifeMonths { get; set; }
}

[Table("ingredient")]
public class Ingredient
{
    [Key]
    [Column("ingrid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IngrId { get; set; }

    [Required(ErrorMessage = "Tên nguyên vật liệu không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên nguyên vật liệu tối đa 100 ký tự.")]
    [Column("ingrname")]
    public string IngrName { get; set; } = string.Empty;

    [Column("quantity", TypeName = "numeric")]
    public decimal? Quantity { get; set; }

    [MaxLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    [Column("unit")]
    public string? Unit { get; set; }

    [Column("price", TypeName = "numeric(18,2)")]
    public decimal? Price { get; set; }

    [MaxLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự.")]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời hạn sử dụng phải là số tháng dương.")]
    public int? ShelfLifeMonths { get; set; }
}

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

[Table("productionrequest")]
public class ProductionRequest
{
    [Column("assignedto")]
    public int? AssignedTo { get; set; }

    [Key]
    [Column("requestid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
    public DateTime? CreatedDate { get; set; }

    [Column("requestcode")]
    public string? RequestCode { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("ManagerId")]
    public UserAccount? Manager { get; set; }

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

[Table("productreceipt")]
public class ProductReceipt
{
    [Key]
    [Column("productreceiptid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductReceiptId { get; set; }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [MaxLength(50, ErrorMessage = "Trạng thái tối đa 50 ký tự.")]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("requestid")]
    public int? RequestId { get; set; }

    [Column("receiptcode")]
    public string? ReceiptCode { get; set; }

    [ForeignKey("RequestedBy")]
    public UserAccount? Requester { get; set; }

    [ForeignKey("ApprovedBy")]
    public UserAccount? Approver { get; set; }

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    public ICollection<ProductReceiptDetail> Details { get; set; } = new List<ProductReceiptDetail>();

    [NotMapped]
    public string DisplayCode => !string.IsNullOrWhiteSpace(ReceiptCode) ? ReceiptCode : $"BG-{ProductReceiptId}";
}

[Table("productreceiptdetail")]
public class ProductReceiptDetail
{
    [Column("productionlogid")]
    public int? ProductionLogId { get; set; }

    [ForeignKey(nameof(ProductionLogId))]
    public ProductionLog? ProductionLog { get; set; }
    [Column("productreceiptid")]
    public int ProductReceiptId { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Số lượng không được để trống.")]
    [Range(0, 100000000, ErrorMessage = "Số lượng phải từ 0 trở lên.")]
    [Column("quantity")]
    public int Quantity { get; set; }

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

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
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

[Table("wip_inventory")]
public class WipInventory
{
    [Key]
    [Column("wipid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    [ForeignKey("IngrId")]
    public Ingredient? Ingredient { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
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
