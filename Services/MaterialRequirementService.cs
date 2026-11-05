using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;

namespace httt.Services;

public class MaterialRequirementService
{
    private readonly ApplicationDbContext _context;

    // Danh sách các đơn vị tính rời rạc / đếm được cần làm tròn lên (Ceiling)
    private static readonly HashSet<string> DiscreteUnits = BomUnitQuantity.DiscreteUnits;

    public MaterialRequirementService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy toàn bộ dữ liệu ban đầu cho trang Tính nhu cầu nguyên vật liệu
    /// </summary>
    public async Task<MaterialRequirementViewModel> GetInitialViewModelAsync(int? requestId, int? productId)
    {
        var vm = new MaterialRequirementViewModel();

        // 1. Lấy danh sách Yêu cầu sản xuất
        var requests = await _context.ProductionRequests
            .AsNoTracking()
            .Include(r => r.Product)
            .OrderByDescending(r => r.RequestId)
            .ToListAsync();

        vm.ProductionRequests = requests.Select(r => new ProductionRequestItemDto
        {
            RequestId = r.RequestId,
            DbRequestCode = r.RequestCode,
            ProductId = r.ProductId,
            ProductName = r.Product?.ProductName ?? $"Sản phẩm #{r.ProductId}",
            QuantityRequired = r.QuantityRequired,
            Unit = r.Product?.Unit ?? "Hộp",
            Status = r.Status,
            RelatedOrderCode = (r.Status == "Approved" || r.Status == "In Production")
                ? (!string.IsNullOrWhiteSpace(r.RequestCode) ? r.RequestCode : $"LSX-{r.RequestId}")
                : null
        }).ToList();

        // 2. Lấy danh sách Lệnh sản xuất (các yêu cầu đã được phê duyệt)
        vm.ProductionOrders = requests
            .Where(r => r.Status == "Approved" || r.Status == "In Production")
            .Select(r => new ProductionOrderItemDto
            {
                RequestId = r.RequestId,
                OrderCode = !string.IsNullOrWhiteSpace(r.RequestCode) ? r.RequestCode : $"LSX-{r.RequestId}",
                ProductId = r.ProductId,
                ProductName = r.Product?.ProductName ?? $"Sản phẩm #{r.ProductId}",
                QuantityRequired = r.QuantityRequired,
                Unit = r.Product?.Unit ?? "Hộp",
                Status = r.Status
            }).ToList();

        // 3. Lấy danh sách Sản phẩm
        var products = await _context.Products.AsNoTracking().OrderBy(p => p.ProductId).ToListAsync();
        var productIdsWithBom = await _context.Boms.AsNoTracking().Select(b => b.ProductId).Distinct().ToListAsync();

        vm.Products = products.Select(p => new ProductItemDto
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Unit = p.Unit ?? "Hộp",
            HasBom = productIdsWithBom.Contains(p.ProductId)
        }).ToList();

        // 4. Lấy danh sách BOM
        var allBoms = await _context.Boms
            .AsNoTracking()
            .Include(b => b.Product)
            .Include(b => b.Ingredient)
            .ToListAsync();

        // Nhóm BOM theo BomCode hoặc BomName
        var groupedBoms = allBoms
            .GroupBy(b => new {
                Code = !string.IsNullOrWhiteSpace(b.BomCode) ? b.BomCode : b.BomName,
                ProductId = b.ProductId
            })
            .Select(g => {
                var first = g.First();
                return new BomItemDto
                {
                    BomId = first.BomId,
                    BomCode = g.Key.Code,
                    BomName = $"Định mức tiêu chuẩn {first.Product?.ProductName ?? g.Key.Code} ({g.Count()} nguyên liệu)",
                    ProductId = g.Key.ProductId,
                    StandardOutput = 1m,
                    Unit = first.Product?.Unit ?? "Hộp",
                    MaterialCount = g.Count()
                };
            }).ToList();

        vm.Boms = groupedBoms;

        // 5. Xác định dữ liệu mặc định được chọn
        ProductionRequest? selectedReq = null;
        if (requestId.HasValue && requestId.Value > 0)
        {
            selectedReq = requests.FirstOrDefault(r => r.RequestId == requestId.Value);
        }

