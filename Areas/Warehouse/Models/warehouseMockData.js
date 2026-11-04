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
                { id: 'xuat-kho', title: 'Xuất kho' }
            ]
        },
        {
            id: 'bao-cao',
            title: 'Báo cáo',
            icon: 'report',
            section: 'middle'
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
            { orderNo: 'LNK00039', orderDate: '25/05/2025', supplier: 'Nguyễn Tiến Huy', deliveryDate: '25/05/2025', productName: 'Viên nén Panadol Extra 500mg', description: 'Lệnh nhập kho theo đơn DMH00028, PNK00039', status: 'Chờ nhập kho', statusType: 'warning' },
            { orderNo: 'LNK00018', orderDate: '28/05/2025', supplier: 'Trần Quốc Tuấn', deliveryDate: '29/05/2025', productName: 'Kháng sinh Amoxicillin 500mg', description: 'Lệnh nhập lô L00081 sau khi đạt phiếu Kiểm nghiệm QC', status: 'Chờ nhập kho', statusType: 'warning' },
            { orderNo: 'LNK00017', orderDate: '25/05/2025', supplier: 'Lê Hoàng Nam', deliveryDate: '26/05/2025', productName: 'Siro Ho Bảo Thanh 125ml', description: 'Nhập thành phẩm Panadol Extra 500mg (Lô SX: 0525)', status: 'Chờ nhập kho', statusType: 'warning' }
        ],
        inboundReceipts: [
            {
                voucherNo: 'PNK00005',
                orderNo: 'PNK00005',
                voucherDate: '24/04/2025 09:25:34',
                warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI',
                partnerName: 'Phan Thanh Hà',
                deliverer: 'Phan Thanh Hà',
                description: 'Nhập kho nguyên liệu Sodium Bicarbonate dược dụng',
                status: 'Chờ nhập kho',
                statusType: 'warning',
                subType: 'Nhập kho mua hàng'
            },
            {
                voucherNo: 'PNK00004',
                orderNo: 'PNK00004',
                voucherDate: '22/04/2025 14:10:00',
                warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI',
                partnerName: 'Nguyễn Văn Minh',
                deliverer: 'Nguyễn Văn Minh',
                description: 'Nhập kho mua hàng theo đơn DMH00022',
                status: 'Chờ nhập kho',
                statusType: 'warning',
                subType: 'Nhập kho mua hàng'
            },
            {
                voucherNo: 'PNK00003',
                orderNo: 'PNK00003',
                voucherDate: '18/04/2025 10:05:12',
                warehouse: 'Kho Nguyên liệu GSP (Ba Đình)',
                partnerName: 'Trần Quốc Tuấn',
                deliverer: 'Trần Quốc Tuấn',
                description: 'Nhập kho nguyên liệu Tá dược Lactose Monohydrate',
                status: 'Chờ nhập kho',
                statusType: 'warning',
                subType: 'Nhập kho mua hàng'
            }
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
            { orderNo: 'LXK00088', orderDate: '31/12/2025', customer: 'Phân xưởng Viên nén GMP', deliveryDue: '31/12/2025', description: 'Lệnh xuất nguyên liệu Paracetamol & Tá dược cho mẻ SX 12', orderStatus: 'Chờ xuất kho', orderStatusType: 'warning', outboundStatus: 'Chưa thực hiện' },
            { orderNo: 'LXK00087', orderDate: '30/12/2025', customer: 'Hệ thống Nhà thuốc Long Châu', deliveryDue: '30/12/2025', description: 'Lệnh xuất kho theo đơn đặt hàng DHTP00402', orderStatus: 'Chờ xuất kho', orderStatusType: 'warning', outboundStatus: 'Đang thực hiện' }
        ],
        outboundReceipts: [
            {
                voucherNo: 'PXK00002',
                orderNo: 'PXK00002',
                voucherDate: '24/04/2025 08:39:28',
                warehouse: 'KHO HƯNG YÊN - Kho chính',
                partnerName: 'Trần Vương',
                receiver: 'Trần Vương',
                description: 'Xuất kho bán hàng cho CÔNG TY TNHH XUẤT NHẬP KHẨU BANACO VIỆT NAM',
                status: 'Chờ xuất kho',
                statusType: 'warning',
                orderStatus: 'Chờ xuất kho',
                orderStatusType: 'warning',
                outboundStatus: 'Chưa thực hiện',
                subType: 'Xuất kho bán hàng'
            },
            {
                voucherNo: 'PXK00001',
                orderNo: 'PXK00001',
                voucherDate: '20/04/2025 15:20:10',
                warehouse: 'KHO HƯNG YÊN - Kho chính',
                partnerName: 'Lê Hoàng Nam',
                receiver: 'Lê Hoàng Nam',
                description: 'Xuất kho bán hàng theo đơn DHTP00390',
                status: 'Chờ xuất kho',
                statusType: 'warning',
                orderStatus: 'Chờ xuất kho',
                orderStatusType: 'warning',
                outboundStatus: 'Chưa thực hiện',
                subType: 'Xuất kho bán hàng'
            }
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

    function makeSvgThumb(bg, svgContent) {
        const fullSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48" width="48" height="48"><rect width="48" height="48" rx="8" fill="${bg}"/>${svgContent}</svg>`;
        return 'data:image/svg+xml;utf8,' + encodeURIComponent(fullSvg);
    }

    // 6.2. Dữ liệu Danh mục VTHH (Sản phẩm & Nguyên vật liệu theo bảng product và ingredient)
    const vthhDatasets = {
        products: [
            {
                id: 1,
                productCode: 'SP001',
                imageUrl: makeSvgThumb('#fee2e2', '<circle cx="24" cy="24" r="14" fill="#ffffff" stroke="#ef4444" stroke-width="2"/><path d="M24 10a14 14 0 0 1 0 28" fill="#ef4444"/><path d="M18 24h12M24 18v12" stroke="#ffffff" stroke-width="2" stroke-linecap="round"/>'),
                productName: 'Viên nén Panadol Extra 500mg',
                unit: 'Hộp',
                quantity: 1250,
                quantityFormatted: '1.250',
                cost: 42000,
                costFormatted: '42.000',
                expiredDate: '31/12/2027',
                description: 'Giảm đau, hạ sốt; Paracetamol 500mg & Caffeine 65mg (GMP-WHO)'
            },
            {
                id: 2,
                productCode: 'SP002',
                imageUrl: makeSvgThumb('#fef3c7', '<g transform="rotate(-35 24 24)"><rect x="14" y="18" width="10" height="12" rx="5" fill="#f59e0b"/><rect x="24" y="18" width="10" height="12" rx="5" fill="#ef4444"/><rect x="14" y="18" width="20" height="12" rx="6" fill="none" stroke="#b45309" stroke-width="1.5"/></g>'),
                productName: 'Kháng sinh Amoxicillin 500mg',
                unit: 'Hộp',
                quantity: 850,
                quantityFormatted: '850',
                cost: 65000,
                costFormatted: '65.000',
                expiredDate: '15/10/2026',
                description: 'Kháng sinh nhóm Aminopenicillin diệt khuẩn hô hấp'
            },
            {
                id: 3,
                productCode: 'SP003',
                imageUrl: makeSvgThumb('#ffedd5', '<rect x="20" y="8" width="8" height="5" rx="1.5" fill="#9a3412"/><path d="M16 16h16a3 3 0 0 1 3 3v18a3 3 0 0 1-3 3H16a3 3 0 0 1-3-3V19a3 3 0 0 1 3-3z" fill="#ea580c"/><rect x="17" y="22" width="14" height="12" rx="2" fill="#fff" opacity="0.9"/><path d="M24 25v6M21 28h6" stroke="#ea580c" stroke-width="1.5" stroke-linecap="round"/>'),
                productName: 'Siro Ho Bảo Thanh 125ml',
                unit: 'Chai',
                quantity: 2100,
                quantityFormatted: '2.100',
                cost: 38000,
                costFormatted: '38.000',
                expiredDate: '20/08/2027',
                description: 'Thuốc ho đông dược bổ phế, giảm ho khan, ho có đờm'
            },
            {
                id: 4,
                productCode: 'SP004',
                imageUrl: makeSvgThumb('#e0f2fe', '<rect x="21" y="8" width="6" height="4" fill="#0284c7"/><path d="M17 15h14a3 3 0 0 1 3 3v19a3 3 0 0 1-3 3H17a3 3 0 0 1-3-3V18a3 3 0 0 1 3-3z" fill="#38bdf8"/><path d="M24 21c-3 4-4 6-4 8a4 4 0 0 0 8 0c0-2-1-4-4-8z" fill="#ffffff"/>'),
                productName: 'Nước muối sinh lý Natri Clorid 0.9%',
                unit: 'Chai',
                quantity: 4500,
                quantityFormatted: '4.500',
                cost: 8500,
                costFormatted: '8.500',
                expiredDate: '12/05/2028',
                description: 'Dung dịch đẳng trương vô trùng rửa mắt, mũi, vết thương'
            },
            {
                id: 5,
                productCode: 'SP005',
                imageUrl: makeSvgThumb('#fff7ed', '<rect x="19" y="8" width="10" height="4" rx="1" fill="#ea580c"/><rect x="18" y="12" width="12" height="26" rx="2" fill="#f97316"/><circle cx="24" cy="22" r="3.5" fill="#fef08a"/><circle cx="24" cy="30" r="3" fill="#ffffff" opacity="0.8"/><circle cx="21" cy="34" r="1.5" fill="#ffffff" opacity="0.6"/>'),
                productName: 'Vitamin C 500mg Sủi (Tuýp 20V)',
                unit: 'Tuýp',
                quantity: 1600,
                quantityFormatted: '1.600',
                cost: 28000,
                costFormatted: '28.000',
                expiredDate: '30/11/2026',
                description: 'Bổ sung đề kháng, tăng cường miễn dịch cơ thể'
            },
            {
                id: 6,
                productCode: 'SP006',
                imageUrl: makeSvgThumb('#f3e8ff', '<rect x="18" y="8" width="12" height="5" rx="1.5" fill="#eab308"/><rect x="15" y="13" width="18" height="24" rx="3" fill="#7e22ce"/><rect x="18" y="19" width="12" height="12" rx="2" fill="#faf5ff"/><circle cx="24" cy="25" r="3" fill="#eab308"/>'),
                productName: 'Glucosamine Orihiro 900 viên',
                unit: 'Lọ',
                quantity: 320,
                quantityFormatted: '320',
                cost: 480000,
                costFormatted: '480.000',
                expiredDate: '31/12/2026',
                description: 'Hỗ trợ tái tạo sụn khớp, giảm thoái hóa khớp'
            },
            {
                id: 7,
                productCode: 'SP007',
                imageUrl: makeSvgThumb('#dcfce7', '<rect x="18" y="9" width="12" height="5" rx="1.5" fill="#15803d"/><rect x="15" y="14" width="18" height="23" rx="3" fill="#16a34a"/><circle cx="24" cy="25.5" r="5.5" fill="#ffffff"/><path d="M24 23c-2 0-3 2-3 4 2-1 4-1 6 0 0-2-1-4-3-4z" fill="#15803d"/>'),
                productName: 'Ginkgo Biloba 120mg Trunature',
                unit: 'Hộp',
                quantity: 540,
                quantityFormatted: '540',
                cost: 350000,
                costFormatted: '350.000',
                expiredDate: '15/09/2027',
                description: 'Tăng cường tuần hoàn não, cải thiện trí nhớ'
            },
            {
                id: 8,
                productCode: 'SP008',
                imageUrl: makeSvgThumb('#fef9c3', '<rect x="22" y="8" width="4" height="6" fill="#ca8a04"/><rect x="17" y="14" width="14" height="23" rx="3" fill="#eab308"/><circle cx="24" cy="25" r="4" fill="#ffffff"/><circle cx="24" cy="25" r="2" fill="#ea580c"/>'),
                productName: 'LineaBon K2 + D3 Nhỏ giọt 10ml',
                unit: 'Lọ',
                quantity: 780,
                quantityFormatted: '780',
                cost: 185000,
                costFormatted: '185.000',
                expiredDate: '10/04/2027',
                description: 'Bổ sung Vitamin D3 & K2 giúp hấp thu canxi tối đa'
            },
            {
                id: 9,
                productCode: 'SP009',
                imageUrl: makeSvgThumb('#ede9fe', '<rect x="13" y="12" width="22" height="24" rx="3" fill="#6366f1" stroke="#4338ca" stroke-width="1.5"/><circle cx="19" cy="18" r="2.5" fill="#c7d2fe"/><circle cx="29" cy="18" r="2.5" fill="#c7d2fe"/><circle cx="19" cy="25" r="2.5" fill="#c7d2fe"/><circle cx="29" cy="25" r="2.5" fill="#c7d2fe"/>'),
                productName: 'Cefuroxime Axetil 500mg',
                unit: 'Hộp',
                quantity: 620,
                quantityFormatted: '620',
                cost: 110000,
                costFormatted: '110.000',
                expiredDate: '18/06/2026',
                description: 'Kháng sinh Cephalosporin thế hệ 2 điều trị nhiễm khuẩn'
            },
            {
                id: 10,
                productCode: 'SP010',
                imageUrl: makeSvgThumb('#ecfdf5', '<rect x="18" y="9" width="12" height="5" rx="1.5" fill="#047857"/><rect x="15" y="14" width="18" height="23" rx="3" fill="#10b981"/><circle cx="24" cy="25.5" r="5" fill="#a855f7"/><path d="M24 23v5M21.5 25.5h5" stroke="#ffffff" stroke-width="1.5" stroke-linecap="round"/>'),
                productName: 'Milk Thistle Extract 1000mg',
                unit: 'Hộp',
                quantity: 410,
                quantityFormatted: '410',
                cost: 290000,
                costFormatted: '290.000',
                expiredDate: '25/11/2027',
                description: 'Chiết xuất kế sữa bảo vệ và giải độc tế bào gan'
            }
        ],
        ingredients: [
            {
                id: 1,
                ingrCode: 'NVL001',
                ingrName: 'Hoạt chất Paracetamol BP/USP',
                unit: 'kg',
                quantity: 500,
                quantityFormatted: '500',
                price: 185000,
                priceFormatted: '185.000',
                expiredDate: '31/12/2028',
                description: 'Hoạt chất dược dụng tinh khiết ≥99.8% đạt chuẩn USP/BP'
            },
            {
                id: 2,
                ingrCode: 'NVL002',
                ingrName: 'Amoxicillin Trihydrate Compacted',
                unit: 'kg',
                quantity: 350,
                quantityFormatted: '350',
                price: 450000,
                priceFormatted: '450.000',
                expiredDate: '20/07/2027',
                description: 'Nguyên liệu kháng sinh dập viên trực tiếp chuẩn EP'
            },
            {
                id: 3,
                ingrCode: 'NVL003',
                ingrName: 'Tá dược Lactose Monohydrate 200 Mesh',
                unit: 'kg',
                quantity: 1200,
                quantityFormatted: '1.200',
                price: 42000,
                priceFormatted: '42.000',
                expiredDate: '15/10/2029',
                description: 'Tá dược độn dập viên nén đạt tiêu chuẩn GMP quốc tế'
            },
            {
                id: 4,
                ingrCode: 'NVL004',
                ingrName: 'Tá dược Tinh bột ngô (Corn Starch)',
                unit: 'kg',
                quantity: 800,
                quantityFormatted: '800',
                price: 25000,
                priceFormatted: '25.000',
                expiredDate: '30/08/2028',
                description: 'Tá dược rã và dính cho sản xuất viên nén dược phẩm'
            },
            {
                id: 5,
                ingrCode: 'NVL005',
                ingrName: 'Tá dược trơn Magnesium Stearate',
                unit: 'kg',
                quantity: 250,
                quantityFormatted: '250',
                price: 88000,
                priceFormatted: '88.000',
                expiredDate: '12/03/2028',
                description: 'Chất bôi trơn dập viên chống bám dính chày cối'
            },
            {
                id: 6,
                ingrCode: 'NVL006',
                ingrName: 'Tá dược dính Povidone K30 (PVP K30)',
                unit: 'kg',
                quantity: 180,
                quantityFormatted: '180',
                price: 210000,
                priceFormatted: '210.000',
                expiredDate: '05/11/2027',
                description: 'Polyme tạo hạt ướt cao cấp kiểm soát độ hòa tan'
            },
            {
                id: 7,
                ingrCode: 'NVL007',
                ingrName: 'Tá dược siêu rã Croscarmellose Sodium',
                unit: 'kg',
                quantity: 150,
                quantityFormatted: '150',
                price: 195000,
                priceFormatted: '195.000',
                expiredDate: '22/09/2028',
                description: 'Giúp viên nén rã nhanh trong dạ dày dưới 3 phút'
            },
            {
                id: 8,
                ingrCode: 'NVL008',
                ingrName: 'Dung môi Ethanol 96% Dược dụng',
                unit: 'Lít',
                quantity: 600,
                quantityFormatted: '600',
                price: 35000,
                priceFormatted: '35.000',
                expiredDate: '10/01/2029',
                description: 'Cồn tinh khiết chiết xuất và bào chế dung dịch'
            },
            {
                id: 9,
                ingrCode: 'NVL009',
                ingrName: 'Lọ thủy tinh nâu 100ml chuẩn GMP',
                unit: 'Lọ',
                quantity: 5000,
                quantityFormatted: '5.000',
                price: 3200,
                priceFormatted: '3.200',
                expiredDate: '31/12/2030',
                description: 'Bao bì cấp 1 cản sáng cho siro và dung dịch thuốc'
            },
            {
                id: 10,
                ingrCode: 'NVL010',
                ingrName: 'Màng nhôm Alu-Alu bấm vỉ dập lạnh',
                unit: 'Cuộn',
                quantity: 120,
                quantityFormatted: '120',
                price: 1450000,
                priceFormatted: '1.450.000',
                expiredDate: '15/04/2029',
                description: 'Bảo vệ hoạt chất chống ẩm, oxy và ánh sáng tuyệt đối'
            }
        ]
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
            partnerNameLabel: 'Tên nhân viên yêu cầu',
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
            title: 'Lệnh nhập kho LNK00018',
            commandNoLabel: 'Số lệnh nhập kho',
            commandNo: 'LNK00018',
            dueDateLabel: 'Hạn nhập kho',
            dueDate: '28/05/2025 00:00:00',
            statusText: 'Chờ nhập kho',
            employeeCode: 'NV0006',
            employeeCodeOptions: ['NV0006', 'NV0018', 'NV0005', 'NV0021'],
            employeeName: 'Trần Quốc Tuấn',
            description: 'Lệnh nhập lô L00081 sau khi đạt phiếu Kiểm nghiệm QC',
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực nhập',
            items: [
                { id: 1, itemCode: 'SP001', itemName: 'Viên nén Panadol Extra 500mg', unit: 'Hộp', reqQty: 150, actualQty: 150, isFull: true, expiryDate: '31/12/2026' },
                { id: 2, itemCode: 'SP002', itemName: 'Kháng sinh Amoxicillin 500mg', unit: 'Hộp', reqQty: 120, actualQty: 120, isFull: true, expiryDate: '31/12/2026' },
                { id: 3, itemCode: 'SP003', itemName: 'Siro Ho Bảo Thanh 125ml', unit: 'Chai', reqQty: 200, actualQty: 200, isFull: true, expiryDate: '31/12/2026' },
                { id: 4, itemCode: 'SP004', itemName: 'Nước muối sinh lý Natri Clorid 0.9%', unit: 'Chai', reqQty: 300, actualQty: 300, isFull: true, expiryDate: '31/12/2026' },
                { id: 5, itemCode: 'SP005', itemName: 'Vitamin C 500mg Sủi (Tuýp 20V)', unit: 'Tuýp', reqQty: 180, actualQty: 180, isFull: true, expiryDate: '31/12/2026' }
            ]
        },
        outboundCommand: {
            docType: 'outbound-command',
            title: 'Lệnh xuất kho LXK00088',
            commandNoLabel: 'Số lệnh xuất kho',
            commandNo: 'LXK00088',
            dueDateLabel: 'Hạn xuất kho',
            dueDate: '31/12/2025 00:00:00',
            statusText: 'Chờ xuất kho',
            employeeCode: 'NV0018',
            employeeCodeOptions: ['NV0018', 'NV0005', 'NV0021'],
            employeeName: 'Nguyễn Tiến Huy',
            description: 'Lệnh xuất nguyên liệu Paracetamol & Tá dược cho mẻ SX 12',
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực xuất',
            items: [
                { id: 1, itemCode: 'NVL001', itemName: 'Hoạt chất Paracetamol BP/USP', unit: 'kg', reqQty: 50, actualQty: 50, isFull: true, expiryDate: '31/12/2026' },
                { id: 2, itemCode: 'NVL002', itemName: 'Tá dược Lactose Monohydrate 200M', unit: 'kg', reqQty: 30, actualQty: 30, isFull: true, expiryDate: '31/12/2026' },
                { id: 3, itemCode: 'NVL003', itemName: 'Tá dược Tinh bột ngô (Corn Starch)', unit: 'kg', reqQty: 15, actualQty: 15, isFull: true, expiryDate: '31/12/2026' },
                { id: 4, itemCode: 'NVL004', itemName: 'Tá dược Povidone K30 (PVP K30)', unit: 'kg', reqQty: 5, actualQty: 5, isFull: true, expiryDate: '31/12/2026' },
                { id: 5, itemCode: 'NVL005', itemName: 'Tá dược trơn Magnesium Stearate', unit: 'kg', reqQty: 2.5, actualQty: 2.5, isFull: true, expiryDate: '31/12/2026' }
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
            activeTab: 'lenh-nhap-kho',
            searchKeyword: '',
            timeFilter: 'Tháng này',
            statusFilter: 'Tất cả'
        },
        // State cho trang Xuất kho
        outbound: {
            activeTab: 'lenh-xuat-kho',
            searchKeyword: '',
            orderStatusFilter: 'Tất cả',
            selectedRows: {}
        },
        // State cho trang Danh mục VTHH (Sản phẩm & Nguyên vật liệu, tìm kiếm riêng)
        vthh: {
            activeTab: 'san-pham', // 'san-pham' | 'nguyen-vat-lieu'
            productsSearchKeyword: '',
            ingredientsSearchKeyword: ''
        },
        vthhModal: null, // { itemType: 'product'|'ingredient', itemId: 1, mode: 'view'|'edit' }
        // Quản lý Modal Chứng từ đang mở:
        // 'purchase-order' | 'sales-order' | 'inbound-command' | 'outbound-command' | 'inbound' | 'outbound' | null
        activeVoucherModal: null,
        activeVoucherDocNo: null
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
                case 'inbound-command': {
                    const activeDocNo = state.activeVoucherDocNo || 'LNK00018';
                    const cmdRow = inboundDatasets.inboundCommands.find(c => c.orderNo === activeDocNo) || inboundDatasets.inboundCommands[0];
                    if (cmdRow) {
                        commandsDetailData.inboundCommand.commandNo = cmdRow.orderNo;
                        commandsDetailData.inboundCommand.title = `Lệnh nhập kho ${cmdRow.orderNo}`;
                        commandsDetailData.inboundCommand.dueDate = `${cmdRow.deliveryDate || cmdRow.orderDate} 00:00:00`;
                        commandsDetailData.inboundCommand.statusText = cmdRow.status;
                        commandsDetailData.inboundCommand.employeeName = cmdRow.supplier;
                        commandsDetailData.inboundCommand.description = cmdRow.description;
                    }
                    return commandsDetailData.inboundCommand;
                }
                case 'outbound-command': {
                    const activeDocNo = state.activeVoucherDocNo || 'LXK00088';
                    const cmdRow = outboundDatasets.outboundCommands.find(c => c.orderNo === activeDocNo) || outboundDatasets.outboundCommands[0];
                    if (cmdRow) {
                        commandsDetailData.outboundCommand.commandNo = cmdRow.orderNo;
                        commandsDetailData.outboundCommand.title = `Lệnh xuất kho ${cmdRow.orderNo}`;
                        commandsDetailData.outboundCommand.statusText = cmdRow.orderStatus;
                        commandsDetailData.outboundCommand.description = cmdRow.description;
                    }
                    return commandsDetailData.outboundCommand;
                }
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

        updateOutboundCommandStatus(orderNo, newStatus) {
            const cmd = outboundDatasets.outboundCommands.find(c => c.orderNo === orderNo);
            if (cmd) {
                cmd.orderStatus = newStatus;
                cmd.orderStatusType = newStatus === 'Đã xuất kho' ? 'success' : 'warning';
                if (commandsDetailData.outboundCommand.commandNo === orderNo) {
                    commandsDetailData.outboundCommand.statusText = newStatus;
                }
                return;
            }
            const receipt = outboundDatasets.outboundReceipts.find(r => r.voucherNo === orderNo || r.orderNo === orderNo);
            if (receipt) {
                receipt.status = newStatus;
                receipt.orderStatus = newStatus;
                receipt.statusType = newStatus === 'Đã xuất kho' ? 'success' : 'warning';
                receipt.orderStatusType = receipt.statusType;
            }
        },

        updateOutboundReceiptStatus(voucherNo, newStatus) {
            const receipt = outboundDatasets.outboundReceipts.find(r => r.voucherNo === voucherNo || r.orderNo === voucherNo);
            if (receipt) {
                receipt.status = newStatus;
                receipt.orderStatus = newStatus;
                receipt.statusType = newStatus === 'Đã xuất kho' ? 'success' : 'warning';
                receipt.orderStatusType = receipt.statusType;
            }
        },

        updateInboundCommandStatus(orderNo, newStatus) {
            const cmd = inboundDatasets.inboundCommands.find(c => c.orderNo === orderNo);
            if (cmd) {
                cmd.status = newStatus;
                cmd.statusType = newStatus === 'Đã nhập kho' ? 'success' : 'warning';
                if (commandsDetailData.inboundCommand.commandNo === orderNo) {
                    commandsDetailData.inboundCommand.statusText = newStatus;
                }
                return;
            }
            const receipt = inboundDatasets.inboundReceipts.find(r => r.voucherNo === orderNo || r.orderNo === orderNo);
            if (receipt) {
                receipt.status = newStatus;
                receipt.statusType = newStatus === 'Đã nhập kho' ? 'success' : 'warning';
            }
        },

        updateInboundReceiptStatus(voucherNo, newStatus) {
            const receipt = inboundDatasets.inboundReceipts.find(r => r.voucherNo === voucherNo || r.orderNo === voucherNo);
            if (receipt) {
                receipt.status = newStatus;
                receipt.statusType = newStatus === 'Đã nhập kho' ? 'success' : 'warning';
            }
        },

        openVoucherModal(voucherType, docNo) {
            state.activeVoucherModal = voucherType;
            state.activeVoucherDocNo = docNo || null;
        },

        closeVoucherModal() {
            state.activeVoucherModal = null;
            state.activeVoucherDocNo = null;
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

        // Cập nhật tab & bộ lọc cho Nhập kho / Xuất kho / Danh mục VTHH
        setOperationTab(pageType, tabId) {
            state.activeVoucherModal = null;
            if (pageType === 'nhap-kho') {
                state.inbound.activeTab = tabId;
            } else if (pageType === 'xuat-kho') {
                state.outbound.activeTab = tabId;
            } else if (pageType === 'danh-muc-vthh') {
                state.vthh.activeTab = tabId;
            }
        },

        setOperationSearch(pageType, keyword) {
            if (pageType === 'nhap-kho') {
                state.inbound.searchKeyword = keyword || '';
            } else if (pageType === 'xuat-kho') {
                state.outbound.searchKeyword = keyword || '';
            } else if (pageType === 'danh-muc-vthh') {
                if (state.vthh.activeTab === 'san-pham') {
                    state.vthh.productsSearchKeyword = keyword || '';
                } else {
                    state.vthh.ingredientsSearchKeyword = keyword || '';
                }
            }
        },

        setOperationStatusFilter(pageType, statusValue) {
            if (pageType === 'nhap-kho') {
                state.inbound.statusFilter = statusValue;
            } else if (pageType === 'xuat-kho') {
                state.outbound.orderStatusFilter = statusValue;
            }
        },

        // Xóa dòng hàng hóa ở Danh mục VTHH (Sản phẩm hoặc Nguyên vật liệu)
        deleteVthhItem(itemType, itemId) {
            if (itemType === 'product') {
                const idx = vthhDatasets.products.findIndex(p => String(p.id) === String(itemId) || p.productCode === String(itemId));
                if (idx !== -1) {
                    return vthhDatasets.products.splice(idx, 1)[0];
                }
            } else if (itemType === 'ingredient') {
                const idx = vthhDatasets.ingredients.findIndex(i => String(i.id) === String(itemId) || i.ingrCode === String(itemId));
                if (idx !== -1) {
                    return vthhDatasets.ingredients.splice(idx, 1)[0];
                }
            }
            return null;
        },

        // Quản lý Modal thông tin chi tiết / chỉnh sửa VTHH
        openVthhModal(itemType, itemId, mode = 'view') {
            state.vthhModal = {
                itemType,
                itemId,
                mode: mode || 'view'
            };
        },

        // Mở pop-up thêm mới sản phẩm hoặc nguyên vật liệu
        openVthhCreateModal(itemType) {
            const isProd = itemType === 'product';
            let nextNum = 1;
            if (isProd) {
                if (vthhDatasets.products.length > 0) {
                    const nums = vthhDatasets.products.map(p => {
                        const m = String(p.productCode).match(/\d+/);
                        return m ? parseInt(m[0], 10) : 0;
                    });
                    nextNum = Math.max(...nums) + 1;
                }
            } else {
                if (vthhDatasets.ingredients.length > 0) {
                    const nums = vthhDatasets.ingredients.map(i => {
                        const m = String(i.ingrCode).match(/\d+/);
                        return m ? parseInt(m[0], 10) : 0;
                    });
                    nextNum = Math.max(...nums) + 1;
                }
            }
            const nextCode = isProd
                ? `SP${String(nextNum).padStart(3, '0')}`
                : `NVL${String(nextNum).padStart(3, '0')}`;

            const defaultImg = isProd
                ? makeSvgThumb('#fee2e2', '<circle cx="24" cy="24" r="14" fill="#ffffff" stroke="#ef4444" stroke-width="2"/><path d="M24 10a14 14 0 0 1 0 28" fill="#ef4444"/><path d="M18 24h12M24 18v12" stroke="#ffffff" stroke-width="2" stroke-linecap="round"/>')
                : '';

            const initialItem = isProd ? {
                id: null,
                productCode: nextCode,
                productName: '',
                imageUrl: defaultImg,
                unit: 'Hộp',
                quantity: 100,
                cost: 50000,
                expiredDate: '31/12/2028',
                description: ''
            } : {
                id: null,
                ingrCode: nextCode,
                ingrName: '',
                unit: 'kg',
                quantity: 50,
                price: 100000,
                expiredDate: '31/12/2028',
                description: ''
            };

            state.vthhModal = {
                itemType,
                mode: 'create',
                item: initialItem
            };
        },

        closeVthhModal() {
            state.vthhModal = null;
        },

        getVthhModalData() {
            if (!state.vthhModal) return null;
            const { itemType, itemId, mode } = state.vthhModal;
            if (mode === 'create') {
                return {
                    isOpen: true,
                    mode: 'create',
                    itemType,
                    item: state.vthhModal.item
                };
            }
            let item = null;
            if (itemType === 'product') {
                item = vthhDatasets.products.find(p => String(p.id) === String(itemId) || p.productCode === String(itemId));
            } else if (itemType === 'ingredient') {
                item = vthhDatasets.ingredients.find(i => String(i.id) === String(itemId) || i.ingrCode === String(itemId));
            }
            if (!item) return null;
            return {
                isOpen: true,
                mode,
                itemType,
                item: { ...item }
            };
        },

        // Thêm mới sản phẩm hoặc nguyên vật liệu vào danh mục
        addVthhItem(itemType, newItem) {
            if (itemType === 'product') {
                const nextId = vthhDatasets.products.length > 0 ? Math.max(...vthhDatasets.products.map(p => p.id)) + 1 : 1;
                const qty = Number(newItem.quantity) || 0;
                const cost = Number(newItem.cost) || 0;
                const item = {
                    id: nextId,
                    productCode: newItem.productCode ? newItem.productCode.trim() : `SP${String(nextId).padStart(3, '0')}`,
                    imageUrl: newItem.imageUrl || makeSvgThumb('#fee2e2', '<circle cx="24" cy="24" r="14" fill="#ffffff" stroke="#ef4444" stroke-width="2"/><path d="M24 10a14 14 0 0 1 0 28" fill="#ef4444"/><path d="M18 24h12M24 18v12" stroke="#ffffff" stroke-width="2" stroke-linecap="round"/>'),
                    productName: newItem.productName ? newItem.productName.trim() : '',
                    unit: newItem.unit || 'Hộp',
                    quantity: qty,
                    quantityFormatted: qty.toLocaleString('vi-VN'),
                    cost: cost,
                    costFormatted: cost.toLocaleString('vi-VN'),
                    expiredDate: newItem.expiredDate || '31/12/2028',
                    description: newItem.description || ''
                };
                vthhDatasets.products.unshift(item);
                return item;
            } else if (itemType === 'ingredient') {
                const nextId = vthhDatasets.ingredients.length > 0 ? Math.max(...vthhDatasets.ingredients.map(i => i.id)) + 1 : 1;
                const qty = Number(newItem.quantity) || 0;
                const price = Number(newItem.price) || 0;
                const item = {
                    id: nextId,
                    ingrCode: newItem.ingrCode ? newItem.ingrCode.trim() : `NVL${String(nextId).padStart(3, '0')}`,
                    ingrName: newItem.ingrName ? newItem.ingrName.trim() : '',
                    unit: newItem.unit || 'kg',
                    quantity: qty,
                    quantityFormatted: qty.toLocaleString('vi-VN'),
                    price: price,
                    priceFormatted: price.toLocaleString('vi-VN'),
                    expiredDate: newItem.expiredDate || '31/12/2028',
                    description: newItem.description || ''
                };
                vthhDatasets.ingredients.unshift(item);
                return item;
            }
            return null;
        },

        updateVthhItem(itemType, itemId, updatedData) {
            if (itemType === 'product') {
                const idx = vthhDatasets.products.findIndex(p => String(p.id) === String(itemId) || p.productCode === String(itemId));
                if (idx !== -1) {
                    const current = vthhDatasets.products[idx];
                    const qty = updatedData.quantity !== undefined ? (Number(updatedData.quantity) || 0) : current.quantity;
                    const cost = updatedData.cost !== undefined ? (Number(updatedData.cost) || 0) : current.cost;
                    vthhDatasets.products[idx] = {
                        ...current,
                        ...updatedData,
                        productCode: current.productCode, // Mã sản phẩm là định danh không được sửa
                        quantity: qty,
                        quantityFormatted: qty.toLocaleString('vi-VN'),
                        cost: cost,
                        costFormatted: cost.toLocaleString('vi-VN')
                    };
                    return vthhDatasets.products[idx];
                }
            } else if (itemType === 'ingredient') {
                const idx = vthhDatasets.ingredients.findIndex(i => String(i.id) === String(itemId) || i.ingrCode === String(itemId));
                if (idx !== -1) {
                    const current = vthhDatasets.ingredients[idx];
                    const qty = updatedData.quantity !== undefined ? (Number(updatedData.quantity) || 0) : current.quantity;
                    const price = updatedData.price !== undefined ? (Number(updatedData.price) || 0) : current.price;
                    vthhDatasets.ingredients[idx] = {
                        ...current,
                        ...updatedData,
                        ingrCode: current.ingrCode, // Mã NVL là định danh không được sửa
                        quantity: qty,
                        quantityFormatted: qty.toLocaleString('vi-VN'),
                        price: price,
                        priceFormatted: price.toLocaleString('vi-VN')
                    };
                    return vthhDatasets.ingredients[idx];
                }
            }
            return null;
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
            if (state.inbound.activeTab === 'don-mua-hang') {
                state.inbound.activeTab = 'lenh-nhap-kho';
            }
            const isVoucherTab = state.inbound.activeTab === 'nhap-kho-tab';
            const isCommandTab = state.inbound.activeTab === 'lenh-nhap-kho';
            let sourceRows = [];

            if (state.inbound.activeTab === 'lenh-nhap-kho') {
                sourceRows = inboundDatasets.inboundCommands;
            } else {
                sourceRows = inboundDatasets.inboundReceipts;
            }

            const combinedKw = (state.inbound.searchKeyword || state.searchKeyword).trim();
            let filtered = isVoucherTab
                ? filterByKeyword(sourceRows, combinedKw, ['voucherNo', 'partnerName', 'description', 'status'])
                : filterByKeyword(sourceRows, combinedKw, ['orderNo', 'supplier', 'productName', 'description', 'status']);

            if (state.inbound.statusFilter && state.inbound.statusFilter !== 'Tất cả') {
                filtered = filtered.filter(r => r.status === state.inbound.statusFilter);
            }

            let columns = [];
            if (isVoucherTab) {
                // Tab Nhập kho NVL: chuẩn hóa giống Xuất kho NVL
                // Bỏ kho nhập, người giao, loại phiếu. Đổi Tên đối tượng giao hàng thành Tên nhân viên yêu cầu.
                // Thêm cột trạng thái và tình trạng nhập kho có 2 nút duyệt và từ chối.
                columns = [
                    { key: 'voucherNo', label: 'Số phiếu nhập kho', isLink: true, width: '150px' },
                    { key: 'voucherDate', label: 'Ngày nhập kho', width: '160px' },
                    { key: 'partnerName', label: 'Tên nhân viên yêu cầu', width: '200px' },
                    { key: 'description', label: 'Ghi chú' },
                    { key: 'status', label: 'Trạng thái', isOrderStatus: true, width: '150px' },
                    { key: 'inboundApproval', label: 'Tình trạng nhập kho', isApprovalButtons: true, width: '180px' }
                ];
            } else {
                // Tab Nhập kho thành phẩm: Lệnh nhập kho
                columns = [
                    { key: 'orderNo', label: 'Số lệnh nhập', isLink: true, width: '130px' },
                    { key: 'orderDate', label: 'Ngày giao', width: '120px' },
                    { key: 'supplier', label: 'Tên nhân viên', width: '180px' },
                    { key: 'productName', label: 'Tên sản phẩm', width: '240px' },
                    { key: 'description', label: 'Ghi chú' },
                    { key: 'status', label: 'Trạng thái', isOrderStatus: true, width: '150px' },
                    { key: 'inboundApproval', label: 'Tình trạng nhập kho', isApprovalButtons: true, width: '180px' }
                ];
            }

            const filters = [
                {
                    id: 'inbound-status-filter',
                    prefix: 'Trạng thái:',
                    value: state.inbound.statusFilter,
                    options: ['Tất cả', 'Chờ nhập kho', 'Đã nhập kho']
                }
            ];

            const targetModalType = state.inbound.activeTab === 'lenh-nhap-kho' ? 'inbound-command' : 'inbound';

            return {
                pageType: 'nhap-kho',
                voucherType: targetModalType,
                canOpenVoucher: true,
                activeTab: state.inbound.activeTab,
                tabs: [
                    { id: 'lenh-nhap-kho', label: 'Nhập kho thành phẩm' },
                    { id: 'nhap-kho-tab', label: 'Nhập kho NVL' }
                ],
                searchValue: state.inbound.searchKeyword,
                filters,
                // Bỏ đi các nút tác vụ chỉ giữ lại nút refresh giống Xuất kho NVL
                showAddButton: false,
                addButtonLabel: 'Thêm',
                hideSplitLayout: true,
                hideSettings: true,
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
            if (state.outbound.activeTab === 'don-dat-hang') {
                state.outbound.activeTab = 'lenh-xuat-kho';
            }
            const isVoucherTab = state.outbound.activeTab === 'xuat-kho-tab';
            const isCommandTab = state.outbound.activeTab === 'lenh-xuat-kho';
            let sourceRows = [];

            if (state.outbound.activeTab === 'lenh-xuat-kho') {
                sourceRows = outboundDatasets.outboundCommands;
            } else {
                sourceRows = outboundDatasets.outboundReceipts;
            }

            const combinedKw = (state.outbound.searchKeyword || state.searchKeyword).trim();
            let filtered = isVoucherTab
                ? filterByKeyword(sourceRows, combinedKw, ['voucherNo', 'partnerName', 'description', 'status'])
                : filterByKeyword(sourceRows, combinedKw, ['orderNo', 'customer', 'description', 'orderStatus', 'outboundStatus']);

            if (state.outbound.orderStatusFilter && state.outbound.orderStatusFilter !== 'Tất cả') {
                filtered = filtered.filter(r => (r.status === state.outbound.orderStatusFilter || r.orderStatus === state.outbound.orderStatusFilter));
            }

            let columns = [];
            if (isVoucherTab) {
                // Tab Xuất kho thành phẩm: bỏ kho xuất, người nhận, loại phiếu. Đổi Tên đối tượng nhận hàng thành Tên nhân viên yêu cầu. Thêm cột trạng thái và tình trạng xuất kho
                columns = [
                    { key: 'voucherNo', label: 'Số phiếu xuất kho', isLink: true, width: '150px' },
                    { key: 'voucherDate', label: 'Ngày xuất kho', width: '160px' },
                    { key: 'partnerName', label: 'Tên nhân viên yêu cầu', width: '200px' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'status', label: 'Trạng thái', isOrderStatus: true, width: '150px' },
                    { key: 'outboundStatus', label: 'Tình trạng xuất kho', isApprovalButtons: true, width: '180px' }
                ];
            } else {
                // Tab Xuất kho NVL: Cột diễn giải đổi thành Ghi chú, tình trạng đơn hàng đổi thành trạng thái, tình trạng xuất kho có 2 nút duyệt và từ chối
                columns = [
                    { key: 'orderNo', label: 'Số lệnh xuất', isLink: true, width: '125px' },
                    { key: 'orderDate', label: 'Ngày giao', width: '125px' },
                    { key: 'customer', label: 'Tên nhân viên yêu cầu', width: '175px' },
                    { key: 'deliveryDue', label: 'Hạn giao hàng', width: '125px' },
                    { key: 'description', label: 'Ghi chú' },
                    { key: 'orderStatus', label: 'Trạng thái', isOrderStatus: true, width: '150px' },
                    { key: 'outboundStatus', label: 'Tình trạng xuất kho', isApprovalButtons: true, width: '180px' }
                ];
            }

            const filters = [
                {
                    id: 'outbound-status-filter',
                    prefix: 'Trạng thái:',
                    value: state.outbound.orderStatusFilter,
                    options: ['Tất cả', 'Chờ xuất kho', 'Đã xuất kho']
                }
            ];

            const targetModalType = state.outbound.activeTab === 'xuat-kho-tab' ? 'outbound' : 'outbound-command';

            return {
                pageType: 'xuat-kho',
                voucherType: targetModalType,
                canOpenVoucher: true,
                activeTab: state.outbound.activeTab,
                tabs: [
                    { id: 'lenh-xuat-kho', label: 'Xuất kho NVL' },
                    { id: 'xuat-kho-tab', label: 'Xuất kho thành phẩm' }
                ],
                searchValue: state.outbound.searchKeyword,
                filters,
                // Bỏ các nút tác vụ thêm, cài đặt, hiển thị, chỉ giữ lại nút refresh như các tab trước
                showAddButton: false,
                addButtonLabel: 'Thêm',
                hideSplitLayout: true,
                hideSettings: true,
                showCheckbox: !isVoucherTab,
                columns,
                rows: filtered,
                totalRecords: filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: false
            };
        },

        // Cấu hình truyền vào TabbedDataGridComponent cho trang Danh mục VTHH
        getVthhGridConfig() {
            const isProductTab = state.vthh.activeTab === 'san-pham';
            const sourceRows = isProductTab ? vthhDatasets.products : vthhDatasets.ingredients;
            const currentKw = (isProductTab
                ? (state.vthh.productsSearchKeyword || '')
                : (state.vthh.ingredientsSearchKeyword || '')).trim();

            const filtered = isProductTab
                ? filterByKeyword(sourceRows, currentKw, ['productCode', 'productName', 'unit', 'description'])
                : filterByKeyword(sourceRows, currentKw, ['ingrCode', 'ingrName', 'unit', 'description']);

            let columns = [];
            if (isProductTab) {
                // Tab Sản phẩm: Cột ảnh ở giữa cột Mã sản phẩm và Tên sản phẩm; Cột thao tác ở cuối
                columns = [
                    { key: 'productCode', label: 'Mã sản phẩm', isLink: true, width: '120px' },
                    { key: 'imageUrl', label: 'Hình ảnh', isImage: true, width: '75px', align: 'center' },
                    { key: 'productName', label: 'Tên sản phẩm', width: '230px' },
                    { key: 'unit', label: 'ĐVT', width: '80px' },
                    { key: 'quantityFormatted', label: 'SL tồn', width: '100px' },
                    { key: 'costFormatted', label: 'Giá vốn (VNĐ)', width: '130px' },
                    { key: 'expiredDate', label: 'Hạn sử dụng', width: '120px' },
                    { key: 'description', label: 'Mô tả / Tiêu chuẩn' },
                    { key: 'action', label: 'Thao tác', isActionDelete: true, width: '95px', align: 'center' }
                ];
            } else {
                // Tab Nguyên vật liệu: Cột thao tác ở cuối
                columns = [
                    { key: 'ingrCode', label: 'Mã nguyên vật liệu', isLink: true, width: '140px' },
                    { key: 'ingrName', label: 'Tên nguyên vật liệu', width: '240px' },
                    { key: 'unit', label: 'ĐVT', width: '80px' },
                    { key: 'quantityFormatted', label: 'SL tồn', width: '100px' },
                    { key: 'priceFormatted', label: 'Đơn giá (VNĐ)', width: '130px' },
                    { key: 'expiredDate', label: 'Hạn sử dụng', width: '120px' },
                    { key: 'description', label: 'Mô tả / Tiêu chuẩn' },
                    { key: 'action', label: 'Thao tác', isActionDelete: true, width: '95px', align: 'center' }
                ];
            }

            return {
                pageType: 'danh-muc-vthh',
                voucherType: null,
                canOpenVoucher: false,
                activeTab: state.vthh.activeTab,
                tabs: [
                    { id: 'san-pham', label: 'Sản phẩm' },
                    { id: 'nguyen-vat-lieu', label: 'Nguyên vật liệu' }
                ],
                searchValue: isProductTab ? state.vthh.productsSearchKeyword : state.vthh.ingredientsSearchKeyword,
                filters: [],
                showAddButton: true,
                addButtonLabel: 'Thêm',
                hideSplitLayout: true,
                hideSettings: true,
                showCheckbox: false,
                columns,
                rows: filtered,
                totalRecords: filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: false
            };
        }
    };
})();
