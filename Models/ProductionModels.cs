using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("product")]
public class Product
{
    [Key]
    [Column("productid")]
    public int ProductId { get; set; }

    [Column("productname")]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }
}

[Table("ingredient")]
public class Ingredient
{
    [Key]
    [Column("ingrid")]
    public int IngrId { get; set; }

    [Column("ingrname")]
    public string IngrName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }
}

[Table("bom")]
public class Bom
{
    [Key]
    [Column("bomid")]
    public int BomId { get; set; }

    [Column("bomname")]
    public string BomName { get; set; } = string.Empty;

    [Column("bomcode")]
    public string? BomCode { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [Column("ingrid")]
    public int? IngrId { get; set; }

    [Column("itemquantity")]
    public int? ItemQuantity { get; set; }

    [Column("materialquantity")]
    public int? MaterialQuantity { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("IngrId")]
    public Ingredient? Ingredient { get; set; }
}

[Table("productionrequest")]
public class ProductionRequest
{
    [Key]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Column("managerid")]
    public int? ManagerId { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [Column("quantityrequired")]
    public int QuantityRequired { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("ManagerId")]
    public UserAccount? Manager { get; set; }

    [NotMapped]
    public int ProducedQuantity { get; set; }
}

[Table("materialreceipt")]
public class MaterialReceipt
{
    [Key]
    [Column("expreceiptid")]
    public int ExpReceiptId { get; set; }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

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
}

[Table("materialreceiptdetail")]
public class MaterialReceiptDetail
{
    [Column("expreceiptid")]
    public int ExpReceiptId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("exportdate")]
    public DateTime? ExportDate { get; set; }

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
    public int ProductReceiptId { get; set; }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("requestid")]
    public int? RequestId { get; set; }

    [ForeignKey("RequestedBy")]
    public UserAccount? Requester { get; set; }

    [ForeignKey("ApprovedBy")]
    public UserAccount? Approver { get; set; }

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    public ICollection<ProductReceiptDetail> Details { get; set; } = new List<ProductReceiptDetail>();
}

[Table("productreceiptdetail")]
public class ProductReceiptDetail
{
    [Column("productreceiptid")]
    public int ProductReceiptId { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}

[Table("supplier")]
public class Supplier
{
    [Key]
    [Column("supplierid")]
    public int SupplierId { get; set; }

    [Column("suppliername")]
    public string SupplierName { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("mail")]
    public string? Mail { get; set; }
}

public class MarkProducedGoodsRequest
{
    public int RequestId { get; set; }
    public int ProducedQuantity { get; set; }
    public bool IsCompleted { get; set; }
    public string? Note { get; set; }
}

public class CreateDrugOrderRequest
{
    public int ProductId { get; set; }
    public int QuantityRequired { get; set; }
    public int? ManagerId { get; set; }
    public string? Note { get; set; }
}

public class AcceptWarehouseOrderRequest
{
    public int RequestId { get; set; }
    public string? Note { get; set; }
}
