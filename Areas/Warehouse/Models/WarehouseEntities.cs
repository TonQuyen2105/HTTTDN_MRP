using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public int? Quantity { get; set; } = 0;

    [MaxLength(20)]
    [Column("unit")]
    public string? Unit { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [MaxLength(255)]
    [Column("imageurl")]
    public string? ImageUrl { get; set; }
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
    public int MaterialQuantity { get; set; }
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
}

[Table("importreceiptdetail")]
public class ImportReceiptDetail
{
    [Column("ipreceiptid")]
    public int IpReceiptId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("cost")]
    public decimal? Cost { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }
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
}

[Table("materialreceiptdetail")]
public class MaterialReceiptDetail
{
    [Column("expreceiptid")]
    public int ExpReceiptId { get; set; }

    [Column("ingrid")]
    public int IngrId { get; set; }

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
}

