using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("saleorder", Schema = "public")]
public class SaleOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("orderid")]
    public int OrderId { get; set; }

    [NotMapped]
    public int Orderid
    {
        get => OrderId;
        set => OrderId = value;
    }

    [Column("createdby")]
    public int? CreatedBy { get; set; }

    [NotMapped]
    public int? Createdby
    {
        get => CreatedBy;
        set => CreatedBy = value;
    }

    [MaxLength(150)]
    [Column("customername")]
    public string? CustomerName { get; set; }

    [NotMapped]
    public string? Customername
    {
        get => CustomerName;
        set => CustomerName = value;
    }

    [MaxLength(20)]
    [Column("customerphone")]
    public string? CustomerPhone { get; set; }

    [NotMapped]
    public string? Customerphone
    {
        get => CustomerPhone;
        set => CustomerPhone = value;
    }

    [MaxLength(255)]
    [Column("shippingaddress")]
    public string? ShippingAddress { get; set; }

    [NotMapped]
    public string? Shippingaddress
    {
        get => ShippingAddress;
        set => ShippingAddress = value;
    }

    [Column("totalamount")]
    public decimal? TotalAmount { get; set; }

    [NotMapped]
    public decimal? Totalamount
    {
        get => TotalAmount;
        set => TotalAmount = value;
    }

    [Column("orderdate")]
    public DateTime? OrderDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public DateTime? Orderdate
    {
        get => OrderDate;
        set => OrderDate = value;
    }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } = "Pending";

    [MaxLength(50)]
    [Column("ordercode")]
    public string? OrderCode { get; set; }

    [NotMapped]
    public string? Ordercode
    {
        get => OrderCode;
        set => OrderCode = value;
    }

    [NotMapped]
    public int? ApprovedBy { get; set; }

    [NotMapped]
    public virtual UserAccount? UserAccount { get; set; }

    [NotMapped]
    public int? CustomerId { get; set; }

    [NotMapped]
    public string? Notes { get; set; }

    public virtual ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
    public virtual ICollection<ProductExportReceipt> ProductExportReceipts { get; set; } = new List<ProductExportReceipt>();
}
