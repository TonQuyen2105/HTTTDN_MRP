/**
 * Areas/Warehouse/Models/warehouseMockData.js
 * ---------------------------------------------------------
 * [MODEL LAYER - MVC]
 * Quản lý dữ liệu Mock (Dược phẩm MRP & dữ liệu chuẩn theo ảnh thiết kế)
 * và trạng thái (State) của phân hệ Kho hàng (Warehouse):
 * 1. Bàn làm việc (ban-lam-viec)
 * 2. Tổng quan (tong-quan)
 * 3. Nhập kho (nhap-kho) + Phiếu nhập kho mua hàng (PNK00005)
 * 4. Xuất kho (xuat-kho) + Phiếu xuất kho bán hàng (PXK00002)
 */

const WarehouseModel = (function () {
    const DOT_COLORS = ['#84cc16', '#f43f5e', '#eab308', '#8b5cf6', '#ec4899'];

    // 1. Cấu hình danh mục Sidebar (tái sử dụng cho SidebarComponent)
    const sidebarMenu = [
        {
            id: 'ban-lam-viec',
            title: 'Bàn làm việc',
            icon: 'workspace',
            badge: 6,
            section: 'top'
        },
        {
            id: 'tong-quan',
            title: 'Tổng quan',
            icon: 'overview',
            section: 'top',
            isDefault: true
        },
        {
            id: 'danh-muc-vthh',
            title: 'Danh mục VTHH',
            icon: 'items',
            section: 'top'
        },
        {
            id: 'quan-ly-kho',
            title: 'Quản lý kho',
            icon: 'warehouse',
            section: 'middle'
        },
        {
            id: 'hoat-dong-kho',
            title: 'Hoạt động kho',
            icon: 'operations',
            section: 'middle',
            expanded: false,
            children: [
                { id: 'nhap-kho', title: 'Nhập kho' },
                { id: 'xuat-kho', title: 'Xuất kho' },
                { id: 'dieu-chuyen', title: 'Điều chuyển' },
                { id: 'kiem-ke', title: 'Kiểm kê' }
            ]
        },
        {
            id: 'bao-cao',
            title: 'Báo cáo',
            icon: 'report',
            section: 'middle'
        },
        {
            id: 'danh-muc',
            title: 'Danh mục',
            icon: 'category',
            hasArrowRight: true,
            section: 'bottom'
        },
        {
            id: 'he-thong',
            title: 'Hệ thống',
            icon: 'system',
            hasArrowRight: true,
            section: 'bottom'
        }
    ];

    // 2. Dữ liệu màn hình "Bàn làm việc"
    const workspaceData = {
        dateText: 'Thứ 7, 10/05/2025',
        userName: 'Nguyễn Tiến Huy',
        greetingSub: 'Chúc bạn một ngày làm việc hiệu quả.',
        promoText: 'Thiết lập sơ đồ kho để tối ưu không gian lưu trữ và nâng cao hiệu suất vận hành ngay hôm nay!',
        orderCard: {
            id: 'ws-orders',
            title: 'Đơn đặt hàng',
            totalPending: 45,
            segments: [
                { label: 'Chưa thực hiện', percentText: '56,96%', percent: 56.96, count: 45, color: '#84cc16' },
                { label: 'Đang thực hiện', percentText: '0,00%', percent: 0, count: 0, color: '#6366f1' },
                { label: 'Quá hạn', percentText: '43,04%', percent: 43.04, count: 34, color: '#f97316', chartColor: '#6366f1' }
            ]
        },
        outboundCommandCard: {
            id: 'ws-outbound-cmd',
            title: 'Lệnh xuất kho',
            totalPending: 10,
            items: [
                { label: 'Chưa thực hiện (10...', fullLabel: 'Chưa thực hiện (100,00%)', count: 10, color: '#84cc16' },
                { label: 'Đang thực hiện (0,...', fullLabel: 'Đang thực hiện (0,00%)', count: 0, color: '#ec4899' },
                { label: 'Chờ xác nhận (0,0...', fullLabel: 'Chờ xác nhận (0,00%)', count: 0, color: '#6366f1' },
                { label: 'Quá hạn (0,00%)', fullLabel: 'Quá hạn (0,00%)', count: 0, color: '#0ea5e9' }
            ]
        }
    };

    // 3. Dữ liệu màn hình "Tổng quan"
    const defaultDatasets = {
        belowMinStock: [
            { id: 1, name: 'Epson EB-X06', warehouse: 'Ba Đình', currentStock: 6, minStock: 10, color: DOT_COLORS[0] },
            { id: 2, name: 'BenQ MS550', warehouse: 'Ba Đình', currentStock: 8, minStock: 10, color: DOT_COLORS[1] },
            { id: 3, name: 'Sony VPL-EX430', warehouse: 'Ba Đình', currentStock: 3, minStock: 10, color: DOT_COLORS[2] },
            { id: 4, name: 'ViewSonic PA503W', warehouse: 'Bắc Ninh', currentStock: 2, minStock: 10, color: DOT_COLORS[3] },
            { id: 5, name: 'Xiaomi Mi Smart Projector 2', warehouse: 'Bắc Ninh', currentStock: 5, minStock: 10, color: DOT_COLORS[4] }
        ],
        aboveMaxStock: [
            { id: 1, name: 'Epson EB-X06', warehouse: 'Ba Đình', currentStock: 523, maxStock: 500, color: DOT_COLORS[0] },
            { id: 2, name: 'BenQ MS550', warehouse: 'Ba Đình', currentStock: 602, maxStock: 500, color: DOT_COLORS[1] },
            { id: 3, name: 'Sony VPL-EX430', warehouse: 'Ba Đình', currentStock: 578, maxStock: 500, color: DOT_COLORS[2] },
            { id: 4, name: 'ViewSonic PA503W', warehouse: 'Bắc Ninh', currentStock: 513, maxStock: 500, color: DOT_COLORS[3] },
            { id: 5, name: 'Xiaomi Mi Smart Projector 2', warehouse: 'Bắc Ninh', currentStock: 645, maxStock: 500, color: DOT_COLORS[4] }
        ],
        expiringSoonByDays: {
            5: [
                { id: 1, name: 'Ginkgo Biloba 120mg Trunature', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 6, color: DOT_COLORS[0] },
                { id: 2, name: 'Glucosamine Orihiro 900 viên', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 8, color: DOT_COLORS[1] },
                { id: 3, name: 'Glucosamine HCL 1500mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 3, color: DOT_COLORS[2] }
            ],
            15: [
                { id: 1, name: 'Ginkgo Biloba 120mg Trunature', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 6, color: DOT_COLORS[0] },
                { id: 2, name: 'Glucosamine Orihiro 900 viên', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 8, color: DOT_COLORS[1] },
                { id: 3, name: 'Glucosamine HCL 1500mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 3, color: DOT_COLORS[2] },
                { id: 4, name: 'LineaBon K2 + D3', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 2, color: DOT_COLORS[3] }
            ],
            30: [
                { id: 1, name: 'Ginkgo Biloba 120mg Trunature', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 6, color: DOT_COLORS[0] },
                { id: 2, name: 'Glucosamine Orihiro 900 viên', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 8, color: DOT_COLORS[1] },
                { id: 3, name: 'Glucosamine HCL 1500mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 3, color: DOT_COLORS[2] },
                { id: 4, name: 'LineaBon K2 + D3', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 2, color: DOT_COLORS[3] },
                { id: 5, name: 'Milk Thistle Extract 1000mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 5, color: DOT_COLORS[4] }
            ],
            60: [
                { id: 1, name: 'Ginkgo Biloba 120mg Trunature', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 6, color: DOT_COLORS[0] },
                { id: 2, name: 'Glucosamine Orihiro 900 viên', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 8, color: DOT_COLORS[1] },
                { id: 3, name: 'Glucosamine HCL 1500mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 3, color: DOT_COLORS[2] },
                { id: 4, name: 'LineaBon K2 + D3', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 2, color: DOT_COLORS[3] },
                { id: 5, name: 'Milk Thistle Extract 1000mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 5, color: DOT_COLORS[4] },
                { id: 6, name: 'Omega-3 Fish Oil 1000mg Kirkland', expiryDate: '15/01/2026', batchNo: 'L00029', quantity: 14, color: DOT_COLORS[0] }
            ],
            90: [
                { id: 1, name: 'Ginkgo Biloba 120mg Trunature', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 6, color: DOT_COLORS[0] },
                { id: 2, name: 'Glucosamine Orihiro 900 viên', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 8, color: DOT_COLORS[1] },
                { id: 3, name: 'Glucosamine HCL 1500mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 3, color: DOT_COLORS[2] },
                { id: 4, name: 'LineaBon K2 + D3', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 2, color: DOT_COLORS[3] },
                { id: 5, name: 'Milk Thistle Extract 1000mg', expiryDate: '31/12/2025', batchNo: 'L00023', quantity: 5, color: DOT_COLORS[4] },
                { id: 6, name: 'Omega-3 Fish Oil 1000mg Kirkland', expiryDate: '15/01/2026', batchNo: 'L00029', quantity: 14, color: DOT_COLORS[0] },
                { id: 7, name: 'Canxi Corbiere Extra 10ml', expiryDate: '10/02/2026', batchNo: 'L00031', quantity: 20, color: DOT_COLORS[1] }
            ]
        },
        expiredItems: [
            { id: 1, name: 'Glucosamine Orihiro 900 viên', expiryDate: '31/12/2024', batchNo: 'L00014', quantity: 6, color: DOT_COLORS[0] },
            { id: 2, name: 'Vitabiotics Wellkid Multi-Vitamin', expiryDate: '31/12/2024', batchNo: 'L00014', quantity: 8, color: DOT_COLORS[1] },
            { id: 3, name: 'Angela Gold (Woman\'s Ginseng)', expiryDate: '31/12/2024', batchNo: 'L00014', quantity: 3, color: DOT_COLORS[2] },
            { id: 4, name: 'Milk Thistle Extract 1000mg', expiryDate: '31/12/2024', batchNo: 'L00014', quantity: 2, color: DOT_COLORS[3] },
            { id: 5, name: 'Blackmores Sulfate 1500 One-A-Day', expiryDate: '31/12/2024', batchNo: 'L00014', quantity: 5, color: DOT_COLORS[4] }
        ]
    };

    const pharmaMrpExtended = {
        belowMinStock: [
            { id: 101, name: 'Hoạt chất Paracetamol BP/USP (Kg)', warehouse: 'Kho Nguyên liệu GSP', currentStock: 6, minStock: 10, color: DOT_COLORS[0] },
            { id: 102, name: 'Amoxicillin Trihydrate Compacted', warehouse: 'Kho Kháng sinh', currentStock: 8, minStock: 10, color: DOT_COLORS[1] },
            { id: 103, name: 'Tá dược Lactose Monohydrate 200M', warehouse: 'Kho Tá dược', currentStock: 3, minStock: 10, color: DOT_COLORS[2] },
            { id: 104, name: 'Vỉ nhôm Alu-Alu 250mm (Cuộn)', warehouse: 'Kho Bao bì cấp 1', currentStock: 2, minStock: 10, color: DOT_COLORS[3] },
            { id: 105, name: 'Magnesium Stearate Dược dụng', warehouse: 'Kho Tá dược', currentStock: 5, minStock: 10, color: DOT_COLORS[4] }
        ],
        aboveMaxStock: [
            { id: 201, name: 'Viên nén Panadol Extra 500mg (Hộp)', warehouse: 'Kho Thành phẩm', currentStock: 523, maxStock: 500, color: DOT_COLORS[0] },
            { id: 202, name: 'Nước muối sinh lý Natri Clorid 0.9%', warehouse: 'Kho Thành phẩm', currentStock: 602, maxStock: 500, color: DOT_COLORS[1] },
            { id: 203, name: 'Vitamin C 500mg Sủi (Tuýp 20 viên)', warehouse: 'Kho Thành phẩm', currentStock: 578, maxStock: 500, color: DOT_COLORS[2] },
            { id: 204, name: 'Vỏ nang cứng Gelatin số 0 (Thùng)', warehouse: 'Kho Bao bì cấp 1', currentStock: 513, maxStock: 500, color: DOT_COLORS[3] },
            { id: 205, name: 'Siro Ho Bảo Thanh 125ml (Chai)', warehouse: 'Kho Bắc Ninh', currentStock: 645, maxStock: 500, color: DOT_COLORS[4] }
        ]
    };

    // 4. Dữ liệu màn hình "Nhập kho"
    const inboundDatasets = {
        defaultOrders: [
            { orderNo: 'DMH00006', orderDate: '28/05/2025', supplier: 'Công ty Cổ phần Thực Phẩm Đông Lạnh Bình An...', deliveryDate: '03/06/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00022', orderDate: '22/05/2025', supplier: 'Công ty Hồng Hà (Demo)', deliveryDate: '31/05/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00005', orderDate: '20/05/2025', supplier: 'Công ty Cổ phần Thực Phẩm Đông Lạnh Bình An...', deliveryDate: '01/06/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00004', orderDate: '20/05/2025', supplier: 'Công ty TNHH Thực Phẩm Thịnh Thu (Demo)', deliveryDate: '01/06/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00102', orderDate: '16/05/2025', supplier: 'Công ty TNHH Thực Phẩm Và Nước Giải Khát NAWO...', deliveryDate: '31/05/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00067', orderDate: '13/05/2025', supplier: 'Công ty TNHH XNK Và Thương Mại Wine 1855 (Demo)', deliveryDate: '-', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00031', orderDate: '11/05/2025', supplier: 'Công ty Cổ phần XNK Hải Anh (demo)', deliveryDate: '31/05/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00089', orderDate: '10/05/2025', supplier: 'Công ty Cổ phần Thực Phẩm Và Giải Khát STELLA...', deliveryDate: '31/05/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00090', orderDate: '04/05/2025', supplier: 'Công ty TNHH Hầm Rượu Châu Á (Demo)', deliveryDate: '28/05/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00048', orderDate: '01/05/2025', supplier: 'Công ty TNHH Hầm Rượu Châu Á (Demo)', deliveryDate: '12/08/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' }
        ],
        pharmaOrders: [
            { orderNo: 'DMH00006', orderDate: '28/05/2025', supplier: 'Công ty CP Dược phẩm Trung ương CPC1', deliveryDate: '03/06/2025', description: 'Nhập hoạt chất Paracetamol BP/USP', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00022', orderDate: '22/05/2025', supplier: 'Công ty TNHH Zuellig Pharma Việt Nam', deliveryDate: '31/05/2025', description: 'Nhập kháng sinh Amoxicillin Trihydrate', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00005', orderDate: '20/05/2025', supplier: 'Công ty CP Dược Hậu Giang (DHG Pharma)', deliveryDate: '01/06/2025', description: 'Nhập tá dược Lactose Monohydrate 200M', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00004', orderDate: '20/05/2025', supplier: 'Công ty CP Traphaco', deliveryDate: '01/06/2025', description: 'Nhập cao khô Diệp hạ châu đạt chuẩn GACP', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00102', orderDate: '16/05/2025', supplier: 'Công ty TNHH Bao bì Dược phẩm Sài Gòn', deliveryDate: '31/05/2025', description: 'Nhập màng nhôm Alu-Alu & vỏ nang Gelatin', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00067', orderDate: '13/05/2025', supplier: 'Công ty CP Dược phẩm Imexpharm', deliveryDate: '-', description: 'Nhập nguyên liệu Cefuroxime Axetil', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00031', orderDate: '11/05/2025', supplier: 'Công ty TNHH Sanofi Aventis Việt Nam', deliveryDate: '31/05/2025', description: 'Nhập nguyên liệu Vitamin D3 + K2 MK7', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00089', orderDate: '10/05/2025', supplier: 'Công ty CP Dược phẩm Pymepharco', deliveryDate: '31/05/2025', description: 'Nhập tá dược Magnesium Stearate', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00090', orderDate: '04/05/2025', supplier: 'Công ty CP Hóa Dược Việt Nam', deliveryDate: '28/05/2025', description: 'Nhập dung môi Ethanol 96% dược dụng', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00048', orderDate: '01/05/2025', supplier: 'Công ty CP Dược phẩm Hà Tây (Hataphar)', deliveryDate: '12/08/2025', description: 'Nhập lọ thủy tinh nâu 100ml đạt chuẩn GMP', status: 'Đang thực hiện', statusType: 'info' }
        ],
        inboundCommands: [
            { orderNo: 'LNK00039', orderDate: '25/05/2025', supplier: 'Công ty TNHH Công Nghệ Điện Tử Sáng Tạo (Demo)', deliveryDate: '25/05/2025', description: 'Lệnh nhập kho theo đơn DMH00028, PNK00039', status: 'Hoàn thành', statusType: 'success' },
            { orderNo: 'LNK00018', orderDate: '28/05/2025', supplier: 'Kho Nguyên liệu GSP (Biệt trữ)', deliveryDate: '29/05/2025', description: 'Lệnh nhập lô L00081 sau khi đạt phiếu Kiểm nghiệm QC', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'LNK00017', orderDate: '25/05/2025', supplier: 'Phân xưởng Viên nén GMP-WHO', deliveryDate: '26/05/2025', description: 'Nhập thành phẩm Panadol Extra 500mg (Lô SX: 0525)', status: 'Hoàn thành', statusType: 'success' }
        ],
        inboundReceipts: [
            { voucherNo: 'PNK00005', voucherDate: '24/04/2025 09:25:34', warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI', partnerName: 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM', deliverer: 'Phan Thanh Hà', description: 'Nhập kho mua hàng của CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM', subType: 'Nhập kho mua hàng' },
            { voucherNo: 'PNK00004', voucherDate: '22/04/2025 14:10:00', warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI', partnerName: 'Công ty Hồng Hà (Demo)', deliverer: 'Nguyễn Văn Minh', description: 'Nhập kho mua hàng theo đơn DMH00022', subType: 'Nhập kho mua hàng' },
            { voucherNo: 'PNK00003', voucherDate: '18/04/2025 10:05:12', warehouse: 'Kho Nguyên liệu GSP (Ba Đình)', partnerName: 'Công ty CP Dược phẩm Trung ương CPC1', deliverer: 'Trần Quốc Tuấn', description: 'Nhập kho nguyên liệu Sodium Bicarbonate dược dụng', subType: 'Nhập kho mua hàng' }
        ]
    };

    // 5. Dữ liệu màn hình "Xuất kho"
    const outboundDatasets = {
        defaultOrders: [
            { orderNo: 'DHTP00402', orderDate: '31/12/2025', customer: 'Organic Good...', deliveryDue: '31/12/2025', description: 'Bán hàng cho Organic Good Store', orderStatus: 'Chưa thực hiện', orderStatusType: 'warning', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00393', orderDate: '31/12/2025', customer: 'Organic Good...', deliveryDue: '31/12/2025', description: 'Bán hàng cho Organic Good Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00401', orderDate: '30/12/2025', customer: 'Việt Organic', deliveryDue: '30/12/2025', description: 'Bán hàng cho Việt Organic', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00392', orderDate: '30/12/2025', customer: 'Việt Organic', deliveryDue: '30/12/2025', description: 'Bán hàng cho Việt Organic', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00400', orderDate: '29/12/2025', customer: 'Mộc Quê Mart', deliveryDue: '29/12/2025', description: 'Bán hàng cho Mộc Quê Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Đang thực hiện' },
            { orderNo: 'DHTP00391', orderDate: '29/12/2025', customer: 'Mộc Quê Mart', deliveryDue: '29/12/2025', description: 'Bán hàng cho Mộc Quê Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00399', orderDate: '28/12/2025', customer: 'Nhà Xanh Mart', deliveryDue: '28/12/2025', description: 'Bán hàng cho Nhà Xanh Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00390', orderDate: '28/12/2025', customer: 'Nhà Xanh Mart', deliveryDue: '28/12/2025', description: 'Bán hàng cho Nhà Xanh Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Hoàn thành' },
            { orderNo: 'DHTP00398', orderDate: '27/12/2025', customer: 'EcoHome Food', deliveryDue: '27/12/2025', description: 'Bán hàng cho EcoHome Food', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00389', orderDate: '27/12/2025', customer: 'EcoHome Food', deliveryDue: '27/12/2025', description: 'Bán hàng cho EcoHome Food', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00397', orderDate: '26/12/2025', customer: 'Bình Dân Store', deliveryDue: '26/12/2025', description: 'Bán hàng cho Bình Dân Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00388', orderDate: '26/12/2025', customer: 'Bình Dân Store', deliveryDue: '26/12/2025', description: 'Bán hàng cho Bình Dân Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00396', orderDate: '25/12/2025', customer: 'Đồng Quê Store', deliveryDue: '25/12/2025', description: 'Bán hàng cho Đồng Quê Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00387', orderDate: '25/12/2025', customer: 'Đồng Quê Store', deliveryDue: '25/12/2025', description: 'Bán hàng cho Đồng Quê Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00395', orderDate: '24/12/2025', customer: 'Việt Fresh Store', deliveryDue: '24/12/2025', description: 'Bán hàng cho Việt Fresh Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00386', orderDate: '24/12/2025', customer: 'Việt Fresh Store', deliveryDue: '24/12/2025', description: 'Bán hàng cho Việt Fresh Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00394', orderDate: '23/12/2025', customer: 'HomePro Mart', deliveryDue: '23/12/2025', description: 'Bán hàng cho HomePro Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00385', orderDate: '23/12/2025', customer: 'HomePro Mart', deliveryDue: '23/12/2025', description: 'Bán hàng cho HomePro Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00384', orderDate: '22/12/2025', customer: 'Việt Sun Store', deliveryDue: '22/12/2025', description: 'Bán hàng cho Việt Sun Store', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00383', orderDate: '21/12/2025', customer: 'Xanh Việt Mart', deliveryDue: '21/12/2025', description: 'Bán hàng cho Xanh Việt Mart', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' }
        ],
        pharmaOrders: [
            { orderNo: 'DHTP00402', orderDate: '31/12/2025', customer: 'Hệ thống Nhà thuốc Long Châu', deliveryDue: '31/12/2025', description: 'Xuất bán Glucosamine Orihiro & Ginkgo Biloba (FEFO)', orderStatus: 'Chưa thực hiện', orderStatusType: 'warning', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00393', orderDate: '31/12/2025', customer: 'Bệnh viện Bạch Mai (Khoa Dược)', deliveryDue: '31/12/2025', description: 'Cung ứng thuốc thầu Kháng sinh Amoxicillin 500mg', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00401', orderDate: '30/12/2025', customer: 'Hệ thống Nhà thuốc Pharmacity', deliveryDue: '30/12/2025', description: 'Xuất bán LineaBon K2 + D3 & Vitamin C 500mg', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00392', orderDate: '30/12/2025', customer: 'Bệnh viện Chợ Rẫy', deliveryDue: '30/12/2025', description: 'Xuất dịch truyền Natri Clorid 0.9% đạt chuẩn GSP', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00400', orderDate: '29/12/2025', customer: 'Phân xưởng Đóng gói GMP Số 1', deliveryDue: '29/12/2025', description: 'Xuất nguyên liệu theo Lệnh sản xuất LSX-2025-089', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Đang thực hiện' },
            { orderNo: 'DHTP00391', orderDate: '29/12/2025', customer: 'Hệ thống Nhà thuốc An Khang', deliveryDue: '29/12/2025', description: 'Bán hàng thành phẩm Siro Ho Bảo Thanh 125ml', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00399', orderDate: '28/12/2025', customer: 'Bệnh viện Đại học Y Dược', deliveryDue: '28/12/2025', description: 'Xuất thuốc kháng sinh Cefuroxime 250mg theo lô FEFO', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'DHTP00390', orderDate: '28/12/2025', customer: 'Công ty CP Dược phẩm Trung ương 2', deliveryDue: '28/12/2025', description: 'Xuất điều phối Panadol Extra 500mg (Lô L00023)', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Hoàn thành' }
        ],
        outboundCommands: [
            { orderNo: 'LXK00088', orderDate: '31/12/2025', customer: 'Phân xưởng Viên nén GMP', deliveryDue: '31/12/2025', description: 'Lệnh xuất nguyên liệu Paracetamol & Tá dược cho mẻ SX 12', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'LXK00087', orderDate: '30/12/2025', customer: 'Hệ thống Nhà thuốc Long Châu', deliveryDue: '30/12/2025', description: 'Lệnh xuất kho theo đơn đặt hàng DHTP00402', orderStatus: 'Hoàn thành', orderStatusType: 'success', outboundStatus: 'Đang thực hiện' }
        ],
        outboundReceipts: [
            { voucherNo: 'PXK00002', voucherDate: '24/04/2025 08:39:28', warehouse: 'KHO HƯNG YÊN - Kho chính', partnerName: 'CÔNG TY TNHH XUẤT NHẬP KHẨU BANACO VIỆT NAM', receiver: 'Trần Vương', description: 'Xuất kho bán hàng cho CÔNG TY TNHH XUẤT NHẬP KHẨU BANACO VIỆT NAM', subType: 'Xuất kho bán hàng' },
            { voucherNo: 'PXK00001', voucherDate: '20/04/2025 15:20:10', warehouse: 'KHO HƯNG YÊN - Kho chính', partnerName: 'Nhà Xanh Mart', receiver: 'Lê Hoàng Nam', description: 'Xuất kho bán hàng theo đơn DHTP00390', subType: 'Xuất kho bán hàng' }
        ]
    };

    // 6. Dữ liệu Chi tiết Phiếu Nhập Kho (PNK00005) & Phiếu Xuất Kho (PXK00002) (Khớp 100% 2 ảnh mẫu)
    const vouchersData = {
        inbound: {
            voucherType: 'inbound',
            voucherNo: 'PNK00005',
            selectedSubType: 'Nhập kho mua hàng',
            isTypeDropdownOpen: true, // Hiển thị sẵn menu xổ xuống đúng như ảnh mẫu 1
            subTypeOptions: [
                'Nhập kho mua hàng',
                'Hàng bán bị trả lại',
                'Nhập khác'
            ],
            partnerCode: 'NCC00001',
            partnerCodeOptions: ['NCC00001', 'NCC00002', 'NCC_DHG_01'],
            partnerName: 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM',
            warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI',
            warehouseOptions: [
                'KHO HƯNG YÊN- HÀNG TƯƠNG LAI',
                'Kho Nguyên liệu GSP (Ba Đình)',
                'Kho Thành phẩm GMP (Bắc Ninh)'
            ],
            branch: 'CÔNG TY TNHH AN PHÚ (MISA TEST)',
            branchOptions: [
                'CÔNG TY TNHH AN PHÚ (MISA TEST)',
                'Chi nhánh Hà Nội',
                'Chi nhánh Bắc Ninh'
            ],
            deliverer: 'Phan Thanh Hà',
            reference: '',
            description: 'Nhập kho mua hàng của CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM',
            voucherDate: '24/04/2025 09:25:34',
            quantityColumnLabel: 'SL nhập',
            items: [
                {
                    id: 1,
                    itemCode: 'BICAR CHU TIM ...',
                    itemName: 'Phụ gia thức ăn chăn nuôi sodium bicarbonate feed grade ( NaHCO3 )',
                    specCode: '',
                    unit: 'kg',
                    location: '',
                    quantity: 500,
                    batchNo: 'LHSBB',
                    expiryDate: '31/12/2026'
                },
                {
                    id: 2,
                    itemCode: 'BICAR FOOD SIN...',
                    itemName: 'Phụ gia thực phẩm Sodium Bicarbonate ( NaHCO3 )',
                    specCode: '',
                    unit: 'kg',
                    location: '',
                    quantity: 500,
                    batchNo: 'LHSX3223',
                    expiryDate: '31/12/2026'
                }
            ]
        },
        outbound: {
            voucherType: 'outbound',
            voucherNo: 'PXK00002',
            selectedSubType: 'Xuất kho bán hàng',
            isTypeDropdownOpen: false,
            subTypeOptions: [
                'Xuất kho bán hàng',
                'Xuất kho sản xuất',
                'Xuất trả lại nhà cung cấp',
                'Xuất khác'
            ],
            partnerCode: 'MB1_BANACO',
            partnerCodeOptions: ['MB1_BANACO', 'KH_LONGCHAU', 'KH_BACHMAI'],
            partnerName: 'CÔNG TY TNHH XUẤT NHẬP KHẨU BANACO VIỆT NAM',
            warehouse: 'KHO HƯNG YÊN - Kho chính',
            warehouseOptions: [
                'KHO HƯNG YÊN - Kho chính',
                'Kho Thành phẩm GMP (Bắc Ninh)',
                'Kho Nguyên liệu GSP (Ba Đình)'
            ],
            branch: 'CÔNG TY TNHH AN PHÚ (MISA TEST)',
            branchOptions: [
                'CÔNG TY TNHH AN PHÚ (MISA TEST)',
                'Chi nhánh Hà Nội',
                'Chi nhánh Bắc Ninh'
            ],
            receiver: 'Trần Vương',
            deliveryAddress: 'Thôn Tựu Liệt, Xã Tam Hiệp, Huyện Thanh Trì, Thành phố Hà Nội, Việt Nam',
            reference: '',
            description: 'Xuất kho bán hàng cho CÔNG TY TNHH XUẤT NHẬP KHẨU BANACO VIỆT NAM',
            voucherDate: '24/04/2025 08:39:28',
            quantityColumnLabel: 'SL xuất',
            items: [
                {
                    id: 1,
                    itemCode: 'BORIC NGA',
                    itemName: 'BORIC ACID ( Axit boric 99.9% Granular ) H3BO3',
                    specCode: '',
                    unit: 'kg',
                    location: '',
                    quantity: 5000,
                    batchNo: '',
                    expiryDate: ''
                },
                {
                    id: 2,
                    itemCode: 'BICAR FEED THO BA...',
                    itemName: 'Phụ gia thức ăn chăn nuôi Sodium Bicarbonate feed grade ( NaHCO3 )',
                    specCode: '',
                    unit: 'kg',
                    location: '',
                    quantity: 5000,
                    batchNo: '',
                    expiryDate: ''
                }
            ]
        }
    };

    // 7. Dữ liệu Chi tiết Đơn Mua Hàng (DMH00006) & Đơn Đặt Hàng (DHTP00402)
    // Dùng chung OrderDetailModalComponent (thay đổi nhãn/cột phù hợp theo chiều Nhập/Xuất)
    const ordersDetailData = {
        purchaseOrder: {
            docType: 'purchase-order',
            title: 'Đơn mua hàng DMH00006',
            orderNo: 'DMH00006',
            orderNoLabel: 'Số đơn hàng',
            orderDate: '28/05/2025',
            orderDateLabel: 'Ngày đơn hàng',
            statusLabel: 'Tình trạng nhập kho',
            statusValue: 'Đang thực hiện',
            statusOptions: ['Chưa thực hiện', 'Đang thực hiện', 'Hoàn thành'],
            partnerCodeLabel: 'Mã nhà cung cấp',
            partnerCode: 'NCC014',
            partnerCodeOptions: ['NCC014', 'NCC00001', 'NCC0006'],
            partnerNameLabel: 'Tên nhà cung cấp',
            partnerName: 'Công ty Cổ phần Thực Phẩm Đông Lạnh Bình An (demo)',
            contactPersonLabel: 'Người liên hệ',
            contactPerson: '',
            phoneLabel: 'Số điện thoại',
            phone: '',
            departmentLabel: 'Đơn vị đề nghị mua',
            department: 'Công ty Sản xuất và phân phối thu...',
            departmentOptions: [
                'Công ty Sản xuất và phân phối thu...',
                'Phòng Kế hoạch Vật tư Dược',
                'Bộ phận Cung ứng GSP'
            ],
            deliveryDateLabel: 'Ngày giao hàng',
            deliveryDate: '03/06/2025',
            description: '',
            referenceTags: ['LNK00059'],
            warehouseColLabel: 'Kho',
            reqQtyColLabel: 'SL yêu cầu',
            doneQtyColLabel: 'SL đã nhận',
            primaryActionLabel: 'Lập lệnh nhập kho',
            primaryActionTarget: 'inbound-command',
            items: [
                { id: 1, itemCode: 'HH052', itemName: 'Ức Gà nguyên xương', specCode: '', warehouse: 'Kho công ty', unit: 'kg', reqQty: 5, doneQty: 0 },
                { id: 2, itemCode: 'HH053', itemName: 'Ức vịt nguyên xương', specCode: '', warehouse: 'Kho công ty', unit: 'kg', reqQty: 10, doneQty: 0 },
                { id: 3, itemCode: 'HH054', itemName: 'Thịt má heo', specCode: '', warehouse: 'Kho công ty', unit: 'kg', reqQty: 12, doneQty: 0 },
                { id: 4, itemCode: 'HH055', itemName: 'Chân giò heo', specCode: '', warehouse: 'Kho công ty', unit: 'kg', reqQty: 5, doneQty: 0 },
                { id: 5, itemCode: 'HH056', itemName: 'Đuôi bò', specCode: '', warehouse: 'Kho công ty', unit: 'kg', reqQty: 13, doneQty: 0 }
            ]
        },
        salesOrder: {
            docType: 'sales-order',
            title: 'Đơn đặt hàng DHTP00402',
            orderNo: 'DHTP00402',
            orderNoLabel: 'Số đơn đặt hàng',
            orderDate: '31/12/2025',
            orderDateLabel: 'Ngày đơn hàng',
            statusLabel: 'Tình trạng xuất kho',
            statusValue: 'Chưa thực hiện',
            statusOptions: ['Chưa thực hiện', 'Đang thực hiện', 'Hoàn thành'],
            partnerCodeLabel: 'Mã khách hàng',
            partnerCode: 'KH0018',
            partnerCodeOptions: ['KH0018', 'MB1_BANACO', 'KH_LONGCHAU'],
            partnerNameLabel: 'Tên khách hàng',
            partnerName: 'Organic Good Store - Hệ thống Phân phối Dược & Thực phẩm',
            contactPersonLabel: 'Người nhận hàng',
            contactPerson: 'Nguyễn Minh Quân',
            phoneLabel: 'Số điện thoại',
            phone: '0904 512 889',
            departmentLabel: 'Đơn vị bán hàng',
            department: 'Công ty Sản xuất và phân phối thu...',
            departmentOptions: [
                'Công ty Sản xuất và phân phối thu...',
                'Phòng Kinh doanh Dược phẩm',
                'Chi nhánh Phân phối Miền Bắc'
            ],
            deliveryDateLabel: 'Hạn giao hàng',
            deliveryDate: '31/12/2025',
            description: 'Bán hàng cho Organic Good Store',
            referenceTags: ['LXK00088'],
            warehouseColLabel: 'Kho xuất',
            reqQtyColLabel: 'SL đặt hàng',
            doneQtyColLabel: 'SL đã xuất',
            primaryActionLabel: 'Lập lệnh xuất kho',
            primaryActionTarget: 'outbound-command',
            items: [
                { id: 1, itemCode: 'DP001', itemName: 'Ginkgo Biloba 120mg Trunature', specCode: 'Hộp 300V', warehouse: 'Kho Thành phẩm', unit: 'Hộp', reqQty: 30, doneQty: 0 },
                { id: 2, itemCode: 'DP002', itemName: 'Glucosamine Orihiro 900 viên', specCode: 'Lọ 900V', warehouse: 'Kho Thành phẩm', unit: 'Lọ', reqQty: 25, doneQty: 0 },
                { id: 3, itemCode: 'DP003', itemName: 'Glucosamine HCL 1500mg Kirkland', specCode: 'Lọ 375V', warehouse: 'Kho Thành phẩm', unit: 'Lọ', reqQty: 40, doneQty: 0 },
                { id: 4, itemCode: 'DP004', itemName: 'LineaBon K2 + D3 Nhỏ giọt 10ml', specCode: 'Lọ 10ml', warehouse: 'Kho Thành phẩm', unit: 'Lọ', reqQty: 15, doneQty: 0 },
                { id: 5, itemCode: 'DP005', itemName: 'Milk Thistle Extract 1000mg', specCode: 'Hộp 180V', warehouse: 'Kho Thành phẩm', unit: 'Hộp', reqQty: 10, doneQty: 0 }
            ]
        }
    };

    // 8. Dữ liệu Chi tiết Lệnh Nhập Kho (LNK00039) & Lệnh Xuất Kho (LXK00088)
    // Dùng chung CommandDetailModalComponent (thay đổi nhãn/cột phù hợp theo chiều Nhập/Xuất)
    const commandsDetailData = {
        inboundCommand: {
            docType: 'inbound-command',
            title: 'Lệnh nhập kho LNK00039',
            topSearchPlaceholder: 'Nhập số đơn mua hàng',
            topSearchOptions: ['Nhập số đơn mua hàng', 'DMH00006', 'DMH00028', 'DMH00022'],
            commandNoLabel: 'Số lệnh nhập kho',
            commandNo: 'LNK00039',
            dueDateLabel: 'Hạn nhập kho',
            dueDate: '25/05/2025 00:00:00',
            statusText: 'Hoàn thành',
            topRightActionLabel: 'Đọc phiếu giao hàng',
            partnerCode: 'NCC0006',
            partnerCodeOptions: ['NCC0006', 'NCC014', 'NCC00001'],
            partnerNameLabel: 'Tên đối tượng giao hàng',
            partnerName: 'Công ty TNHH Công Nghệ Điện Tử Sáng Tạo (Demo)',
            warehouseLabel: 'Kho nhập',
            warehouse: 'Kho nguyên vật liệu',
            warehouseOptions: ['Kho nguyên vật liệu', 'Kho Nguyên liệu GSP', 'Kho Thành phẩm GMP'],
            branch: 'Công ty Sản xuất và phân phối dược p',
            branchOptions: ['Công ty Sản xuất và phân phối dược p', 'CÔNG TY TNHH AN PHÚ (MISA TEST)'],
            personLabel: 'Người giao',
            personName: '',
            description: '',
            referenceTags: ['DMH00028', 'PNK00039'],
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực nhập',
            items: [
                { id: 1, itemCode: 'Phi LSS', itemName: 'Bộ bàn ghế', specCode: '', unit: 'Amp', location: '', reqQty: 15, actualQty: 15, isFull: true, batchNo: '', expiryDate: '' },
                { id: 2, itemCode: 'Phi PCS', itemName: 'Tiền xăng xe', specCode: '', unit: 'Vial', location: '', reqQty: 12, actualQty: 12, isFull: true, batchNo: '', expiryDate: '' },
                { id: 3, itemCode: 'Phi RSS', itemName: 'Thép S45C phi 16  2', specCode: '', unit: 'Set', location: '', reqQty: 35, actualQty: 35, isFull: true, batchNo: '', expiryDate: '' },
                { id: 4, itemCode: 'Phi THC', itemName: 'Thép S45C phi 18  2', specCode: '', unit: 'Set', location: '', reqQty: 18, actualQty: 18, isFull: true, batchNo: '', expiryDate: '' },
                { id: 5, itemCode: 'HH004', itemName: 'Dây điện/dây dẹt 2x0.75/2x0.7', specCode: '', unit: 'Vial', location: '', reqQty: 150, actualQty: 150, isFull: true, batchNo: '', expiryDate: '' },
                { id: 6, itemCode: 'HH005_ACT1', itemName: 'Xe máy Yamaha FZ150i 2', specCode: '', unit: 'Vial', location: '', reqQty: 120, actualQty: 120, isFull: true, batchNo: '', expiryDate: '' },
                { id: 7, itemCode: 'xang2', itemName: 'Bộ bàn ghế  2', specCode: '', unit: 'Set', location: '', reqQty: 35, actualQty: 35, isFull: true, batchNo: '', expiryDate: '' },
                { id: 8, itemCode: 'xang2', itemName: 'Bộ bàn ghế  2', specCode: '', unit: 'Set', location: '', reqQty: 18, actualQty: 18, isFull: true, batchNo: '', expiryDate: '' },
                { id: 9, itemCode: 'Xang xe', itemName: 'Tiền xăng xe 2', specCode: '', unit: 'Vial', location: '', reqQty: 15, actualQty: 15, isFull: true, batchNo: '', expiryDate: '' }
            ]
        },
        outboundCommand: {
            docType: 'outbound-command',
            title: 'Lệnh xuất kho LXK00088',
            topSearchPlaceholder: 'Chọn số đơn đặt hàng',
            topSearchOptions: ['Chọn số đơn đặt hàng', 'DHTP00402', 'DHTP00393', 'DHTP00401'],
            commandNoLabel: 'Số lệnh xuất kho',
            commandNo: 'LXK00088',
            dueDateLabel: 'Hạn xuất kho',
            dueDate: '31/12/2025 00:00:00',
            statusText: 'Đang thực hiện',
            topRightActionLabel: 'Đề xuất lô theo FEFO',
            partnerCode: 'KH0018',
            partnerCodeOptions: ['KH0018', 'MB1_BANACO', 'KH_LONGCHAU'],
            partnerNameLabel: 'Tên đối tượng nhận hàng',
            partnerName: 'Hệ thống Nhà thuốc FPT Long Châu',
            warehouseLabel: 'Kho xuất',
            warehouse: 'Kho thành phẩm GMP',
            warehouseOptions: ['Kho thành phẩm GMP', 'Kho nguyên vật liệu', 'KHO HƯNG YÊN - Kho chính'],
            branch: 'Công ty Sản xuất và phân phối dược p',
            branchOptions: ['Công ty Sản xuất và phân phối dược p', 'CÔNG TY TNHH AN PHÚ (MISA TEST)'],
            personLabel: 'Người nhận',
            personName: 'Lê Hoàng Nam',
            description: 'Xuất thuốc & thực phẩm bảo vệ sức khỏe theo đơn DHTP00402',
            referenceTags: ['DHTP00402', 'PXK00002'],
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực xuất',
            items: [
                { id: 1, itemCode: 'DP001', itemName: 'Ginkgo Biloba 120mg Trunature', specCode: 'Hộp 300V', unit: 'Vial', location: 'Kệ A1-01', reqQty: 30, actualQty: 30, isFull: true, batchNo: 'L00023', expiryDate: '31/12/2025' },
                { id: 2, itemCode: 'DP002', itemName: 'Glucosamine Orihiro 900 viên', specCode: 'Lọ 900V', unit: 'Set', location: 'Kệ A1-02', reqQty: 25, actualQty: 25, isFull: true, batchNo: 'L00023', expiryDate: '31/12/2025' },
                { id: 3, itemCode: 'DP003', itemName: 'Glucosamine HCL 1500mg Kirkland', specCode: 'Lọ 375V', unit: 'Vial', location: 'Kệ A2-04', reqQty: 40, actualQty: 40, isFull: true, batchNo: 'L00023', expiryDate: '31/12/2025' },
                { id: 4, itemCode: 'DP004', itemName: 'LineaBon K2 + D3 Nhỏ giọt 10ml', specCode: 'Lọ 10ml', unit: 'Amp', location: 'Kệ B1-01', reqQty: 15, actualQty: 15, isFull: true, batchNo: 'L00023', expiryDate: '31/12/2025' },
                { id: 5, itemCode: 'DP005', itemName: 'Milk Thistle Extract 1000mg', specCode: 'Hộp 180V', unit: 'Set', location: 'Kệ B1-03', reqQty: 10, actualQty: 10, isFull: true, batchNo: 'L00023', expiryDate: '31/12/2025' }
            ]
        }
    };

    // State hiện tại của giao diện Kho hàng
    const state = {
        activeMenuId: 'tong-quan',
        sidebarCollapsed: false,
        searchKeyword: '',
        usePharmaFullData: false,
        // Tham số cấu hình cho thẻ "Hàng hóa sắp hết hạn" ở Tổng quan
        expiringDaysTitle: 30,
        pendingDaysParam: 5,
        isParamPopoverOpen: true,
        lastUpdatedMap: {
            'card-below-min': '08:00',
            'card-above-max': '08:00',
            'card-expiring-soon': '08:00',
            'card-expired': '08:00'
        },
        // State cho trang Nhập kho
        inbound: {
            activeTab: 'don-mua-hang',
            searchKeyword: '',
            timeFilter: 'Tháng này',
            statusFilter: 'Tất cả'
        },
        // State cho trang Xuất kho
        outbound: {
            activeTab: 'don-dat-hang',
            searchKeyword: '',
            orderStatusFilter: 'Tất cả',
            selectedRows: {}
        },
        // Quản lý Modal Chứng từ đang mở:
        // 'purchase-order' | 'sales-order' | 'inbound-command' | 'outbound-command' | 'inbound' | 'outbound' | null
        activeVoucherModal: null
    };

    function filterByKeyword(list, kw, keys) {
        const cleanKw = (kw || '').trim().toLowerCase();
        if (!cleanKw) return list;
        return list.filter(item =>
            keys.some(k => item[k] && String(item[k]).toLowerCase().includes(cleanKw))
        );
    }

    return {
        getState() {
            return state;
        },

        getSidebarMenu() {
            return sidebarMenu;
        },

        getWorkspaceData() {
            return workspaceData;
        },

        getVoucherData(voucherType) {
            switch (voucherType) {
                case 'purchase-order':
                    return ordersDetailData.purchaseOrder;
                case 'sales-order':
                    return ordersDetailData.salesOrder;
                case 'inbound-command':
                    return commandsDetailData.inboundCommand;
                case 'outbound-command':
                    return commandsDetailData.outboundCommand;
                case 'outbound':
                    return vouchersData.outbound;
                case 'inbound':
                default:
                    return vouchersData.inbound;
            }
        },

        removeReferenceTag(docType, tagText) {
            const doc = this.getVoucherData(docType);
            if (doc && Array.isArray(doc.referenceTags)) {
                doc.referenceTags = doc.referenceTags.filter(t => t !== tagText);
            }
        },

        toggleCommandRowFull(docType, rowId, isChecked) {
            const doc = this.getVoucherData(docType);
            if (doc && Array.isArray(doc.items)) {
                const row = doc.items.find(i => i.id === Number(rowId));
                if (row) {
                    row.isFull = Boolean(isChecked);
                    row.actualQty = row.isFull ? row.reqQty : 0;
                }
            }
        },

        openVoucherModal(voucherType) {
            state.activeVoucherModal = voucherType;
        },

        closeVoucherModal() {
            state.activeVoucherModal = null;
        },

        addVoucherItemRow(voucherType) {
            const v = this.getVoucherData(voucherType);
            const nextId = v.items.length > 0 ? Math.max(...v.items.map(i => i.id)) + 1 : 1;
            if (voucherType === 'inbound') {
                v.items.push({
                    id: nextId,
                    itemCode: 'PARA-API-500',
                    itemName: 'Hoạt chất Paracetamol BP/USP Dược dụng',
                    specCode: 'GMP-01',
                    unit: 'kg',
                    location: 'Kệ A1-02',
                    quantity: 250,
                    batchNo: `LH${1000 + nextId}`,
                    expiryDate: '31/12/2027'
                });
            } else {
                v.items.push({
                    id: nextId,
                    itemCode: 'GLUCOSAMINE-900',
                    itemName: 'Glucosamine Orihiro 900 viên (Xuất theo FEFO)',
                    specCode: 'Hộp 900V',
                    unit: 'kg',
                    location: 'Kệ B2-05',
                    quantity: 1000,
                    batchNo: 'L00023',
                    expiryDate: '31/12/2025'
                });
            }
        },

        deleteVoucherItemRow(voucherType, rowId) {
            const v = this.getVoucherData(voucherType);
            v.items = v.items.filter(item => item.id !== Number(rowId));
        },

        clearAllVoucherItems(voucherType) {
            const v = this.getVoucherData(voucherType);
            v.items = [];
        },

        setActiveMenu(menuId) {
            state.activeMenuId = menuId;
            state.activeVoucherModal = null;
            const opsMenu = sidebarMenu.find(m => m.id === 'hoat-dong-kho');
            if (opsMenu && opsMenu.children.some(c => c.id === menuId)) {
                opsMenu.expanded = true;
            }
        },

        toggleSubmenu(menuId) {
            const item = sidebarMenu.find(m => m.id === menuId);
            if (item && item.children) {
                item.expanded = !item.expanded;
            }
        },

        toggleSidebarCollapse() {
            state.sidebarCollapsed = !state.sidebarCollapsed;
            return state.sidebarCollapsed;
        },

        setSearchKeyword(keyword) {
            state.searchKeyword = keyword || '';
        },

        setParamPopoverOpen(isOpen) {
            state.isParamPopoverOpen = Boolean(isOpen);
        },

        setPendingDaysParam(days) {
            state.pendingDaysParam = Number(days) || 5;
        },

        applyExpiringDaysParam(days) {
            const numDays = Number(days) || 30;
            state.pendingDaysParam = numDays;
            state.expiringDaysTitle = numDays;
            state.isParamPopoverOpen = false;
        },

        toggleDataMode() {
            state.usePharmaFullData = !state.usePharmaFullData;
            return state.usePharmaFullData;
        },

        refreshCardTimestamp(cardId) {
            const now = new Date();
            const hh = String(now.getHours()).padStart(2, '0');
            const mm = String(now.getMinutes()).padStart(2, '0');
            state.lastUpdatedMap[cardId] = `${hh}:${mm}`;
            return state.lastUpdatedMap[cardId];
        },

        // Cập nhật tab & bộ lọc cho Nhập kho / Xuất kho (KHÔNG tự động bật modal Phiếu khi chuyển tab)
        setOperationTab(pageType, tabId) {
            state.activeVoucherModal = null;
            if (pageType === 'nhap-kho') {
                state.inbound.activeTab = tabId;
            } else if (pageType === 'xuat-kho') {
                state.outbound.activeTab = tabId;
            }
        },

        setOperationSearch(pageType, keyword) {
            if (pageType === 'nhap-kho') {
                state.inbound.searchKeyword = keyword || '';
            } else if (pageType === 'xuat-kho') {
                state.outbound.searchKeyword = keyword || '';
            }
        },

        setOperationStatusFilter(pageType, statusValue) {
            if (pageType === 'nhap-kho') {
                state.inbound.statusFilter = statusValue;
            } else if (pageType === 'xuat-kho') {
                state.outbound.orderStatusFilter = statusValue;
            }
        },

        // Lấy cấu hình 4 thẻ Dashboard ở trang Tổng quan
        getDashboardCardsConfig() {
            const sourceBelowMin = state.usePharmaFullData
                ? pharmaMrpExtended.belowMinStock
                : defaultDatasets.belowMinStock;

            const sourceAboveMax = state.usePharmaFullData
                ? pharmaMrpExtended.aboveMaxStock
                : defaultDatasets.aboveMaxStock;

            const daysKey = defaultDatasets.expiringSoonByDays[state.expiringDaysTitle]
                ? state.expiringDaysTitle
                : 30;
            const sourceExpiring = defaultDatasets.expiringSoonByDays[daysKey];

            return [
                {
                    id: 'card-below-min',
                    title: 'Tồn kho dưới mức tối thiểu',
                    hasSettings: false,
                    lastUpdated: state.lastUpdatedMap['card-below-min'],
                    columns: [
                        { key: 'name', label: 'Tên hàng hóa', align: 'left', isProductCol: true },
                        { key: 'warehouse', label: 'Kho', align: 'left' },
                        { key: 'currentStock', label: 'SL tồn', align: 'right' },
                        { key: 'minStock', label: 'SL tồn tối thiểu', align: 'right' }
                    ],
                    rows: filterByKeyword(sourceBelowMin, state.searchKeyword, ['name', 'warehouse'])
                },
                {
                    id: 'card-above-max',
                    title: 'Tồn kho vượt mức tối đa',
                    hasSettings: false,
                    lastUpdated: state.lastUpdatedMap['card-above-max'],
                    columns: [
                        { key: 'name', label: 'Tên hàng hóa', align: 'left', isProductCol: true },
                        { key: 'warehouse', label: 'Kho', align: 'left' },
                        { key: 'currentStock', label: 'SL tồn', align: 'right' },
                        { key: 'maxStock', label: 'SL tồn tối đa', align: 'right' }
                    ],
                    rows: filterByKeyword(sourceAboveMax, state.searchKeyword, ['name', 'warehouse'])
                },
                {
                    id: 'card-expiring-soon',
                    title: `Hàng hóa sắp hết hạn trong ${state.expiringDaysTitle} ngày`,
                    hasSettings: true,
                    lastUpdated: state.lastUpdatedMap['card-expiring-soon'],
                    columns: [
                        { key: 'name', label: 'Tên hàng hóa', align: 'left', isProductCol: true },
                        { key: 'expiryDate', label: 'Hạn sử dụng', align: 'left' },
                        { key: 'batchNo', label: 'Số lô', align: 'left' },
                        { key: 'quantity', label: 'Số lượng', align: 'right' }
                    ],
                    rows: filterByKeyword(sourceExpiring, state.searchKeyword, ['name', 'batchNo'])
                },
                {
                    id: 'card-expired',
                    title: 'Hàng hóa quá hạn sử dụng',
                    hasSettings: false,
                    lastUpdated: state.lastUpdatedMap['card-expired'],
                    columns: [
                        { key: 'name', label: 'Tên hàng hóa', align: 'left', isProductCol: true },
                        { key: 'expiryDate', label: 'Hạn sử dụng', align: 'left' },
                        { key: 'batchNo', label: 'Số lô', align: 'left' },
                        { key: 'quantity', label: 'Số lượng', align: 'right' }
                    ],
                    rows: filterByKeyword(defaultDatasets.expiredItems, state.searchKeyword, ['name', 'batchNo'])
                }
            ];
        },

        // Cấu hình truyền vào TabbedDataGridComponent cho trang Nhập kho
        getInboundGridConfig() {
            const isVoucherTab = state.inbound.activeTab === 'nhap-kho-tab';
            let sourceRows = [];

            if (state.inbound.activeTab === 'don-mua-hang') {
                sourceRows = state.usePharmaFullData ? inboundDatasets.pharmaOrders : inboundDatasets.defaultOrders;
            } else if (state.inbound.activeTab === 'lenh-nhap-kho') {
                sourceRows = inboundDatasets.inboundCommands;
            } else {
                sourceRows = inboundDatasets.inboundReceipts;
            }

            const combinedKw = (state.inbound.searchKeyword || state.searchKeyword).trim();
            let filtered = isVoucherTab
                ? filterByKeyword(sourceRows, combinedKw, ['voucherNo', 'warehouse', 'partnerName', 'deliverer', 'description', 'subType'])
                : filterByKeyword(sourceRows, combinedKw, ['orderNo', 'supplier', 'description', 'status']);

            if (!isVoucherTab && state.inbound.statusFilter !== 'Tất cả') {
                filtered = filtered.filter(r => r.status === state.inbound.statusFilter);
            }

            const columns = isVoucherTab
                ? [
                    { key: 'voucherNo', label: 'Số phiếu nhập kho', isLink: true, width: '140px' },
                    { key: 'voucherDate', label: 'Ngày nhập kho', width: '155px' },
                    { key: 'warehouse', label: 'Kho nhập', width: '220px' },
                    { key: 'partnerName', label: 'Tên đối tượng giao hàng', width: '270px' },
                    { key: 'deliverer', label: 'Người giao', width: '135px' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'subType', label: 'Loại phiếu', isStatusLink: true, width: '155px' }
                ]
                : [
                    { key: 'orderNo', label: state.inbound.activeTab === 'lenh-nhap-kho' ? 'Số lệnh nhập' : 'Số đơn hàng', isLink: true, width: '130px' },
                    { key: 'orderDate', label: state.inbound.activeTab === 'lenh-nhap-kho' ? 'Ngày lệnh' : 'Ngày đơn hàng', width: '135px' },
                    { key: 'supplier', label: 'Tên nhà cung cấp', width: '290px' },
                    { key: 'deliveryDate', label: 'Ngày giao hàng', width: '140px' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'status', label: 'Tình trạng nhập kho', isStatusLink: true, width: '160px' }
                ];

            const filters = isVoucherTab
                ? [
                    {
                        id: 'inbound-time-filter',
                        prefix: 'Thời gian:',
                        value: state.inbound.timeFilter,
                        options: ['Tháng này', 'Quý này', 'Năm nay', 'Tất cả']
                    }
                ]
                : [
                    {
                        id: 'inbound-time-filter',
                        prefix: 'Thời gian:',
                        value: state.inbound.timeFilter,
                        options: ['Tháng này', 'Quý này', 'Năm nay', 'Tất cả']
                    },
                    {
                        id: 'inbound-status-filter',
                        prefix: 'Tình trạng nhập kho:',
                        value: state.inbound.statusFilter,
                        options: ['Tất cả', 'Đang thực hiện', 'Hoàn thành']
                    }
                ];

            const targetModalType = state.inbound.activeTab === 'don-mua-hang'
                ? 'purchase-order'
                : (state.inbound.activeTab === 'lenh-nhap-kho' ? 'inbound-command' : 'inbound');

            return {
                pageType: 'nhap-kho',
                voucherType: targetModalType,
                canOpenVoucher: true,
                activeTab: state.inbound.activeTab,
                tabs: [
                    { id: 'don-mua-hang', label: 'Đơn mua hàng' },
                    { id: 'lenh-nhap-kho', label: 'Lệnh nhập kho' },
                    { id: 'nhap-kho-tab', label: 'Nhập kho' }
                ],
                searchValue: state.inbound.searchKeyword,
                filters,
                showAddButton: true,
                addButtonLabel: 'Thêm',
                showCheckbox: false,
                columns,
                rows: filtered,
                totalRecords: filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: false
            };
        },

        // Cấu hình truyền vào TabbedDataGridComponent cho trang Xuất kho
        getOutboundGridConfig() {
            const isVoucherTab = state.outbound.activeTab === 'xuat-kho-tab';
            let sourceRows = [];

            if (state.outbound.activeTab === 'don-dat-hang') {
                sourceRows = state.usePharmaFullData ? outboundDatasets.pharmaOrders : outboundDatasets.defaultOrders;
            } else if (state.outbound.activeTab === 'lenh-xuat-kho') {
                sourceRows = outboundDatasets.outboundCommands;
            } else {
                sourceRows = outboundDatasets.outboundReceipts;
            }

            const combinedKw = (state.outbound.searchKeyword || state.searchKeyword).trim();
            let filtered = isVoucherTab
                ? filterByKeyword(sourceRows, combinedKw, ['voucherNo', 'warehouse', 'partnerName', 'receiver', 'description', 'subType'])
                : filterByKeyword(sourceRows, combinedKw, ['orderNo', 'customer', 'description', 'orderStatus', 'outboundStatus']);

            if (!isVoucherTab && state.outbound.orderStatusFilter !== 'Tất cả') {
                filtered = filtered.filter(r => r.orderStatus === state.outbound.orderStatusFilter);
            }

            const isDefaultFullList = !state.usePharmaFullData &&
                state.outbound.activeTab === 'don-dat-hang' &&
                !combinedKw &&
                state.outbound.orderStatusFilter === 'Tất cả';

            const columns = isVoucherTab
                ? [
                    { key: 'voucherNo', label: 'Số phiếu xuất kho', isLink: true, width: '140px' },
                    { key: 'voucherDate', label: 'Ngày xuất kho', width: '155px' },
                    { key: 'warehouse', label: 'Kho xuất', width: '210px' },
                    { key: 'partnerName', label: 'Tên đối tượng nhận hàng', width: '270px' },
                    { key: 'receiver', label: 'Người nhận', width: '135px' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'subType', label: 'Loại phiếu', isStatusLink: true, width: '155px' }
                ]
                : [
                    { key: 'orderNo', label: state.outbound.activeTab === 'lenh-xuat-kho' ? 'Số lệnh xuất' : 'Số đơn hàng', isLink: true, width: '125px' },
                    { key: 'orderDate', label: state.outbound.activeTab === 'lenh-xuat-kho' ? 'Ngày lệnh' : 'Ngày đơn hàng', width: '125px' },
                    { key: 'customer', label: 'Tên khách hàng', width: '165px' },
                    { key: 'deliveryDue', label: 'Hạn giao hàng', width: '125px' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'orderStatus', label: 'Tình trạng đơn hàng', isOrderStatus: true, width: '160px' },
                    { key: 'outboundStatus', label: 'Tình trạng xuất kho', width: '150px' }
                ];

            const filters = isVoucherTab
                ? [
                    {
                        id: 'outbound-time-filter',
                        prefix: 'Thời gian:',
                        value: 'Tháng này',
                        options: ['Tháng này', 'Quý này', 'Năm nay', 'Tất cả']
                    }
                ]
                : [
                    {
                        id: 'outbound-status-filter',
                        prefix: 'Tình trạng đơn hàng:',
                        value: state.outbound.orderStatusFilter,
                        options: ['Tất cả', 'Chưa thực hiện', 'Hoàn thành']
                    }
                ];

            const targetModalType = state.outbound.activeTab === 'don-dat-hang'
                ? 'sales-order'
                : (state.outbound.activeTab === 'lenh-xuat-kho' ? 'outbound-command' : 'outbound');

            return {
                pageType: 'xuat-kho',
                voucherType: targetModalType,
                canOpenVoucher: true,
                activeTab: state.outbound.activeTab,
                tabs: [
                    { id: 'don-dat-hang', label: 'Đơn đặt hàng' },
                    { id: 'lenh-xuat-kho', label: 'Lệnh xuất kho' },
                    { id: 'xuat-kho-tab', label: 'Xuất kho' }
                ],
                searchValue: state.outbound.searchKeyword,
                filters,
                showAddButton: state.outbound.activeTab !== 'don-dat-hang',
                addButtonLabel: 'Thêm',
                showCheckbox: !isVoucherTab,
                columns,
                rows: filtered,
                totalRecords: isDefaultFullList ? 413 : filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: isDefaultFullList
            };
        }
    };
})();
