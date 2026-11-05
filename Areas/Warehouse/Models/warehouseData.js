/**
 * Areas/Warehouse/Models/warehouseData.js
 * ---------------------------------------------------------
 * [MODEL LAYER - MVC]
 * Quản lý dữ liệu và trạng thái (State) của phân hệ Kho hàng (Warehouse).
 * 
 * ĐẶC BIỆT: Dữ liệu Danh mục VTHH (Sản phẩm & Nguyên vật liệu) và Nhà cung cấp
 * được kết nối trực tiếp với Cơ sở dữ liệu Supabase thông qua REST Web API:
 *   - /api/warehouse/products
 *   - /api/warehouse/ingredients
 *   - /api/warehouse/suppliers
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
            { orderNo: 'DMH00022', orderDate: '22/05/2025', supplier: 'Công ty Hồng Hà (Demo)', deliveryDate: '31/05/2025', description: '-', status: 'Đang thực hiện', statusType: 'info' }
        ],
        pharmaOrders: [
            { orderNo: 'DMH00006', orderDate: '28/05/2025', supplier: 'Công ty CP Dược phẩm Trung ương CPC1', deliveryDate: '03/06/2025', description: 'Nhập hoạt chất Paracetamol BP/USP', status: 'Đang thực hiện', statusType: 'info' },
            { orderNo: 'DMH00022', orderDate: '22/05/2025', supplier: 'Công ty TNHH Zuellig Pharma Việt Nam', deliveryDate: '31/05/2025', description: 'Nhập kháng sinh Amoxicillin Trihydrate', status: 'Đang thực hiện', statusType: 'info' }
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
                creatorName: 'Nguyễn Tiến Huy',
                employeeCode: 'NV0005',
                supplierName: 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM',
                supplierCode: 'NCC0006',
                warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI',
                partnerName: 'Nguyễn Tiến Huy',
                deliverer: 'Phan Thanh Hà',
                description: 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM',
                status: 'Chờ nhập kho',
                statusType: 'warning',
                subType: 'Nhập kho mua hàng',
                items: [
                    {
                        id: 1,
                        itemCode: 'BICAR CHU TIM ...',
                        itemName: 'Phụ gia thức ăn chăn nuôi sodium bicarbonate feed grade ( NaHCO3 )',
                        unit: 'kg',
                        quantity: 500,
                        price: 25000,
                        amount: 12500000,
                        expiryDate: '31/12/2026'
                    },
                    {
                        id: 2,
                        itemCode: 'BICAR FOOD SIN...',
                        itemName: 'Phụ gia thực phẩm Sodium Bicarbonate ( NaHCO3 )',
                        unit: 'kg',
                        quantity: 500,
                        price: 30000,
                        amount: 15000000,
                        expiryDate: '31/12/2026'
                    }
                ]
            }
        ]
    };

    // 5. Dữ liệu màn hình "Xuất kho"
    const outboundDatasets = {
        defaultOrders: [
            { orderNo: 'DHTP00402', orderDate: '31/12/2025', customer: 'Organic Good...', deliveryDue: '31/12/2025', description: 'Bán hàng cho Organic Good Store', orderStatus: 'Chưa thực hiện', orderStatusType: 'warning', outboundStatus: 'Chưa thực hiện' }
        ],
        pharmaOrders: [
            { orderNo: 'DHTP00402', orderDate: '31/12/2025', customer: 'Hệ thống Nhà thuốc Long Châu', deliveryDue: '31/12/2025', description: 'Xuất bán Glucosamine Orihiro & Ginkgo Biloba (FEFO)', orderStatus: 'Chưa thực hiện', orderStatusType: 'warning', outboundStatus: 'Chưa thực hiện' }
        ],
        outboundCommands: [
            { orderNo: 'LXK00088', orderDate: '31/12/2025', customer: 'Phân xưởng Viên nén GMP', deliveryDue: '31/12/2025', description: 'Lệnh xuất nguyên liệu Paracetamol & Tá dược cho mẻ SX 12', orderStatus: 'Chờ xuất kho', orderStatusType: 'warning', outboundStatus: 'Chưa thực hiện' }
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
            }
        ]
    };

    // 6. Dữ liệu Chi tiết Phiếu Nhập Kho & Phiếu Xuất Kho
    const vouchersData = {
        inbound: {
            voucherType: 'inbound',
            voucherNo: 'PNK00005',
            selectedSubType: 'Nhập kho mua hàng',
            employeeCode: 'NV0005',
            employeeName: 'Nguyễn Tiến Huy',
            supplierCode: 'NCC0006',
            supplierName: 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM',
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
                    price: 25000,
                    amount: 12500000,
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
                    price: 30000,
                    amount: 15000000,
                    batchNo: 'LHSX3223',
                    expiryDate: '31/12/2026'
                }
            ]
        },
        outbound: {
            voucherType: 'outbound',
            voucherNo: 'PXK00002',
            selectedSubType: 'Xuất kho bán hàng',
            partnerCode: 'MB1_BANACO',
            partnerName: 'CÔNG TY TNHH XUẤT NHẬP KHẨU BANACO VIỆT NAM',
            warehouse: 'KHO HƯNG YÊN - Kho chính',
            receiver: 'Trần Vương',
            voucherDate: '24/04/2025 08:39:28',
            quantityColumnLabel: 'SL xuất',
            items: [
                {
                    id: 1,
                    itemCode: 'BORIC NGA',
                    itemName: 'BORIC ACID ( Axit boric 99.9% Granular ) H3BO3',
                    unit: 'kg',
                    quantity: 5000,
                    batchNo: '',
                    expiryDate: ''
                }
            ]
        }
    };

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
            partnerNameLabel: 'Tên nhà cung cấp',
            partnerName: 'Công ty Cổ phần Thực Phẩm Đông Lạnh Bình An (demo)',
            warehouseColLabel: 'Kho',
            reqQtyColLabel: 'SL yêu cầu',
            doneQtyColLabel: 'SL đã nhận',
            primaryActionLabel: 'Lập lệnh nhập kho',
            primaryActionTarget: 'inbound-command',
            items: [
                { id: 1, itemCode: 'HH052', itemName: 'Ức Gà nguyên xương', unit: 'kg', reqQty: 5, doneQty: 0 }
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
            partnerNameLabel: 'Tên nhân viên yêu cầu',
            partnerName: 'Organic Good Store - Hệ thống Phân phối Dược & Thực phẩm',
            warehouseColLabel: 'Kho xuất',
            reqQtyColLabel: 'SL đặt hàng',
            doneQtyColLabel: 'SL đã xuất',
            primaryActionLabel: 'Lập lệnh xuất kho',
            primaryActionTarget: 'outbound-command',
            items: [
                { id: 1, itemCode: 'DP001', itemName: 'Ginkgo Biloba 120mg Trunature', unit: 'Hộp', reqQty: 30, doneQty: 0 }
            ]
        }
    };

    const commandsDetailData = {
        inboundCommand: {
            docType: 'inbound-command',
            title: 'Lệnh nhập kho LNK00018',
            commandNo: 'LNK00018',
            dueDate: '28/05/2025 00:00:00',
            statusText: 'Chờ nhập kho',
            employeeName: 'Trần Quốc Tuấn',
            description: 'Lệnh nhập lô L00081 sau khi đạt phiếu Kiểm nghiệm QC',
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực nhập',
            items: [
                { id: 1, itemCode: 'SP001', itemName: 'Viên nén Panadol Extra 500mg', unit: 'Hộp', reqQty: 150, actualQty: 150, isFull: true, expiryDate: '31/12/2026' }
            ]
        },
        outboundCommand: {
            docType: 'outbound-command',
            title: 'Lệnh xuất kho LXK00088',
            commandNo: 'LXK00088',
            dueDate: '31/12/2025 00:00:00',
            statusText: 'Chờ xuất kho',
            employeeName: 'Nguyễn Tiến Huy',
            description: 'Lệnh xuất nguyên liệu Paracetamol & Tá dược cho mẻ SX 12',
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực xuất',
            items: [
                { id: 1, itemCode: 'NVL001', itemName: 'Hoạt chất Paracetamol BP/USP', unit: 'kg', reqQty: 50, actualQty: 50, isFull: true, expiryDate: '31/12/2026' }
            ]
        }
    };

    // =========================================================================
    // DỮ LIỆU THỰC TẾ TỪ CƠ SỞ DỮ LIỆU (Supabase PostgreSQL qua API)
    // =========================================================================
    const vthhDatasets = {
        products: [],
        ingredients: []
    };

    let supplierList = [];

    // State của giao diện Kho hàng
    const state = {
        activeMenuId: 'tong-quan',
        sidebarCollapsed: false,
        searchKeyword: '',
        usePharmaFullData: false,
        expiringDaysTitle: 30,
        pendingDaysParam: 5,
        isParamPopoverOpen: false,
        lastUpdatedMap: {
            'card-below-min': '08:00',
            'card-above-max': '08:00',
            'card-expiring-soon': '08:00',
            'card-expired': '08:00'
        },
        inbound: {
            activeTab: 'lenh-nhap-kho',
            searchKeyword: '',
            timeFilter: 'Tháng này',
            statusFilter: 'Tất cả'
        },
        outbound: {
            activeTab: 'lenh-xuat-kho',
            searchKeyword: '',
            orderStatusFilter: 'Tất cả',
            selectedRows: {}
        },
        vthh: {
            activeTab: 'san-pham', // 'san-pham' | 'nguyen-vat-lieu'
            productsSearchKeyword: '',
            ingredientsSearchKeyword: '',
            isLoading: false
        },
        vthhModal: null, // { itemType: 'product'|'ingredient', itemId: 1, mode: 'view'|'edit'|'create', item: {...} }
        activeVoucherModal: null,
        activeVoucherDocNo: null,
        activeVoucherMode: 'view',
        inboundNewDraft: null
    };

    // =========================================================================
    // API CALLS (BẤT ĐỒNG BỘ TRỰC TIẾP TỚI SUPABASE VIA C# BACKEND)
    // =========================================================================
    async function fetchProductsApi(keyword = '') {
        try {
            state.vthh.isLoading = true;
            const url = '/api/warehouse/products' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            vthhDatasets.products = data.map(p => ({
                id: p.id || p.productId,
                productId: p.productId || p.id,
                productCode: p.productCode || `SP${String(p.productId || p.id).padStart(3, '0')}`,
                imageUrl: p.imageUrl || '',
                productName: p.productName || '',
                unit: p.unit || 'Hộp',
                quantity: Number(p.quantity) || 0,
                quantityFormatted: (Number(p.quantity) || 0).toLocaleString('vi-VN'),
                cost: Number(p.cost) || 0,
                costFormatted: (Number(p.cost) || 0).toLocaleString('vi-VN'),
                expiredDate: p.expiredDate || '31/12/2028',
                description: p.description || '',
                shelfLifeMonths: p.shelfLifeMonths || 0
            }));
            return vthhDatasets.products;
        } catch (err) {
            console.error('Lỗi khi tải danh sách sản phẩm từ API:', err);
            return vthhDatasets.products;
        } finally {
            state.vthh.isLoading = false;
        }
    }

    async function fetchIngredientsApi(keyword = '') {
        try {
            state.vthh.isLoading = true;
            const url = '/api/warehouse/ingredients' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            vthhDatasets.ingredients = data.map(i => ({
                id: i.id || i.ingrId,
                ingrId: i.ingrId || i.id,
                ingrCode: i.ingrCode || `NVL${String(i.ingrId || i.id).padStart(3, '0')}`,
                ingrName: i.ingrName || '',
                unit: i.unit || 'kg',
                quantity: Number(i.quantity) || 0,
                quantityFormatted: (Number(i.quantity) || 0).toLocaleString('vi-VN', { minimumFractionDigits: 0, maximumFractionDigits: 2 }),
                price: Number(i.price) || 0,
                priceFormatted: (Number(i.price) || 0).toLocaleString('vi-VN'),
                imageUrl: i.imageUrl || '',
                expiredDate: i.expiredDate || '31/12/2028',
                description: i.description || '',
                shelfLifeMonths: i.shelfLifeMonths || 0
            }));
            return vthhDatasets.ingredients;
        } catch (err) {
            console.error('Lỗi khi tải danh sách nguyên vật liệu từ API:', err);
            return vthhDatasets.ingredients;
        } finally {
            state.vthh.isLoading = false;
        }
    }

    async function fetchSuppliersApi() {
        try {
            const res = await fetch('/api/warehouse/suppliers');
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            supplierList = data.map(s => ({
                id: s.id || s.supplierId,
                supplierId: s.supplierId || s.id,
                code: s.code || `NCC${String(s.supplierId || s.id).padStart(4, '0')}`,
                name: s.name || s.supplierName,
                supplierName: s.name || s.supplierName,
                phone: s.phone || '',
                address: s.address || '',
                mail: s.mail || ''
            }));
            return supplierList;
        } catch (err) {
            console.error('Lỗi khi tải danh sách nhà cung cấp từ API:', err);
            return supplierList;
        }
    }

    async function fetchMaterialReceiptsApi(keyword = '') {
        try {
            const url = '/api/warehouse/material-receipts' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            outboundDatasets.outboundCommands = data;
            return data;
        } catch (err) {
            console.error('Lỗi khi tải danh sách lệnh xuất NVL:', err);
            return outboundDatasets.outboundCommands;
        }
    }

    async function fetchProductReceiptsApi(keyword = '') {
        try {
            const url = '/api/warehouse/product-receipts' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            inboundDatasets.inboundCommands = data;
            return data;
        } catch (err) {
            console.error('Lỗi khi tải danh sách lệnh nhập kho thành phẩm:', err);
            return inboundDatasets.inboundCommands;
        }
    }

    function filterByKeyword(list, kw, keys) {
        const cleanKw = (kw || '').trim().toLowerCase();
        if (!cleanKw) return list;
        return list.filter(item =>
            keys.some(k => item[k] && String(item[k]).toLowerCase().includes(cleanKw))
        );
    }

    function getCurrentVnDateTime() {
        const now = new Date();
        const d = String(now.getDate()).padStart(2, '0');
        const m = String(now.getMonth() + 1).padStart(2, '0');
        const y = now.getFullYear();
        const hh = String(now.getHours()).padStart(2, '0');
        const mm = String(now.getMinutes()).padStart(2, '0');
        const ss = String(now.getSeconds()).padStart(2, '0');
        return `${d}/${m}/${y} ${hh}:${mm}:${ss}`;
    }

    return {
        // Khởi tạo và nạp dữ liệu ban đầu từ Supabase
        async initData() {
            try {
                await fetchProductsApi();
            } catch (e) {
                console.warn('Lỗi tải sản phẩm:', e);
            }
            try {
                await fetchIngredientsApi();
            } catch (e) {
                console.warn('Lỗi tải NVL:', e);
            }
            try {
                await fetchSuppliersApi();
            } catch (e) {
                console.warn('Lỗi tải NCC:', e);
            }
            try {
                await fetchMaterialReceiptsApi();
            } catch (e) {
                console.warn('Lỗi tải Lệnh xuất NVL:', e);
            }
            try {
                await fetchProductReceiptsApi();
            } catch (e) {
                console.warn('Lỗi tải Lệnh nhập kho thành phẩm:', e);
            }
        },

        getState() {
            return state;
        },

        getSidebarMenu() {
            return sidebarMenu;
        },

        getWorkspaceData() {
            return workspaceData;
        },

        getSuppliersList() {
            if (supplierList.length > 0) return supplierList;
            return [
                { code: 'NCC0046', name: 'Công ty Cổ phần Dược phẩm Trung ương CPC1' },
                { code: 'NCC0047', name: 'Công ty Cổ phần Hóa Dược Việt Nam (VCP)' },
                { code: 'NCC0048', name: 'Công ty Cổ phần Dược Hậu Giang (DHG Pharma)' }
            ];
        },

        getIngredientsList() {
            return vthhDatasets.ingredients;
        },

        // API tải danh sách VTHH
        async loadProducts(keyword = '') {
            return fetchProductsApi(keyword);
        },

        async loadIngredients(keyword = '') {
            return fetchIngredientsApi(keyword);
        },

        async loadSuppliers() {
            return fetchSuppliersApi();
        },

        // =====================================================================
        // CRUD DANH MỤC VTHH (GỌI TRỰC TIẾP BACKEND API SUPABASE)
        // =====================================================================
        // THAO TÁC CRUD TRÊN VTHH (SẢN PHẨM & NGUYÊN VẬT LIỆU) QUA WEB API
        // =====================================================================
        async addVthhItem(itemType, newItem) {
            const isProd = itemType === 'product';
            if (isProd) {
                const payload = {
                    productName: (newItem.productName || '').trim(),
                    quantity: parseInt(newItem.quantity, 10) || 0,
                    unit: (newItem.unit || 'Hộp').trim(),
                    cost: parseFloat(newItem.cost) || 0,
                    imageUrl: (newItem.imageUrl || '').trim(),
                    description: (newItem.description || '').trim(),
                    shelfLifeMonths: 24
                };
                const res = await fetch('/api/warehouse/products', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                if (!res.ok) {
                    const err = await res.json().catch(() => ({ message: 'Thêm sản phẩm thất bại.' }));
                    throw new Error(err.message || 'Thêm sản phẩm thất bại.');
                }
                const saved = await res.json();
                const newProd = {
                    id: saved.id || saved.productId,
                    productId: saved.productId || saved.id,
                    productCode: saved.productCode || `SP${String(saved.productId || saved.id).padStart(3, '0')}`,
                    imageUrl: saved.imageUrl || payload.imageUrl || '',
                    productName: saved.productName || payload.productName,
                    unit: saved.unit || payload.unit,
                    quantity: Number(saved.quantity ?? payload.quantity) || 0,
                    quantityFormatted: (Number(saved.quantity ?? payload.quantity) || 0).toLocaleString('vi-VN'),
                    cost: Number(saved.cost ?? payload.cost) || 0,
                    costFormatted: (Number(saved.cost ?? payload.cost) || 0).toLocaleString('vi-VN'),
                    expiredDate: saved.expiredDate || '31/12/2028',
                    description: saved.description || payload.description || '',
                    shelfLifeMonths: saved.shelfLifeMonths || 24
                };
                vthhDatasets.products.unshift(newProd);
            } else {
                const payload = {
                    ingrName: (newItem.ingrName || '').trim(),
                    quantity: parseFloat(newItem.quantity) || 0,
                    unit: (newItem.unit || 'kg').trim(),
                    price: parseFloat(newItem.price) || 0,
                    imageUrl: (newItem.imageUrl || '').trim(),
                    description: (newItem.description || '').trim(),
                    shelfLifeMonths: 24
                };
                const res = await fetch('/api/warehouse/ingredients', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                if (!res.ok) {
                    const err = await res.json().catch(() => ({ message: 'Thêm nguyên vật liệu thất bại.' }));
                    throw new Error(err.message || 'Thêm nguyên vật liệu thất bại.');
                }
                const saved = await res.json();
                const newIngr = {
                    id: saved.id || saved.ingrId,
                    ingrId: saved.ingrId || saved.id,
                    ingrCode: saved.ingrCode || `NVL${String(saved.ingrId || saved.id).padStart(3, '0')}`,
                    ingrName: saved.ingrName || payload.ingrName,
                    unit: saved.unit || payload.unit,
                    quantity: Number(saved.quantity ?? payload.quantity) || 0,
                    quantityFormatted: (Number(saved.quantity ?? payload.quantity) || 0).toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
                    price: Number(saved.price ?? payload.price) || 0,
                    priceFormatted: (Number(saved.price ?? payload.price) || 0).toLocaleString('vi-VN'),
                    imageUrl: saved.imageUrl || '',
                    description: saved.description || payload.description || '',
                    expiredDate: saved.expiredDate || '31/12/2028',
                    shelfLifeMonths: saved.shelfLifeMonths || 24
                };
                vthhDatasets.ingredients.unshift(newIngr);
            }
        },

        async updateVthhItem(itemType, itemId, updatedData) {
            const isProd = itemType === 'product';
            const numId = parseInt(String(itemId).replace(/\D/g, ''), 10) || itemId;
            if (isProd) {
                const payload = {
                    productName: (updatedData.productName || '').trim(),
                    quantity: parseInt(updatedData.quantity, 10) || 0,
                    unit: (updatedData.unit || 'Hộp').trim(),
                    cost: parseFloat(updatedData.cost) || 0,
                    imageUrl: (updatedData.imageUrl || '').trim(),
                    description: (updatedData.description || '').trim()
                };
                const res = await fetch(`/api/warehouse/products/${numId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                if (!res.ok) {
                    const err = await res.json().catch(() => ({ message: 'Cập nhật sản phẩm thất bại.' }));
                    throw new Error(err.message || 'Cập nhật sản phẩm thất bại.');
                }
                // Đồng bộ ngay trong bộ nhớ để giao diện cập nhật tức thì
                const existing = vthhDatasets.products.find(p => p.id === numId || p.productId === numId || p.productCode === String(itemId));
                if (existing) {
                    if (payload.productName) existing.productName = payload.productName;
                    if (payload.unit) existing.unit = payload.unit;
                    if (payload.quantity !== undefined) {
                        existing.quantity = payload.quantity;
                        existing.quantityFormatted = payload.quantity.toLocaleString('vi-VN');
                    }
                    if (payload.cost !== undefined) {
                        existing.cost = payload.cost;
                        existing.costFormatted = payload.cost.toLocaleString('vi-VN');
                    }
                    if (payload.imageUrl !== undefined) existing.imageUrl = payload.imageUrl || '';
                    if (payload.description !== undefined) existing.description = payload.description;
                }
            } else {
                const payload = {
                    ingrName: (updatedData.ingrName || '').trim(),
                    quantity: parseFloat(updatedData.quantity) || 0,
                    unit: (updatedData.unit || 'kg').trim(),
                    price: parseFloat(updatedData.price) || 0,
                    imageUrl: (updatedData.imageUrl || '').trim(),
                    description: (updatedData.description || '').trim()
                };
                const res = await fetch(`/api/warehouse/ingredients/${numId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                if (!res.ok) {
                    const err = await res.json().catch(() => ({ message: 'Cập nhật nguyên vật liệu thất bại.' }));
                    throw new Error(err.message || 'Cập nhật nguyên vật liệu thất bại.');
                }
                // Đồng bộ ngay trong bộ nhớ để giao diện cập nhật tức thì
                const existing = vthhDatasets.ingredients.find(i => i.id === numId || i.ingrId === numId || i.ingrCode === String(itemId));
                if (existing) {
                    if (payload.ingrName) existing.ingrName = payload.ingrName;
                    if (payload.unit) existing.unit = payload.unit;
                    if (payload.quantity !== undefined) {
                        existing.quantity = payload.quantity;
                        existing.quantityFormatted = payload.quantity.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                    }
                    if (payload.price !== undefined) {
                        existing.price = payload.price;
                        existing.priceFormatted = payload.price.toLocaleString('vi-VN');
                    }
                    if (payload.imageUrl !== undefined) existing.imageUrl = payload.imageUrl || '';
                    if (payload.description !== undefined) existing.description = payload.description;
                }
            }
        },

        async deleteVthhItem(itemType, itemId) {
            const isProd = itemType === 'product';
            const numId = parseInt(String(itemId).replace(/\D/g, ''), 10) || itemId;
            const endpoint = isProd ? `/api/warehouse/products/${numId}` : `/api/warehouse/ingredients/${numId}`;

            const res = await fetch(endpoint, { method: 'DELETE' });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Không thể xóa bản ghi do ràng buộc dữ liệu.' }));
                throw new Error(err.message || 'Không thể xóa bản ghi do ràng buộc dữ liệu.');
            }

            if (isProd) {
                vthhDatasets.products = vthhDatasets.products.filter(p => p.id !== numId && p.productId !== numId && p.productCode !== String(itemId));
            } else {
                vthhDatasets.ingredients = vthhDatasets.ingredients.filter(i => i.id !== numId && i.ingrId !== numId && i.ingrCode !== String(itemId));
            }
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
                        const m = String(p.productCode || p.id).match(/\d+/);
                        return m ? parseInt(m[0], 10) : 0;
                    });
                    nextNum = Math.max(...nums) + 1;
                }
            } else {
                if (vthhDatasets.ingredients.length > 0) {
                    const nums = vthhDatasets.ingredients.map(i => {
                        const m = String(i.ingrCode || i.id).match(/\d+/);
                        return m ? parseInt(m[0], 10) : 0;
                    });
                    nextNum = Math.max(...nums) + 1;
                }
            }
            const nextCode = isProd
                ? `SP${String(nextNum).padStart(3, '0')}`
                : `NVL${String(nextNum).padStart(3, '0')}`;

            const initialItem = isProd ? {
                id: null,
                productCode: nextCode,
                productName: '',
                imageUrl: '',
                unit: 'Hộp',
                quantity: 100,
                cost: 50000,
                expiredDate: '31/12/2028',
                description: ''
            } : {
                id: null,
                ingrCode: nextCode,
                ingrName: '',
                imageUrl: '',
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
                item = vthhDatasets.products.find(p => String(p.id) === String(itemId) || p.productCode === String(itemId) || String(p.productId) === String(itemId));
            } else if (itemType === 'ingredient') {
                item = vthhDatasets.ingredients.find(i => String(i.id) === String(itemId) || i.ingrCode === String(itemId) || String(i.ingrId) === String(itemId));
            }
            if (!item) return null;
            return {
                isOpen: true,
                mode,
                itemType,
                item: { ...item }
            };
        },

        // Cấu hình DataGrid Danh mục VTHH (Sản phẩm & Nguyên vật liệu)
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
        },

        // =====================================================================
        // CÁC HÀM HỖ TRỢ TRANG KHÁC (Bàn làm việc, Tổng quan, Nhập kho, Xuất kho)
        // =====================================================================
        getVoucherData(voucherType) {
            switch (voucherType) {
                case 'purchase-order':
                    return ordersDetailData.purchaseOrder;
                case 'sales-order':
                    return ordersDetailData.salesOrder;
                case 'inbound-command': {
                    const activeDocNo = state.activeVoucherDocNo;
                    const cmdRow = inboundDatasets.inboundCommands.find(c =>
                        c.orderNo === activeDocNo || c.receiptCode === activeDocNo || String(c.id) === String(activeDocNo) || String(c.productReceiptId) === String(activeDocNo)
                    );
                    if (cmdRow) {
                        return {
                            docType: 'inbound-command',
                            receiptId: cmdRow.productReceiptId || cmdRow.id,
                            title: `Lệnh nhập kho thành phẩm ${cmdRow.orderNo}`,
                            commandNo: cmdRow.orderNo,
                            commandNoLabel: 'Số lệnh / Số phiếu',
                            dueDate: cmdRow.deliveryDate || cmdRow.dueDate || cmdRow.orderDate,
                            dueDateLabel: 'Hạn nhập kho',
                            statusText: cmdRow.orderStatus || 'Chờ nhập kho',
                            rawStatus: cmdRow.status,
                            employeeCode: cmdRow.employeeCode || 'NV0005',
                            employeeName: cmdRow.employeeName || cmdRow.supplier || 'Nguyễn Tiến Huy',
                            department: 'Phân xưởng Sản xuất GMP',
                            description: cmdRow.description || '',
                            reqQtyColLabel: 'SL Yêu cầu',
                            actualQtyColLabel: 'SL thực nhập',
                            items: (cmdRow.items && cmdRow.items.length > 0) ? cmdRow.items : []
                        };
                    }
                    return commandsDetailData.inboundCommand;
                }
                case 'outbound-command': {
                    const activeDocNo = state.activeVoucherDocNo;
                    const cmdRow = outboundDatasets.outboundCommands.find(c =>
                        c.orderNo === activeDocNo || c.expReceiptCode === activeDocNo || String(c.id) === String(activeDocNo) || String(c.expReceiptId) === String(activeDocNo)
                    );
                    if (cmdRow) {
                        return {
                            docType: 'outbound-command',
                            receiptId: cmdRow.expReceiptId || cmdRow.id,
                            title: `Lệnh xuất nguyên vật liệu ${cmdRow.orderNo}`,
                            commandNo: cmdRow.orderNo,
                            commandNoLabel: 'Số lệnh / Số phiếu',
                            dueDate: cmdRow.deliveryDue || cmdRow.dueDate || cmdRow.orderDate,
                            dueDateLabel: 'Hạn xuất kho',
                            statusText: cmdRow.orderStatus || 'Chờ xuất kho',
                            rawStatus: cmdRow.status,
                            employeeCode: cmdRow.employeeCode || 'NV0005',
                            employeeName: cmdRow.employeeName || 'Nguyễn Tiến Huy',
                            department: cmdRow.customer || cmdRow.department || 'Phân xưởng Viên nén GMP',
                            description: cmdRow.description || '',
                            reqQtyColLabel: 'SL Yêu cầu',
                            actualQtyColLabel: 'SL thực xuất',
                            items: (cmdRow.items && cmdRow.items.length > 0) ? cmdRow.items : [
                                { id: 1, itemCode: 'NVL001', itemName: 'Nguyên vật liệu xuất kho', unit: 'kg', reqQty: 50, actualQty: 50, unitCost: 100000, price: 100000, amount: 5000000, isFull: true, expiryDate: '31/12/2027', note: 'Đủ tồn kho' }
                            ]
                        };
                    }
                    return commandsDetailData.outboundCommand;
                }
                case 'outbound':
                    return vouchersData.outbound;
                case 'inbound': {
                    if (state.activeVoucherMode === 'create' && state.inboundNewDraft) {
                        return state.inboundNewDraft;
                    }
                    const activeDocNo = state.activeVoucherDocNo || 'PNK00005';
                    const receiptRow = inboundDatasets.inboundReceipts.find(r => r.voucherNo === activeDocNo || r.orderNo === activeDocNo);
                    if (receiptRow) {
                        vouchersData.inbound.mode = 'view';
                        vouchersData.inbound.voucherNo = receiptRow.voucherNo;
                        vouchersData.inbound.employeeCode = receiptRow.employeeCode || 'NV0005';
                        vouchersData.inbound.employeeName = receiptRow.creatorName || 'Nguyễn Tiến Huy';
                        vouchersData.inbound.supplierCode = receiptRow.supplierCode || 'NCC0006';
                        vouchersData.inbound.supplierName = receiptRow.supplierName || 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM';
                        vouchersData.inbound.voucherDate = receiptRow.voucherDate;
                        if (receiptRow.items && receiptRow.items.length > 0) {
                            vouchersData.inbound.items = receiptRow.items;
                        }
                    } else {
                        vouchersData.inbound.mode = 'view';
                    }
                    return vouchersData.inbound;
                }
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
                const row = doc.items.find(i => String(i.id) === String(rowId));
                if (row) {
                    row.isFull = Boolean(isChecked);
                    const qty = Number(row.reqQty) || Number(row.quantity) || 0;
                    row.actualQty = row.isFull ? qty : 0;
                    const totalActualQty = doc.items.reduce((s, x) => s + (Number(x.actualQty) || 0), 0);
                    return {
                        row,
                        actualQty: row.actualQty,
                        totalActualQty
                    };
                }
            }
            return null;
        },

        updateOutboundCommandStatus(orderNo, newStatus) {
            const cmd = outboundDatasets.outboundCommands.find(c => c.orderNo === orderNo);
            if (cmd) {
                cmd.orderStatus = newStatus;
                cmd.orderStatusType = newStatus === 'Đã xuất kho' ? 'success' : 'warning';
            }
        },

        updateOutboundReceiptStatus(voucherNo, newStatus) {
            const receipt = outboundDatasets.outboundReceipts.find(r => r.voucherNo === voucherNo || r.orderNo === voucherNo);
            if (receipt) {
                receipt.status = newStatus;
                receipt.statusType = newStatus === 'Đã xuất kho' ? 'success' : 'warning';
            }
        },

        updateInboundCommandStatus(orderNo, newStatus) {
            const cmd = inboundDatasets.inboundCommands.find(c => c.orderNo === orderNo);
            if (cmd) {
                cmd.status = newStatus;
                cmd.statusType = newStatus === 'Đã nhập kho' ? 'success' : 'warning';
            }
        },

        updateInboundReceiptStatus(voucherNo, newStatus) {
            const receipt = inboundDatasets.inboundReceipts.find(r => r.voucherNo === voucherNo || r.orderNo === voucherNo);
            if (receipt) {
                receipt.status = newStatus;
                receipt.statusType = newStatus === 'Đã nhập kho' ? 'success' : 'warning';
            }
        },

        openVoucherModal(voucherType, docNo, mode = 'view') {
            state.activeVoucherModal = voucherType;
            state.activeVoucherDocNo = docNo;
            state.activeVoucherMode = mode || 'view';

            if (voucherType === 'inbound' && mode === 'create') {
                const suppliers = this.getSuppliersList();
                const firstSupplier = suppliers[0] || { code: 'NCC0046', name: 'Công ty Cổ phần Dược phẩm Trung ương CPC1' };
                state.inboundNewDraft = {
                    voucherType: 'inbound',
                    mode: 'create',
                    voucherNo: `PNK${String(inboundDatasets.inboundReceipts.length + 6).padStart(5, '0')}`,
                    selectedSubType: 'Nhập kho mua hàng',
                    employeeCode: 'NV0005',
                    employeeName: 'Nguyễn Tiến Huy',
                    supplierCode: firstSupplier.code,
                    supplierName: firstSupplier.name,
                    voucherDate: getCurrentVnDateTime(),
                    quantityColumnLabel: 'SL nhập',
                    items: []
                };
                this.addInboundNewItemRow();
            }
        },

        closeVoucherModal() {
            state.activeVoucherModal = null;
            state.activeVoucherDocNo = null;
            state.activeVoucherMode = 'view';
            state.inboundNewDraft = null;
        },

        setInboundNewSupplier(supplierCode) {
            if (!state.inboundNewDraft) return;
            const suppliers = this.getSuppliersList();
            const sup = suppliers.find(s => s.code === supplierCode);
            if (sup) {
                state.inboundNewDraft.supplierCode = sup.code;
                state.inboundNewDraft.supplierName = sup.name;
            }
        },

        addInboundNewItemRow() {
            if (!state.inboundNewDraft) return;
            const ingrList = this.getIngredientsList();
            const firstIngr = ingrList[0] || { ingrCode: 'NVL041', ingrName: 'Hoạt chất Paracetamol', unit: 'kg', price: 185000, expiredDate: '31/12/2028' };
            const nextId = (state.inboundNewDraft.items.length > 0 ? Math.max(...state.inboundNewDraft.items.map(i => i.id)) : 0) + 1;
            const qty = 100;
            const price = firstIngr.price || 0;
            state.inboundNewDraft.items.push({
                id: nextId,
                itemCode: firstIngr.ingrCode,
                itemName: firstIngr.ingrName,
                unit: firstIngr.unit,
                quantity: qty,
                price: price,
                amount: qty * price,
                expiryDate: firstIngr.expiredDate || '31/12/2028'
            });
        },

        clearInboundNewItemRows() {
            if (!state.inboundNewDraft) return;
            state.inboundNewDraft.items = [];
        },

        deleteInboundNewItemRow(rowId) {
            if (!state.inboundNewDraft) return;
            const id = Number(rowId);
            state.inboundNewDraft.items = state.inboundNewDraft.items.filter(i => i.id !== id);
        },

        changeInboundNewItem(rowId, ingrCode) {
            if (!state.inboundNewDraft) return;
            const row = state.inboundNewDraft.items.find(i => i.id === Number(rowId));
            if (!row) return;
            const ingrList = this.getIngredientsList();
            const ingr = ingrList.find(i => i.ingrCode === ingrCode);
            if (ingr) {
                row.itemCode = ingr.ingrCode;
                row.itemName = ingr.ingrName;
                row.unit = ingr.unit;
                row.price = ingr.price;
                row.expiryDate = ingr.expiredDate;
                row.amount = (row.quantity || 0) * (row.price || 0);
            }
        },

        changeInboundNewItemQty(rowId, newQty) {
            if (!state.inboundNewDraft) return null;
            const row = state.inboundNewDraft.items.find(i => i.id === Number(rowId));
            if (!row) return null;
            const qty = parseFloat(newQty) || 0;
            row.quantity = qty;
            row.amount = qty * (row.price || 0);
            return row;
        },

        saveInboundReceipt(isDraft = false) {
            if (!state.inboundNewDraft) return { success: false, message: 'Không có dữ liệu phiếu!' };
            const draft = state.inboundNewDraft;
            if (!draft.items || draft.items.length === 0) {
                return { success: false, message: 'Vui lòng thêm ít nhất 1 dòng nguyên vật liệu!' };
            }
            const statusText = isDraft ? 'Lưu nháp' : 'Chờ nhập kho';
            const statusType = isDraft ? 'neutral' : 'warning';
            const newReceipt = {
                voucherNo: draft.voucherNo,
                orderNo: draft.voucherNo,
                voucherDate: draft.voucherDate,
                creatorName: draft.employeeName,
                employeeCode: draft.employeeCode,
                supplierName: draft.supplierName,
                supplierCode: draft.supplierCode,
                warehouse: 'KHO HƯNG YÊN- HÀNG TƯƠNG LAI',
                partnerName: draft.employeeName,
                deliverer: '',
                description: draft.supplierName,
                status: statusText,
                statusType: statusType,
                subType: draft.selectedSubType || 'Nhập kho mua hàng',
                items: JSON.parse(JSON.stringify(draft.items))
            };
            inboundDatasets.inboundReceipts.unshift(newReceipt);
            this.closeVoucherModal();
            return { success: true, voucherNo: newReceipt.voucherNo };
        },

        addVoucherItemRow(voucherType) {
            const vData = vouchersData[voucherType];
            if (vData && Array.isArray(vData.items)) {
                const nextId = (vData.items.length > 0 ? Math.max(...vData.items.map(i => i.id)) : 0) + 1;
                vData.items.push({
                    id: nextId,
                    itemCode: '',
                    itemName: '',
                    specCode: '',
                    unit: 'kg',
                    location: '',
                    quantity: 1,
                    batchNo: '',
                    expiryDate: ''
                });
            }
        },

        deleteVoucherItemRow(voucherType, rowId) {
            const vData = vouchersData[voucherType];
            if (vData && Array.isArray(vData.items)) {
                vData.items = vData.items.filter(i => i.id !== Number(rowId));
            }
        },

        clearAllVoucherItems(voucherType) {
            const vData = vouchersData[voucherType];
            if (vData && Array.isArray(vData.items)) {
                vData.items = [];
            }
        },

        setActiveMenu(menuId) {
            state.activeMenuId = menuId;
        },

        toggleSubmenu(menuId) {
            const item = sidebarMenu.find(m => m.id === menuId);
            if (item) {
                item.expanded = !item.expanded;
            }
        },

        toggleSidebarCollapse() {
            state.sidebarCollapsed = !state.sidebarCollapsed;
        },

        setSearchKeyword(kw) {
            state.searchKeyword = kw;
        },

        setParamPopoverOpen(isOpen) {
            state.isParamPopoverOpen = isOpen;
        },

        setPendingDaysParam(days) {
            state.pendingDaysParam = Number(days);
        },

        applyExpiringDaysParam() {
            state.expiringDaysTitle = state.pendingDaysParam;
            state.isParamPopoverOpen = false;
            state.lastUpdatedMap['card-expiring-soon'] = new Date().toTimeString().substring(0, 5);
        },

        toggleDataMode() {
            state.usePharmaFullData = !state.usePharmaFullData;
        },

        refreshCardTimestamp(cardId) {
            state.lastUpdatedMap[cardId] = new Date().toTimeString().substring(0, 5);
        },

        setOperationTab(pageType, tabId) {
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

        getInboundGridConfig() {
            if (state.inbound.activeTab === 'don-mua-hang') {
                state.inbound.activeTab = 'lenh-nhap-kho';
            }
            const isVoucherTab = state.inbound.activeTab === 'nhap-kho-tab';
            const isCommandTab = state.inbound.activeTab === 'lenh-nhap-kho';
            let sourceRows = [];

            if (isVoucherTab) {
                sourceRows = inboundDatasets.inboundReceipts;
            } else if (isCommandTab) {
                sourceRows = inboundDatasets.inboundCommands;
            } else {
                sourceRows = state.usePharmaFullData ? inboundDatasets.pharmaOrders : inboundDatasets.defaultOrders;
            }

            const filteredByStatus = (state.inbound.statusFilter === 'Tất cả' || !state.inbound.statusFilter)
                ? sourceRows
                : sourceRows.filter(r => (r.orderStatus === state.inbound.statusFilter || r.status === state.inbound.statusFilter));

            const filtered = filterByKeyword(
                filteredByStatus,
                state.inbound.searchKeyword,
                isVoucherTab
                    ? ['voucherNo', 'orderNo', 'creatorName', 'employeeCode', 'warehouse', 'supplierName', 'supplierCode', 'status']
                    : isCommandTab
                        ? ['orderNo', 'supplier', 'productName', 'description', 'status', 'orderStatus']
                        : ['orderNo', 'supplier', 'description', 'status']
            );

            let columns = [];
            if (isVoucherTab) {
                columns = [
                    { key: 'voucherDate', label: 'Ngày chứng từ', width: '150px' },
                    { key: 'voucherNo', label: 'Số chứng từ', isLink: true, width: '130px' },
                    { key: 'creatorName', label: 'Người lập phiếu', width: '160px' },
                    { key: 'supplierCode', label: 'Mã nhà cung cấp', width: '140px' },
                    { key: 'supplierName', label: 'Nhà cung cấp', width: '260px' },
                    { key: 'warehouse', label: 'Kho nhập', width: '220px' },
                    { key: 'status', label: 'Trạng thái', isBadge: true, width: '140px' }
                ];
            } else if (isCommandTab) {
                columns = [
                    { key: 'orderNo', label: 'Số lệnh', isLink: true },
                    { key: 'orderDate', label: 'Ngày lệnh' },
                    { key: 'supplier', label: 'Người lập' },
                    { key: 'productName', label: 'Tên hàng hóa' },
                    { key: 'deliveryDate', label: 'Hạn nhập kho' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'status', label: 'Tình trạng', isBadge: true },
                    { key: 'action', label: 'Thao tác', isActionInboundProduct: true }
                ];
            } else {
                columns = [
                    { key: 'orderDate', label: 'Ngày đơn hàng' },
                    { key: 'orderNo', label: 'Số đơn hàng', isLink: true },
                    { key: 'supplier', label: 'Nhà cung cấp' },
                    { key: 'deliveryDate', label: 'Hạn giao hàng' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'status', label: 'Tình trạng nhập kho', isBadge: true }
                ];
            }

            return {
                pageType: 'nhap-kho',
                voucherType: isVoucherTab ? 'inbound' : isCommandTab ? 'inbound-command' : 'purchase-order',
                canOpenVoucher: true,
                activeTab: state.inbound.activeTab,
                tabs: [
                    { id: 'lenh-nhap-kho', label: 'Nhập kho thành phẩm' },
                    { id: 'nhap-kho-tab', label: 'Nhập kho' }
                ],
                searchValue: state.inbound.searchKeyword,
                filters: isCommandTab ? [
                    { id: 'inbound-status-filter', prefix: 'Trạng thái:', value: state.inbound.statusFilter || 'Tất cả', options: ['Tất cả', 'Chờ nhập kho', 'Đã nhập kho', 'Từ chối'] }
                ] : [
                    { id: 'inbound-time-filter', value: state.inbound.timeFilter, options: ['Hôm nay', 'Tuần này', 'Tháng này', 'Quý này', 'Năm nay'] },
                    { id: 'inbound-status-filter', value: state.inbound.statusFilter, options: ['Tất cả', 'Chưa thực hiện', 'Đang thực hiện', 'Chờ nhập kho', 'Đã nhập kho', 'Hoàn thành'] }
                ],
                showAddButton: isVoucherTab,
                addButtonLabel: 'Lập phiếu nhập nvl',
                columns,
                rows: filtered,
                totalRecords: filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: false
            };
        },

        getOutboundGridConfig() {
            const isCommandTab = state.outbound.activeTab === 'lenh-xuat-kho';
            const isVoucherTab = state.outbound.activeTab === 'xuat-kho-tab';
            let sourceRows = [];

            if (isVoucherTab) {
                sourceRows = outboundDatasets.outboundReceipts;
            } else if (isCommandTab) {
                sourceRows = outboundDatasets.outboundCommands;
            } else {
                sourceRows = state.usePharmaFullData ? outboundDatasets.pharmaOrders : outboundDatasets.defaultOrders;
            }

            const filteredByStatus = state.outbound.orderStatusFilter === 'Tất cả'
                ? sourceRows
                : sourceRows.filter(r => (r.orderStatus || r.status) === state.outbound.orderStatusFilter);

            const filtered = filterByKeyword(
                filteredByStatus,
                state.outbound.searchKeyword,
                isVoucherTab
                    ? ['voucherNo', 'orderNo', 'partnerName', 'receiver', 'warehouse', 'description', 'status']
                    : isCommandTab
                        ? ['orderNo', 'customer', 'department', 'description', 'orderStatus']
                        : ['orderNo', 'customer', 'description', 'orderStatus', 'outboundStatus']
            );

            let columns = [];
            if (isVoucherTab) {
                columns = [
                    { key: 'voucherDate', label: 'Ngày chứng từ' },
                    { key: 'voucherNo', label: 'Số chứng từ', isLink: true },
                    { key: 'partnerName', label: 'Đối tượng' },
                    { key: 'warehouse', label: 'Kho xuất' },
                    { key: 'description', label: 'Lý do xuất' },
                    { key: 'status', label: 'Trạng thái', isBadge: true }
                ];
            } else if (isCommandTab) {
                columns = [
                    { key: 'orderDate', label: 'Ngày lệnh', width: '110px' },
                    { key: 'orderNo', label: 'Số lệnh', isLink: true, width: '125px' },
                    { key: 'customer', label: 'Khách hàng / Bộ phận', width: '220px' },
                    { key: 'deliveryDue', label: 'Hạn xuất kho', width: '110px' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'orderStatus', label: 'Trạng thái', isBadge: true, width: '130px' },
                    { key: 'action', label: 'Thao tác', isActionOutbound: true, width: '180px', align: 'center' }
                ];
            } else {
                columns = [
                    { key: 'orderDate', label: 'Ngày đơn hàng' },
                    { key: 'orderNo', label: 'Số đơn hàng', isLink: true },
                    { key: 'customer', label: 'Khách hàng' },
                    { key: 'deliveryDue', label: 'Hạn giao hàng' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'orderStatus', label: 'Tình trạng đơn hàng', isBadge: true },
                    { key: 'outboundStatus', label: 'Tình trạng xuất kho' }
                ];
            }

            return {
                pageType: 'xuat-kho',
                voucherType: isVoucherTab ? 'outbound' : isCommandTab ? 'outbound-command' : 'sales-order',
                canOpenVoucher: true,
                activeTab: state.outbound.activeTab,
                tabs: [
                    { id: 'lenh-xuat-kho', label: 'Xuất nguyên vật liệu' },
                    { id: 'xuat-kho-tab', label: 'Xuất kho' }
                ],
                searchValue: state.outbound.searchKeyword,
                filters: [
                    { id: 'outbound-status-filter', prefix: 'Trạng thái:', value: state.outbound.orderStatusFilter, options: ['Tất cả', 'Chờ xuất kho', 'Đã xuất kho', 'Từ chối'] }
                ],
                columns,
                rows: filtered,
                totalRecords: filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: false
            };
        },

        async fetchMaterialReceiptsApi(keyword = '') {
            return fetchMaterialReceiptsApi(keyword);
        },

        async approveMaterialReceipt(receiptId) {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/material-receipts/${numId}/approve`, {
                method: 'PUT'
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Duyệt xuất kho thất bại.' }));
                throw new Error(err.message || 'Duyệt xuất kho thất bại.');
            }
            const data = await res.json();
            const existing = outboundDatasets.outboundCommands.find(c => c.id === numId || c.expReceiptId === numId);
            if (existing) {
                existing.status = 'Approved';
                existing.orderStatus = 'Đã xuất kho';
            }
            return data;
        },

        async rejectMaterialReceipt(receiptId, reason = '') {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/material-receipts/${numId}/reject`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reason: reason || 'Thủ kho từ chối cấp phát vật tư.' })
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Từ chối cấp phát thất bại.' }));
                throw new Error(err.message || 'Từ chối cấp phát thất bại.');
            }
            const data = await res.json();
            const existing = outboundDatasets.outboundCommands.find(c => c.id === numId || c.expReceiptId === numId);
            if (existing) {
                existing.status = 'Rejected';
                existing.orderStatus = 'Từ chối';
            }
            return data;
        },

        async fetchProductReceiptsApi(keyword = '') {
            return fetchProductReceiptsApi(keyword);
        },

        async getProductReceiptDetailApi(receiptId) {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/product-receipts/${numId}`);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            return data;
        },

        async approveProductReceipt(receiptId) {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/product-receipts/${numId}/approve`, {
                method: 'PUT'
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Duyệt nhận thành phẩm thất bại.' }));
                throw new Error(err.message || 'Duyệt nhận thành phẩm thất bại.');
            }
            const data = await res.json();
            const existing = inboundDatasets.inboundCommands.find(c => c.id === numId || c.productReceiptId === numId);
            if (existing) {
                existing.status = 'Approved';
                existing.orderStatus = 'Đã nhập kho';
                existing.statusType = 'success';
            }
            return data;
        },

        async rejectProductReceipt(receiptId, reason = '') {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/product-receipts/${numId}/reject`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reason: reason || 'Thủ kho từ chối nhận thành phẩm.' })
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Từ chối nhận hàng thất bại.' }));
                throw new Error(err.message || 'Từ chối nhận hàng thất bại.');
            }
            const data = await res.json();
            const existing = inboundDatasets.inboundCommands.find(c => c.id === numId || c.productReceiptId === numId);
            if (existing) {
                existing.status = 'Rejected';
                existing.orderStatus = 'Từ chối';
                existing.statusType = 'danger';
            }
            return data;
        }
    };
})();