        if (selectedReq == null && (productId.HasValue && productId.Value > 0))
        {
            var prod = products.FirstOrDefault(p => p.ProductId == productId.Value);
            if (prod != null)
            {
                vm.ProductId = prod.ProductId;
                vm.ProductName = prod.ProductName;
                vm.ProductUnit = prod.Unit ?? "Hộp";
                vm.TargetQuantity = 100m; // Số lượng thử nghiệm mặc định
                vm.IsTrial = true;
            }
        }
        else if (selectedReq != null)
        {
            vm.RequestId = selectedReq.RequestId;
            vm.ProductId = selectedReq.ProductId;
            vm.ProductName = selectedReq.Product?.ProductName ?? string.Empty;
            vm.ProductUnit = selectedReq.Product?.Unit ?? "Hộp";
            vm.TargetQuantity = Convert.ToDecimal(selectedReq.QuantityRequired);

            if (selectedReq.Status == "Approved" || selectedReq.Status == "In Production")
            {
                vm.ProductionOrderCode = !string.IsNullOrWhiteSpace(selectedReq.RequestCode) ? selectedReq.RequestCode : $"LSX-{selectedReq.RequestId}";
                vm.IsLinkedToOrder = true;
            }
            else
            {
                vm.ProductionOrderCode = string.Empty;
                vm.IsLinkedToOrder = false;
            }
        }
        else if (requests.Any())
        {
            // Mặc định chọn yêu cầu đầu tiên
            selectedReq = requests.First();
            vm.RequestId = selectedReq.RequestId;
            vm.ProductId = selectedReq.ProductId;
            vm.ProductName = selectedReq.Product?.ProductName ?? string.Empty;
            vm.ProductUnit = selectedReq.Product?.Unit ?? "Hộp";
            vm.TargetQuantity = Convert.ToDecimal(selectedReq.QuantityRequired);

            if (selectedReq.Status == "Approved" || selectedReq.Status == "In Production")
            {
                vm.ProductionOrderCode = !string.IsNullOrWhiteSpace(selectedReq.RequestCode) ? selectedReq.RequestCode : $"LSX-{selectedReq.RequestId}";
                vm.IsLinkedToOrder = true;
            }
            else
            {
                vm.ProductionOrderCode = string.Empty;
                vm.IsLinkedToOrder = false;
            }
        }

        // Chọn BOM mặc định cho sản phẩm đã chọn
        if (vm.ProductId.HasValue && vm.ProductId.Value > 0)
        {
            var matchingBoms = groupedBoms.Where(b => b.ProductId == vm.ProductId.Value).ToList();
            if (matchingBoms.Any())
            {
                vm.HasBom = true;
                var defBom = matchingBoms.First();
                vm.BomCode = defBom.BomCode;
                vm.BomName = defBom.BomName;
                vm.BomStandardOutput = defBom.StandardOutput;
            }
            else
            {
                vm.HasBom = false;
                vm.BomCode = null;
                vm.BomName = string.Empty;
                vm.BomStandardOutput = 1m;
            }
        }

        vm.RequiredDate = DateTime.Now.AddDays(1);
        vm.Note = vm.IsLinkedToOrder
            ? $"Cấp phát nguyên vật liệu phục vụ {vm.ProductionOrderCode} - {vm.ProductName}"
            : $"Tính nhu cầu nguyên vật liệu sản xuất {vm.ProductName}";

        // 6. Kiểm tra xem đã có phiếu yêu cầu cấp vật tư đang xử lý cho lệnh này chưa
        if (vm.RequestId.HasValue && vm.RequestId.Value > 0)
        {
            var reqId = vm.RequestId.Value;
            var existingSlip = await _context.MaterialReceipts
                .AsNoTracking()
                .Where(m => m.RequestId == reqId
                         && m.Status != "Cancelled" && m.Status != "Rejected")
                .OrderByDescending(m => m.ExpReceiptId)
                .FirstOrDefaultAsync();

            if (existingSlip != null)
            {
                vm.HasExistingSlip = true;
                vm.ExistingReceiptId = existingSlip.ExpReceiptId;
                vm.ExistingReceiptCode = existingSlip.DisplayCode;
                vm.ExistingReceiptStatus = existingSlip.Status switch
                {
                    "Completed" => "Được cấp",
                    "Approved" => "Được cấp",
                    "Pending" => "Chờ cấp",
                    _ => existingSlip.Status
                };
            }
        }

