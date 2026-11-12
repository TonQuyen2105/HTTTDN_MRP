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
            id: 'yeu-cau-san-xuat',
            title: 'Yêu cầu sản xuất',
            icon: 'production',
            section: 'middle'
        },
        {
            id: 'nha-cung-cap',
            title: 'Nhà cung cấp',
            icon: 'supplier',
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
        }
    ];

    // 2. Dữ liệu màn hình "Bàn làm việc" (Đã loại bỏ khỏi hệ thống)
    const workspaceData = {};

    // 3. Dữ liệu màn hình "Tổng quan"
    const defaultDatasets = {
        belowMinStock: [],
        aboveMaxStock: [],
        expiringSoonByDays: { 5: [], 15: [], 30: [], 60: [], 90: [] },
        expiredItems: []
    };

    const pharmaMrpExtended = {
        belowMinStock: [],
        aboveMaxStock: []
    };

    // 4. Dữ liệu màn hình "Nhập kho" (Nạp từ API)
    const inboundDatasets = {
        defaultOrders: [],
        pharmaOrders: [],
        inboundCommands: [],
        inboundReceipts: []
    };

    // 5. Dữ liệu màn hình "Xuất kho" (Nạp từ API)
    const outboundDatasets = {
        defaultOrders: [],
        pharmaOrders: [],
        outboundCommands: [],
        outboundReceipts: []
    };

    // 6. Dữ liệu Chi tiết Phiếu Nhập Kho & Phiếu Xuất Kho
    const vouchersData = {
        inbound: {
            voucherType: 'inbound',
            voucherNo: '',
            selectedSubType: 'Nhập kho mua hàng',
            employeeCode: '',
            employeeName: '',
            supplierCode: '',
            supplierName: '',
            voucherDate: '',
            quantityColumnLabel: 'SL nhập',
            items: []
        },
        outbound: {
            voucherType: 'outbound',
            voucherNo: '',
            selectedSubType: 'Xuất kho bán hàng',
            partnerCode: '',
            partnerName: '',
            warehouse: '',
            receiver: '',
            voucherDate: '',
            quantityColumnLabel: 'SL xuất',
            items: []
        }
    };

    const ordersDetailData = {
        purchaseOrder: {
            docType: 'purchase-order',
            title: 'Đơn mua hàng',
            orderNo: '',
            orderNoLabel: 'Số đơn hàng',
            orderDate: '',
            orderDateLabel: 'Ngày đơn hàng',
            statusLabel: 'Tình trạng nhập kho',
            statusValue: '',
            statusOptions: ['Chưa thực hiện', 'Đang thực hiện', 'Hoàn thành'],
            partnerCodeLabel: 'Mã nhà cung cấp',
            partnerCode: '',
            partnerNameLabel: 'Tên nhà cung cấp',
            partnerName: '',
            warehouseColLabel: 'Kho',
            reqQtyColLabel: 'SL yêu cầu',
            doneQtyColLabel: 'SL đã nhận',
            primaryActionLabel: 'Lập lệnh nhập kho',
            primaryActionTarget: 'inbound-command',
            items: []
        },
        salesOrder: {
            docType: 'sales-order',
            title: 'Đơn đặt hàng',
            orderNo: '',
            orderNoLabel: 'Số đơn đặt hàng',
            orderDate: '',
            orderDateLabel: 'Ngày đơn hàng',
            statusLabel: 'Tình trạng xuất kho',
            statusValue: '',
            statusOptions: ['Chưa thực hiện', 'Đang thực hiện', 'Hoàn thành'],
            partnerCodeLabel: 'Mã khách hàng',
            partnerCode: '',
            partnerNameLabel: 'Tên nhân viên yêu cầu',
            partnerName: '',
            warehouseColLabel: 'Kho xuất',
            reqQtyColLabel: 'SL đặt hàng',
            doneQtyColLabel: 'SL đã xuất',
            primaryActionLabel: 'Lập lệnh xuất kho',
            primaryActionTarget: 'outbound-command',
            items: []
        }
    };

    const commandsDetailData = {
        inboundCommand: {
            docType: 'inbound-command',
            title: 'Lệnh nhập kho',
            commandNo: '',
            statusText: '',
            employeeName: '',
            description: '',
            reqQtyColLabel: 'SL nhập',
            actualQtyColLabel: 'Thực nhập',
            items: []
        },
        outboundCommand: {
            docType: 'outbound-command',
            title: 'Lệnh xuất kho',
            commandNo: '',
            statusText: '',
            employeeName: '',
            description: '',
            reqQtyColLabel: 'SL Yêu cầu',
            actualQtyColLabel: 'SL thực xuất',
            items: []
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
        inboundLocalDrafts: (() => {
            try {
                const saved = localStorage.getItem('wh_inbound_drafts');
                return saved ? JSON.parse(saved) : [];
            } catch (e) {
                return [];
            }
        })(),
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
        inboundNewDraft: null,
        productionRequest: {
            activeTab: 'tat-ca', // 'tat-ca' | 'cho-xu-ly' | 'dang-san-xuat' | 'hoan-thanh'
            searchKeyword: '',
            statusFilter: 'Tất cả',
            assignmentFilter: 'Tất cả', // 'Tất cả' | 'Đã phân công' | 'Chưa phân công'
            selectedRows: {},
            isLoading: false
        },
        productionRequests: [],
        assignableUsers: [],
        productionRequestModal: null, // { mode: 'create'|'edit'|'assign'|'detail', item: {...} }
        supplier: {
            activeTab: 'tat-ca',
            searchKeyword: '',
            currentPage: 1,
            pageSize: 20,
            isLoading: false
        },
        supplierModal: null, // { mode: 'create'|'edit'|'detail', item: {...} }
        report: {
            periodType: 'month', // 'month' | 'quarter' | 'year'
            year: 2026,
            periodValue: 11,
            itemType: 'all', // 'all' | 'ingredient' | 'product'
            searchKeyword: '',
            isLoading: false,
            reportData: null,
            stockCardModal: null
        },
        isUserDropdownOpen: false,
        previousMenuId: 'tong-quan',
        currentUser: null
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
                materialCost: Number(p.materialCost) || 0,
                materialCostFormatted: (Number(p.materialCost) || 0).toLocaleString('vi-VN'),
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
                cost: Number(i.cost) || 0,
                costFormatted: (Number(i.cost) || 0).toLocaleString('vi-VN'),
                imageUrl: i.imageUrl || '',
                expiredDate: i.expiredDate || '31/12/2028',
                description: i.description || '',
                shelfLifeMonths: i.shelfLifeMonths || 0,
                hasImportReceipts: Boolean(i.hasImportReceipts)
            }));
            return vthhDatasets.ingredients;
        } catch (err) {
            console.error('Lỗi khi tải danh sách nguyên vật liệu từ API:', err);
            return vthhDatasets.ingredients;
        } finally {
            state.vthh.isLoading = false;
        }
    }

    async function fetchIngredientDetailApi(id) {
        try {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/warehouse/ingredients/${numId}`);
            if (!res.ok) return null;
            const data = await res.json();

            const formatted = {
                id: data.id || data.ingrId,
                ingrId: data.ingrId || data.id,
                ingrCode: data.ingrCode || `NVL${String(data.ingrId || data.id).padStart(3, '0')}`,
                ingrName: data.ingrName || '',
                unit: data.unit || 'kg',
                quantity: Number(data.quantity) || 0,
                quantityFormatted: (Number(data.quantity) || 0).toLocaleString('vi-VN', { minimumFractionDigits: 0, maximumFractionDigits: 2 }),
                price: Number(data.price) || 0,
                priceFormatted: (Number(data.price) || 0).toLocaleString('vi-VN'),
                cost: Number(data.cost) || 0,
                costFormatted: (Number(data.cost) || 0).toLocaleString('vi-VN'),
                imageUrl: data.imageUrl || '',
                description: data.description || '',
                shelfLifeMonths: data.shelfLifeMonths || 0,
                hasImportReceipts: data.hasImportReceipts !== undefined
                    ? Boolean(data.hasImportReceipts)
                    : (Array.isArray(data.importReceipts) && data.importReceipts.length > 0),
                importReceipts: (data.importReceipts || []).map(r => ({
                    ipReceiptId: r.ipReceiptId,
                    voucherNo: r.voucherNo,
                    orderNo: r.voucherNo,
                    importDate: r.importDate || '',
                    supplierId: r.supplierId,
                    supplierCode: r.supplierCode || '',
                    supplierName: r.supplierName || 'Chưa xác định',
                    quantity: Number(r.quantity) || 0,
                    quantityFormatted: (Number(r.quantity) || 0).toLocaleString('vi-VN'),
                    unit: r.unit || data.unit || 'kg',
                    cost: Number(r.cost) || 0,
                    costFormatted: (Number(r.cost) || 0).toLocaleString('vi-VN'),
                    amount: Number(r.amount) || ((Number(r.quantity) || 0) * (Number(r.cost) || 0)),
                    amountFormatted: (Number(r.amount) || ((Number(r.quantity) || 0) * (Number(r.cost) || 0))).toLocaleString('vi-VN'),
                    expiredDate: r.expiredDate || '31/12/2028',
                    isExpired: !!r.isExpired,
                    isDisposed: !!r.isDisposed,
                    status: r.status || 'Approve',
                    statusType: r.statusType || 'success'
                }))
            };

            const idx = vthhDatasets.ingredients.findIndex(i => String(i.id) === String(numId) || String(i.ingrId) === String(numId) || i.ingrCode === formatted.ingrCode);
            if (idx !== -1) {
                vthhDatasets.ingredients[idx] = { ...vthhDatasets.ingredients[idx], ...formatted };
            }

            if (state.vthhModal && (String(state.vthhModal.itemId) === String(id) || String(state.vthhModal.itemId) === String(numId) || state.vthhModal.itemId === formatted.ingrCode)) {
                state.vthhModal.item = { ...(state.vthhModal.item || {}), ...formatted };
            }

            return formatted;
        } catch (err) {
            console.error('Lỗi khi tải chi tiết nguyên vật liệu từ API:', err);
            return null;
        }
    }

    async function disposeExpiredBatchApi(ingrId, receiptId) {
        const numIngrId = parseInt(String(ingrId).replace(/\D/g, ''), 10) || ingrId;
        const numReceiptId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
        const res = await fetch(`/api/warehouse/ingredients/${numIngrId}/dispose-expired-batch/${numReceiptId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
        const data = await res.json();
        if (!res.ok) {
            throw new Error(data.message || 'Không thể hủy nguyên vật liệu hết hạn.');
        }
        return data;
    }

    async function disposeExpiredProductBatchApi(productId, receiptId) {
        const numProdId = parseInt(String(productId).replace(/\D/g, ''), 10) || productId;
        const numReceiptId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
        const res = await fetch(`/api/warehouse/products/${numProdId}/dispose-expired-batch/${numReceiptId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
        const data = await res.json();
        if (!res.ok) {
            throw new Error(data.message || 'Không thể hủy thành phẩm hết hạn.');
        }
        return data;
    }

    function isReturnRefundProductReceipt(r) {
        if (!r) return false;
        const code = String(r.voucherNo || r.orderNo || r.receiptCode || '').trim().toUpperCase();
        const source = String(r.source || '').trim().toLowerCase();
        const note = String(r.note || '').trim().toLowerCase();
        return code.startsWith('HOAN-')
            || source.includes('hoàn hàng')
            || source.includes('hoàn lại')
            || note.includes('hoàn lại số lượng đã xuất')
            || note.includes('hoàn lại kho');
    }

    async function fetchProductDetailApi(id) {
        try {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/warehouse/products/${numId}`);
            if (!res.ok) return null;
            const data = await res.json();

            let receipts = (data.importReceipts || data.productReceipts || [])
                .filter(r => {
                    if (isReturnRefundProductReceipt(r)) return false;
                    if (r && r.isDisposed) return true;
                    const st = String(r && (r.rawStatus || r.status) || '').trim().toLowerCase();
                    return st === 'approved' || st === 'approve' || st === 'đã nhập kho' || st === 'đã hủy';
                })
                .map(r => ({
                    productReceiptId: r.productReceiptId,
                    voucherNo: r.voucherNo,
                    orderNo: r.orderNo || r.voucherNo,
                    importDate: r.importDate || '',
                    creatorName: r.creatorName || 'Nguyễn Tiến Huy',
                    employeeCode: r.employeeCode || 'NV0005',
                    source: r.source || 'Phân xưởng sản xuất',
                    quantity: Number(r.quantity) || 0,
                    quantityFormatted: (Number(r.quantity) || 0).toLocaleString('vi-VN'),
                    unit: r.unit || data.unit || 'Hộp',
                    cost: Number(r.cost) || 0,
                    costFormatted: (Number(r.cost) || 0).toLocaleString('vi-VN'),
                    amount: Number(r.amount) || ((Number(r.quantity) || 0) * (Number(r.cost) || 0)),
                    amountFormatted: (Number(r.amount) || ((Number(r.quantity) || 0) * (Number(r.cost) || 0))).toLocaleString('vi-VN'),
                    expiredDate: r.expiredDate || '31/12/2028',
                    isExpired: Boolean(r.isExpired),
                    isDisposed: Boolean(r.isDisposed),
                    status: r.status || (r.isDisposed ? 'Đã hủy' : 'Approved'),
                    statusType: r.statusType || (r.isDisposed ? 'danger' : 'success'),
                    note: r.note || ''
                }));

                        // Giá vốn của sản phẩm: lấy theo materialCost hoặc giá cao nhất từ các phiếu nhập thành phẩm
            const maxReceiptCost = receipts.length > 0 ? Math.max(...receipts.map(r => Number(r.cost) || 0)) : 0;
            const finalMaterialCost = (Number(data.materialCost) > 0) ? Number(data.materialCost) : (maxReceiptCost > 0 ? maxReceiptCost : 0);
            const finalCost = Number(data.cost) || 0;

            const formatted = {
                id: data.id || data.productId,
                productId: data.productId || data.id,
                productCode: data.productCode || `SP${String(data.productId || data.id).padStart(3, '0')}`,
                productName: data.productName || '',
                unit: data.unit || 'Hộp',
                quantity: Number(data.quantity) || 0,
                quantityFormatted: (Number(data.quantity) || 0).toLocaleString('vi-VN'),
                cost: finalCost,
                costFormatted: finalCost.toLocaleString('vi-VN'),
                materialCost: finalMaterialCost,
                materialCostFormatted: finalMaterialCost.toLocaleString('vi-VN'),
                imageUrl: data.imageUrl || '',
                description: data.description || '',
                shelfLifeMonths: data.shelfLifeMonths || 0,
                expiredDate: data.expiredDate || '31/12/2028',
                importReceipts: receipts,
                productReceipts: receipts
            };

            const idx = vthhDatasets.products.findIndex(p => String(p.id) === String(numId) || String(p.productId) === String(numId) || p.productCode === formatted.productCode);
            if (idx !== -1) {
                vthhDatasets.products[idx] = { ...vthhDatasets.products[idx], ...formatted };
            }

            if (state.vthhModal && (String(state.vthhModal.itemId) === String(id) || String(state.vthhModal.itemId) === String(numId) || state.vthhModal.itemId === formatted.productCode)) {
                state.vthhModal.item = { ...(state.vthhModal.item || {}), ...formatted };
            }

            return formatted;
        } catch (err) {
            console.error('Lỗi khi tải chi tiết sản phẩm từ API:', err);
            return null;
        }
    }

    async function fetchSuppliersApi() {
        try {
            const res = await fetch('/api/warehouse/suppliers');
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            supplierList = data.map(s => {
                const cost = (s.importCost !== undefined && s.importCost !== null) ? Number(s.importCost) : 0;
                return {
                    id: s.id || s.supplierId,
                    supplierId: s.supplierId || s.id,
                    code: s.code || `NCC${String(s.supplierId || s.id).padStart(4, '0')}`,
                    name: s.name || s.supplierName,
                    supplierName: s.name || s.supplierName,
                    phone: s.phone || '',
                    address: s.address || '',
                    mail: s.mail || '',
                    importCost: cost,
                    importCostFormatted: `${cost}%`
                };
            });
            state.suppliers = supplierList;
            return supplierList;
        } catch (err) {
            console.error('Lỗi khi tải danh sách nhà cung cấp từ API:', err);
            return supplierList;
        }
    }

    async function createSupplierApi(dto) {
        const res = await fetch('/api/warehouse/suppliers', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });
        if (!res.ok) {
            const errData = await res.json().catch(() => ({}));
            throw new Error(errData.message || `Lỗi máy chủ (${res.status})`);
        }
        const created = await res.json();
        await fetchSuppliersApi();
        return created;
    }

    async function updateSupplierApi(id, dto) {
        const res = await fetch(`/api/warehouse/suppliers/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });
        if (!res.ok) {
            const errData = await res.json().catch(() => ({}));
            throw new Error(errData.message || `Lỗi máy chủ (${res.status})`);
        }
        const updated = await res.json();
        await fetchSuppliersApi();
        return updated;
    }

    async function deleteSupplierApi(id) {
        const res = await fetch(`/api/warehouse/suppliers/${id}`, {
            method: 'DELETE'
        });
        if (!res.ok) {
            const errData = await res.json().catch(() => ({}));
            throw new Error(errData.message || `Lỗi máy chủ (${res.status})`);
        }
        const result = await res.json();
        await fetchSuppliersApi();
        return result;
    }

    async function fetchReportDataApi(customParams = {}) {
        try {
            state.report.isLoading = true;
            const periodType = customParams.periodType || state.report.periodType || 'month';
            const year = customParams.year !== undefined ? customParams.year : (state.report.year || 2026);
            const periodValue = customParams.periodValue !== undefined ? customParams.periodValue : (state.report.periodValue || 11);
            const itemType = customParams.itemType || state.report.itemType || 'all';
            const search = customParams.search !== undefined ? customParams.search : (state.report.searchKeyword || '');

            const url = `/api/warehouse/reports/period-stats?periodType=${encodeURIComponent(periodType)}&year=${encodeURIComponent(year)}&periodValue=${encodeURIComponent(periodValue)}&itemType=${encodeURIComponent(itemType)}&search=${encodeURIComponent(search)}`;
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            state.report.reportData = data;
            return data;
        } catch (err) {
            console.error('Lỗi khi tải dữ liệu báo cáo:', err);
            return null;
        } finally {
            state.report.isLoading = false;
        }
    }

    async function fetchStockCardApi(itemId, itemType) {
        try {
            const res = await fetch(`/api/warehouse/reports/stock-card?itemId=${encodeURIComponent(itemId)}&itemType=${encodeURIComponent(itemType)}`);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            state.report.stockCardModal = data;
            return data;
        } catch (err) {
            console.error('Lỗi khi tải thẻ kho:', err);
            return null;
        }
    }

    async function fetchMaterialReceiptsApi(keyword = '') {
        try {
            const url = '/api/warehouse/material-receipts' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            outboundDatasets.outboundCommands = Array.isArray(data)
                ? data.filter(r => !String(r.orderNo || r.expReceiptCode || '').trim().toUpperCase().startsWith('HMY-'))
                : data;
            return outboundDatasets.outboundCommands;
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
            if (Array.isArray(data)) {
                // Điều kiện: phải có requestId, status = ['Approve', 'Pending', 'Rejected'] và receiptcode khác NULL
                inboundDatasets.inboundCommands = data.filter(r => {
                    const hasRequestId = r.requestId !== null && r.requestId !== undefined && Number(r.requestId) > 0;
                    const st = (r.status || r.rawStatus || r.orderStatus || '').toLowerCase();
                    const hasValidCode = r.receiptCode && String(r.receiptCode).trim() !== '';
                    const isAllowedStatus = st === 'pending' || st === 'chờ nhập kho' ||
                                            st === 'approve' || st === 'approved' || st === 'đã nhập kho' ||
                                            st === 'reject' || st === 'rejected' || st === 'từ chối';
                    return hasRequestId && hasValidCode && isAllowedStatus;
                });
            }
            return inboundDatasets.inboundCommands;
        } catch (err) {
            console.error('Lỗi khi tải danh sách lệnh nhập kho thành phẩm:', err);
            return inboundDatasets.inboundCommands;
        }
    }

    async function fetchImportReceiptsApi(keyword = '') {
        try {
            const url = '/api/warehouse/import-receipts' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            inboundDatasets.inboundReceipts = data;
            return data;
        } catch (err) {
            console.error('Lỗi khi tải danh sách phiếu nhập kho nguyên vật liệu:', err);
            return inboundDatasets.inboundReceipts;
        }
    }

    async function fetchProductExportsApi(keyword = '') {
        try {
            const url = '/api/warehouse/product-exports' + (keyword ? `?keyword=${encodeURIComponent(keyword)}` : '');
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const data = await res.json();
            if (Array.isArray(data)) {
                outboundDatasets.outboundReceipts = data.filter(r => {
                    const code = String(r.voucherNo || r.orderNo || r.expReceiptCode || '').trim().toUpperCase();
                    return !code.startsWith('HSP-');
                });
            }
            return outboundDatasets.outboundReceipts;
        } catch (err) {
            console.error('Lỗi khi tải danh sách phiếu xuất kho thành phẩm:', err);
            return outboundDatasets.outboundReceipts;
        }
    }

    async function fetchProductExportDetailApi(idOrCode) {
        try {
            const numId = parseInt(String(idOrCode).replace(/\D/g, ''), 10) || idOrCode;
            const res = await fetch(`/api/warehouse/product-exports/${numId}`);
            if (!res.ok) throw new Error(`HTTP error ${res.status}`);
            const detail = await res.json();
            const idx = outboundDatasets.outboundReceipts.findIndex(r =>
                r.id === detail.id || r.exportReceiptId === detail.exportReceiptId || r.voucherNo === detail.voucherNo
            );
            if (idx >= 0) {
                outboundDatasets.outboundReceipts[idx] = { ...outboundDatasets.outboundReceipts[idx], ...detail };
            } else {
                outboundDatasets.outboundReceipts.unshift(detail);
            }
            return detail;
        } catch (err) {
            console.error('Lỗi khi tải chi tiết phiếu xuất kho thành phẩm:', err);
            return null;
        }
    }

    function filterByKeyword(list, kw, keys) {
        const cleanKw = (kw || '').trim().toLowerCase();
        if (!cleanKw) return list;
        return list.filter(item =>
            keys.some(k => item[k] && String(item[k]).toLowerCase().includes(cleanKw))
        );
    }

    function getCurrentUserEmployeeInfo() {
        const u = state.currentUser;
        if (!u || !u.userId) {
            return { userId: null, employeeCode: '', employeeName: '' };
        }
        return {
            userId: u.userId,
            employeeCode: u.employeeCode || `NV${String(u.userId).padStart(4, '0')}`,
            employeeName: u.fullName || u.username || ''
        };
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

    // Danh mục các công việc của thủ kho cần thực hiện trong hệ thống MRP
    const warehouseKeeperTasks = [
        {
            id: 1,
            title: 'Tiếp nhận & Kiểm tra nhập kho nguyên vật liệu từ NCC',
            description: 'Tiếp nhận hàng từ NCC, đối chiếu số lượng thực tế, kiểm tra chứng từ COA, quy cách bao bì, kiểm tra số lô và hạn dùng; lập và duyệt Phiếu nhập kho (PNK).',
            documents: ['Phiếu nhập kho (PNK)', 'Biên bản giao hàng NCC', 'Phiếu kiểm nghiệm COA'],
            frequency: 'Hàng ngày / Khi phát sinh lô hàng mới',
            priority: 'Rất cao',
            priorityClass: 'danger',
            module: 'Nhập kho',
            menuId: 'nhap-kho'
        },
        {
            id: 2,
            title: 'Cấp phát nguyên vật liệu phục vụ Lệnh sản xuất',
            description: 'Căn cứ Lệnh sản xuất (LSX) đã được phê duyệt, kiểm tra định mức BOM, xuất đúng số lượng NVL theo nguyên tắc FEFO (hết hạn trước - xuất trước) và lập Phiếu xuất kho NVL.',
            documents: ['Phiếu xuất kho NVL (PXK)', 'Lệnh sản xuất (LSX)', 'Định mức nguyên vật liệu (BOM)'],
            frequency: 'Theo kế hoạch từng lô sản xuất',
            priority: 'Rất cao',
            priorityClass: 'danger',
            module: 'Yêu cầu sản xuất',
            menuId: 'yeu-cau-san-xuat'
        },
        {
            id: 3,
            title: 'Nhập kho lưu trữ thành phẩm sau sản xuất',
            description: 'Tiếp nhận thành phẩm đã hoàn thành kiểm nghiệm chất lượng từ phân xưởng sản xuất, kiểm tra quy cách đóng gói, xếp vị trí kho thành phẩm và lập Phiếu nhập thành phẩm.',
            documents: ['Phiếu nhập thành phẩm (PNK)', 'Lệnh sản xuất liên quan (LSX)', 'Biên bản bàn giao xưởng'],
            frequency: 'Hàng ngày / Khi hoàn thành lô sản xuất',
            priority: 'Cao',
            priorityClass: 'warning',
            module: 'Nhập kho',
            menuId: 'nhap-kho'
        },
        {
            id: 4,
            title: 'Xuất kho thành phẩm giao hàng / bán hàng',
            description: 'Kiểm tra đơn hàng xuất bán, bốc dỡ và đóng gói thành phẩm theo số lô quy định, bàn giao cho đơn vị vận chuyển / khách hàng và ký biên bản bàn giao hàng hóa.',
            documents: ['Phiếu xuất kho bán hàng (PXK)', 'Đơn đặt hàng bán', 'Biên bản giao nhận hàng hóa'],
            frequency: 'Hàng ngày / Khi có đơn bán hàng',
            priority: 'Cao',
            priorityClass: 'warning',
            module: 'Xuất kho',
            menuId: 'xuat-kho'
        },
        {
            id: 5,
            title: 'Giám sát định mức tồn kho & Cảnh báo an toàn (Min / Max)',
            description: 'Theo dõi hàng tồn kho tức thời, kiểm tra các mặt hàng chạm ngưỡng tồn tối thiểu (dưới Min) để kịp thời đề xuất mua sắm; kiểm tra hàng tồn vượt mức tối đa (vượt Max) nhằm tối ưu vốn lưu kho.',
            documents: ['Bảng cảnh báo tồn an toàn Min / Max', 'Thẻ định mức tồn kho an toàn'],
            frequency: 'Hàng ngày đầu ca làm việc',
            priority: 'Trung bình',
            priorityClass: 'info',
            module: 'Tổng quan',
            menuId: 'tong-quan'
        },
        {
            id: 6,
            title: 'Quản lý thông tin & Theo dõi đối tác Nhà cung cấp',
            description: 'Tra cứu, cập nhật thông tin liên hệ, địa chỉ, mã số thuế và đánh giá uy tín cung ứng của các đơn vị cung cấp nguyên vật liệu dược phẩm.',
            documents: ['Hồ sơ Nhà cung cấp', 'Hợp đồng mua hàng'],
            frequency: 'Định kỳ / Khi tiếp nhận NCC mới',
            priority: 'Tiêu chuẩn',
            priorityClass: 'neutral',
            module: 'Nhà cung cấp',
            menuId: 'nha-cung-cap'
        },
        {
            id: 7,
            title: 'Lập báo cáo tổng hợp Nhập - Xuất - Tồn và kiểm kê Thẻ kho',
            description: 'Tổng hợp số liệu tồn đầu kỳ, tổng phát sinh nhập, phát sinh xuất và tồn cuối kỳ theo tháng, quý, năm; kiểm tra lịch sử biến động Thẻ kho phục vụ kiểm kê thực tế.',
            documents: ['Báo cáo tổng hợp Nhập - Xuất - Tồn', 'Thẻ kho (Stock Card)', 'Biên bản kiểm kê kho định kỳ'],
            frequency: 'Hàng tháng / Định kỳ quý & năm',
            priority: 'Rất cao',
            priorityClass: 'danger',
            module: 'Báo cáo',
            menuId: 'bao-cao'
        }
    ];

    return {
        getWarehouseKeeperTasks() {
            return warehouseKeeperTasks;
        },

        // Khởi tạo và nạp dữ liệu ban đầu từ Supabase
        async initData() {
            await Promise.allSettled([
                this.fetchCurrentUserApi().catch(e => console.warn('Lỗi tải tài khoản hiện tại:', e)),
                fetchProductsApi().catch(e => console.warn('Lỗi tải sản phẩm:', e)),
                fetchIngredientsApi().catch(e => console.warn('Lỗi tải NVL:', e)),
                fetchSuppliersApi().catch(e => console.warn('Lỗi tải NCC:', e)),
                fetchMaterialReceiptsApi().catch(e => console.warn('Lỗi tải Lệnh xuất NVL:', e)),
                fetchProductReceiptsApi().catch(e => console.warn('Lỗi tải Lệnh nhập kho thành phẩm:', e)),
                fetchImportReceiptsApi().catch(e => console.warn('Lỗi tải Phiếu nhập kho NVL:', e)),
                fetchProductExportsApi().catch(e => console.warn('Lỗi tải Phiếu xuất kho thành phẩm:', e)),
                this.fetchProductionRequestsApi().catch(e => console.warn('Lỗi tải Lệnh sản xuất:', e)),
                this.fetchAssignableUsersApi().catch(e => console.warn('Lỗi tải Danh sách phân công:', e))
            ]);
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

        async fetchIngredientsApi(keyword = '') {
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
                    quantity: 0,
                    unit: (newItem.unit || '').trim(),
                    cost: 0, // Giá bán được khóa mặc định 0 khi thêm mới
                    materialCost: 0, // Giá vốn được khóa khi thêm mới, cập nhật từ phiếu nhập kho thành phẩm
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
                    unit: saved.unit || payload.unit || '',
                    quantity: 0,
                    quantityFormatted: '0',
                    cost: Number(saved.cost ?? payload.cost) || 0,
                    costFormatted: (Number(saved.cost ?? payload.cost) || 0).toLocaleString('vi-VN'),
                    materialCost: Number(saved.materialCost ?? payload.materialCost) || 0,
                    materialCostFormatted: (Number(saved.materialCost ?? payload.materialCost) || 0).toLocaleString('vi-VN'),
                    expiredDate: '',
                    description: saved.description || payload.description || '',
                    shelfLifeMonths: saved.shelfLifeMonths || 24
                };
                vthhDatasets.products.unshift(newProd);
            } else {
                const cleanIngrName = (newItem.ingrName || '').trim();
                const cleanUnit = (newItem.unit || '').trim();
                const cleanPrice = typeof newItem.price === 'string' ? (parseFloat(newItem.price.replace(/[^\d.]/g, '')) || 0) : (Number(newItem.price) || 0);
                if (!cleanIngrName) {
                    throw new Error('Tên nguyên vật liệu không được để trống.');
                }
                if (!cleanUnit) {
                    throw new Error('Đơn vị tính (ĐVT) không được để trống.');
                }
                if (cleanUnit.length > 20) {
                    throw new Error('Đơn vị tính (ĐVT) tối đa 20 ký tự.');
                }
                if (cleanPrice <= 0) {
                    throw new Error('Đơn giá nguyên vật liệu không được bằng 0. Vui lòng nhập đơn giá hợp lệ lớn hơn 0.');
                }
                const cleanCost = typeof newItem.cost === 'string' ? (parseFloat(newItem.cost.replace(/[^\d.]/g, '')) || 0) : (Number(newItem.cost) || 0);

                const payload = {
                    ingrName: cleanIngrName,
                    quantity: 0,
                    unit: cleanUnit,
                    price: cleanPrice,
                    cost: cleanCost,
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
                    cost: Number(saved.cost ?? payload.cost) || 0,
                    costFormatted: (Number(saved.cost ?? payload.cost) || 0).toLocaleString('vi-VN'),
                    imageUrl: saved.imageUrl || '',
                    description: saved.description || payload.description || '',
                    expiredDate: saved.expiredDate || '31/12/2028',
                    shelfLifeMonths: saved.shelfLifeMonths || 24,
                    hasImportReceipts: Boolean(saved.hasImportReceipts)
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
                const saved = await res.json().catch(() => ({}));
                // Đồng bộ ngay trong bộ nhớ để giao diện cập nhật tức thì
                const existing = vthhDatasets.products.find(p => p.id === numId || p.productId === numId || p.productCode === String(itemId));
                if (existing) {
                    if (payload.productName) existing.productName = payload.productName;
                    if (updatedData.unit !== undefined) existing.unit = payload.unit;
                    if (payload.imageUrl !== undefined) existing.imageUrl = payload.imageUrl || '';
                    if (payload.description !== undefined) existing.description = payload.description;
                    if (saved && saved.cost !== undefined) {
                        existing.cost = Number(saved.cost) || 0;
                        existing.costFormatted = (Number(saved.cost) || 0).toLocaleString('vi-VN');
                    }
                    if (saved && saved.materialCost !== undefined) {
                        existing.materialCost = Number(saved.materialCost) || 0;
                        existing.materialCostFormatted = (Number(saved.materialCost) || 0).toLocaleString('vi-VN');
                    }
                }
            } else {
                const cleanIngrName = (updatedData.ingrName || '').trim();
                const cleanPrice = typeof updatedData.price === 'string' ? (parseFloat(updatedData.price.replace(/[^\d.]/g, '')) || 0) : (Number(updatedData.price) || 0);
                const cleanCost = typeof updatedData.cost === 'string' ? (parseFloat(updatedData.cost.replace(/[^\d.]/g, '')) || 0) : (Number(updatedData.cost) || 0);
                if (!cleanIngrName) {
                    throw new Error('Tên nguyên vật liệu không được để trống.');
                }
                if (cleanPrice <= 0) {
                    throw new Error('Đơn giá nguyên vật liệu không được bằng 0. Vui lòng nhập đơn giá hợp lệ lớn hơn 0.');
                }

                const existing = vthhDatasets.ingredients.find(i => i.id === numId || i.ingrId === numId || i.ingrCode === String(itemId));
                const payload = {
                    ingrName: cleanIngrName,
                    quantity: parseFloat(updatedData.quantity) || 0,
                    unit: updatedData.unit !== undefined ? (updatedData.unit || '').trim() : (existing?.unit || ''),
                    price: cleanPrice,
                    cost: cleanCost,
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
                const saved = await res.json().catch(() => ({}));
                // Đồng bộ ngay trong bộ nhớ để giao diện cập nhật tức thì
                if (existing) {
                    if (payload.ingrName) existing.ingrName = payload.ingrName;
                    if (saved.unit !== undefined && saved.unit !== '') {
                        existing.unit = saved.unit;
                    } else if (updatedData.unit !== undefined && payload.unit) {
                        existing.unit = payload.unit;
                    }
                    if (saved.hasImportReceipts !== undefined) {
                        existing.hasImportReceipts = Boolean(saved.hasImportReceipts);
                    }
                    if (payload.quantity !== undefined) {
                        existing.quantity = payload.quantity;
                        existing.quantityFormatted = payload.quantity.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                    }
                    if (payload.price !== undefined) {
                        existing.price = payload.price;
                        existing.priceFormatted = payload.price.toLocaleString('vi-VN');
                    }
                    if (payload.cost !== undefined) {
                        existing.cost = payload.cost;
                        existing.costFormatted = payload.cost.toLocaleString('vi-VN');
                    }
                    if (payload.imageUrl !== undefined) existing.imageUrl = payload.imageUrl || '';
                    if (payload.description !== undefined) existing.description = payload.description;
                    if (updatedData.expiredDate !== undefined) existing.expiredDate = (updatedData.expiredDate || '').trim();
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
            let item = null;
            if (itemType === 'product') {
                item = vthhDatasets.products.find(p => String(p.id) === String(itemId) || p.productCode === String(itemId) || String(p.productId) === String(itemId));
            } else if (itemType === 'ingredient') {
                item = vthhDatasets.ingredients.find(i => String(i.id) === String(itemId) || i.ingrCode === String(itemId) || String(i.ingrId) === String(itemId));
            }
            state.vthhModal = {
                itemType,
                itemId,
                mode: mode || 'view',
                item: item ? { ...item } : null
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
                unit: '',
                quantity: 0,
                cost: 0,
                materialCost: 0,
                description: ''
            } : {
                id: null,
                ingrCode: nextCode,
                ingrName: '',
                imageUrl: '',
                unit: '',
                quantity: 0,
                price: 0,
                cost: 0,
                expiredDate: '',
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
            let item = (state.vthhModal && state.vthhModal.item) ? state.vthhModal.item : null;
            if (!item) {
                if (itemType === 'product') {
                    item = vthhDatasets.products.find(p => String(p.id) === String(itemId) || p.productCode === String(itemId) || String(p.productId) === String(itemId));
                } else if (itemType === 'ingredient') {
                    item = vthhDatasets.ingredients.find(i => String(i.id) === String(itemId) || i.ingrCode === String(itemId) || String(i.ingrId) === String(itemId));
                }
            }
            if (!item) return null;

            // Xử lý danh sách chi tiết theo từng phiếu nhập cho nguyên vật liệu
            let importReceipts = [];
            if (itemType === 'ingredient') {
                if (item.importReceipts && item.importReceipts.length > 0) {
                    importReceipts = [...item.importReceipts];
                }

                // Gộp các phiếu nhập vừa tạo dạng local draft hoặc mới nhập trong phiên làm việc
                const itemCode = item.ingrCode || `NVL${String(item.id || item.ingrId).padStart(3, '0')}`;
                (state.inboundLocalDrafts || []).forEach(draft => {
                    const matchItem = (draft.items || []).find(it => it.itemCode === itemCode || String(it.ingrId) === String(item.id || item.ingrId));
                    if (matchItem && !importReceipts.some(r => r.voucherNo === draft.voucherNo)) {
                        importReceipts.unshift({
                            ipReceiptId: draft.id || draft.voucherNo,
                            voucherNo: draft.voucherNo,
                            orderNo: draft.voucherNo,
                            importDate: draft.voucherDate || 'Hôm nay',
                            supplierCode: draft.supplierCode || '',
                            supplierName: draft.supplierName || 'Chưa xác định',
                            quantity: Number(matchItem.quantity) || 1,
                            quantityFormatted: (Number(matchItem.quantity) || 1).toLocaleString('vi-VN'),
                            unit: matchItem.unit || item.unit || 'kg',
                            cost: Number(matchItem.price) || item.price || 0,
                            costFormatted: (Number(matchItem.price) || item.price || 0).toLocaleString('vi-VN'),
                            amount: (Number(matchItem.quantity) || 1) * (Number(matchItem.price) || item.price || 0),
                            amountFormatted: ((Number(matchItem.quantity) || 1) * (Number(matchItem.price) || item.price || 0)).toLocaleString('vi-VN'),
                            expiredDate: matchItem.expiryDate || '31/12/2028',
                            status: draft.status || 'Lưu nháp',
                            statusType: 'info',
                            isDraft: true
                        });
                    }
                });
            }

            const dbReceipts = importReceipts.filter(r => !r.isDraft);
            return {
                isOpen: true,
                mode,
                itemType,
                item: {
                    ...item,
                    hasImportReceipts: Boolean(item.hasImportReceipts) || dbReceipts.length > 0,
                    importReceipts
                }
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
                : filterByKeyword(sourceRows, currentKw, ['ingrCode', 'ingrName', 'unit']);

            let columns = [];
            if (isProductTab) {
                columns = [
                    { key: 'productCode', label: 'Mã sản phẩm', isLink: true, width: '120px' },
                    { key: 'imageUrl', label: 'Hình ảnh', isImage: true, width: '75px', align: 'center' },
                    { key: 'productName', label: 'Tên sản phẩm', width: '220px' },
                    { key: 'unit', label: 'ĐVT', width: '75px' },
                    { key: 'quantityFormatted', label: 'SL tồn', width: '90px' },
                    { key: 'costFormatted', label: 'Giá bán (VNĐ)', width: '125px', align: 'right' },
                    { key: 'materialCostFormatted', label: 'Giá vốn (VNĐ)', width: '125px', align: 'right' },
                    { key: 'description', label: 'Mô tả / Tiêu chuẩn' },
                    { key: 'action', label: 'Thao tác', isActionDelete: true, width: '90px', align: 'center' }
                ];
            } else {
                // Bỏ đi cột Hạn sử dụng và Mô tả ở trang nguyên vật liệu theo yêu cầu
                columns = [
                    { key: 'ingrCode', label: 'Mã nguyên vật liệu', isLink: true, width: '160px' },
                    { key: 'ingrName', label: 'Tên nguyên vật liệu' },
                    { key: 'unit', label: 'ĐVT', width: '90px', align: 'center' },
                    { key: 'quantityFormatted', label: 'SL tồn', width: '120px', align: 'right' },
                    { key: 'priceFormatted', label: 'Đơn giá (VNĐ)', width: '140px', align: 'right' },
                    { key: 'costFormatted', label: 'Giá nhập (VNĐ)', width: '140px', align: 'right' },
                    { key: 'action', label: 'Thao tác', isActionDelete: true, width: '105px', align: 'center' }
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
                            statusText: cmdRow.orderStatus || 'Chờ nhập kho',
                            rawStatus: cmdRow.status,
                            employeeCode: cmdRow.employeeCode || 'NV0005',
                            employeeName: cmdRow.employeeName || cmdRow.supplier || 'Nguyễn Tiến Huy',
                            department: 'Phân xưởng Sản xuất GMP',
                            description: cmdRow.description || '',
                            requestId: cmdRow.requestId,
                            requestCode: cmdRow.requestCode || (cmdRow.requestId ? `LSX${String(cmdRow.requestId).padStart(5, '0')}` : null),
                            productionRequestCode: cmdRow.productionRequestCode || cmdRow.requestCode || (cmdRow.requestId ? `LSX${String(cmdRow.requestId).padStart(5, '0')}` : null),
                            assignedTo: cmdRow.assignedTo,
                            assignedToName: cmdRow.assignedToName,
                            assignedToEmployeeCode: cmdRow.assignedToEmployeeCode,
                            reqQtyColLabel: 'SL nhập',
                            actualQtyColLabel: 'Thực nhập',
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
                            statusText: cmdRow.orderStatus || 'Chờ xuất kho',
                            rawStatus: cmdRow.status,
                            employeeCode: cmdRow.employeeCode || 'NV0005',
                            employeeName: cmdRow.employeeName || 'Nguyễn Tiến Huy',
                            department: cmdRow.customer || cmdRow.department || 'Phân xưởng Viên nén GMP',
                            description: cmdRow.description || '',
                            requestId: cmdRow.requestId,
                            requestCode: cmdRow.requestCode || (cmdRow.requestId ? `LSX${String(cmdRow.requestId).padStart(5, '0')}` : null),
                            productionRequestCode: cmdRow.productionRequestCode || cmdRow.requestCode || (cmdRow.requestId ? `LSX${String(cmdRow.requestId).padStart(5, '0')}` : null),
                            assignedTo: cmdRow.assignedTo,
                            assignedToName: cmdRow.assignedToName,
                            assignedToEmployeeCode: cmdRow.assignedToEmployeeCode,
                            canApprove: cmdRow.canApprove,
                            hasStockIssue: cmdRow.hasStockIssue,
                            hasExpiryIssue: cmdRow.hasExpiryIssue,
                            approvalBlockReason: cmdRow.approvalBlockReason,
                            reqQtyColLabel: 'SL Yêu cầu',
                            actualQtyColLabel: 'SL thực xuất',
                            items: (cmdRow.items && cmdRow.items.length > 0) ? cmdRow.items : []
                        };
                    }
                    return commandsDetailData.outboundCommand;
                }
                case 'outbound': {
                    const activeDocNo = state.activeVoucherDocNo;
                    const receiptRow = outboundDatasets.outboundReceipts.find(r =>
                        r.voucherNo === activeDocNo || r.orderNo === activeDocNo || String(r.id) === String(activeDocNo) || String(r.exportReceiptId) === String(activeDocNo)
                    );
                    if (receiptRow) {
                        return {
                            voucherType: 'outbound',
                            mode: 'view',
                            exportReceiptId: receiptRow.exportReceiptId || receiptRow.id,
                            id: receiptRow.id,
                            voucherNo: receiptRow.voucherNo,
                            orderNo: receiptRow.voucherNo,
                            orderId: receiptRow.orderId,
                            orderCode: receiptRow.orderCode || (receiptRow.orderId ? `DH${String(receiptRow.orderId).padStart(5, '0')}` : ''),
                            partnerName: receiptRow.partnerName || receiptRow.customer || '',
                            customer: receiptRow.customer || receiptRow.partnerName || '',
                            receiver: receiptRow.receiver || receiptRow.customer || '',
                            customerPhone: receiptRow.customerPhone || '',
                            shippingAddress: receiptRow.shippingAddress || receiptRow.deliveryAddress || '',
                            deliveryAddress: receiptRow.shippingAddress || receiptRow.deliveryAddress || '',
                            warehouse: receiptRow.warehouse || 'Kho thành phẩm GMP',
                            description: receiptRow.description || '',
                            voucherDate: receiptRow.voucherDate || receiptRow.createdDate || '',
                            status: receiptRow.status || 'Chờ xuất kho',
                            rawStatus: receiptRow.rawStatus || receiptRow.status,
                            orderStatus: receiptRow.status,
                            statusType: receiptRow.statusType || 'warning',
                            employeeCode: receiptRow.employeeCode || 'NV0005',
                            employeeName: receiptRow.employeeName || receiptRow.creatorName || 'DS. Phan Thanh Hà',
                            creatorName: receiptRow.creatorName || 'DS. Phan Thanh Hà',
                            approverName: receiptRow.approverName || '',
                            canApprove: receiptRow.canApprove !== false,
                            hasStockIssue: receiptRow.hasStockIssue === true,
                            hasExpiryIssue: receiptRow.hasExpiryIssue === true,
                            approvalBlockReason: receiptRow.approvalBlockReason || (receiptRow.hasStockIssue ? 'Có sản phẩm không đủ điều kiện tồn kho để xuất' : ''),
                            quantityColumnLabel: 'SL xuất',
                            selectedSubType: 'Xuất kho thành phẩm',
                            partnerCodeOptions: [receiptRow.orderCode || 'DH00001', 'KH001', 'KH002'],
                            warehouseOptions: ['Kho thành phẩm GMP', 'KHO HƯNG YÊN - Kho chính'],
                            branchOptions: ['Tổng công ty Dược GMP', 'Chi nhánh Hà Nội', 'Chi nhánh TP.HCM'],
                            items: (receiptRow.items || []).map((item, idx) => ({
                                id: item.id || idx + 1,
                                productId: item.productId,
                                itemCode: item.productCode || item.itemCode || `SP${String(item.productId).padStart(3, '0')}`,
                                itemName: item.productName || item.itemName || 'Sản phẩm',
                                specCode: item.specCode || 'GMP-VN',
                                unit: item.unit || 'Hộp',
                                location: item.location || 'KHO-TP-A1',
                                quantity: Number(item.quantity) || 0,
                                stockQty: item.stockQty !== undefined ? Number(item.stockQty) : null,
                                isOutOfStock: item.isOutOfStock === true,
                                expiredStockQty: item.expiredStockQty !== undefined ? Number(item.expiredStockQty) : 0,
                                validStockQty: item.validStockQty !== undefined ? Number(item.validStockQty) : null,
                                conditionStatus: item.conditionStatus || '',
                                conditionType: item.conditionType || '',
                                isItemEligible: item.isItemEligible !== false,
                                blockReason: item.blockReason || '',
                                price: Number(item.price) || 0,
                                amount: (item.amount !== undefined && item.amount !== null) ? Number(item.amount) : (Number(item.quantity) || 0) * (Number(item.price) || 0),
                                batchNo: item.batchNo || 'LOT-2026-TP',
                                expiryDate: item.expiryDate || '2028-12-31'
                            }))
                        };
                    }
                    return vouchersData.outbound;
                }
                case 'inbound': {
                    if (state.activeVoucherMode === 'create' && state.inboundNewDraft) {
                        return state.inboundNewDraft;
                    }
                    const empInfo = getCurrentUserEmployeeInfo();
                    const activeDocNo = state.activeVoucherDocNo || 'PNK00018';
                    const draftRow = (state.inboundLocalDrafts || []).find(d =>
                        d.voucherNo === activeDocNo || d.orderNo === activeDocNo || String(d.id) === String(activeDocNo) || String(d.ipReceiptId) === String(activeDocNo)
                    );
                    if (draftRow) {
                        return {
                            voucherType: 'inbound',
                            mode: 'view',
                            isDraft: true,
                            receiptId: draftRow.id,
                            ipReceiptId: draftRow.id,
                            voucherNo: draftRow.voucherNo,
                            orderNo: draftRow.voucherNo,
                            employeeCode: draftRow.employeeCode || empInfo.employeeCode,
                            employeeName: draftRow.creatorName || empInfo.employeeName,
                            supplierCode: draftRow.supplierCode || '',
                            supplierName: draftRow.supplierName || 'Chưa chọn nhà cung cấp',
                            voucherDate: draftRow.voucherDate,
                            warehouse: draftRow.warehouse || 'KHO HƯNG YÊN - HÀNG TƯƠNG LAI',
                            status: 'Lưu nháp',
                            orderStatus: 'Lưu nháp',
                            statusType: 'secondary',
                            items: draftRow.items || []
                        };
                    }

                    const receiptRow = inboundDatasets.inboundReceipts.find(r =>
                        r.voucherNo === activeDocNo || r.orderNo === activeDocNo || String(r.id) === String(activeDocNo) || String(r.ipReceiptId) === String(activeDocNo)
                    );
                    if (receiptRow) {
                        vouchersData.inbound.mode = 'view';
                        vouchersData.inbound.isDraft = false;
                        vouchersData.inbound.receiptId = receiptRow.ipReceiptId || receiptRow.id;
                        vouchersData.inbound.ipReceiptId = receiptRow.ipReceiptId || receiptRow.id;
                        vouchersData.inbound.voucherNo = receiptRow.voucherNo;
                        vouchersData.inbound.employeeCode = receiptRow.employeeCode || empInfo.employeeCode;
                        vouchersData.inbound.employeeName = receiptRow.employeeName || receiptRow.creatorName || empInfo.employeeName;
                        vouchersData.inbound.supplierCode = receiptRow.supplierCode || 'NCC0046';
                        vouchersData.inbound.supplierName = receiptRow.supplierName || 'Công ty Cổ phần Dược phẩm Trung ương CPC1';
                        vouchersData.inbound.voucherDate = receiptRow.voucherDate;
                        vouchersData.inbound.warehouse = receiptRow.warehouse || 'KHO HƯNG YÊN - HÀNG TƯƠNG LAI';
                        vouchersData.inbound.status = receiptRow.status;
                        vouchersData.inbound.orderStatus = receiptRow.status;
                        vouchersData.inbound.statusType = receiptRow.statusType;
                        if (receiptRow.items && receiptRow.items.length > 0) {
                            vouchersData.inbound.items = receiptRow.items;
                        }
                    } else {
                        vouchersData.inbound.mode = 'view';
                        vouchersData.inbound.isDraft = false;
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

        isDraftReceipt(receiptId) {
            const sid = String(receiptId);
            return sid.startsWith('draft_') || (state.inboundLocalDrafts || []).some(d => String(d.id) === sid || d.voucherNo === sid);
        },

        openVoucherModal(voucherType, docNo, mode = 'view') {
            state.activeVoucherModal = voucherType;
            state.activeVoucherDocNo = docNo;
            state.activeVoucherMode = mode || 'view';

            if (voucherType === 'inbound' && mode === 'create') {
                const totalExisting = (inboundDatasets.inboundReceipts || []).length + (state.inboundLocalDrafts || []).length + 10;
                const empInfo = getCurrentUserEmployeeInfo();
                state.inboundNewDraft = {
                    voucherType: 'inbound',
                    mode: 'create',
                    voucherNo: `PNK${String(totalExisting).padStart(5, '0')}`,
                    selectedSubType: 'Nhập kho mua hàng',
                    employeeCode: empInfo.employeeCode,
                    employeeName: empInfo.employeeName,
                    supplierCode: '',
                    supplierName: '',
                    supplierId: null,
                    voucherDate: getCurrentVnDateTime(),
                    quantityColumnLabel: 'SL nhập',
                    items: []
                };
                // Tuyệt đối không tự động tạo dòng nguyên vật liệu khi mở modal (bảng trống)
            }
        },

        closeVoucherModal() {
            state.activeVoucherModal = null;
            state.activeVoucherDocNo = null;
            state.activeVoucherMode = 'view';
            state.inboundNewDraft = null;
        },

        setInboundNewSupplier(supplierVal) {
            if (!state.inboundNewDraft) return;
            if (!supplierVal) {
                state.inboundNewDraft.supplierId = null;
                state.inboundNewDraft.supplierCode = '';
                state.inboundNewDraft.supplierName = '';
                state.inboundNewDraft.importCost = 0;
            } else {
                const suppliers = this.getSuppliersList();
                const sup = suppliers.find(s => s.code === supplierVal || s.name === supplierVal);
                if (sup) {
                    state.inboundNewDraft.supplierId = sup.id || sup.supplierId;
                    state.inboundNewDraft.supplierCode = sup.code;
                    state.inboundNewDraft.supplierName = sup.name;
                    state.inboundNewDraft.importCost = (sup.importCost !== undefined && sup.importCost !== null) ? Number(sup.importCost) : 0;
                }
            }

            // Tính lại đơn giá và thành tiền cho tất cả các dòng NVL theo tỷ lệ chi phí nhập mới
            const importCostPct = Number(state.inboundNewDraft.importCost) || 0;
            const ingrList = this.getIngredientsList();
            if (Array.isArray(state.inboundNewDraft.items)) {
                state.inboundNewDraft.items.forEach(row => {
                    if (row.itemCode) {
                        const ingr = ingrList.find(i => i.ingrCode === row.itemCode);
                        const basePrice = (row.rawPrice !== undefined && row.rawPrice !== null)
                            ? Number(row.rawPrice)
                            : (ingr ? Number(ingr.price || 0) : 0);
                        row.rawPrice = basePrice;
                        // Công thức: Đơn giá NVL + (Đơn giá NVL * import_cost / 100)
                        row.price = Math.round(basePrice + (basePrice * importCostPct / 100));
                        const qty = parseFloat(row.quantity) || 0;
                        row.amount = Math.round(qty * row.price);
                    }
                });
            }
        },

        addInboundNewItemRow() {
            if (!state.inboundNewDraft) return { success: false };
            const ingrList = this.getIngredientsList();
            const currentItems = state.inboundNewDraft.items || [];
            if (ingrList.length > 0 && currentItems.length >= ingrList.length) {
                const selectedCount = new Set(currentItems.filter(i => i.itemCode).map(i => i.itemCode)).size;
                return {
                    success: false,
                    message: selectedCount >= ingrList.length
                        ? 'Tất cả nguyên vật liệu trong danh mục đã được thêm vào phiếu này. Không được thêm nguyên vật liệu trùng lặp!'
                        : `Chỉ có tối đa ${ingrList.length} nguyên vật liệu khác nhau trong danh mục. Vui lòng chọn nguyên vật liệu cho các dòng hiện có!`
                };
            }
            const nextId = (currentItems.length > 0 ? Math.max(...currentItems.map(i => i.id)) : 0) + 1;
            state.inboundNewDraft.items.push({
                id: nextId,
                itemCode: '',
                itemName: '',
                unit: '',
                quantity: '',
                rawPrice: 0,
                price: 0,
                amount: 0,
                expiryDate: '-'
            });
            return { success: true };
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
            if (!state.inboundNewDraft) return { success: false };
            const row = state.inboundNewDraft.items.find(i => i.id === Number(rowId));
            if (!row) return { success: false };
            if (!ingrCode) {
                row.itemCode = '';
                row.itemName = '';
                row.unit = '';
                row.rawPrice = 0;
                row.price = 0;
                row.expiryDate = '-';
                row.amount = 0;
                return { success: true };
            }
            const ingrList = this.getIngredientsList();
            const ingr = ingrList.find(i => i.ingrCode === ingrCode);
            const isDuplicate = state.inboundNewDraft.items.some(i => i.id !== Number(rowId) && i.itemCode === ingrCode);
            if (isDuplicate) {
                const label = ingr ? `${ingr.ingrName} (${ingr.ingrCode})` : ingrCode;
                return {
                    success: false,
                    duplicate: true,
                    message: `Nguyên vật liệu '${label}' đã được thêm trước đó trong phiếu này. Không được thêm nguyên liệu mới trùng với nguyên liệu đã thêm trước đó!`
                };
            }
            if (ingr) {
                row.itemCode = ingr.ingrCode;
                row.itemName = ingr.ingrName;
                row.unit = (ingr.unit || '').trim() || 'kg';
                row.rawPrice = Number(ingr.price || 0);
                const importCostPct = Number(state.inboundNewDraft.importCost) || 0;
                // Công thức: Đơn giá NVL + (Đơn giá NVL * import_cost / 100)
                row.price = Math.round(row.rawPrice + (row.rawPrice * importCostPct / 100));
                row.expiryDate = ingr.expiredDate || '31/12/2026';
                row.ingrId = ingr.id || ingr.ingrId;
                const qty = parseFloat(row.quantity) || 0;
                row.amount = Math.round(qty * (row.price || 0));
            }
            return { success: true };
        },

        changeInboundNewItemUnit(rowId, newUnit) {
            if (!state.inboundNewDraft) return null;
            const row = state.inboundNewDraft.items.find(i => i.id === Number(rowId));
            if (!row) return null;
            row.unit = String(newUnit ?? '');
            return row;
        },

        changeInboundNewItemQty(rowId, newQty) {
            if (!state.inboundNewDraft) return null;
            const row = state.inboundNewDraft.items.find(i => i.id === Number(rowId));
            if (!row) return null;
            let strVal = String(newQty || '').trim();
            if (strVal.length > 6) {
                strVal = strVal.slice(0, 6);
            }
            let qty = parseFloat(strVal) || 0;
            if (qty > 999999) qty = 999999;
            row.quantity = qty;
            row.amount = Math.round(qty * (row.price || 0));
            return row;
        },

        async saveInboundReceipt(isDraft = false) {
            if (!state.inboundNewDraft) return { success: false, message: 'Không có dữ liệu phiếu!' };
            const draft = state.inboundNewDraft;

            // Kiểm tra không có dòng nguyên vật liệu trùng nhau
            const selectedItems = (draft.items || []).filter(it => it.itemCode);
            const seenCodes = new Set();
            for (const it of selectedItems) {
                if (seenCodes.has(it.itemCode)) {
                    return {
                        success: false,
                        message: `Nguyên vật liệu '${it.itemName || it.itemCode} (${it.itemCode})' bị trùng lặp. Không được thêm nguyên liệu mới trùng với nguyên liệu đã thêm trước đó!`
                    };
                }
                seenCodes.add(it.itemCode);
            }

            if (isDraft) {
                // Lưu nháp: CHƯA LƯU VÀO DATABASE mà chỉ nằm trên giao diện (local state)
                const draftId = `draft_${Date.now()}`;
                const empInfo = getCurrentUserEmployeeInfo();
                const newDraft = {
                    id: draftId,
                    receiptId: draftId,
                    ipReceiptId: draftId,
                    isDraft: true,
                    voucherNo: draft.voucherNo,
                    orderNo: draft.voucherNo,
                    voucherDate: draft.voucherDate || getCurrentVnDateTime(),
                    supplierId: draft.supplierId || null,
                    supplierCode: draft.supplierCode || '',
                    supplierName: draft.supplierName || 'Chưa chọn nhà cung cấp',
                    warehouse: 'KHO HƯNG YÊN - HÀNG TƯƠNG LAI',
                    status: 'Lưu nháp',
                    orderStatus: 'Lưu nháp',
                    statusType: 'secondary',
                    creatorName: draft.employeeName || empInfo.employeeName,
                    employeeCode: draft.employeeCode || empInfo.employeeCode,
                    items: JSON.parse(JSON.stringify(draft.items || []))
                };

                state.inboundLocalDrafts.unshift(newDraft);
                try {
                    localStorage.setItem('wh_inbound_drafts', JSON.stringify(state.inboundLocalDrafts));
                } catch (e) {}

                this.closeVoucherModal();
                return { success: true, isDraft: true, voucherNo: draft.voucherNo };
            }

            // Nhấn Lưu: Bắt buộc chọn NCC và có ít nhất 1 NVL hợp lệ, lưu vào DB với status = "Approve"
            if (!draft.supplierCode && !draft.supplierName) {
                return { success: false, message: 'Vui lòng chọn nhà cung cấp!' };
            }
            const validItems = (draft.items || []).filter(it => it.itemCode && Number(it.quantity) > 0);
            if (validItems.length === 0) {
                return { success: false, message: 'Phiếu nhập phải có ít nhất 1 dòng nguyên vật liệu với số lượng nhập lớn hơn 0!' };
            }

            const suppliers = this.getSuppliersList();
            const sup = suppliers.find(s => s.code === draft.supplierCode || s.name === draft.supplierName) || suppliers[0];
            const supplierId = draft.supplierId || (sup ? (sup.id || sup.supplierId) : 46);

            const ingrList = this.getIngredientsList();
            const apiItems = validItems.map(it => {
                const found = ingrList.find(i => i.ingrCode === it.itemCode);
                const ingrId = it.ingrId || (found ? (found.id || found.ingrId) : 41);
                return {
                    ingrId: Number(ingrId),
                    quantity: Number(it.quantity) || 1,
                    cost: Number(it.price) || 0,
                    unit: String(found?.unit || it.unit || 'kg').trim()
                };
            });

            try {
                const empInfo = getCurrentUserEmployeeInfo();
                const payload = {
                    supplierId: Number(supplierId),
                    createdBy: empInfo.userId || undefined,
                    ipReceiptCode: draft.voucherNo,
                    status: 'Approve',
                    isDraft: false,
                    items: apiItems
                };
                const res = await fetch('/api/warehouse/import-receipts', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const resText = await res.text();
                let resData = {};
                try { resData = JSON.parse(resText); } catch (_) { resData = { message: resText }; }
                if (!res.ok) throw new Error(resData.message || `Lỗi máy chủ (${res.status})`);

                await Promise.all([
                    fetchImportReceiptsApi(),
                    fetchIngredientsApi()
                ]);
                this.closeVoucherModal();
                return { success: true, isDraft: false, voucherNo: resData.voucherNo || draft.voucherNo };
            } catch (err) {
                console.error('Lỗi khi lưu phiếu nhập kho NVL:', err);
                return { success: false, message: err.message || 'Lỗi khi lưu phiếu nhập kho!' };
            }
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

        toggleUserDropdown(forceState) {
            state.isUserDropdownOpen = typeof forceState === 'boolean' ? forceState : !state.isUserDropdownOpen;
        },

        closeUserDropdown() {
            state.isUserDropdownOpen = false;
        },

        navigateToEmployeeProfile() {
            if (state.activeMenuId !== 'thong-tin-nhan-vien') {
                state.previousMenuId = state.activeMenuId;
            }
            state.activeMenuId = 'thong-tin-nhan-vien';
            state.isUserDropdownOpen = false;
        },

        backFromEmployeeProfile() {
            state.activeMenuId = state.previousMenuId || 'tong-quan';
        },

        getCurrentUser() {
            return state.currentUser;
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
                state.inbound.statusFilter = 'Tất cả';
            } else if (pageType === 'xuat-kho') {
                state.outbound.activeTab = tabId;
                state.outbound.orderStatusFilter = 'Tất cả';
            } else if (pageType === 'danh-muc-vthh') {
                state.vthh.activeTab = tabId;
            } else if (pageType === 'yeu-cau-san-xuat') {
                state.productionRequest.activeTab = tabId;
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
            } else if (pageType === 'yeu-cau-san-xuat') {
                state.productionRequest.searchKeyword = keyword || '';
            } else if (pageType === 'nha-cung-cap') {
                if (!state.supplier) state.supplier = { activeTab: 'tat-ca', searchKeyword: '', currentPage: 1, pageSize: 20 };
                state.supplier.searchKeyword = keyword || '';
                state.supplier.currentPage = 1;
            }
        },

        setOperationStatusFilter(pageType, statusValue) {
            if (pageType === 'nhap-kho') {
                state.inbound.statusFilter = statusValue;
            } else if (pageType === 'xuat-kho') {
                state.outbound.orderStatusFilter = statusValue;
            } else if (pageType === 'yeu-cau-san-xuat') {
                state.productionRequest.statusFilter = statusValue;
            }
        },

        getDashboardCardsConfig() {
            const allItems = [
                ...(vthhDatasets.ingredients || []).map(i => ({
                    id: i.id || i.ingrId,
                    name: i.name || i.ingrName || 'Nguyên vật liệu',
                    warehouse: 'Kho nguyên liệu GSP',
                    currentStock: Number(i.quantity) || 0,
                    minStock: 100,
                    maxStock: 500,
                    color: DOT_COLORS[(i.id || 0) % DOT_COLORS.length]
                })),
                ...(vthhDatasets.products || []).map(p => ({
                    id: p.id || p.productId,
                    name: p.name || p.productName || 'Thành phẩm',
                    warehouse: 'Kho thành phẩm',
                    currentStock: Number(p.quantity) || 0,
                    minStock: 50,
                    maxStock: 500,
                    color: DOT_COLORS[(p.id || 0) % DOT_COLORS.length]
                }))
            ];

            const realBelowMin = allItems.filter(item => item.currentStock <= item.minStock);
            const realAboveMax = allItems.filter(item => item.currentStock >= item.maxStock);

            // Thu thập các lô có hạn dùng từ các phiếu nhập thực tế
            const realReceipts = (inboundDatasets.inboundReceipts || []).flatMap(r => (r.items || []).map(it => ({
                id: it.id,
                name: it.itemName,
                expiryDate: it.expiryDate || it.expiredDate || '',
                batchNo: it.batchNo || it.lotNo || `L${String(r.id || 1).padStart(4, '0')}`,
                quantity: it.quantity || 0,
                color: DOT_COLORS[(it.id || 0) % DOT_COLORS.length]
            }))).filter(x => x.expiryDate);

            const now = new Date();
            const realExpired = [];
            const realExpiring = [];
            const daysLimit = Number(state.expiringDaysTitle) || 30;

            realReceipts.forEach(item => {
                const parts = item.expiryDate.split(/[\/\-]/);
                if (parts.length === 3) {
                    const exp = new Date(Number(parts[2]), Number(parts[1]) - 1, Number(parts[0]));
                    const diffDays = Math.ceil((exp - now) / (1000 * 60 * 60 * 24));
                    if (diffDays < 0) {
                        realExpired.push(item);
                    } else if (diffDays <= daysLimit) {
                        realExpiring.push(item);
                    }
                }
            });

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
                    rows: filterByKeyword(realBelowMin, state.searchKeyword, ['name', 'warehouse'])
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
                    rows: filterByKeyword(realAboveMax, state.searchKeyword, ['name', 'warehouse'])
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
                    rows: filterByKeyword(realExpiring, state.searchKeyword, ['name', 'batchNo'])
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
                    rows: filterByKeyword(realExpired, state.searchKeyword, ['name', 'batchNo'])
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
                sourceRows = [
                    ...(state.inboundLocalDrafts || []),
                    ...(inboundDatasets.inboundReceipts || [])
                ];
            } else if (isCommandTab) {
                sourceRows = inboundDatasets.inboundCommands;
            } else {
                sourceRows = state.usePharmaFullData ? inboundDatasets.pharmaOrders : inboundDatasets.defaultOrders;
            }

            const filteredByStatus = (state.inbound.statusFilter === 'Tất cả' || !state.inbound.statusFilter)
                ? sourceRows
                : sourceRows.filter(r => {
                    const filter = state.inbound.statusFilter;
                    if (r.orderStatus === filter || r.status === filter) return true;
                    if ((filter === 'Approve' || filter === 'Approved' || filter === 'Đã nhập kho') &&
                        (r.status === 'Approved' || r.status === 'Approve' || r.orderStatus === 'Đã nhập kho')) return true;
                    if ((filter === 'Pending' || filter === 'Chờ nhập kho') &&
                        (r.status === 'Pending' || r.orderStatus === 'Chờ nhập kho')) return true;
                    if ((filter === 'Rejected' || filter === 'Reject' || filter === 'Từ chối') &&
                        (r.status === 'Rejected' || r.status === 'Reject' || r.orderStatus === 'Từ chối')) return true;
                    return false;
                });

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
                    { key: 'creatorName', label: 'Người lập phiếu', width: '150px' },
                    { key: 'supplierCode', label: 'Mã nhà cung cấp', width: '130px' },
                    { key: 'supplierName', label: 'Nhà cung cấp', width: '250px' },
                    { key: 'status', label: 'Trạng thái', isBadge: true, width: '130px' },
                    { key: 'action', label: 'Thao tác', isActionImportReceipt: true, width: '190px' }
                ];
            } else if (isCommandTab) {
                columns = [
                    { key: 'orderNo', label: 'Số lệnh', isLink: true, width: '120px' },
                    { key: 'requestCode', label: 'Mã yêu cầu SX', width: '130px' },
                    { key: 'orderDate', label: 'Ngày lệnh', width: '110px' },
                    { key: 'supplier', label: 'Người lập', width: '130px' },
                    { key: 'productName', label: 'Tên hàng hóa' },
                    { key: 'description', label: 'Diễn giải' },
                    { key: 'status', label: 'Tình trạng', isBadge: true, width: '120px' },
                    { key: 'action', label: 'Thao tác', isActionInboundProduct: true, width: '110px', align: 'center' }
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
                    { id: 'nhap-kho-tab', label: 'Nhập kho nguyên vật liệu' }
                ],
                searchValue: state.inbound.searchKeyword,
                filters: [
                    {
                        id: 'inbound-status-filter',
                        prefix: 'Trạng thái:',
                        value: state.inbound.statusFilter || 'Tất cả',
                        options: isVoucherTab
                            ? ['Tất cả', 'Lưu nháp', 'Approve', 'Reject']
                            : ['Tất cả', 'Pending', 'Approve', 'Rejected']
                    }
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
                : sourceRows.filter(r => {
                    const rowStatus = String(r.orderStatus || r.status || r.rawStatus || '').trim();
                    if (state.outbound.orderStatusFilter === 'Cancel') {
                        const lower = rowStatus.toLowerCase();
                        return lower === 'cancel' || lower === 'cancelled' || lower === 'canceled' || rowStatus === 'Đã hủy';
                    }
                    return rowStatus === state.outbound.orderStatusFilter;
                });

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
                    { key: 'voucherDate', label: 'Ngày chứng từ', width: '135px' },
                    { key: 'voucherNo', label: 'Số chứng từ', isLink: true, width: '115px' },
                    { key: 'partnerName', label: 'Khách hàng / Đối tượng', width: '220px' },
                    { key: 'warehouse', label: 'Kho xuất', width: '145px' },
                    { key: 'description', label: 'Lý do xuất' },
                    { key: 'status', label: 'Trạng thái', isBadge: true, width: '110px' },
                    { key: 'action', label: 'Thao tác', isActionProductExport: true, width: '170px', align: 'center' }
                ];
            } else if (isCommandTab) {
                columns = [
                    { key: 'orderDate', label: 'Ngày lệnh', width: '110px' },
                    { key: 'orderNo', label: 'Số lệnh', isLink: true, width: '125px' },
                    { key: 'requestCode', label: 'Mã yêu cầu SX', width: '130px' },
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
                    { id: 'xuat-kho-tab', label: 'Xuất kho thành phẩm' }
                ],
                searchValue: state.outbound.searchKeyword,
                filters: [
                    { id: 'outbound-status-filter', prefix: 'Trạng thái:', value: state.outbound.orderStatusFilter, options: ['Tất cả', 'Chờ xuất kho', 'Đã xuất kho', 'Từ chối', 'Cancel'] }
                ],
                columns,
                rows: filtered,
                totalRecords: filtered.length,
                rangeText: filtered.length > 0 ? `1 - ${filtered.length}` : '0 - 0',
                hasMorePages: false
            };
        },

        async fetchProductExportsApi(keyword = '') {
            return fetchProductExportsApi(keyword);
        },

        async fetchProductExportDetailApi(idOrCode) {
            return fetchProductExportDetailApi(idOrCode);
        },

        async approveProductExport(receiptId) {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/product-exports/${numId}/approve`, {
                method: 'PUT'
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Duyệt xuất kho thất bại.' }));
                throw new Error(err.message || 'Duyệt xuất kho thất bại.');
            }
            const data = await res.json();
            const existing = outboundDatasets.outboundReceipts.find(c => c.id === numId || c.exportReceiptId === numId || c.voucherNo === receiptId);
            if (existing) {
                existing.status = 'Đã xuất kho';
                existing.orderStatus = 'Đã xuất kho';
                existing.statusType = 'success';
                existing.canApprove = false;
            }
            return data;
        },

        async rejectProductExport(receiptId, reason = '') {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/product-exports/${numId}/reject`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reason: reason || 'Thủ kho từ chối xuất kho thành phẩm.' })
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({ message: 'Từ chối xuất kho thất bại.' }));
                throw new Error(err.message || 'Từ chối xuất kho thất bại.');
            }
            const data = await res.json();
            const existing = outboundDatasets.outboundReceipts.find(c => c.id === numId || c.exportReceiptId === numId || c.voucherNo === receiptId);
            if (existing) {
                existing.status = 'Từ chối';
                existing.orderStatus = 'Từ chối';
                existing.statusType = 'danger';
                existing.canApprove = false;
            }
            return data;
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

        async fetchMaterialReceiptDetailApi(receiptId) {
            try {
                const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
                const res = await fetch(`/api/warehouse/material-receipts/${numId}`);
                if (!res.ok) throw new Error(`HTTP error ${res.status}`);
                const data = await res.json();
                const existing = outboundDatasets.outboundCommands.find(c => c.id === numId || c.expReceiptId === numId);
                if (existing) {
                    Object.assign(existing, data);
                }
                return data;
            } catch (err) {
                console.error('Lỗi khi tải chi tiết phiếu xuất NVL:', err);
                return null;
            }
        },

        async fetchProductReceiptDetailApi(receiptId) {
            try {
                const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
                const res = await fetch(`/api/warehouse/product-receipts/${numId}`);
                if (!res.ok) throw new Error(`HTTP error ${res.status}`);
                const data = await res.json();
                const existing = inboundDatasets.inboundCommands.find(c => c.id === numId || c.productReceiptId === numId);
                if (existing) {
                    Object.assign(existing, data);
                }
                return data;
            } catch (err) {
                console.error('Lỗi khi tải chi tiết phiếu nhập thành phẩm:', err);
                return null;
            }
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
        },

        async fetchImportReceiptsApi(keyword = '') {
            return fetchImportReceiptsApi(keyword);
        },

        async fetchImportReceiptDetailApi(receiptId) {
            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            try {
                const res = await fetch(`/api/warehouse/import-receipts/${numId}`);
                if (!res.ok) throw new Error(`HTTP error ${res.status}`);
                const data = await res.json();
                const empInfo = getCurrentUserEmployeeInfo();
                const existing = inboundDatasets.inboundReceipts.find(r => r.ipReceiptId === numId || r.voucherNo === data.voucherNo || r.id === numId);
                const empCode = data.employeeCode || existing?.employeeCode || empInfo.employeeCode;
                const empName = data.employeeName || data.creatorName || existing?.creatorName || empInfo.employeeName;
                if (existing) {
                    existing.items = data.items;
                    existing.status = data.status;
                    existing.orderStatus = data.status;
                    existing.statusType = data.statusType;
                    existing.employeeCode = empCode;
                    existing.creatorName = empName;
                }
                if (vouchersData.inbound && (vouchersData.inbound.voucherNo === data.voucherNo || vouchersData.inbound.receiptId === numId)) {
                    vouchersData.inbound.items = data.items;
                    vouchersData.inbound.status = data.status;
                    vouchersData.inbound.orderStatus = data.status;
                    vouchersData.inbound.statusType = data.statusType;
                    vouchersData.inbound.supplierName = data.supplierName;
                    vouchersData.inbound.supplierCode = data.supplierCode;
                    vouchersData.inbound.employeeCode = empCode;
                    vouchersData.inbound.employeeName = empName;
                }
                return data;
            } catch (err) {
                console.error('Lỗi khi tải chi tiết phiếu nhập kho NVL:', err);
                return null;
            }
        },

        async approveImportReceiptApi(receiptId) {
            const isDraft = this.isDraftReceipt(receiptId);
            if (isDraft) {
                const draft = (state.inboundLocalDrafts || []).find(d => String(d.id) === String(receiptId) || d.voucherNo === receiptId);
                if (!draft) {
                    throw new Error('Không tìm thấy phiếu nháp!');
                }
                if (!draft.supplierCode && !draft.supplierName) {
                    throw new Error('Phiếu nháp chưa chọn nhà cung cấp hợp lệ. Vui lòng kiểm tra lại!');
                }
                const validItems = (draft.items || []).filter(it => it.itemCode && Number(it.quantity) > 0);
                if (validItems.length === 0) {
                    throw new Error('Phiếu nháp phải có ít nhất 1 dòng nguyên vật liệu với số lượng nhập lớn hơn 0!');
                }

                const suppliers = this.getSuppliersList();
                const sup = suppliers.find(s => s.code === draft.supplierCode || s.name === draft.supplierName) || suppliers[0];
                const supplierId = draft.supplierId || (sup ? (sup.id || sup.supplierId) : 46);

                const ingrList = this.getIngredientsList();
                const apiItems = validItems.map(it => {
                    const found = ingrList.find(i => i.ingrCode === it.itemCode);
                    const ingrId = it.ingrId || (found ? (found.id || found.ingrId) : 41);
                    return {
                        ingrId: Number(ingrId),
                        quantity: Number(it.quantity) || 1,
                        cost: Number(it.price) || 0,
                        unit: String(found?.unit || it.unit || 'kg').trim() || undefined
                    };
                });

                const empInfo = getCurrentUserEmployeeInfo();
                const payload = {
                    supplierId: Number(supplierId),
                    createdBy: empInfo.userId || undefined,
                    ipReceiptCode: draft.voucherNo,
                    status: 'Approve',
                    isDraft: false,
                    items: apiItems
                };
                const res = await fetch('/api/warehouse/import-receipts', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const resText = await res.text();
                let resData = {};
                try { resData = JSON.parse(resText); } catch (_) { resData = { message: resText }; }
                if (!res.ok) throw new Error(resData.message || `Lỗi máy chủ (${res.status})`);

                // Xóa khỏi danh sách nháp local
                state.inboundLocalDrafts = state.inboundLocalDrafts.filter(d => String(d.id) !== String(receiptId) && d.voucherNo !== receiptId);
                try {
                    localStorage.setItem('wh_inbound_drafts', JSON.stringify(state.inboundLocalDrafts));
                } catch (e) {}

                await Promise.all([
                    fetchImportReceiptsApi(),
                    fetchIngredientsApi()
                ]);
                return {
                    success: true,
                    status: 'Approve',
                    message: `Nhập kho phiếu ${draft.voucherNo} thành công (Trạng thái: Approve)!`
                };
            }

            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/import-receipts/${numId}/approve`, {
                method: 'PUT'
            });
            const resText = await res.text();
            let data = {};
            try { data = JSON.parse(resText); } catch (_) { data = { message: resText }; }
            if (!res.ok) {
                throw new Error(data.message || `Nhập kho thất bại (${res.status}).`);
            }
            const existing = inboundDatasets.inboundReceipts.find(r => r.id === numId || r.ipReceiptId === numId);
            if (existing) {
                existing.status = 'Approve';
                existing.orderStatus = 'Approve';
                existing.statusType = 'success';
            }
            return data;
        },

        async rejectImportReceiptApi(receiptId, reason = '') {
            const isDraft = this.isDraftReceipt(receiptId);
            if (isDraft) {
                const draft = (state.inboundLocalDrafts || []).find(d => String(d.id) === String(receiptId) || d.voucherNo === receiptId);
                if (!draft) {
                    throw new Error('Không tìm thấy phiếu nháp!');
                }
                const suppliers = this.getSuppliersList();
                const sup = suppliers.find(s => s.code === draft.supplierCode || s.name === draft.supplierName) || suppliers[0];
                const supplierId = draft.supplierId || (sup ? (sup.id || sup.supplierId) : 46);

                const ingrList = this.getIngredientsList();
                const itemsToSave = (draft.items && draft.items.length > 0) ? draft.items : [{ itemCode: 'NVL041', quantity: 1, price: 0 }];
                const apiItems = itemsToSave.map(it => {
                    const found = ingrList.find(i => i.ingrCode === it.itemCode);
                    const ingrId = it.ingrId || (found ? (found.id || found.ingrId) : 41);
                    return {
                        ingrId: Number(ingrId),
                        quantity: Number(it.quantity) || 1,
                        cost: Number(it.price) || 0
                    };
                });

                const payload = {
                    supplierId: Number(supplierId),
                    ipReceiptCode: draft.voucherNo,
                    status: 'Reject',
                    isReject: true,
                    items: apiItems
                };
                const res = await fetch('/api/warehouse/import-receipts', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const resText = await res.text();
                let resData = {};
                try { resData = JSON.parse(resText); } catch (_) { resData = { message: resText }; }
                if (!res.ok) throw new Error(resData.message || `Lỗi máy chủ (${res.status})`);

                // Xóa khỏi danh sách nháp local
                state.inboundLocalDrafts = state.inboundLocalDrafts.filter(d => String(d.id) !== String(receiptId) && d.voucherNo !== receiptId);
                try {
                    localStorage.setItem('wh_inbound_drafts', JSON.stringify(state.inboundLocalDrafts));
                } catch (e) {}

                await fetchImportReceiptsApi();
                return {
                    success: true,
                    status: 'Reject',
                    message: `Đã từ chối nhập kho phiếu ${draft.voucherNo} (Trạng thái: Reject)!`
                };
            }

            const numId = parseInt(String(receiptId).replace(/\D/g, ''), 10) || receiptId;
            const res = await fetch(`/api/warehouse/import-receipts/${numId}/reject`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reason: reason || 'Không nhập kho NVL.' })
            });
            const resText = await res.text();
            let data = {};
            try { data = JSON.parse(resText); } catch (_) { data = { message: resText }; }
            if (!res.ok) {
                throw new Error(data.message || `Từ chối nhập kho thất bại (${res.status}).`);
            }
            const existing = inboundDatasets.inboundReceipts.find(r => r.id === numId || r.ipReceiptId === numId);
            if (existing) {
                existing.status = 'Reject';
                existing.orderStatus = 'Reject';
                existing.statusType = 'danger';
            }
            return data;
        },

        async fetchIngredientDetailApi(id) {
            return fetchIngredientDetailApi(id);
        },

        async disposeExpiredBatchApi(ingrId, receiptId) {
            return disposeExpiredBatchApi(ingrId, receiptId);
        },

        async disposeExpiredProductBatchApi(productId, receiptId) {
            return disposeExpiredProductBatchApi(productId, receiptId);
        },

        async fetchProductDetailApi(id) {
            return fetchProductDetailApi(id);
        },

        async fetchProductionRequestsApi(params = {}) {
            try {
                const query = new URLSearchParams();
                if (params.keyword) query.append('keyword', params.keyword);
                if (params.status) query.append('status', params.status);
                if (params.assignedTo !== undefined && params.assignedTo !== null) query.append('assignedTo', params.assignedTo);
                if (params.productId) query.append('productId', params.productId);
                const qs = query.toString();
                const res = await fetch('/api/production-requests' + (qs ? `?${qs}` : ''));
                if (!res.ok) throw new Error(`Lỗi tải danh sách lệnh sản xuất (${res.status})`);
                const data = await res.json();
                state.productionRequests = Array.isArray(data) ? data : [];
                return state.productionRequests;
            } catch (err) {
                console.error('Lỗi khi nạp danh sách lệnh sản xuất:', err);
                return state.productionRequests || [];
            }
        },

        async fetchProductionRequestDetailApi(id) {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/production-requests/${numId}`);
            if (!res.ok) throw new Error(`Lỗi tải chi tiết lệnh sản xuất (${res.status})`);
            return await res.json();
        },

        async createProductionRequestApi(dto) {
            const res = await fetch('/api/production-requests', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });
            const data = await res.json();
            if (!res.ok) throw new Error(data.message || 'Tạo lệnh sản xuất thất bại.');
            await this.fetchProductionRequestsApi();
            return data;
        },

        async updateProductionRequestApi(id, dto) {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/production-requests/${numId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });
            const data = await res.json();
            if (!res.ok) throw new Error(data.message || 'Cập nhật lệnh sản xuất thất bại.');
            await this.fetchProductionRequestsApi();
            return data;
        },

        async assignProductionRequest(id, assignedTo) {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/production-requests/${numId}/assign`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ assignedTo: assignedTo ? parseInt(assignedTo, 10) : null })
            });
            const data = await res.json();
            if (!res.ok) throw new Error(data.message || 'Lỗi cập nhật phân công lệnh sản xuất.');
            await this.fetchProductionRequestsApi();
            return data;
        },

        async updateProductionRequestStatusApi(id, status) {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/production-requests/${numId}/status`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ status })
            });
            const data = await res.json();
            if (!res.ok) throw new Error(data.message || 'Cập nhật trạng thái lệnh sản xuất thất bại.');
            await this.fetchProductionRequestsApi();
            return data;
        },

        async deleteProductionRequestApi(id) {
            const numId = parseInt(String(id).replace(/\D/g, ''), 10) || id;
            const res = await fetch(`/api/production-requests/${numId}`, {
                method: 'DELETE'
            });
            const data = await res.json();
            if (!res.ok) throw new Error(data.message || 'Xóa lệnh sản xuất thất bại.');
            await this.fetchProductionRequestsApi();
            return data;
        },

        async fetchCurrentUserApi() {
            try {
                const res = await fetch('/api/warehouse/production-requests/current-user');
                if (!res.ok) throw new Error(`Lỗi tải thông tin tài khoản (${res.status})`);
                const data = await res.json();
                if (data && data.userId) {
                    state.currentUser = {
                        userId: data.userId,
                        employeeCode: data.employeeCode || '',
                        username: data.username || '',
                        fullName: data.fullName || data.username || '',
                        role: data.role || 'Warehouse',
                        phone: data.phone || '',
                        status: data.status || 'Active'
                    };
                }
                return state.currentUser;
            } catch (err) {
                console.warn('Không tải được thông tin người dùng đang đăng nhập:', err);
                return state.currentUser;
            }
        },

        async fetchAssignableUsersApi() {
            try {
                const res = await fetch('/api/production-requests/assignable-users');
                if (!res.ok) throw new Error(`Lỗi tải danh sách nhân sự (${res.status})`);
                const data = await res.json();
                state.assignableUsers = Array.isArray(data) ? data : [];
                return state.assignableUsers;
            } catch (err) {
                console.error('Lỗi nạp nhân sự phân công:', err);
                return state.assignableUsers || [];
            }
        },

        setPrTab(tabId) {
            if (state.productionRequest) {
                state.productionRequest.activeTab = tabId;
            }
        },

        setPrStatusFilter(val) {
            if (state.productionRequest) {
                state.productionRequest.statusFilter = val;
            }
        },

        setPrAssignmentFilter(val) {
            if (state.productionRequest) {
                state.productionRequest.assignmentFilter = val;
                state.productionRequest.receiveFilter = val;
            }
        },

        setPrReceiveFilter(val) {
            if (state.productionRequest) {
                state.productionRequest.receiveFilter = val;
                state.productionRequest.assignmentFilter = val;
            }
        },

        async receiveProductionRequest(requestId) {
            let currentUsr = state.currentUser;
            if ((!currentUsr || !currentUsr.userId) && this.fetchCurrentUserApi) {
                currentUsr = await this.fetchCurrentUserApi();
            }
            const numId = parseInt(String(requestId).replace(/\D/g, ''), 10) || requestId;
            const res = await fetch(`/api/warehouse/production-requests/${numId}/receive`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ receiverId: currentUsr?.userId || null })
            });
            const resText = await res.text();
            let data = {};
            try { data = JSON.parse(resText); } catch (_) { data = { message: resText }; }
            if (!res.ok) {
                throw new Error(data.message || 'Tiếp nhận yêu cầu sản xuất thất bại.');
            }
            await this.fetchProductionRequestsApi();
            return data;
        },

        setPrSearchKeyword(kw) {
            if (state.productionRequest) {
                state.productionRequest.searchKeyword = kw || '';
            }
        },

        getProductionRequestGridConfig() {
            const prState = state.productionRequest || {
                activeTab: 'tat-ca',
                searchKeyword: '',
                statusFilter: 'Tất cả',
                assignmentFilter: 'Tất cả'
            };

            let rows = [...(state.productionRequests || [])];

            // Phân loại theo tab
            if (prState.activeTab === 'cho-xu-ly') {
                rows = rows.filter(r => r.status === 'Pending' || r.status === 'Chờ xử lý');
            } else if (prState.activeTab === 'dang-san-xuat') {
                rows = rows.filter(r => r.status === 'In Production' || r.status === 'In Progress' || r.status === 'Đang sản xuất');
            } else if (prState.activeTab === 'hoan-thanh') {
                rows = rows.filter(r => r.status === 'Completed' || r.status === 'Đã hoàn thành');
            }

            // Lọc theo Status dropdown
            if (prState.statusFilter && prState.statusFilter !== 'Tất cả') {
                rows = rows.filter(r => r.status === prState.statusFilter);
            }

            // Lọc theo Tiếp nhận dropdown
            const recFilter = prState.receiveFilter || prState.assignmentFilter;
            if (recFilter === 'Đã tiếp nhận' || recFilter === 'Đã phân công') {
                rows = rows.filter(r => r.assignedTo != null);
            } else if (recFilter === 'Chưa tiếp nhận' || recFilter === 'Chưa phân công') {
                rows = rows.filter(r => r.assignedTo == null);
            }

            // Lọc theo từ khóa tìm kiếm
            if (prState.searchKeyword && prState.searchKeyword.trim()) {
                const kw = prState.searchKeyword.trim().toLowerCase();
                rows = rows.filter(r =>
                    (r.requestCode && r.requestCode.toLowerCase().includes(kw)) ||
                    (r.orderNo && r.orderNo.toLowerCase().includes(kw)) ||
                    (r.productName && r.productName.toLowerCase().includes(kw)) ||
                    (r.productCode && r.productCode.toLowerCase().includes(kw)) ||
                    (r.managerName && r.managerName.toLowerCase().includes(kw)) ||
                    (r.managerEmployeeCode && r.managerEmployeeCode.toLowerCase().includes(kw)) ||
                    (r.assignedToName && r.assignedToName.toLowerCase().includes(kw)) ||
                    (r.assignedToEmployeeCode && r.assignedToEmployeeCode.toLowerCase().includes(kw))
                );
            }

            const allRows = state.productionRequests || [];
            const summaryStats = {
                total: allRows.length,
                pending: allRows.filter(r => r.status === 'Pending' || r.status === 'Chờ xử lý').length,
                inProduction: allRows.filter(r => r.status === 'In Production' || r.status === 'In Progress' || r.status === 'Đang sản xuất').length,
                completed: allRows.filter(r => r.status === 'Completed' || r.status === 'Đã hoàn thành').length,
                unreceived: allRows.filter(r => r.assignedTo == null).length,
                unassigned: allRows.filter(r => r.assignedTo == null).length
            };

            const columns = [
                { key: 'requestCode', label: 'Mã lệnh SX', width: '130px', isPrLink: true },
                { key: 'createdDate', label: 'Ngày lập', width: '150px' },
                { key: 'productCode', label: 'Mã SP', width: '90px' },
                { key: 'productName', label: 'Tên thành phẩm sản xuất' },
                { key: 'unit', label: 'ĐVT', width: '70px', align: 'center' },
                { key: 'quantityRequired', label: 'Số lượng SX', width: '110px', align: 'right', isNumber: true },
                { key: 'managerName', label: 'Người lập', width: '150px' },
                { key: 'assignedToName', label: 'Nhân viên tiếp nhận', width: '190px', isPrAssigned: true },
                { key: 'status', label: 'Trạng thái', width: '130px', align: 'center', isPrStatus: true },
                { key: 'actions', label: 'Thao tác', width: '110px', align: 'center', isPrActions: true }
            ];

            return {
                pageType: 'yeu-cau-san-xuat',
                tabs: [
                    { id: 'tat-ca', label: `Tất cả (${summaryStats.total})` },
                    { id: 'cho-xu-ly', label: `Chờ xử lý (${summaryStats.pending})` },
                    { id: 'dang-san-xuat', label: `Đang sản xuất (${summaryStats.inProduction})` },
                    { id: 'hoan-thanh', label: `Đã hoàn thành (${summaryStats.completed})` }
                ],
                activeTab: prState.activeTab,
                filters: [
                    {
                        id: 'pr-status-filter',
                        prefix: 'Trạng thái:',
                        value: prState.statusFilter,
                        options: ['Tất cả', 'Pending', 'In Production', 'Completed', 'Cancelled']
                    },
                    {
                        id: 'pr-receive-filter',
                        prefix: 'Tiếp nhận:',
                        value: prState.receiveFilter || prState.assignmentFilter || 'Tất cả',
                        options: ['Tất cả', 'Đã tiếp nhận', 'Chưa tiếp nhận']
                    }
                ],
                searchValue: prState.searchKeyword || '',
                searchKeyword: prState.searchKeyword || '',
                columns,
                rows,
                totalRecords: rows.length,
                rangeText: rows.length > 0 ? `1 - ${rows.length}` : '0 - 0',
                showAddButton: true,
                addButtonLabel: 'Lập yêu cầu sản xuất',
                summaryStats
            };
        },

        openCreatePrModal() {
            const nextIdx = (state.productionRequests?.length || 0) + 1;
            const code = 'LSX' + String(nextIdx).padStart(5, '0');

            state.productionRequestModal = {
                mode: 'create',
                item: {
                    requestId: 0,
                    requestCode: code,
                    status: 'Pending',
                    items: [] // Danh sách thành phẩm yêu cầu bắt đầu TRỐNG
                }
            };
        },

        addPrItemRow() {
            if (!state.productionRequestModal || !state.productionRequestModal.item) return;
            const item = state.productionRequestModal.item;
            if (!Array.isArray(item.items)) item.items = [];
            const nextId = (item.items.length > 0 ? Math.max(...item.items.map(i => i.id || 0)) : 0) + 1;

            // Thêm 1 dòng trống vào bảng (chưa chọn sản phẩm, mã & tên & ĐVT trống)
            item.items.push({
                id: nextId,
                productId: null,
                productCode: '',
                productName: '',
                unit: '',
                quantityRequired: 100
            });
        },

        removePrItemRow(rowId) {
            if (!state.productionRequestModal || !state.productionRequestModal.item) return;
            const item = state.productionRequestModal.item;
            if (!Array.isArray(item.items)) return;
            item.items = item.items.filter(i => i.id !== Number(rowId));
        },

        changePrItemProduct(rowId, productId) {
            if (!state.productionRequestModal || !state.productionRequestModal.item) return;
            const item = state.productionRequestModal.item;
            if (!Array.isArray(item.items)) return;
            const row = item.items.find(i => i.id === Number(rowId));
            if (!row) return;
            const prod = (vthhDatasets.products || []).find(p => (p.id || p.productId) == productId);
            if (prod) {
                row.productId = prod.id || prod.productId;
                row.productCode = prod.productCode || '';
                row.productName = prod.productName || '';
                row.unit = prod.unit || 'Hộp';
            } else {
                row.productId = null;
                row.productCode = '';
                row.productName = '';
                row.unit = '';
            }
        },

        changePrItemQty(rowId, qty) {
            if (!state.productionRequestModal || !state.productionRequestModal.item) return;
            const item = state.productionRequestModal.item;
            if (!Array.isArray(item.items)) return;
            const row = item.items.find(i => i.id === Number(rowId));
            if (!row) return;
            row.quantityRequired = Math.max(1, parseInt(qty, 10) || 1);
        },

        openEditPrModal(item) {
            if (item.status !== 'Pending') {
                throw new Error(`Lệnh sản xuất '${item.requestCode}' đang ở trạng thái '${item.status}', thông tin đã bị khóa không được chỉnh sửa. Chỉ lệnh ở trạng thái 'Pending' mới được chỉnh sửa.`);
            }
            const prod = (vthhDatasets.products || []).find(p => (p.id || p.productId) === item.productId);
            state.productionRequestModal = {
                mode: 'edit',
                item: {
                    ...item,
                    productCode: item.productCode || (prod ? prod.productCode : ''),
                    unit: item.unit || (prod ? prod.unit : 'Hộp'),
                    status: 'Pending'
                }
            };
        },

        openAssignPrModal(item) {
            if (item.status !== 'Pending') {
                throw new Error(`Lệnh sản xuất '${item.requestCode}' đang ở trạng thái '${item.status}', không thể thay đổi phân công. Chỉ lệnh ở trạng thái 'Pending' mới được phân công.`);
            }
            state.productionRequestModal = {
                mode: 'assign',
                item: { ...item }
            };
        },

        openDetailPrModal(item) {
            state.productionRequestModal = {
                mode: 'detail',
                item: { ...item }
            };
        },

        closePrModal() {
            state.productionRequestModal = null;
        },

        getPrModalData() {
            if (!state.productionRequestModal) return null;
            return {
                ...state.productionRequestModal,
                assignableUsers: state.assignableUsers || [],
                products: vthhDatasets.products || []
            };
        },

        // --- NHÀ CUNG CẤP (SUPPLIERS) CRUD ---
        async fetchSuppliersApi() {
            return fetchSuppliersApi();
        },

        async createSupplierApi(dto) {
            return createSupplierApi(dto);
        },

        async updateSupplierApi(id, dto) {
            return updateSupplierApi(id, dto);
        },

        async deleteSupplierApi(id) {
            return deleteSupplierApi(id);
        },

        setSupplierSearch(keyword) {
            if (!state.supplier) {
                state.supplier = { activeTab: 'tat-ca', searchKeyword: '', currentPage: 1, pageSize: 20 };
            }
            state.supplier.searchKeyword = keyword || '';
            state.supplier.currentPage = 1;
        },

        setSupplierPage(page) {
            if (!state.supplier) state.supplier = { activeTab: 'tat-ca', searchKeyword: '', currentPage: 1, pageSize: 20 };
            state.supplier.currentPage = page || 1;
        },

        openCreateSupplierModal() {
            state.supplierModal = {
                mode: 'create',
                item: {
                    supplierId: 0,
                    code: 'Tự động sinh (NCC...)',
                    supplierName: '',
                    phone: '',
                    address: '',
                    mail: ''
                }
            };
        },

        openEditSupplierModal(item) {
            state.supplierModal = {
                mode: 'edit',
                item: { ...item }
            };
        },

        openDetailSupplierModal(item) {
            state.supplierModal = {
                mode: 'detail',
                item: { ...item }
            };
        },

        closeSupplierModal() {
            state.supplierModal = null;
        },

        getSupplierModalData() {
            return state.supplierModal;
        },

        getSupplierDataGridConfig() {
            const supState = state.supplier || {
                activeTab: 'tat-ca',
                searchKeyword: '',
                currentPage: 1,
                pageSize: 20
            };

            let items = [...(supplierList || [])];

            const kw = (supState.searchKeyword || '').toLowerCase().trim();
            if (kw) {
                items = items.filter(s =>
                    (s.code && s.code.toLowerCase().includes(kw)) ||
                    (s.supplierName && s.supplierName.toLowerCase().includes(kw)) ||
                    (s.name && s.name.toLowerCase().includes(kw)) ||
                    (s.phone && s.phone.toLowerCase().includes(kw)) ||
                    (s.mail && s.mail.toLowerCase().includes(kw)) ||
                    (s.address && s.address.toLowerCase().includes(kw))
                );
            }

            const totalRows = items.length;
            const pageSize = supState.pageSize || 20;
            const currentPage = supState.currentPage || 1;
            const totalPages = Math.max(1, Math.ceil(totalRows / pageSize));
            const validPage = Math.min(Math.max(1, currentPage), totalPages);
            const startIndex = (validPage - 1) * pageSize;
            const paginatedRows = items.slice(startIndex, startIndex + pageSize);

            const columns = [
                { key: 'code', label: 'Mã nhà cung cấp', width: '140px', isSupplierCode: true },
                { key: 'supplierName', label: 'Tên nhà cung cấp', minWidth: '240px' },
                { key: 'importCostFormatted', label: 'Tỷ lệ phí nhập (%)', width: '150px', align: 'center' },
                { key: 'phone', label: 'Số điện thoại', width: '140px' },
                { key: 'mail', label: 'Email', width: '200px' },
                { key: 'address', label: 'Địa chỉ', minWidth: '240px' },
                { key: 'actions', label: 'Thao tác', width: '130px', align: 'center', isSupplierActions: true }
            ];

            return {
                pageType: 'nha-cung-cap',
                tabs: [
                    { id: 'tat-ca', label: `Tất cả nhà cung cấp (${supplierList?.length || 0})` }
                ],
                activeTab: supState.activeTab,
                filters: [],
                showAddButton: true,
                addButtonLabel: 'Thêm nhà cung cấp',
                searchKeyword: supState.searchKeyword,
                searchValue: supState.searchKeyword || '',
                searchPlaceholder: 'Tìm theo mã, tên, SĐT, email...',
                columns,
                rows: paginatedRows,
                totalRecords: totalRows,
                rangeText: totalRows > 0 ? `${startIndex + 1} - ${Math.min(startIndex + pageSize, totalRows)}` : '0 - 0',
                hasMorePages: validPage < totalPages,
                pagination: {
                    totalRows,
                    currentPage: validPage,
                    totalPages,
                    pageSize,
                    startIndex: totalRows > 0 ? startIndex + 1 : 0,
                    endIndex: Math.min(startIndex + pageSize, totalRows)
                }
            };
        },

        // --- BÁO CÁO NHẬP - XUẤT - TỒN & LƯU KHO ---
        async fetchReportDataApi(params) {
            return fetchReportDataApi(params);
        },

        async fetchStockCardApi(itemId, itemType) {
            return fetchStockCardApi(itemId, itemType);
        },

        closeStockCardModal() {
            state.report.stockCardModal = null;
        },

        setReportPeriodType(type) {
            if (!state.report) state.report = { periodType: 'month', year: 2026, periodValue: 11, itemType: 'all', searchKeyword: '' };
            state.report.periodType = type;
            if (type === 'month') {
                state.report.periodValue = state.report.periodValue || 11;
            } else if (type === 'quarter') {
                state.report.periodValue = 4;
            }
        },

        setReportPeriodValue(val) {
            if (!state.report) state.report = { periodType: 'month', year: 2026, periodValue: 11, itemType: 'all', searchKeyword: '' };
            state.report.periodValue = Number(val) || 1;
        },

        setReportYear(yr) {
            if (!state.report) state.report = { periodType: 'month', year: 2026, periodValue: 11, itemType: 'all', searchKeyword: '' };
            state.report.year = Number(yr) || 2026;
        },

        setReportItemType(type) {
            if (!state.report) state.report = { periodType: 'month', year: 2026, periodValue: 11, itemType: 'all', searchKeyword: '' };
            state.report.itemType = type;
        },

        setReportSearch(kw) {
            if (!state.report) state.report = { periodType: 'month', year: 2026, periodValue: 11, itemType: 'all', searchKeyword: '' };
            state.report.searchKeyword = kw || '';
        },

        getReportDataConfig() {
            const rState = state.report || { periodType: 'month', year: 2026, periodValue: 11, itemType: 'all', searchKeyword: '' };
            const rData = rState.reportData;

            return {
                period: rData?.period || {
                    type: rState.periodType,
                    year: rState.year,
                    value: rState.periodValue,
                    title: rState.periodType === 'month' ? `Tháng ${rState.periodValue}/${rState.year}` : (rState.periodType === 'quarter' ? `Quý ${rState.periodValue}/${rState.year}` : `Năm ${rState.year}`),
                    startDate: '',
                    endDate: ''
                },
                summary: rData?.summary || {
                    totalItems: 0,
                    totalStockQty: 0,
                    totalInQty: 0,
                    totalOutQty: 0,
                    totalWipQty: 0,
                    totalStockValue: 0,
                    lowStockCount: 0,
                    ingredientCount: 0,
                    productCount: 0
                },
                chartData: rData?.chartData || [],
                items: rData?.items || [],
                availableYears: rData?.availableYears || [2026, 2025, 2024],
                filters: {
                    periodType: rState.periodType,
                    year: rState.year,
                    periodValue: rState.periodValue,
                    itemType: rState.itemType,
                    search: rState.searchKeyword
                },
                isLoading: rState.isLoading || false,
                stockCardModal: rState.stockCardModal
            };
        }
    };
})();

