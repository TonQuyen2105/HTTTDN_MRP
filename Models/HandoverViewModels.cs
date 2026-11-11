using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

/// <summary>
/// Thực thể ghi nhận lịch sử từng lần sản xuất (Production Log)
/// Tách rời hoàn toàn khỏi phiếu bàn giao kho
/// </summary>
[Table("production_log")]
public class ProductionLog
{
    [Key]
    [Column("logid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Required]
    [Column("requestid")]
    public int RequestId { get; set; }

    [Required]
    [Column("productid")]
    public int ProductId { get; set; }

    [Required]
    [Column("producedquantity")]
    public int ProducedQuantity { get; set; }

    [Column("producedat", TypeName = "timestamp with time zone")]
    public DateTimeOffset ProducedAt { get; set; }

    [Column("expireddate", TypeName = "date")]
    public DateOnly? ExpiredDate { get; set; }

    [Column("createdat", TypeName = "timestamp with time zone")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("operationkey")]
    public Guid OperationKey { get; set; }

    [Column("createdby")]
    public int CreatedBy { get; set; }

    [MaxLength(255)]
    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey("RequestId")]
    public ProductionRequest? ProductionRequest { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("CreatedBy")]
    public UserAccount? Creator { get; set; }
}

/// <summary>
/// DTO gửi từ client khi lập phiếu bàn giao thành phẩm sang kho
/// </summary>
public class HandoverSlipCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lần sản xuất.")]
    public int ProductionLogId { get; set; }
    [Required(ErrorMessage = "Mã yêu cầu sản xuất không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã yêu cầu sản xuất không hợp lệ.")]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã sản phẩm không hợp lệ.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Số lượng bàn giao không được để trống.")]
    [Range(1, 100_000_000, ErrorMessage = "Số lượng bàn giao phải là số nguyên dương lớn hơn 0.")]
    public int Quantity { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    public string? Note { get; set; }

    [Required(ErrorMessage = "Mã thao tác (Idempotency Key) không được để trống.")]
    [MaxLength(100, ErrorMessage = "Mã thao tác tối đa 100 ký tự.")]
    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>
/// DTO hiển thị từng dòng trong Tab 'Chờ bàn giao'
/// </summary>
public class PendingHandoverItemDto
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Hộp";
    public int QuantityRequired { get; set; }

    public int TotalProduced { get; set; }
    public int WarehouseReceived { get; set; }
    public int PendingHandover { get; set; }
    public int AvailableForHandover { get; set; }

    public List<HandoverProductionOption> ProductionOptions { get; set; } = new();
    public string? ProductionWarning { get; set; }

    public decimal WipQuantity { get; set; }
    public bool IsConsistent { get; set; }
    public string? DiscrepancyReason { get; set; }
}

/// <summary>
/// DTO hiển thị từng dòng trong Tab 'Phiếu bàn giao'
/// </summary>
public class HandoverSlipItemDto
{
    public DateTime? ExpiredDate { get; set; }
    public int? ProductionLogId { get; set; }
    public int ProductReceiptId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public DateTime? CreatedDate { get; set; }
    public int? RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Hộp";
    public int Quantity { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public string Status { get; set; } = "Pending";
    public string StatusText { get; set; } = "Chờ kho nhận";
    public string? Note { get; set; }
}

/// <summary>
/// ViewModel chính cho toàn bộ trang Bàn giao thành phẩm (2 Tab)
/// </summary>
public class HandoverPageViewModel
{
    public List<PendingHandoverItemDto> PendingItems { get; set; } = new();
    public List<HandoverSlipItemDto> HandoverSlips { get; set; } = new();
    public string ActiveTab { get; set; } = "pending";
    public int TotalPendingOrdersCount => PendingItems.Count(p => p.IsConsistent && p.AvailableForHandover > 0);
    public int InconsistentCount => PendingItems.Count(p => !p.IsConsistent);
    public int TotalSlipsCount => HandoverSlips.Count;
}

public class HandoverProductionOption
{
    public int LogId { get; set; }
    public string ProducedDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "Chưa cập nhật";
    public int Available { get; set; }
}