        // 7. Nếu có đủ thông tin và có BOM, thực hiện tính toán ngay
        if (vm.HasBom && vm.ProductId.HasValue && vm.TargetQuantity > 0 && !string.IsNullOrWhiteSpace(vm.BomCode))
        {
            await CalculateMaterialRequirementInternalAsync(vm);
        }

        return vm;
    }

    /// <summary>
    /// API tính toán nhu cầu nguyên vật liệu dựa trên tham số đầu vào
    /// </summary>
    public async Task<MaterialRequirementViewModel> CalculateAsync(CalculateRequirementApiRequest req)
    {
        var vm = new MaterialRequirementViewModel
        {
            RequestId = req.RequestId,
            ProductionOrderCode = req.ProductionOrderCode,
            ProductId = req.ProductId,
            TargetQuantity = req.TargetQuantity,
            BomCode = req.BomCode,
            BomStandardOutput = 1m,
            RequiredDate = req.RequiredDate ?? DateTime.Now.AddDays(1),
            Note = req.Note
        };

        // Kiểm tra tính hợp lệ cơ bản
        if (vm.ProductId <= 0)
        {
            vm.ErrorMessage = "Vui lòng chọn sản phẩm cần sản xuất.";
            return vm;
        }

        if (vm.TargetQuantity <= 0)
        {
            vm.ErrorMessage = "Số lượng thành phẩm cần sản xuất phải lớn hơn 0.";
            return vm;
        }

        if (vm.BomStandardOutput <= 0)
        {
            vm.ErrorMessage = "Sản lượng chuẩn của BOM phải lớn hơn 0.";
            return vm;
        }

        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == vm.ProductId);
        if (product == null)
        {
            vm.ErrorMessage = "Không tìm thấy sản phẩm được chọn trong cơ sở dữ liệu.";
            return vm;
        }

        vm.ProductName = product.ProductName;
        vm.ProductUnit = product.Unit ?? "Hộp";

        // Kiểm tra xem đã có phiếu yêu cầu cấp vật tư chưa
        if (vm.RequestId.HasValue && vm.RequestId.Value > 0)
        {
            var reqId = vm.RequestId.Value;
            var existingSlip = await _context.MaterialReceipts
                .AsNoTracking()
                .Where(m => m.RequestId == reqId
                         && m.Status != "Cancelled" && m.Status != "Rejected")
                .OrderByDescending(m => m.ExpReceiptId)
                .FirstOrDefaultAsync();

            if (existingSlip != null)
            {
                vm.HasExistingSlip = true;
                vm.ExistingReceiptId = existingSlip.ExpReceiptId;
                vm.ExistingReceiptCode = existingSlip.DisplayCode;
                vm.ExistingReceiptStatus = existingSlip.Status switch
                {
                    "Completed" => "Được cấp",
                    "Approved" => "Được cấp",
                    "Pending" => "Chờ cấp",
                    _ => existingSlip.Status
                };
            }
        }

        await CalculateMaterialRequirementInternalAsync(vm);
        return vm;
    }

    /// <summary>
    /// Hàm tính toán chi tiết và đối chiếu tồn kho trên máy chủ
    /// </summary>
    private async Task CalculateMaterialRequirementInternalAsync(MaterialRequirementViewModel vm)
    {
        if (!vm.ProductId.HasValue || vm.ProductId <= 0) return;

        // Truy vấn các dòng BOM của sản phẩm này
        var query = _context.Boms
            .AsNoTracking()
            .Include(b => b.Ingredient)
            .Where(b => b.ProductId == vm.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(vm.BomCode))
        {
            query = query.Where(b => b.BomCode == vm.BomCode || b.BomName == vm.BomCode);
        }

        var bomItems = await query.ToListAsync();

        if (!bomItems.Any())
        {
            // Kiểm tra xem sản phẩm có bất kỳ BOM nào không
            var anyBom = await _context.Boms.AsNoTracking().AnyAsync(b => b.ProductId == vm.ProductId.Value);
            vm.HasBom = anyBom;
            vm.IsCalculated = false;
            vm.ErrorMessage = anyBom 
                ? "Không tìm thấy dữ liệu nguyên vật liệu cho mã BOM đã chọn."
                : $"Sản phẩm '{vm.ProductName}' chưa có định mức nguyên vật liệu (BOM) nào trong hệ thống.";
            return;
        }

        vm.HasBom = true;
        var firstBom = bomItems.First();
        vm.BomName = firstBom.BomName;
        vm.BomStandardOutput = 1m;

        // Lấy tồn kho sản xuất (WIP) cho Lệnh sản xuất đang chọn (lọc theo requestid)
        var wipDict = new Dictionary<int, decimal>();
        if (vm.RequestId.HasValue && vm.RequestId.Value > 0)
        {
            var reqId = vm.RequestId.Value;
            var wipRows = await _context.WipInventories
                .AsNoTracking()
                .Where(w => w.RequestId == reqId && w.IngrId.HasValue)
                .ToListAsync();

            wipDict = wipRows
                .GroupBy(w => w.IngrId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(w => w.Quantity ?? 0m));
        }

        // Kiểm tra tính hợp lệ của định mức BOM
        if (bomItems.Any(b => b.ItemQuantity <= 0 || b.MaterialQuantity <= 0 || b.Ingredient == null))
        {
            vm.IsCalculated = false;
            vm.ErrorMessage = "BOM thiếu nguyên vật liệu hoặc định mức/sản lượng gốc không hợp lệ. Hãy sửa BOM trước khi tính.";
            return;
        }

        var lines = new List<MaterialRequirementLineDto>();
        int stt = 1;

        foreach (var item in bomItems)
        {
            if (item.Ingredient == null) continue;

            var ingr = item.Ingredient;
            var bomNorm = BomUnitQuantity.PerUnit(item.MaterialQuantity, item.ItemQuantity);

            var ingrUnit = ingr.Unit?.Trim() ?? "kg";
            var stockUnit = ingr.Unit?.Trim() ?? "kg";

            // Định mức hiển thị cho 1 thành phẩm; tính từ tỷ lệ gốc để không làm tròn sớm.
            bool isDiscrete = DiscreteUnits.Contains(ingrUnit);
            decimal totalRequired = BomUnitQuantity.Required(
                item.MaterialQuantity, item.ItemQuantity, vm.TargetQuantity, isDiscrete);

            // Tồn kho thực tế tại kho nguyên vật liệu chính (Kho tổng)
            decimal? availableStock = ingr.Quantity;
            bool hasStock = availableStock.HasValue;

            // Tồn kho tại phân xưởng sản xuất (WIP Inventory) theo đúng Lệnh sản xuất đang chọn
            decimal wipStock = wipDict.TryGetValue(ingr.IngrId, out var ws) ? ws : 0m;

            // Kiểm tra cùng đơn vị tính
            bool isUnitMismatch = !string.Equals(ingrUnit, stockUnit, StringComparison.OrdinalIgnoreCase);

            decimal shortage;
            string statusText;
            string noteText;

            if (isUnitMismatch)
            {
                shortage = totalRequired;
                statusText = "Lệch ĐVT";
                noteText = $"Đơn vị tính BOM ({ingrUnit}) khác kho ({stockUnit}), chưa hỗ trợ quy đổi tự động";
            }
            else if (wipStock >= totalRequired)
            {
                shortage = 0m;
                statusText = "Đủ tồn SX";
                decimal surplus = wipStock - totalRequired;
                noteText = surplus > 0
                    ? $"Đủ cấp cho lệnh (Dư tại xưởng: {surplus:N2} {ingrUnit})"
                    : "Vừa đủ tồn kho sản xuất cho lệnh này";
            }
            else
            {
                shortage = totalRequired - wipStock;
                statusText = "Thiếu tồn SX";
                var mainStockDesc = hasStock ? $"Kho tổng hiện có: {availableStock!.Value:N2} {stockUnit}" : "Kho tổng chưa có số liệu";
                noteText = $"Thiếu hụt {shortage:N2} {ingrUnit} tại kho sản xuất ({mainStockDesc})";
            }

            lines.Add(new MaterialRequirementLineDto
            {
                Stt = stt++,
                IngrId = ingr.IngrId,
                IngrName = ingr.IngrName,
                Unit = ingrUnit,
                StockUnit = stockUnit,
                IsUnitMismatch = isUnitMismatch,
                BomNorm = bomNorm,
                BomStandardOutput = 1m,
                TotalRequired = totalRequired,
                AvailableStock = availableStock,
                WipStock = wipStock,
                Shortage = shortage,
                IsDiscrete = isDiscrete,
                StatusText = statusText,
                Note = noteText
            });
        }

        vm.Lines = lines;
        vm.IsCalculated = true;
        vm.CheckedAt = DateTime.Now;
        vm.TotalMaterialTypes = lines.Count;
        vm.SufficientCount = lines.Count(l => l.IsSufficient);
        vm.ShortageCount = lines.Count(l => l.Shortage > 0);
        vm.UnknownStockCount = lines.Count(l => !l.HasStockData);

        if (lines.Any(l => l.IsUnitMismatch))
        {
            vm.Conclusion = "Có nguyên vật liệu bị lệch đơn vị tính";
        }
        else if (vm.ShortageCount == 0 && vm.TotalMaterialTypes > 0)
        {
            vm.Conclusion = "Đủ nguyên vật liệu tại kho sản xuất";
        }
        else
        {
            vm.Conclusion = $"Cần cấp thêm NVL cho xưởng ({vm.ShortageCount} loại thiếu)";
        }
    }

    /// <summary>
    /// Tạo phiếu yêu cầu cấp nguyên vật liệu gửi kho (Material Requisition)
    /// </summary>
    public async Task<(bool Success, string Message, int ReceiptId, string ReceiptCode)> CreateRequisitionSlipAsync(CreateRequisitionApiRequest req, int requestedByUserId)
    {
        // 1. Kiểm tra tính hợp lệ cơ bản & danh tính người dùng
        if (requestedByUserId <= 0)
            return (false, "Chưa xác định được danh tính người dùng hợp lệ (chưa đăng nhập). Thao tác tạo phiếu yêu cầu cấp nguyên vật liệu bị từ chối.", 0, "");

        if (req.ProductId <= 0)
            return (false, "Sản phẩm không hợp lệ.", 0, "");

        if (req.TargetQuantity <= 0)
            return (false, "Số lượng sản xuất phải lớn hơn 0.", 0, "");

        // 2. Nghiệp vụ: Phải liên kết với Lệnh sản xuất trước khi tạo phiếu cấp chính thức
        if (string.IsNullOrWhiteSpace(req.ProductionOrderCode))
            return (false, "Chỉ được tạo phiếu cấp nguyên vật liệu chính thức khi đã liên kết với Lệnh sản xuất (Production Order) hợp lệ.", 0, "");

        // 3. Kiểm tra xem đã có phiếu đang xử lý cho cùng lệnh/nhu cầu chưa
        var existingSlip = await _context.MaterialReceipts
            .AsNoTracking()
            .Where(m => req.RequestId.HasValue && m.RequestId == req.RequestId.Value
                     && m.Status != "Cancelled" && m.Status != "Rejected")
            .OrderByDescending(m => m.ExpReceiptId)
            .FirstOrDefaultAsync();

        if (existingSlip != null)
        {
            var existingCode = existingSlip.DisplayCode;
            return (false, $"Lệnh sản xuất '{req.ProductionOrderCode}' đã có phiếu yêu cầu cấp vật tư đang xử lý: {existingCode} (Trạng thái: {existingSlip.Status}).", existingSlip.ExpReceiptId, existingCode);
        }

        // 4. Tính toán lại nhu cầu ở máy chủ để bảo đảm dữ liệu tin cậy
        var calcVm = new MaterialRequirementViewModel
        {
            ProductId = req.ProductId,
            TargetQuantity = req.TargetQuantity,
            BomCode = req.BomCode,
            BomStandardOutput = 1m
        };

        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == req.ProductId);
        if (product == null)
            return (false, "Sản phẩm không tồn tại.", 0, "");

        calcVm.ProductName = product.ProductName;
        await CalculateMaterialRequirementInternalAsync(calcVm);

        if (!calcVm.IsCalculated || !calcVm.Lines.Any())
            return (false, "Không thể tạo phiếu vì không tìm thấy dữ liệu định mức BOM hợp lệ.", 0, "");

        // Kiểm tra lỗi lệch đơn vị tính nếu có
        if (calcVm.Lines.Any(l => l.IsUnitMismatch))
            return (false, "Không thể tạo phiếu do có nguyên liệu bị lệch đơn vị tính giữa BOM và kho.", 0, "");

        // 5. Lưu phiếu và chi tiết vào cơ sở dữ liệu trong Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = new MaterialReceipt
            {
                RequestedBy = requestedByUserId > 0 ? requestedByUserId : (int?)null,
                ApprovedBy = null, // Chờ thủ kho hoặc quản lý phân xưởng duyệt
                CreatedDate = DateTime.UtcNow,
                Status = "Pending", // Khởi tạo ở trạng thái chờ duyệt xuất
                RequestId = req.RequestId,
                ProductionOrderCode = req.ProductionOrderCode,
                BomCode = req.BomCode,
                BomName = calcVm.BomName,
                RequiredDate = req.RequiredDate.HasValue
                    ? DateTime.SpecifyKind(req.RequiredDate.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow.AddDays(1),
                Note = !string.IsNullOrWhiteSpace(req.Note)
                    ? (req.Note.Length > 255 ? req.Note.Substring(0, 255) : req.Note)
                    : $"Yêu cầu cấp nguyên vật liệu phục vụ {req.ProductionOrderCode} ({calcVm.ProductName})"
            };

            _context.MaterialReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            // Thêm chi tiết từng dòng nguyên vật liệu
            // Yêu cầu cấp lượng còn thiếu cho kho sản xuất (hoặc toàn bộ nếu xưởng chưa có)
            foreach (var line in calcVm.Lines)
            {
                decimal neededQty = (line.Shortage.HasValue && line.Shortage.Value > 0) ? line.Shortage.Value : line.TotalRequired;
                string detailNote;
                if (line.Shortage > 0)
                {
                    detailNote = $"Yêu cầu cấp {neededQty:N2} {line.Unit} (Tồn xưởng: {line.WipStock:N2} {line.Unit}, Thiếu: {line.Shortage:N2} {line.Unit})";
                }
                else
                {
                    detailNote = $"Xưởng đã đủ tồn kho sản xuất ({line.WipStock:N2} {line.Unit}) cho Lô SX {req.ProductionOrderCode}";
                }

                if (detailNote.Length > 255)
                {
                    detailNote = detailNote.Substring(0, 255);
                }

                var detail = new MaterialReceiptDetail
                {
                    ExpReceiptId = receipt.ExpReceiptId,
                    IngrId = line.IngrId,
                    Quantity = neededQty,
                    BomNorm = line.BomNorm,
                    Unit = line.Unit,
                    RequiredQuantity = line.TotalRequired,
                    AvailableQuantity = line.AvailableStock,
                    ShortageQuantity = line.Shortage,
                    Status = "Pending",
                    Note = detailNote,
                    CreatedDate = DateTime.UtcNow
                };

                _context.MaterialReceiptDetails.Add(detail);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            string receiptCode = receipt.DisplayCode;
            return (true, $"Tạo phiếu yêu cầu cấp nguyên vật liệu thành công: {receiptCode}", receipt.ExpReceiptId, receiptCode);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi lưu cơ sở dữ liệu: {ex.Message}", 0, "");
        }
    }
}
