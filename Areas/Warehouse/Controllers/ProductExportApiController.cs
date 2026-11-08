using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using httt.Data;
using httt.Models;
using httt.Areas.Warehouse.Models;

namespace httt.Areas.Warehouse.Controllers;

[ApiController]
[Route("api/warehouse/product-exports")]
[Produces("application/json")]
[Tags("7. Xuất kho thành phẩm (Product Exports)")]
public class ProductExportApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductExportApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/warehouse/product-exports : Danh sách tất cả lệnh/phiếu xuất kho thành phẩm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProductExports([FromQuery] string? status = null, [FromQuery] string? keyword = null)
    {
        await SeedSampleDataIfEmptyAsync();

        var query = _context.ProductExportReceipts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
        {
            if (status == "Chờ xuất kho" || status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Status == "Pending" || e.Status == "Chờ xuất kho");
            else if (status == "Đã xuất kho" || status.Equals("Approved", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Status == "Approved" || e.Status == "Đã xuất kho" || e.Status == "Completed");
            else if (status == "Từ chối" || status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Status == "Rejected" || e.Status == "Từ chối");
        }

        var list = await query.OrderByDescending(e => e.CreatedDate).ToListAsync();
        var receiptIds = list.Select(e => e.ExportReceiptId).ToList();

        var details = await _context.ProductExportReceiptDetails
            .AsNoTracking()
            .Where(d => receiptIds.Contains(d.ExportReceiptId))
            .ToListAsync();

        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products
            .AsNoTracking()
            .Where(p => prodIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var orderIds = list.Select(e => e.OrderId).Where(o => o.HasValue).Select(o => o!.Value).Distinct().ToList();
        var saleOrders = await _context.SaleOrders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.OrderId))
            .ToDictionaryAsync(o => o.OrderId);

        var userIds = list.Select(e => e.RequestedBy).Where(u => u.HasValue).Select(u => u!.Value).Distinct().ToList();
        var users = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserID))
            .ToDictionaryAsync(u => u.UserID);

        var result = list.Select(e =>
        {
            var eDetails = details.Where(d => d.ExportReceiptId == e.ExportReceiptId).ToList();
            saleOrders.TryGetValue(e.OrderId ?? 0, out var so);
            users.TryGetValue(e.RequestedBy ?? 0, out var u);

            string creatorName = u != null ? (!string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username) : "Nguyễn Tiến Huy";
            string customerName = so?.CustomerName ?? "Công ty Dược phẩm / Đại lý";
            string orderCode = so?.OrderCode ?? (e.OrderId.HasValue ? $"DH{e.OrderId:D5}" : "");

            string displayStatus = "Chờ xuất kho";
            string statusType = "warning";
            if (e.Status == "Approved" || e.Status == "Đã xuất kho" || e.Status == "Completed")
            {
                displayStatus = "Đã xuất kho";
                statusType = "success";
            }
            else if (e.Status == "Rejected" || e.Status == "Từ chối")
            {
                displayStatus = "Từ chối";
                statusType = "danger";
            }

            var items = eDetails.Select((d, idx) =>
            {
                prods.TryGetValue(d.ProductId, out var prod);
                var cost = d.Price ?? prod?.Cost ?? 0;
                var stockQty = prod?.Quantity ?? 0;
                var isOutOfStock = stockQty < d.Quantity;

                return new
                {
                    id = idx + 1,
                    productId = d.ProductId,
                    itemCode = $"SP{d.ProductId:D3}",
                    productCode = $"SP{d.ProductId:D3}",
                    itemName = prod?.ProductName ?? "N/A",
                    productName = prod?.ProductName ?? "N/A",
                    unit = prod?.Unit ?? "Hộp",
                    quantity = d.Quantity,
                    price = cost,
                    amount = d.Quantity * cost,
                    status = d.Status,
                    note = d.Note ?? "",
                    stockQty,
                    isOutOfStock
                };
            }).ToList();

            string desc = eDetails.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note))?.Note
                ?? (so != null ? $"Xuất kho bán hàng cho {so.CustomerName} (Đơn #{orderCode})" : $"Xuất kho thành phẩm theo lệnh {e.ExpReceiptCode}");

            var hasStockIssue = items.Any(it => it.isOutOfStock);
            var isPending = e.Status == "Pending" || e.Status == "Chờ xuất kho";
            var canApprove = isPending && !hasStockIssue;

            return new
            {
                id = e.ExportReceiptId,
                exportReceiptId = e.ExportReceiptId,
                voucherNo = e.ExpReceiptCode ?? $"PXK_BH{e.ExportReceiptId:D4}",
                orderNo = e.ExpReceiptCode ?? $"PXK_BH{e.ExportReceiptId:D4}",
                voucherDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                orderDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
                createdDate = e.CreatedDate.HasValue ? e.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                warehouse = "Kho thành phẩm GMP",
                partnerName = customerName,
                customer = customerName,
                receiver = customerName,
                customerPhone = so?.CustomerPhone ?? "",
                shippingAddress = so?.ShippingAddress ?? "",
                orderId = e.OrderId,
                orderCode,
                description = desc,
                status = displayStatus,
                rawStatus = e.Status,
                orderStatus = displayStatus,
                statusType,
                creatorName,
                employeeName = creatorName,
                totalItems = items.Count,
                totalQuantity = items.Sum(i => i.quantity),
                totalAmount = items.Sum(i => i.amount),
                hasStockIssue,
                canApprove,
                items
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            result = result.Where(r =>
                (r.voucherNo != null && r.voucherNo.ToLower().Contains(kw)) ||
                (r.partnerName != null && r.partnerName.ToLower().Contains(kw)) ||
                (r.description != null && r.description.ToLower().Contains(kw)) ||
                (r.orderStatus != null && r.orderStatus.ToLower().Contains(kw)) ||
                (r.orderCode != null && r.orderCode.ToLower().Contains(kw))
            ).ToList();
        }

        return Ok(result);
    }

    /// <summary>
    /// GET /api/warehouse/product-exports/{id} : Xem chi tiết đơn xuất bán thành phẩm
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductExportById(int id)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        var prodIds = details.Select(d => d.ProductId).Distinct().ToList();
        var prods = await _context.Products.Where(p => prodIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

        SaleOrder? so = null;
        if (receipt.OrderId.HasValue)
        {
            so = await _context.SaleOrders.FindAsync(receipt.OrderId.Value);
        }

        UserAccount? u = null;
        if (receipt.RequestedBy.HasValue)
        {
            u = await _context.UserAccounts.FindAsync(receipt.RequestedBy.Value);
        }

        UserAccount? au = null;
        if (receipt.ApprovedBy.HasValue)
        {
            au = await _context.UserAccounts.FindAsync(receipt.ApprovedBy.Value);
        }

        string creatorName = u != null ? (!string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username) : "Nguyễn Tiến Huy";
        string approverName = au != null ? (!string.IsNullOrWhiteSpace(au.FullName) ? au.FullName : au.Username) : "";
        string customerName = so?.CustomerName ?? "Công ty Dược phẩm / Đại lý";
        string orderCode = so?.OrderCode ?? (receipt.OrderId.HasValue ? $"DH{receipt.OrderId:D5}" : "");

        string displayStatus = "Chờ xuất kho";
        string statusType = "warning";
        if (receipt.Status == "Approved" || receipt.Status == "Đã xuất kho" || receipt.Status == "Completed")
        {
            displayStatus = "Đã xuất kho";
            statusType = "success";
        }
        else if (receipt.Status == "Rejected" || receipt.Status == "Từ chối")
        {
            displayStatus = "Từ chối";
            statusType = "danger";
        }

        var items = details.Select((d, idx) =>
        {
            prods.TryGetValue(d.ProductId, out var prod);
            var cost = d.Price ?? prod?.Cost ?? 0;
            var stockQty = prod?.Quantity ?? 0;
            var isOutOfStock = stockQty < d.Quantity;

            return new
            {
                id = idx + 1,
                productId = d.ProductId,
                itemCode = $"SP{d.ProductId:D3}",
                productCode = $"SP{d.ProductId:D3}",
                itemName = prod?.ProductName ?? "N/A",
                productName = prod?.ProductName ?? "N/A",
                unit = prod?.Unit ?? "Hộp",
                quantity = d.Quantity,
                reqQty = d.Quantity,
                actualQty = d.Quantity,
                price = cost,
                amount = d.Quantity * cost,
                status = d.Status,
                note = d.Note ?? "",
                stockQty,
                isOutOfStock
            };
        }).ToList();

        var hasStockIssue = items.Any(it => it.isOutOfStock);
        var isPending = receipt.Status == "Pending" || receipt.Status == "Chờ xuất kho";
        var canApprove = isPending && !hasStockIssue;

        string approvalBlockReason = "";
        if (hasStockIssue)
        {
            var firstOut = items.First(it => it.isOutOfStock);
            approvalBlockReason = $"Không đủ tồn kho: '{firstOut.itemName}' chỉ còn {firstOut.stockQty} {firstOut.unit} (yêu cầu xuất {firstOut.quantity} {firstOut.unit}).";
        }

        string desc = details.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Note))?.Note
            ?? (so != null ? $"Xuất kho bán hàng cho {so.CustomerName} (Đơn #{orderCode})" : $"Xuất kho thành phẩm theo lệnh {receipt.ExpReceiptCode}");

        return Ok(new
        {
            receiptId = receipt.ExportReceiptId,
            exportReceiptId = receipt.ExportReceiptId,
            voucherNo = receipt.ExpReceiptCode ?? $"PXK_BH{receipt.ExportReceiptId:D4}",
            orderNo = receipt.ExpReceiptCode ?? $"PXK_BH{receipt.ExportReceiptId:D4}",
            voucherType = "outbound",
            voucherDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            orderDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy") : "",
            createdDate = receipt.CreatedDate.HasValue ? receipt.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
            status = displayStatus,
            rawStatus = receipt.Status,
            orderStatus = displayStatus,
            statusType,
            warehouse = "Kho thành phẩm GMP",
            partnerName = customerName,
            customer = customerName,
            receiver = customerName,
            customerPhone = so?.CustomerPhone ?? "",
            shippingAddress = so?.ShippingAddress ?? "",
            orderId = receipt.OrderId,
            orderCode,
            description = desc,
            creatorName,
            employeeName = creatorName,
            employeeCode = u != null ? $"NV{u.UserID:D4}" : "NV0005",
            approverName,
            canApprove,
            hasStockIssue,
            approvalBlockReason,
            totalItems = items.Count,
            totalQuantity = items.Sum(i => i.quantity),
            totalAmount = items.Sum(i => i.amount),
            items
        });
    }

    /// <summary>
    /// PUT /api/warehouse/product-exports/{id}/approve : Duyệt xuất hàng thành phẩm -> Trừ tồn kho thành phẩm trong CSDL
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveProductExport(int id)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        if (receipt.Status == "Approved" || receipt.Status == "Đã xuất kho" || receipt.Status == "Completed")
        {
            return BadRequest(new { message = "Lệnh xuất hàng này đã được duyệt xuất kho trước đó." });
        }

        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();

        // 1. Kiểm tra tồn kho thành phẩm
        foreach (var d in details)
        {
            var prod = await _context.Products.FindAsync(d.ProductId);
            if (prod == null)
            {
                return BadRequest(new { message = $"Không tìm thấy sản phẩm có ID {d.ProductId} trong kho." });
            }
            if ((prod.Quantity ?? 0) < d.Quantity)
            {
                return BadRequest(new { message = $"Không đủ tồn kho để xuất: '{prod.ProductName}' chỉ còn {prod.Quantity ?? 0} {prod.Unit} (yêu cầu xuất {d.Quantity} {prod.Unit})." });
            }
        }

        // 2. Trừ tồn kho và đổi trạng thái
        foreach (var d in details)
        {
            var prod = await _context.Products.FindAsync(d.ProductId);
            if (prod != null)
            {
                prod.Quantity = Math.Max(0, (prod.Quantity ?? 0) - d.Quantity);
            }
            d.Status = "Approved";
        }

        receipt.Status = "Approved";
        var firstUser = await _context.UserAccounts.AsNoTracking().FirstOrDefaultAsync();
        if (firstUser != null) receipt.ApprovedBy = firstUser.UserID;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Duyệt xuất kho thành phẩm {receipt.ExpReceiptCode} thành công. Đã trừ tồn kho tương ứng.",
            exportReceiptId = receipt.ExportReceiptId,
            status = "Approved",
            orderStatus = "Đã xuất kho"
        });
    }

    /// <summary>
    /// PUT /api/warehouse/product-exports/{id}/reject : Từ chối xuất hàng thành phẩm
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectProductExport(int id, [FromBody] RejectDto? dto)
    {
        var receipt = await _context.ProductExportReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { message = $"Không tìm thấy lệnh xuất hàng ID {id}." });
        }

        receipt.Status = "Rejected";
        var details = await _context.ProductExportReceiptDetails.Where(d => d.ExportReceiptId == id).ToListAsync();
        foreach (var d in details)
        {
            d.Status = "Rejected";
            if (!string.IsNullOrWhiteSpace(dto?.Reason)) d.Note = dto.Reason;
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            success = true,
            message = $"Đã từ chối lệnh xuất hàng {receipt.ExpReceiptCode}.",
            exportReceiptId = receipt.ExportReceiptId,
            status = "Rejected",
            orderStatus = "Từ chối"
        });
    }

    /// <summary>
    /// POST /api/warehouse/product-exports : Tạo mới phiếu xuất kho thành phẩm
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProductExport([FromBody] CreateProductExportDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn ít nhất một thành phẩm cần xuất kho." });
        }

        var now = DateTime.UtcNow;

        // Tạo sale order mới nếu có thông tin khách hàng mà chưa có orderId
        int? orderId = dto.OrderId;
        if (!orderId.HasValue && !string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            var so = new SaleOrder
            {
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone?.Trim(),
                ShippingAddress = dto.ShippingAddress?.Trim(),
                OrderDate = now,
                Status = "Approved",
                OrderCode = $"DH{now:yyyyMMddHHmmss}"
            };
            _context.SaleOrders.Add(so);
            await _context.SaveChangesAsync();
            orderId = so.OrderId;
        }

        var maxId = await _context.ProductExportReceipts.MaxAsync(e => (int?)e.ExportReceiptId) ?? 0;
        var nextId = maxId + 1;

        var receipt = new ProductExportReceipt
        {
            ExportReceiptId = nextId,
            ExpReceiptCode = !string.IsNullOrWhiteSpace(dto.ExpReceiptCode) ? dto.ExpReceiptCode : $"PXK_BH{nextId:D4}",
            OrderId = orderId,
            CreatedDate = now,
            Status = dto.Status ?? "Chờ xuất kho",
            RequestedBy = dto.RequestedBy ?? (await _context.UserAccounts.Select(u => (int?)u.UserID).FirstOrDefaultAsync())
        };

        _context.ProductExportReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var it in dto.Items)
        {
            var detail = new ProductExportReceiptDetail
            {
                ExportReceiptId = receipt.ExportReceiptId,
                ProductId = it.ProductId,
                Quantity = it.Quantity,
                Price = it.Price,
                Status = receipt.Status,
                Note = it.Note,
                CreatedDate = now
            };
            _context.ProductExportReceiptDetails.Add(detail);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Tạo phiếu xuất kho thành phẩm {receipt.ExpReceiptCode} thành công.",
            exportReceiptId = receipt.ExportReceiptId,
            expReceiptCode = receipt.ExpReceiptCode
        });
    }

    /// <summary>
    /// GET /api/warehouse/product-exports/sale-orders : Danh sách đơn bán hàng để liên kết khi tạo phiếu xuất
    /// </summary>
    [HttpGet("sale-orders")]
    public async Task<IActionResult> GetSaleOrders()
    {
        var orders = await _context.SaleOrders.AsNoTracking().OrderByDescending(o => o.OrderDate).ToListAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Tự động khởi tạo dữ liệu mẫu thực tế nếu bảng ProductExportReceipts trong DB đang trống
    /// </summary>
    private async Task SeedSampleDataIfEmptyAsync()
    {
        if (await _context.ProductExportReceipts.AnyAsync()) return;

        var prods = await _context.Products.Take(4).ToListAsync();
        if (prods.Count == 0) return;

        var user = await _context.UserAccounts.FirstOrDefaultAsync();
        var now = DateTime.UtcNow;

        // 1. Tạo Đơn bán hàng 1
        var so1 = new SaleOrder
        {
            CustomerName = "CÔNG TY DƯỢC PHẨM TRUNG ƯƠNG 1 (CPC1)",
            CustomerPhone = "024.3845.6789",
            ShippingAddress = "160 Tôn Đức Thắng, Hàng Bột, Đống Đa, Hà Nội",
            OrderDate = now.AddHours(-4),
            Status = "Approved",
            OrderCode = "DH00001",
            TotalAmount = 25000000
        };
        _context.SaleOrders.Add(so1);

        // 2. Tạo Đơn bán hàng 2
        var so2 = new SaleOrder
        {
            CustomerName = "HỆ THỐNG NHÀ THUỐC FPT LONG CHÂU",
            CustomerPhone = "1800.6928",
            ShippingAddress = "379-381 Hai Bà Trưng, P. Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh",
            OrderDate = now.AddDays(-1),
            Status = "Approved",
            OrderCode = "DH00002",
            TotalAmount = 45000000
        };
        _context.SaleOrders.Add(so2);

        // 3. Tạo Đơn bán hàng 3
        var so3 = new SaleOrder
        {
            CustomerName = "CÔNG TY CỔ PHẦN DƯỢC PHẨM PHARMACITY",
            CustomerPhone = "1800.6821",
            ShippingAddress = "248A Nơ Trang Long, P. 12, Q. Bình Thạnh, TP. Hồ Chí Minh",
            OrderDate = now.AddDays(-3),
            Status = "Approved",
            OrderCode = "DH00003",
            TotalAmount = 32000000
        };
        _context.SaleOrders.Add(so3);

        await _context.SaveChangesAsync();

        // 4. Tạo Phiếu xuất 1 (Chờ xuất kho)
        var pxk1 = new ProductExportReceipt
        {
            ExportReceiptId = 1,
            ExpReceiptCode = "PXK_BH0001",
            OrderId = so1.OrderId,
            RequestedBy = user?.UserID,
            CreatedDate = now.AddHours(-3),
            Status = "Chờ xuất kho"
        };
        _context.ProductExportReceipts.Add(pxk1);

        // 5. Tạo Phiếu xuất 2 (Đã xuất kho)
        var pxk2 = new ProductExportReceipt
        {
            ExportReceiptId = 2,
            ExpReceiptCode = "PXK_BH0002",
            OrderId = so2.OrderId,
            RequestedBy = user?.UserID,
            ApprovedBy = user?.UserID,
            CreatedDate = now.AddDays(-1),
            Status = "Đã xuất kho"
        };
        _context.ProductExportReceipts.Add(pxk2);

        // 6. Tạo Phiếu xuất 3 (Chờ xuất kho)
        var pxk3 = new ProductExportReceipt
        {
            ExportReceiptId = 3,
            ExpReceiptCode = "PXK_BH0003",
            OrderId = so3.OrderId,
            RequestedBy = user?.UserID,
            CreatedDate = now.AddHours(-1),
            Status = "Chờ xuất kho"
        };
        _context.ProductExportReceipts.Add(pxk3);

        await _context.SaveChangesAsync();

        // Chi tiết cho PXK1
        _context.ProductExportReceiptDetails.Add(new ProductExportReceiptDetail
        {
            ExportReceiptId = pxk1.ExportReceiptId,
            ProductId = prods[0].ProductId,
            Quantity = 50,
            Price = prods[0].Cost ?? 120000,
            Status = "Chờ xuất kho",
            Note = "Xuất kho bán hàng theo đơn #DH00001 cho CPC1",
            CreatedDate = now.AddHours(-3)
        });
        if (prods.Count > 1)
        {
            _context.ProductExportReceiptDetails.Add(new ProductExportReceiptDetail
            {
                ExportReceiptId = pxk1.ExportReceiptId,
                ProductId = prods[1].ProductId,
                Quantity = 30,
                Price = prods[1].Cost ?? 180000,
                Status = "Chờ xuất kho",
                Note = "Xuất kho bán hàng theo đơn #DH00001 cho CPC1",
                CreatedDate = now.AddHours(-3)
            });
        }

        // Chi tiết cho PXK2
        _context.ProductExportReceiptDetails.Add(new ProductExportReceiptDetail
        {
            ExportReceiptId = pxk2.ExportReceiptId,
            ProductId = prods[0].ProductId,
            Quantity = 100,
            Price = prods[0].Cost ?? 120000,
            Status = "Đã xuất kho",
            Note = "Đã giao hàng và hóa đơn cho Long Châu",
            CreatedDate = now.AddDays(-1)
        });

        // Chi tiết cho PXK3
        if (prods.Count > 2)
        {
            _context.ProductExportReceiptDetails.Add(new ProductExportReceiptDetail
            {
                ExportReceiptId = pxk3.ExportReceiptId,
                ProductId = prods[2].ProductId,
                Quantity = 40,
                Price = prods[2].Cost ?? 250000,
                Status = "Chờ xuất kho",
                Note = "Xuất đơn hàng cho Pharmacity",
                CreatedDate = now.AddHours(-1)
            });
        }

        await _context.SaveChangesAsync();
    }
}

public class CreateProductExportDto
{
    public string? ExpReceiptCode { get; set; }
    public int? OrderId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Status { get; set; } = "Chờ xuất kho";
    public int? RequestedBy { get; set; }
    public List<CreateProductExportItemDto> Items { get; set; } = new();
}

public class CreateProductExportItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? Price { get; set; }
    public string? Note { get; set; }
}
