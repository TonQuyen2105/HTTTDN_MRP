using System;
using System.Collections.Generic;

namespace httt.Models;

public class MaterialRequirementViewModel
{
    // Danh sách nguồn để chọn
    public List<ProductionRequestItemDto> ProductionRequests { get; set; } = new();
    public List<ProductionOrderItemDto> ProductionOrders { get; set; } = new();
    public List<ProductItemDto> Products { get; set; } = new();
    public List<BomItemDto> Boms { get; set; } = new();

    // Dữ liệu đầu vào
    public int? RequestId { get; set; }
    public string? ProductionOrderCode { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductUnit { get; set; } = "Hộp";
    public decimal TargetQuantity { get; set; }
    public string? BomCode { get; set; }
    public string BomName { get; set; } = string.Empty;
    public decimal BomStandardOutput { get; set; } = 1m;
    public DateTime? RequiredDate { get; set; }
    public string? Note { get; set; }

    // Trạng thái kiểm tra & ràng buộc nghiệp vụ
    public bool HasBom { get; set; } = true;
    public bool IsTrial { get; set; }
    public bool IsLinkedToOrder { get; set; }
    public bool HasExistingSlip { get; set; }
    public int? ExistingReceiptId { get; set; }
    public string? ExistingReceiptCode { get; set; }
    public string? ExistingReceiptStatus { get; set; }

    // Kết quả tính toán
    public bool IsCalculated { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.Now;
    public int TotalMaterialTypes { get; set; }
    public int SufficientCount { get; set; }
    public int ShortageCount { get; set; }
    public int UnknownStockCount { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public bool IsAllSufficient => IsCalculated && ShortageCount == 0 && TotalMaterialTypes > 0 && !Lines.Any(l => l.IsUnitMismatch);

    public List<MaterialRequirementLineDto> Lines { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ProductionRequestItemDto
{
    public int RequestId { get; set; }
    public string? DbRequestCode { get; set; }
    public string RequestCode => !string.IsNullOrWhiteSpace(DbRequestCode) ? DbRequestCode : "Chưa có mã";
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityRequired { get; set; }
    public string Unit { get; set; } = "Hộp";
    public string Status { get; set; } = string.Empty;
    public string? RelatedOrderCode { get; set; }
    public bool IsApprovedOrder => Status == "Approved" || Status == "In Production";
}

public class ProductionOrderItemDto
{
    public int RequestId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityRequired { get; set; }
    public string Unit { get; set; } = "Hộp";
    public string Status { get; set; } = string.Empty;
}

public class ProductItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Hộp";
    public bool HasBom { get; set; }
}

public class BomItemDto
{
    public int BomId { get; set; }
    public string BomCode { get; set; } = string.Empty;
    public string BomName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public decimal StandardOutput { get; set; }
    public string Unit { get; set; } = "Hộp";
    public int MaterialCount { get; set; }
}

public class MaterialRequirementLineDto
{
    public int Stt { get; set; }
    public int IngrId { get; set; }
    public string IngrCode => $"INGR{IngrId:D4}";
    public string IngrName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string StockUnit { get; set; } = string.Empty;
    public bool IsUnitMismatch { get; set; }
    public decimal BomNorm { get; set; }
    public decimal BomStandardOutput { get; set; }
    public decimal TotalRequired { get; set; }
    public decimal? AvailableStock { get; set; }
    public decimal WipStock { get; set; }
    public decimal? Shortage { get; set; }
    public bool HasStockData => AvailableStock.HasValue;
    public bool IsSufficient => Shortage == 0 && !IsUnitMismatch;
    public bool IsDiscrete { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class CalculateRequirementApiRequest
{
    public int? RequestId { get; set; }
    public string? ProductionOrderCode { get; set; }
    public int ProductId { get; set; }
    public decimal TargetQuantity { get; set; }
    public string? BomCode { get; set; }
    public decimal? BomStandardOutput { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? Note { get; set; }
}

public class CreateRequisitionApiRequest
{
    public int? RequestId { get; set; }
    public string? ProductionOrderCode { get; set; }
    public int ProductId { get; set; }
    public decimal TargetQuantity { get; set; }
    public string? BomCode { get; set; }
    public decimal? BomStandardOutput { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? Note { get; set; }
}
