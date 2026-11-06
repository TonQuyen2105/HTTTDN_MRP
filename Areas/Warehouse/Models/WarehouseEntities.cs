using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using httt.Models;

namespace httt.Areas.Warehouse.Models;

[Table("supplier")]
public class Supplier
{
    [Key]
    [Column("supplierid")]
    public int SupplierId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("suppliername")]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(100)]
    [Column("mail")]
    public string? Mail { get; set; }
}

[Table("ingredient")]
public class Ingredient
{
    [Key]
    [Column("ingrid")]
    public int IngrId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("ingrname")]
    public string IngrName { get; set; } = string.Empty;

    [Column("quantity")]
    public decimal? Quantity { get; set; } = 0;

    [MaxLength(20)]
    [Column("unit")]
    public string? Unit { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [MaxLength(255)]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    public int? ShelfLifeMonths { get; set; } = 0;
}

[Table("product")]
public class Product
{
    [Key]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("productname")]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int? Quantity { get; set; } = 0;

    [MaxLength(20)]
    [Column("unit")]
    public string? Unit { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [MaxLength(255)]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("shelflifemonths")]
    public int? ShelfLifeMonths { get; set; } = 0;
}

[Table("bom")]
public class Bom
{
    [Key]
    [Column("bomid")]
    public int BomId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("bomname")]
    public string BomName { get; set; } = string.Empty;

    [Column("productid")]
    public int ProductId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [Column("itemquantity")]
    public int ItemQuantity { get; set; }

    [Column("materialquantity")]
    public decimal MaterialQuantity { get; set; }

    [MaxLength(100)]
    [Column("bomcode")]
    public string? BomCode { get; set; }
}

[Table("productionrequest")]
public class ProductionRequest
{
    [Key]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Column("managerid")]
    public int ManagerId { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [Column("quantityrequired")]
    public int QuantityRequired { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("requestcode")]
    public string? RequestCode { get; set; }

    [Column("assignedto")]
    public int? AssignedTo { get; set; }

    [JsonIgnore]
    [ForeignKey("AssignedTo")]
    public virtual UserAccount? AssignedToUser { get; set; }

    [JsonIgnore]
    [ForeignKey("ManagerId")]
    public virtual UserAccount? Manager { get; set; }

    [JsonIgnore]
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}

[Table("importreceipt")]
public class ImportReceipt
{
    [Key]
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [Column("supplierid")]
    public int? SupplierId { get; set; }

    [Column("createdby")]
    public int? CreatedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("ipreceiptcode")]
    public string? IpReceiptCode { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }
}

[Table("importreceiptdetail")]
public class ImportReceiptDetail
{
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }
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

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [Column("expreceiptcode")]
    public string? ExpReceiptCode { get; set; }

    [Column("requestid")]
    public int? RequestId { get; set; }
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

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [MaxLength(255)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("exportdate")]
    public DateTime? ExportDate { get; set; }

    [Column("unitcost")]
    public decimal? UnitCost { get; set; }
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

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [Column("requestid")]
    public int? RequestId { get; set; }

    [Column("receiptcode")]
    public string? ReceiptCode { get; set; }
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

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [MaxLength(255)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("expireddate")]
    public DateTime? ExpiredDate { get; set; }
}

[Table("productexportreceipt")]
public class ProductExportReceipt
{
    [Key]
    [Column("exportreceiptid")]
    public int ExportReceiptId { get; set; }

    [Column("requestedby")]
    public int? RequestedBy { get; set; }

    [Column("approvedby")]
    public int? ApprovedBy { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [Column("expreceiptcode")]
    public string? ExpReceiptCode { get; set; }

    [Column("orderid")]
    public int? OrderId { get; set; }
}

[Table("productexportreceiptdetail")]
public class ProductExportReceiptDetail
{
    [Column("exportreceiptid")]
    public int ExportReceiptId { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }
}

[Table("saleorder")]
public class SaleOrder
{
    [Key]
    [Column("orderid")]
    public int OrderId { get; set; }

    [Column("createdby")]
    public int? CreatedBy { get; set; }

    [MaxLength(150)]
    [Column("customername")]
    public string? CustomerName { get; set; }

    [MaxLength(20)]
    [Column("customerphone")]
    public string? CustomerPhone { get; set; }

    [MaxLength(255)]
    [Column("shippingaddress")]
    public string? ShippingAddress { get; set; }

    [Column("totalamount")]
    public decimal? TotalAmount { get; set; }

    [Column("orderdate")]
    public DateTime? OrderDate { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [MaxLength(50)]
    [Column("ordercode")]
    public string? OrderCode { get; set; }
}

[Table("saleorderdetail")]
public class SaleOrderDetail
{
    [Column("orderid")]
    public int OrderId { get; set; }

    [Column("productid")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unitprice")]
    public decimal? UnitPrice { get; set; }
}

[Table("wip_inventory")]
public class WipInventory
{
    [Key]
    [Column("wipid")]
    public int WipId { get; set; }

    [Column("requestid")]
    public int RequestId { get; set; }

    [Column("ingrid")]
    public int? IngrId { get; set; }

    [Column("productid")]
    public int? ProductId { get; set; }

    [Column("quantity")]
    public decimal? Quantity { get; set; } = 0;

    [Column("lastupdated")]
    public DateTime? LastUpdated { get; set; }
}
