/**
 * Areas/Warehouse/Controllers/warehouseController.js
 * ---------------------------------------------------------
 * [CONTROLLER LAYER - MVC]
 * Điều phối luồng dữ liệu giữa WarehouseModel và WarehouseComponents (View).
 * Xử lý sự kiện người dùng:
 * - Chuyển đổi mục trên Sidebar (Bàn làm việc, Tổng quan, Nhập kho, Xuất kho; các mục khác để trống maincontent)
 * - Đóng/mở menu con "Hoạt động kho" & thu gọn Sidebar mượt mà
 * - Mở / đóng / tương tác trên Phiếu nhập kho (PNK00005) & Phiếu xuất kho (PXK00002):
 *   + Chọn loại phiếu (Nhập kho mua hàng / Hàng bán bị trả lại / Nhập khác / Xuất kho bán hàng...)
 *   + Thêm dòng hàng hóa, xóa từng dòng, xóa hết dòng và tự động tính lại tổng SL
 * - Mở / đóng / áp dụng hộp thoại "Chọn tham số" (Số ngày trước hạn)
 */

const WarehouseController = (function (Model, View) {
    let headerMount = null;
    let sidebarMount = null;
    let mainMount = null;

    function escapeHtml(str) {
        return String(str || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    async function init() {
        headerMount = document.getElementById('wh-header-mount');
        sidebarMount = document.getElementById('wh-sidebar-mount');
        mainMount = document.getElementById('wh-main-mount');

        if (!headerMount || !sidebarMount || !mainMount) {
            return;
        }

        renderAll();
        bindEvents();

        // Nạp dữ liệu thật ban đầu từ Supabase qua API
        if (Model.initData) {
            try {
                await Model.initData();
                renderHeaderOnly();
                renderMainOnly();
            } catch (err) {
                console.error('Lỗi khởi tạo dữ liệu kho:', err);
            }
        }
    }

    function getViewPayloads() {
        const state = Model.getState();
        return {
            workspaceData: Model.getWorkspaceData(),
            cardsConfig: Model.getDashboardCardsConfig(),
            inboundConfig: Model.getInboundGridConfig(),
            outboundConfig: Model.getOutboundGridConfig(),
            vthhConfig: Model.getVthhGridConfig ? Model.getVthhGridConfig() : null,
            vthhModalData: Model.getVthhModalData ? Model.getVthhModalData() : null,
            prConfig: Model.getProductionRequestGridConfig ? Model.getProductionRequestGridConfig() : null,
            prModalData: Model.getPrModalData ? Model.getPrModalData() : null,
            supplierConfig: Model.getSupplierDataGridConfig ? Model.getSupplierDataGridConfig() : null,
            supplierModalData: Model.getSupplierModalData ? Model.getSupplierModalData() : null,
            reportConfig: Model.getReportDataConfig ? Model.getReportDataConfig() : null,
            currentUser: Model.getCurrentUser ? Model.getCurrentUser() : state.currentUser,
            warehouseTasks: Model.getWarehouseKeeperTasks ? Model.getWarehouseKeeperTasks() : [],
            activeVoucherData: state.activeVoucherModal
                ? Model.getVoucherData(state.activeVoucherModal)
                : null
        };
    }

    function renderAll() {
        const state = Model.getState();
        const menuItems = Model.getSidebarMenu();
        headerMount.innerHTML = View.renderHeader(state);
        sidebarMount.innerHTML = View.renderSidebar(menuItems, state);
        renderMainOnly();
    }

    function renderHeaderOnly() {
        const state = Model.getState();
        if (headerMount) {
            headerMount.innerHTML = View.renderHeader(state);
        }
    }

    function syncSidebarDomState() {
        const state = Model.getState();
        const menuItems = Model.getSidebarMenu();
        const sidebarEl = document.getElementById('wh-sidebar');

        if (!sidebarEl) {
            sidebarMount.innerHTML = View.renderSidebar(menuItems, state);
            return;
        }

        sidebarEl.classList.toggle('collapsed', Boolean(state.sidebarCollapsed));

        menuItems.forEach(function (item) {
            const hasChildren = Array.isArray(item.children) && item.children.length > 0;
            const navBtn = sidebarEl.querySelector(`.wh-nav-item[data-menu-id="${item.id}"]`);
            if (navBtn) {
                if (!hasChildren) {
                    navBtn.classList.toggle('active', item.id === state.activeMenuId);
                }
                navBtn.classList.toggle('expanded', Boolean(item.expanded));
            }

            if (hasChildren) {
                const submenuEl = sidebarEl.querySelector(`[data-submenu-for="${item.id}"]`);
                if (submenuEl) {
                    submenuEl.classList.toggle('open', Boolean(item.expanded));
                }
                item.children.forEach(function (child) {
                    const childBtn = sidebarEl.querySelector(`.wh-submenu-item[data-menu-id="${child.id}"]`);
                    if (childBtn) {
                        childBtn.classList.toggle('active', child.id === state.activeMenuId);
                    }
                });
            }
        });
    }

    function renderMainOnly() {
        const state = Model.getState();

        // Bảo toàn vị trí cuộn của trang chính, bảng dữ liệu và modal để tránh giật/reset giao diện
        const mainScrollTop = mainMount ? mainMount.scrollTop : 0;
        const mainScrollLeft = mainMount ? mainMount.scrollLeft : 0;

        const tableScrollEl = mainMount ? mainMount.querySelector('.wh-dg-table-scroll') : null;
        const tableScrollTop = tableScrollEl ? tableScrollEl.scrollTop : 0;
        const tableScrollLeft = tableScrollEl ? tableScrollEl.scrollLeft : 0;

        const reportScrollEl = mainMount ? mainMount.querySelector('.wh-report-table-wrapper') : null;
        const reportScrollTop = reportScrollEl ? reportScrollEl.scrollTop : 0;
        const reportScrollLeft = reportScrollEl ? reportScrollEl.scrollLeft : 0;

        const modalBodyEl = mainMount ? mainMount.querySelector('.wh-voucher-body') : null;
        const modalScrollTop = modalBodyEl ? modalBodyEl.scrollTop : 0;

        mainMount.innerHTML = View.renderMainContent(state, getViewPayloads());

        // Phục hồi chính xác vị trí cuộn ngay lập tức sau khi gán innerHTML
        if (mainMount) {
            mainMount.scrollTop = mainScrollTop;
            mainMount.scrollLeft = mainScrollLeft;
        }
        const newTableScrollEl = mainMount ? mainMount.querySelector('.wh-dg-table-scroll') : null;
        if (newTableScrollEl) {
            newTableScrollEl.scrollTop = tableScrollTop;
            newTableScrollEl.scrollLeft = tableScrollLeft;
        }
        const newReportScrollEl = mainMount ? mainMount.querySelector('.wh-report-table-wrapper') : null;
        if (newReportScrollEl) {
            newReportScrollEl.scrollTop = reportScrollTop;
            newReportScrollEl.scrollLeft = reportScrollLeft;
        }
        const newModalBodyEl = mainMount ? mainMount.querySelector('.wh-voucher-body') : null;
        if (newModalBodyEl) {
            newModalBodyEl.scrollTop = modalScrollTop;
        }
    }

    // Cập nhật nội dung Modal phiếu nhập/xuất kho mượt mà, không giật màn hình hoặc reset DOM
    function updateVoucherModalDom() {
        const overlay = document.getElementById('wh-voucher-overlay');
        if (!overlay) {
            renderMainOnly();
            return;
        }
        const state = Model.getState();
        const voucherData = Model.getVoucherData(state.activeVoucherModal || 'inbound');
        if (!voucherData) {
            renderMainOnly();
            return;
        }

        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = View.renderStockVoucherModal(voucherData);
        const newOverlay = tempDiv.firstElementChild;
        if (!newOverlay) {
            renderMainOnly();
            return;
        }

        const oldBody = overlay.querySelector('.wh-voucher-body');
        const oldScrollTop = oldBody ? oldBody.scrollTop : 0;

        const oldWindow = overlay.querySelector('.wh-voucher-window');
        const newWindow = newOverlay.querySelector('.wh-voucher-window');
        if (oldWindow && newWindow) {
            oldWindow.innerHTML = newWindow.innerHTML;
            const newBody = overlay.querySelector('.wh-voucher-body');
            if (newBody) newBody.scrollTop = oldScrollTop;
        } else {
            overlay.replaceWith(newOverlay);
        }
    }

    function bindEvents() {
        document.addEventListener('click', async function (e) {
            // Tự động đóng dropdown chọn thành phẩm nếu click ra ngoài picker
            if (!e.target.closest('#pr-product-picker-dropdown') && !e.target.closest('#pr-product-picker-trigger') && !e.target.closest('.wh-pr-searchable-select')) {
                const prDropdown = document.getElementById('pr-product-picker-dropdown');
                if (prDropdown && prDropdown.style.display !== 'none') {
                    prDropdown.style.display = 'none';
                }
                document.querySelectorAll('.wh-pr-dropdown-menu').forEach(el => el.style.display = 'none');
                document.querySelectorAll('.wh-pr-select-trigger').forEach(el => el.classList.remove('is-open'));
            }

            // Tự động đóng user dropdown nếu click ra ngoài container
            if (!e.target.closest('.wh-user-profile-menu-container')) {
                if (Model.getState().isUserDropdownOpen) {
                    Model.closeUserDropdown();
                    renderHeaderOnly();
                }
            }

            const actionEl = e.target.closest('[data-action]');
            if (!actionEl) {
                return;
            }

            const action = actionEl.getAttribute('data-action');

            switch (action) {
                case 'toggle-user-dropdown': {
                    e.preventDefault();
                    e.stopPropagation();
                    Model.toggleUserDropdown();
                    renderHeaderOnly();
                    break;
                }

                case 'view-employee-profile': {
                    e.preventDefault();
                    Model.navigateToEmployeeProfile();
                    renderHeaderOnly();
                    renderMainOnly();
                    syncSidebarDomState();
                    break;
                }

                case 'back-from-profile': {
                    e.preventDefault();
                    Model.backFromEmployeeProfile();
                    renderMainOnly();
                    syncSidebarDomState();
                    break;
                }

                case 'logout-user': {
                    e.preventDefault();
                    Model.closeUserDropdown();
                    renderHeaderOnly();
                    window.location.href = '/Warehouse/Account/Logout';
                    break;
                }

                case 'edit-profile-toast': {
                    e.preventDefault();
                    if (window.WhNotification) {
                        WhNotification.toast('Chức năng cập nhật hồ sơ: Vui lòng liên hệ Phòng Quản lý Kho vận & Cung ứng hoặc Quản trị hệ thống để cập nhật thông tin.', 'info');
                    } else {
                        alert('Chức năng cập nhật hồ sơ: Vui lòng liên hệ Phòng Quản lý Kho vận & Cung ứng hoặc Quản trị hệ thống để cập nhật thông tin.');
                    }
                    break;
                }

                case 'go-overview': {
                    e.preventDefault();
                    Model.setActiveMenu('tong-quan');
                    syncSidebarDomState();
                    renderMainOnly();
                    break;
                }

                case 'select-menu': {
                    e.preventDefault();
                    const menuId = actionEl.getAttribute('data-menu-id');
                    if (menuId) {
                        Model.setActiveMenu(menuId);
                        syncSidebarDomState();
                        renderMainOnly();
                        if (menuId === 'danh-muc-vthh') {
                            await Model.loadProducts();
                            await Model.loadIngredients();
                            renderMainOnly();
                        } else if (menuId === 'xuat-kho') {
                            await Promise.all([
                                Model.fetchMaterialReceiptsApi(),
                                Model.fetchProductExportsApi()
                            ]);
                            renderMainOnly();
                        } else if (menuId === 'yeu-cau-san-xuat') {
                            await Promise.all([
                                Model.fetchProductionRequestsApi(),
                                Model.fetchAssignableUsersApi(),
                                Model.loadProducts ? Model.loadProducts() : null
                            ]);
                            renderMainOnly();
                        } else if (menuId === 'nhap-kho') {
                            await Model.fetchProductReceiptsApi();
                            await Model.fetchImportReceiptsApi();
                            renderMainOnly();
                        } else if (menuId === 'nha-cung-cap') {
                            await Model.fetchSuppliersApi();
                            renderMainOnly();
                        } else if (menuId === 'bao-cao') {
                            await Model.fetchReportDataApi();
                            renderMainOnly();
                        }
                    }
                    break;
                }

                case 'toggle-submenu': {
                    e.preventDefault();
                    const menuId = actionEl.getAttribute('data-menu-id');
                    if (menuId) {
                        if (Model.getState().sidebarCollapsed) {
                            Model.toggleSidebarCollapse();
                        }
                        Model.toggleSubmenu(menuId);
                        syncSidebarDomState();
                    }
                    break;
                }

                case 'collapse-sidebar': {
                    e.preventDefault();
                    Model.toggleSidebarCollapse();
                    syncSidebarDomState();
                    break;
                }

                case 'switch-op-tab': {
                    e.preventDefault();
                    const pageType = actionEl.getAttribute('data-page-type');
                    const tabId = actionEl.getAttribute('data-tab-id');
                    if (pageType && tabId) {
                        Model.setOperationTab(pageType, tabId);
                        renderMainOnly();
                        if (pageType === 'danh-muc-vthh') {
                            if (tabId === 'san-pham') {
                                await Model.loadProducts();
                            } else {
                                await Model.loadIngredients();
                            }
                            renderMainOnly();
                        } else if (pageType === 'xuat-kho') {
                            if (tabId === 'lenh-xuat-kho') {
                                await Model.fetchMaterialReceiptsApi();
                            } else if (tabId === 'xuat-kho-tab') {
                                await Model.fetchProductExportsApi();
                            }
                            renderMainOnly();
                        } else if (pageType === 'nhap-kho') {
                            if (tabId === 'lenh-nhap-kho') {
                                await Model.fetchProductReceiptsApi();
                            } else {
                                await Model.fetchImportReceiptsApi();
                            }
                            renderMainOnly();
                        }
                    }
                    break;
                }

                // --- CÁC SỰ KIỆN TRÊN ĐƠN HÀNG, LỆNH KHO & PHIẾU KHO ---
                case 'open-voucher-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const vType = actionEl.getAttribute('data-voucher-type') || 'inbound';
                    const docNo = actionEl.getAttribute('data-doc-no') || '';
                    const receiptId = actionEl.getAttribute('data-receipt-id') || docNo;
                    // Nếu đang từ Đơn mua hàng bấm "Lập lệnh nhập kho" -> chuyển sang tab Nhập kho thành phẩm và mở chi tiết
                    if (vType === 'inbound-command') {
                        Model.setOperationTab('nhap-kho', 'lenh-nhap-kho');
                    } else if (vType === 'outbound-command') {
                        Model.setOperationTab('xuat-kho', 'lenh-xuat-kho');
                    } else if (vType === 'inbound') {
                        Model.setOperationTab('nhap-kho', 'nhap-kho-tab');
                    } else if (vType === 'outbound') {
                        Model.setOperationTab('xuat-kho', 'xuat-kho-tab');
                    }
                    const mode = (docNo || receiptId) ? 'view' : 'create';
                    if (vType === 'inbound' && Model.fetchCurrentUserApi) {
                        const curUser = Model.getCurrentUser ? Model.getCurrentUser() : null;
                        if (!curUser || !curUser.userId) {
                            await Model.fetchCurrentUserApi();
                        }
                    }
                    Model.openVoucherModal(vType, receiptId || docNo, mode);
                    renderMainOnly();
                    if (vType === 'inbound' && mode === 'view' && (receiptId || docNo)) {
                        await Model.fetchImportReceiptDetailApi(receiptId || docNo);
                        renderMainOnly();
                    } else if (vType === 'outbound-command' && mode === 'view' && (receiptId || docNo)) {
                        await Model.fetchMaterialReceiptDetailApi(receiptId || docNo);
                        renderMainOnly();
                    } else if (vType === 'inbound-command' && mode === 'view' && (receiptId || docNo)) {
                        await Model.fetchProductReceiptDetailApi(receiptId || docNo);
                        renderMainOnly();
                    } else if (vType === 'outbound' && (receiptId || docNo)) {
                        await Model.fetchProductExportDetailApi(receiptId || docNo);
                        renderMainOnly();
                    }
                    break;
                }

                case 'close-voucher-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    Model.closeVoucherModal();
                    renderMainOnly();
                    break;
                }

                // Thao tác trên bảng tạo mới Phiếu nhập NVL
                case 'add-inbound-item-row': {
                    e.preventDefault();
                    const addRes = Model.addInboundNewItemRow();
                    if (addRes && addRes.success === false && addRes.message) {
                        WhNotification.alert(addRes.message, { type: 'warning', title: 'Không thể thêm dòng' });
                        return;
                    }
                    updateVoucherModalDom();
                    break;
                }

                case 'clear-inbound-item-rows': {
                    e.preventDefault();
                    const confirmed = await WhNotification.confirm('Bạn có chắc chắn muốn xóa tất cả các dòng nguyên vật liệu trong phiếu này không?', {
                        title: 'Xóa danh sách vật tư',
                        confirmText: 'Xóa tất cả',
                        cancelText: 'Hủy bỏ',
                        type: 'warning'
                    });
                    if (confirmed) {
                        Model.clearInboundNewItemRows();
                        updateVoucherModalDom();
                        WhNotification.toast('Đã làm trống danh sách vật tư', 'info');
                    }
                    break;
                }

                case 'delete-inbound-item-row': {
                    e.preventDefault();
                    const rowId = actionEl.getAttribute('data-row-id');
                    if (rowId) {
                        Model.deleteInboundNewItemRow(rowId);
                        updateVoucherModalDom();
                    }
                    break;
                }

                case 'save-inbound-voucher': {
                    e.preventDefault();
                    const origHtml = actionEl.innerHTML;
                    try {
                        actionEl.disabled = true;
                        actionEl.innerHTML = '<span>Đang lưu...</span>';
                        const res = await Model.saveInboundReceipt(false);
                        if (!res.success) {
                            WhNotification.alert(res.message, { type: 'warning', title: 'Thông tin chưa hợp lệ' });
                        } else {
                            WhNotification.toast('Lập phiếu và nhập kho ' + res.voucherNo + ' thành công! (Trạng thái: Approve)', 'success');
                            renderMainOnly();
                        }
                    } finally {
                        actionEl.disabled = false;
                        actionEl.innerHTML = origHtml;
                    }
                    break;
                }

                case 'save-draft-inbound-voucher': {
                    e.preventDefault();
                    const origHtml = actionEl.innerHTML;
                    try {
                        actionEl.disabled = true;
                        actionEl.innerHTML = '<span>Đang lưu nháp...</span>';
                        const res = await Model.saveInboundReceipt(true);
                        if (!res.success) {
                            WhNotification.alert(res.message, { type: 'warning', title: 'Thông tin chưa hợp lệ' });
                        } else {
                            WhNotification.toast('Đã lưu nháp phiếu nhập kho ' + res.voucherNo + ' trên giao diện (chưa lưu CSDL)!', 'info');
                            renderMainOnly();
                        }
                    } finally {
                        actionEl.disabled = false;
                        actionEl.innerHTML = origHtml;
                    }
                    break;
                }

                case 'remove-ref-tag': {
                    e.preventDefault();
                    const docType = actionEl.getAttribute('data-doc-type');
                    const tagText = actionEl.getAttribute('data-tag');
                    if (docType && tagText) {
                        Model.removeReferenceTag(docType, tagText);
                        renderMainOnly();
                    }
                    break;
                }

                case 'toggle-cmd-row-full': {
                    e.stopPropagation();
                    const docType = actionEl.getAttribute('data-doc-type');
                    const rowId = actionEl.getAttribute('data-row-id');
                    if (docType && rowId) {
                        const updated = Model.toggleCommandRowFull(docType, rowId, actionEl.checked);
                        if (updated) {
                            // Cập nhật DOM trực tiếp tại dòng (in-place), không re-render toàn bộ giao diện làm reset cuộn
                            const tr = actionEl.closest('tr');
                            if (tr) {
                                const qtyCell = tr.querySelector('[data-cell="actual-qty"]');
                                if (qtyCell) {
                                    qtyCell.textContent = updated.actualQty > 0
                                        ? updated.actualQty.toLocaleString('vi-VN')
                                        : '0';
                                }
                            }

                            // Cập nhật tổng số lượng thực nhập ở dòng tfoot của bảng modal
                            const modalTable = actionEl.closest('table') || (mainMount ? mainMount.querySelector('.wh-vtable') : null);
                            if (modalTable) {
                                const totalCell = modalTable.querySelector('[data-cell="total-actual-qty"]');
                                if (totalCell) {
                                    totalCell.textContent = updated.totalActualQty > 0
                                        ? updated.totalActualQty.toLocaleString('vi-VN')
                                        : '0';
                                }
                            }
                        }
                    }
                    break;
                }

                // Xử lý nút Duyệt / Từ chối trên Lệnh xuất nguyên vật liệu (Material Issue)
                case 'approve-material-receipt':
                case 'approve-material-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const orderNo = actionEl.getAttribute('data-order-no') || receiptId;
                    const canApprove = actionEl.getAttribute('data-can-approve') !== 'false';
                    const blockReason = actionEl.getAttribute('data-block-reason');

                    if (!receiptId) return;

                    // Kiểm tra điều kiện tồn kho & hạn sử dụng
                    if (!canApprove) {
                        await WhNotification.alert(
                            blockReason || 'Phiếu yêu cầu xuất vật tư này không đủ điều kiện duyệt do thiếu tồn kho hoặc nguyên vật liệu có hạn sử dụng chỉ còn ≤ 1 ngày.',
                            {
                                type: 'error',
                                title: 'Không thể duyệt phiếu xuất vật tư',
                                confirmText: 'Đã hiểu'
                            }
                        );
                        return;
                    }

                    const confirmed = await WhNotification.confirm(
                        `Bạn có chắc chắn muốn duyệt xuất kho cho phiếu "${orderNo}" không?\n\nThao tác này sẽ trừ tồn kho nguyên vật liệu chính và chuyển vật tư vào tồn kho xưởng (WIP).`,
                        {
                            title: 'Xác nhận duyệt xuất nguyên vật liệu',
                            confirmText: 'Duyệt xuất kho',
                            cancelText: 'Hủy bỏ',
                            type: 'info'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang duyệt...';
                        try {
                            const res = await Model.approveMaterialReceipt(receiptId);
                            WhNotification.toast(res.message || `Duyệt cấp phát vật tư ${orderNo} thành công!`, 'success');
                            if (action === 'approve-material-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchMaterialReceiptsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi duyệt xuất kho!', {
                                type: 'error',
                                title: 'Không thể duyệt xuất kho'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                case 'reject-material-receipt':
                case 'reject-material-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const orderNo = actionEl.getAttribute('data-order-no') || receiptId;
                    if (!receiptId) return;

                    const reason = await WhNotification.prompt(
                        `Nhập lý do từ chối cấp phát vật tư cho phiếu "${orderNo}":`,
                        'Thiếu hàng thực tế hoặc chất lượng không đạt',
                        {
                            title: 'Từ chối cấp phát vật tư',
                            confirmText: 'Từ chối cấp phát',
                            cancelText: 'Hủy bỏ',
                            type: 'warning'
                        }
                    );

                    if (reason !== null && reason.trim()) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang xử lý...';
                        try {
                            const res = await Model.rejectMaterialReceipt(receiptId, reason.trim());
                            WhNotification.toast(res.message || `Đã từ chối cấp phát vật tư ${orderNo}!`, 'warning');
                            if (action === 'reject-material-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchMaterialReceiptsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi từ chối cấp phát!', {
                                type: 'error',
                                title: 'Lỗi xử lý'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xử lý nút Duyệt / Từ chối Xuất kho thành phẩm (Product Export)
                case 'approve-product-export':
                case 'approve-product-export-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const voucherNo = actionEl.getAttribute('data-voucher-no') || receiptId;
                    const canApprove = actionEl.getAttribute('data-can-approve') !== 'false';
                    const blockReason = actionEl.getAttribute('data-block-reason');

                    if (!receiptId) return;

                    // Kiểm tra điều kiện tồn kho
                    if (!canApprove) {
                        await WhNotification.alert(
                            blockReason || 'Phiếu xuất kho thành phẩm này không đủ điều kiện duyệt do có sản phẩm vượt quá số lượng tồn kho hiện tại.',
                            {
                                type: 'error',
                                title: 'Không thể duyệt xuất kho',
                                confirmText: 'Đã hiểu'
                            }
                        );
                        return;
                    }

                    const confirmed = await WhNotification.confirm(
                        `Bạn có chắc chắn muốn duyệt xuất kho cho phiếu "${voucherNo}" không?\n\nThao tác này sẽ trừ số lượng tồn kho thành phẩm trong cơ sở dữ liệu.`,
                        {
                            title: 'Xác nhận duyệt xuất kho thành phẩm',
                            confirmText: 'Duyệt xuất kho',
                            cancelText: 'Hủy bỏ',
                            type: 'info'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang duyệt...';
                        try {
                            const res = await Model.approveProductExport(receiptId);
                            WhNotification.toast(res.message || `Duyệt xuất kho thành phẩm ${voucherNo} thành công!`, 'success');
                            if (action === 'approve-product-export-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchProductExportsApi();
                            if (Model.loadProducts) await Model.loadProducts();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi duyệt xuất kho!', {
                                type: 'error',
                                title: 'Không thể duyệt xuất kho'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                case 'reject-product-export':
                case 'reject-product-export-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const voucherNo = actionEl.getAttribute('data-voucher-no') || receiptId;
                    if (!receiptId) return;

                    const reason = await WhNotification.prompt(
                        `Nhập lý do từ chối xuất kho cho phiếu "${voucherNo}":`,
                        'Khách hàng hủy đơn / Không đủ điều kiện xuất hàng',
                        {
                            title: 'Từ chối xuất kho thành phẩm',
                            confirmText: 'Từ chối xuất kho',
                            cancelText: 'Hủy bỏ',
                            type: 'warning'
                        }
                    );

                    if (reason !== null && reason.trim()) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang xử lý...';
                        try {
                            const res = await Model.rejectProductExport(receiptId, reason.trim());
                            WhNotification.toast(res.message || `Đã từ chối xuất kho thành phẩm ${voucherNo}!`, 'warning');
                            if (action === 'reject-product-export-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchProductExportsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi từ chối xuất kho!', {
                                type: 'error',
                                title: 'Không thể từ chối xuất kho'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xử lý nút Duyệt / Từ chối Nhập kho thành phẩm (Product Receipt)
                case 'approve-product-receipt':
                case 'approve-product-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const orderNo = actionEl.getAttribute('data-order-no') || receiptId;
                    if (!receiptId) return;

                    const confirmed = await WhNotification.confirm(
                        `Bạn có chắc chắn muốn duyệt nhập kho thành phẩm cho lệnh "${orderNo}" không?\n\nThao tác này sẽ cộng hàng vào tồn kho chính và tự động kích hoạt hạn sử dụng sản phẩm.`,
                        {
                            title: 'Xác nhận duyệt nhập kho thành phẩm',
                            confirmText: 'Duyệt nhập kho',
                            cancelText: 'Hủy bỏ',
                            type: 'info'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang duyệt...';
                        try {
                            const res = await Model.approveProductReceipt(receiptId);
                            WhNotification.toast(res.message || `Duyệt nhập kho thành phẩm ${orderNo} thành công!`, 'success');
                            if (action === 'approve-product-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchProductReceiptsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi duyệt nhập kho thành phẩm!', {
                                type: 'error',
                                title: 'Không thể duyệt nhập kho'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                case 'reject-product-receipt':
                case 'reject-product-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const orderNo = actionEl.getAttribute('data-order-no') || receiptId;
                    if (!receiptId) return;

                    const reason = await WhNotification.prompt(
                        `Nhập lý do từ chối nhận thành phẩm cho lệnh "${orderNo}":`,
                        'Quy cách bao bì hoặc tiêu chuẩn chất lượng chưa đạt',
                        {
                            title: 'Từ chối nhận thành phẩm',
                            confirmText: 'Từ chối',
                            cancelText: 'Hủy bỏ',
                            type: 'warning'
                        }
                    );

                    if (reason !== null && reason.trim()) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang xử lý...';
                        try {
                            const res = await Model.rejectProductReceipt(receiptId, reason.trim());
                            WhNotification.toast(res.message || `Đã từ chối nhận thành phẩm ${orderNo}!`, 'warning');
                            if (action === 'reject-product-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchProductReceiptsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi từ chối nhận thành phẩm!', {
                                type: 'error',
                                title: 'Lỗi xử lý'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xử lý nút Nhập / Không nhập kho nguyên vật liệu (Import Receipt)
                case 'approve-import-receipt':
                case 'approve-import-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const voucherNo = actionEl.getAttribute('data-voucher-no') || receiptId;
                    if (!receiptId) return;

                    const confirmed = await WhNotification.confirm(
                        `Bạn có chắc chắn muốn nhập kho nguyên vật liệu cho phiếu <strong>"${voucherNo}"</strong> không?<br/><br/>Hệ thống sẽ lưu phiếu vào CSDL với trạng thái <strong>Approve</strong>, tự động cộng tồn kho nguyên vật liệu và cập nhật hạn sử dụng.`,
                        {
                            title: 'Xác nhận nhập kho nguyên vật liệu',
                            confirmText: 'Nhập',
                            cancelText: 'Hủy bỏ',
                            type: 'info'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang xử lý...';
                        try {
                            const res = await Model.approveImportReceiptApi(receiptId);
                            WhNotification.toast(res.message || `Nhập kho phiếu ${voucherNo} thành công (Trạng thái: Approve)!`, 'success');
                            if (action === 'approve-import-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchImportReceiptsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi nhập kho nguyên vật liệu!', {
                                type: 'error',
                                title: 'Không thể nhập kho'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                case 'reject-import-receipt':
                case 'reject-import-modal': {
                    e.preventDefault();
                    e.stopPropagation();
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const voucherNo = actionEl.getAttribute('data-voucher-no') || receiptId;
                    if (!receiptId) return;

                    const reason = await WhNotification.prompt(
                        `Nhập lý do không nhập nguyên vật liệu cho phiếu <strong>"${voucherNo}"</strong>:<br/><span style="font-size:12px;color:#64748b;">(Hệ thống sẽ lưu phiếu vào CSDL với trạng thái <strong>Reject</strong>)</span>`,
                        'Hàng không đạt kiểm nghiệm QC hoặc sai quy cách giao nhận',
                        {
                            title: 'Xác nhận không nhập nguyên vật liệu',
                            confirmText: 'Không nhập',
                            cancelText: 'Hủy bỏ',
                            type: 'warning'
                        }
                    );

                    if (reason !== null && reason.trim()) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang xử lý...';
                        try {
                            const res = await Model.rejectImportReceiptApi(receiptId, reason.trim());
                            WhNotification.toast(res.message || `Đã từ chối nhập kho phiếu ${voucherNo} (Trạng thái: Reject)!`, 'info');
                            if (action === 'reject-import-modal') {
                                Model.closeVoucherModal();
                            }
                            await Model.fetchImportReceiptsApi();
                            renderMainOnly();
                        } catch (err) {
                            await WhNotification.alert(err.message || 'Lỗi khi xử lý không nhập kho!', {
                                type: 'error',
                                title: 'Lỗi xử lý'
                            });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xử lý nút Duyệt / Từ chối trên Lệnh xuất kho (mẫu cũ)
                case 'approve-outbound-cmd': {
                    e.preventDefault();
                    const orderNo = actionEl.getAttribute('data-order-no');
                    if (orderNo) {
                        Model.updateOutboundCommandStatus(orderNo, 'Đã xuất kho');
                        renderMainOnly();
                    }
                    break;
                }

                case 'reject-outbound-cmd': {
                    e.preventDefault();
                    const orderNo = actionEl.getAttribute('data-order-no');
                    if (orderNo) {
                        Model.updateOutboundCommandStatus(orderNo, 'Chờ xuất kho');
                        renderMainOnly();
                    }
                    break;
                }

                // Xử lý nút Duyệt / Từ chối trên Lệnh nhập kho
                case 'approve-inbound-cmd': {
                    e.preventDefault();
                    const orderNo = actionEl.getAttribute('data-order-no');
                    if (orderNo) {
                        Model.updateInboundCommandStatus(orderNo, 'Đã nhập kho');
                        renderMainOnly();
                    }
                    break;
                }

                case 'reject-inbound-cmd': {
                    e.preventDefault();
                    const orderNo = actionEl.getAttribute('data-order-no');
                    if (orderNo) {
                        Model.updateInboundCommandStatus(orderNo, 'Chờ nhập kho');
                        renderMainOnly();
                    }
                    break;
                }

                case 'add-voucher-row': {
                    e.preventDefault();
                    const vType = actionEl.getAttribute('data-voucher-type');
                    if (vType) {
                        Model.addVoucherItemRow(vType);
                        renderMainOnly();
                    }
                    break;
                }

                // Mở Modal Thêm mới VTHH (Sản phẩm hoặc Nguyên vật liệu)
                case 'create-vthh-item': {
                    e.preventDefault();
                    const itemType = actionEl.getAttribute('data-item-type');
                    if (itemType && Model.openVthhCreateModal) {
                        Model.openVthhCreateModal(itemType);
                        renderMainOnly();
                    }
                    break;
                }

                // Lưu hàng hóa VTHH mới tạo
                case 'save-new-vthh-item': {
                    e.preventDefault();
                    const itemType = actionEl.getAttribute('data-item-type');
                    const formEl = document.getElementById('wh-vthh-form');
                    if (formEl && Model.addVthhItem) {
                        const formData = new FormData(formEl);
                        const newItem = {};
                        for (const [key, value] of formData.entries()) {
                            newItem[key] = value;
                        }
                        const nameKey = itemType === 'product' ? 'productName' : 'ingrName';
                        if (!newItem[nameKey] || !newItem[nameKey].trim()) {
                            WhNotification.alert('Vui lòng nhập tên ' + (itemType === 'product' ? 'sản phẩm!' : 'nguyên vật liệu!'), { type: 'warning' });
                            return;
                        }
                        if (itemType === 'ingredient') {
                            const priceVal = parseFloat(String(newItem.price || '').replace(/,/g, ''));
                            if (isNaN(priceVal) || priceVal <= 0) {
                                WhNotification.alert('Đơn giá nguyên vật liệu không được bằng 0. Vui lòng nhập đơn giá hợp lệ lớn hơn 0!', { type: 'warning' });
                                return;
                            }
                        }
                        const origHtml = actionEl.innerHTML;
                        try {
                            actionEl.disabled = true;
                            actionEl.innerHTML = '<span>Đang lưu...</span>';
                            await Model.addVthhItem(itemType, newItem);
                            WhNotification.toast(`Thêm mới ${itemType === 'product' ? 'sản phẩm' : 'nguyên vật liệu'} thành công!`, 'success');
                            Model.closeVthhModal();
                            renderMainOnly();
                        } catch (err) {
                            WhNotification.alert(err.message || 'Lỗi khi lưu dữ liệu!', { type: 'error' });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xem chi tiết VTHH (Chế độ khóa / chỉ xem) khi bấm vào mã SP hoặc mã NVL
                case 'view-vthh-item': {
                    e.preventDefault();
                    e.stopPropagation();
                    const itemType = actionEl.getAttribute('data-item-type');
                    const itemId = actionEl.getAttribute('data-item-id');
                    if (itemType && itemId && Model.openVthhModal) {
                        Model.openVthhModal(itemType, itemId, 'view');
                        renderMainOnly();
                        if (itemType === 'ingredient' && Model.fetchIngredientDetailApi) {
                            try {
                                await Model.fetchIngredientDetailApi(itemId);
                                renderMainOnly();
                            } catch (_) {}
                        } else if (itemType === 'product' && Model.fetchProductDetailApi) {
                            try {
                                await Model.fetchProductDetailApi(itemId);
                                renderMainOnly();
                            } catch (_) {}
                        }
                    }
                    break;
                }

                // Chỉnh sửa VTHH (Chế độ cho phép sửa thông tin & ảnh, mã bị khóa)
                case 'edit-vthh-item': {
                    e.preventDefault();
                    e.stopPropagation();
                    const itemType = actionEl.getAttribute('data-item-type');
                    const itemId = actionEl.getAttribute('data-item-id');
                    if (itemType && itemId && Model.openVthhModal) {
                        Model.openVthhModal(itemType, itemId, 'edit');
                        renderMainOnly();
                        if (itemType === 'ingredient' && Model.fetchIngredientDetailApi) {
                            try {
                                await Model.fetchIngredientDetailApi(itemId);
                                renderMainOnly();
                            } catch (_) {}
                        } else if (itemType === 'product' && Model.fetchProductDetailApi) {
                            try {
                                await Model.fetchProductDetailApi(itemId);
                                renderMainOnly();
                            } catch (_) {}
                        }
                    }
                    break;
                }

                // Đóng Modal VTHH
                case 'close-vthh-modal': {
                    e.preventDefault();
                    if (Model.closeVthhModal) {
                        Model.closeVthhModal();
                        renderMainOnly();
                    }
                    break;
                }

                // Lưu thay đổi khi chỉnh sửa VTHH
                case 'save-vthh-item': {
                    e.preventDefault();
                    const itemType = actionEl.getAttribute('data-item-type');
                    const itemId = actionEl.getAttribute('data-item-id');
                    const formEl = document.getElementById('wh-vthh-form');
                    if (formEl && Model.updateVthhItem) {
                        const formData = new FormData(formEl);
                        const updatedFields = {};
                        for (const [key, value] of formData.entries()) {
                            updatedFields[key] = value;
                        }
                        const nameKey = itemType === 'product' ? 'productName' : 'ingrName';
                        if (updatedFields[nameKey] !== undefined && !updatedFields[nameKey].trim()) {
                            WhNotification.alert('Vui lòng không để trống tên ' + (itemType === 'product' ? 'sản phẩm!' : 'nguyên vật liệu!'), { type: 'warning' });
                            return;
                        }
                        const origHtml = actionEl.innerHTML;
                        try {
                            actionEl.disabled = true;
                            actionEl.innerHTML = '<span>Đang lưu...</span>';
                            await Model.updateVthhItem(itemType, itemId, updatedFields);
                            WhNotification.toast('Cập nhật thông tin thành công!', 'success');
                            Model.closeVthhModal();
                            renderMainOnly();
                        } catch (err) {
                            WhNotification.alert(err.message || 'Lỗi khi cập nhật dữ liệu!', { type: 'error' });
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xử lý xóa dòng trong trang Danh mục VTHH (Sản phẩm hoặc Nguyên vật liệu)
                case 'delete-vthh-row': {
                    e.preventDefault();
                    e.stopPropagation();
                    const itemType = actionEl.getAttribute('data-item-type');
                    const itemId = actionEl.getAttribute('data-item-id');
                    const itemName = actionEl.getAttribute('data-item-name') || '';
                    const typeLabel = itemType === 'product' ? 'sản phẩm' : 'nguyên vật liệu';
                    
                    const confirmed = await WhNotification.confirm(`Bạn có chắc chắn muốn xóa ${typeLabel} "${itemName}" không?`, {
                        title: `Xác nhận xóa ${typeLabel}`,
                        confirmText: 'Xóa',
                        cancelText: 'Hủy bỏ',
                        type: 'error'
                    });
                    if (confirmed) {
                        if (Model.deleteVthhItem) {
                            try {
                                await Model.deleteVthhItem(itemType, itemId);
                                WhNotification.toast(`Đã xóa ${typeLabel} "${itemName}" thành công!`, 'success');
                                renderMainOnly();
                            } catch (err) {
                                WhNotification.alert(err.message || `Không thể xóa ${typeLabel}!`, { type: 'error' });
                            }
                        }
                    }
                    break;
                }

                // Xử lý hủy lô nguyên vật liệu đã hết hạn sử dụng
                case 'dispose-expired-batch': {
                    e.preventDefault();
                    e.stopPropagation();
                    const ingrId = actionEl.getAttribute('data-ingr-id');
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const voucherNo = actionEl.getAttribute('data-voucher-no') || '';
                    const qty = actionEl.getAttribute('data-qty') || '';
                    const unit = actionEl.getAttribute('data-unit') || 'kg';

                    const confirmed = await WhNotification.confirm(
                        `Bạn có chắc chắn muốn hủy <strong>${Number(qty).toLocaleString('vi-VN')} ${unit}</strong> nguyên vật liệu hết hạn từ phiếu <strong>${voucherNo}</strong> không?<br><small style="color:#64748b;">Hệ thống sẽ trừ số lượng này khỏi tồn kho và ghi nhận giao dịch 'Hủy nguyên vật liệu hết hạn' vào thẻ kho và báo cáo thống kê.</small>`,
                        {
                            title: 'Xác nhận hủy NVL hết hạn',
                            confirmText: 'Xác nhận hủy',
                            cancelText: 'Hủy bỏ',
                            type: 'error'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        try {
                            actionEl.disabled = true;
                            actionEl.innerHTML = '<span>Đang hủy...</span>';
                            const res = await Model.disposeExpiredBatchApi(ingrId, receiptId);
                            WhNotification.toast(res.message || 'Đã hủy nguyên vật liệu hết hạn thành công!', 'success');
                            // Làm mới danh sách và chi tiết NVL
                            if (Model.fetchIngredientsApi) {
                                await Model.fetchIngredientsApi();
                            } else if (Model.loadIngredients) {
                                await Model.loadIngredients();
                            }
                            await Model.fetchIngredientDetailApi(ingrId);
                            // Làm mới báo cáo nếu đang mở
                            if (Model.fetchPeriodStatsApi) {
                                try { await Model.fetchPeriodStatsApi(); } catch (_) {}
                            }
                            renderMainOnly();
                        } catch (err) {
                            WhNotification.alert(err.message || 'Có lỗi xảy ra khi hủy nguyên vật liệu hết hạn!', { type: 'error' });
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                // Xử lý hủy lô thành phẩm đã hết hạn sử dụng
                case 'dispose-expired-product-batch': {
                    e.preventDefault();
                    e.stopPropagation();
                    const productId = actionEl.getAttribute('data-product-id');
                    const receiptId = actionEl.getAttribute('data-receipt-id');
                    const voucherNo = actionEl.getAttribute('data-voucher-no') || '';
                    const qty = actionEl.getAttribute('data-qty') || '';
                    const unit = actionEl.getAttribute('data-unit') || 'Hộp';

                    const confirmed = await WhNotification.confirm(
                        `Bạn có chắc chắn muốn hủy <strong>${Number(qty).toLocaleString('vi-VN')} ${unit}</strong> thành phẩm hết hạn từ lệnh <strong>${voucherNo}</strong> không?<br><small style="color:#64748b;">Hệ thống sẽ trừ số lượng này khỏi tồn kho và ghi nhận giao dịch 'Hủy sản phẩm hết hạn' vào thẻ kho và báo cáo thống kê.</small>`,
                        {
                            title: 'Xác nhận hủy sản phẩm hết hạn',
                            confirmText: 'Xác nhận hủy',
                            cancelText: 'Hủy bỏ',
                            type: 'error'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        try {
                            actionEl.disabled = true;
                            actionEl.innerHTML = '<span>Đang hủy...</span>';
                            const res = await Model.disposeExpiredProductBatchApi(productId, receiptId);
                            WhNotification.toast(res.message || 'Đã hủy thành phẩm hết hạn thành công!', 'success');
                            if (Model.fetchProductsApi) {
                                await Model.fetchProductsApi();
                            } else if (Model.loadProducts) {
                                await Model.loadProducts();
                            }
                            await Model.fetchProductDetailApi(productId);
                            if (Model.fetchProductExportsApi) {
                                try { await Model.fetchProductExportsApi(); } catch (_) {}
                            }
                            if (Model.fetchPeriodStatsApi) {
                                try { await Model.fetchPeriodStatsApi(); } catch (_) {}
                            }
                            renderMainOnly();
                        } catch (err) {
                            WhNotification.alert(err.message || 'Có lỗi xảy ra khi hủy thành phẩm hết hạn!', { type: 'error' });
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                case 'delete-voucher-row': {
                    e.preventDefault();
                    const vType = actionEl.getAttribute('data-voucher-type');
                    const rowId = actionEl.getAttribute('data-row-id');
                    if (vType && rowId) {
                        Model.deleteVoucherItemRow(vType, rowId);
                        renderMainOnly();
                    }
                    break;
                }

                case 'clear-voucher-rows': {
                    e.preventDefault();
                    const vType = actionEl.getAttribute('data-voucher-type');
                    if (vType) {
                        Model.clearAllVoucherItems(vType);
                        renderMainOnly();
                    }
                    break;
                }

                // --- CÁC SỰ KIỆN TRÊN TRANG TỔNG QUAN ---
                case 'toggle-param-popover': {
                    e.preventDefault();
                    const currentOpen = Model.getState().isParamPopoverOpen;
                    Model.setParamPopoverOpen(!currentOpen);
                    renderMainOnly();
                    break;
                }

                case 'close-param-popover': {
                    e.preventDefault();
                    Model.setParamPopoverOpen(false);
                    renderMainOnly();
                    break;
                }

                case 'apply-param-popover': {
                    e.preventDefault();
                    const selectEl = document.getElementById('wh-param-days-select');
                    const selectedDays = selectEl ? Number(selectEl.value) : 5;
                    Model.applyExpiringDaysParam(selectedDays);
                    renderMainOnly();
                    break;
                }

                case 'refresh-card': {
                    e.preventDefault();
                    const cardId = actionEl.getAttribute('data-card-id') || actionEl.closest('[data-card-id]')?.getAttribute('data-card-id');
                    if (cardId) {
                        const cardSection = document.querySelector(`[data-card-id="${cardId}"]`);
                        const refreshBtn = cardSection ? (cardSection.querySelector('.wh-card-action-btn[data-action="refresh-card"]') || cardSection.querySelector('.wh-reload-link')) : actionEl;
                        if (refreshBtn) {
                            refreshBtn.classList.add('spinning');
                        }
                        try {
                            // Tải lại toàn bộ số liệu thống kê từ database
                            await Promise.allSettled([
                                Model.loadProducts(),
                                Model.loadIngredients(),
                                Model.fetchProductReceiptsApi(),
                                Model.fetchImportReceiptsApi(),
                                Model.fetchMaterialReceiptsApi(),
                                Model.fetchProductExportsApi(),
                                Model.fetchProductionRequestsApi()
                            ]);
                        } catch (err) {
                            console.error('Lỗi khi làm mới thẻ:', err);
                        }
                        Model.refreshCardTimestamp(cardId);
                        renderMainOnly();
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast('Đã làm mới dữ liệu thẻ thành công!', 'success');
                        }
                    }
                    break;
                }

                // --- CÁC THAO TÁC TRÊN YÊU CẦU SẢN XUẤT ---
                case 'open-create-pr-modal': {
                    e.preventDefault();
                    Model.openCreatePrModal();
                    renderMainOnly();
                    break;
                }

                case 'close-pr-modal': {
                    e.preventDefault();
                    Model.closePrModal();
                    renderMainOnly();
                    break;
                }

                case 'toggle-pr-product-dropdown': {
                    e.preventDefault();
                    e.stopPropagation();
                    const rowId = actionEl.getAttribute('data-row-id');
                    const dropdown = document.getElementById(`pr-prod-dropdown-${rowId}`);
                    if (!dropdown) return;

                    const isCurrentlyOpen = dropdown.style.display === 'block';

                    // Đóng tất cả dropdown đang mở
                    document.querySelectorAll('.wh-pr-dropdown-menu').forEach(el => el.style.display = 'none');
                    document.querySelectorAll('.wh-pr-select-trigger').forEach(el => el.classList.remove('is-open'));

                    if (!isCurrentlyOpen) {
                        dropdown.style.display = 'block';
                        actionEl.classList.add('is-open');

                        // Reset search input và focus
                        const searchInput = dropdown.querySelector('.wh-pr-product-search-input');
                        if (searchInput) {
                            searchInput.value = '';
                            setTimeout(() => searchInput.focus(), 60);
                        }
                        dropdown.querySelectorAll('.wh-pr-dropdown-item').forEach(it => it.style.display = 'flex');
                        const noRes = dropdown.querySelector('.wh-pr-dropdown-no-results');
                        if (noRes) noRes.style.display = 'none';
                    }
                    break;
                }

                case 'select-pr-product-item': {
                    e.preventDefault();
                    e.stopPropagation();
                    const rowId = actionEl.getAttribute('data-row-id');
                    const pid = actionEl.getAttribute('data-pid') || actionEl.getAttribute('data-id');
                    if (rowId && pid && Model.changePrItemProduct) {
                        Model.changePrItemProduct(rowId, pid);
                        renderMainOnly();
                    }
                    break;
                }

                case 'add-pr-item-row': {
                    e.preventDefault();
                    if (Model.addPrItemRow) {
                        Model.addPrItemRow();
                        renderMainOnly();
                    }
                    break;
                }

                case 'remove-pr-item-row': {
                    e.preventDefault();
                    const rowId = actionEl.getAttribute('data-row-id');
                    if (rowId && Model.removePrItemRow) {
                        Model.removePrItemRow(rowId);
                        renderMainOnly();
                    }
                    break;
                }

                case 'save-create-pr': {
                    e.preventDefault();
                    const codeInput = document.getElementById('pr-form-code');
                    const requestCode = codeInput ? codeInput.value.trim() : '';

                    const modalData = Model.getPrModalData ? Model.getPrModalData() : null;
                    const rawItems = modalData && modalData.item && Array.isArray(modalData.item.items)
                        ? modalData.item.items
                        : [];

                    // Chuẩn bị danh sách sản phẩm yêu cầu sản xuất
                    let items = rawItems
                        .map(it => ({
                            productId: parseInt(it.productId, 10) || 0,
                            quantityRequired: parseInt(it.quantityRequired, 10) || 0
                        }))
                        .filter(it => it.productId > 0 && it.quantityRequired > 0);

                    // Fallback nếu người dùng dùng form cũ 1 sản phẩm
                    if (items.length === 0) {
                        const prodSelect = document.getElementById('pr-form-product');
                        const qtyInput = document.getElementById('pr-form-quantity');
                        const pid = prodSelect ? parseInt(prodSelect.value, 10) : 0;
                        const qty = qtyInput ? parseInt(qtyInput.value, 10) : 0;
                        if (pid > 0 && qty > 0) {
                            items = [{ productId: pid, quantityRequired: qty }];
                        }
                    }

                    if (items.length === 0) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert('Vui lòng chọn ít nhất một thành phẩm với số lượng hợp lệ (> 0).', { type: 'warning' });
                        } else {
                            alert('Vui lòng chọn ít nhất một thành phẩm với số lượng hợp lệ.');
                        }
                        return;
                    }

                    actionEl.disabled = true;
                    try {
                        let currentUser = Model.getCurrentUser ? Model.getCurrentUser() : null;
                        if ((!currentUser || !currentUser.userId) && Model.fetchCurrentUserApi) {
                            currentUser = await Model.fetchCurrentUserApi();
                        }

                        const payload = {
                            requestCode,
                            items,
                            status: 'Pending'
                        };
                        if (currentUser && currentUser.userId) {
                            payload.managerId = currentUser.userId;
                        }

                        const res = await Model.createProductionRequestApi(payload);
                        Model.closePrModal();
                        renderMainOnly();
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(res.message || `Đã tạo lệnh sản xuất '${requestCode}' thành công cho ${items.length} loại thành phẩm!`, 'success');
                        }
                    } catch (err) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(err.message || 'Lỗi khi tạo lệnh sản xuất.', { type: 'error' });
                        } else {
                            alert(err.message);
                        }
                    } finally {
                        actionEl.disabled = false;
                    }
                    break;
                }

                case 'edit-pr-item': {
                    e.preventDefault();
                    const prId = actionEl.getAttribute('data-pr-id');
                    const state = Model.getState();
                    const item = (state.productionRequests || []).find(r => String(r.requestId) === String(prId));
                    if (item) {
                        if (item.status !== 'Pending') {
                            View.showNotification
                                ? View.showNotification(`Lệnh sản xuất '${item.requestCode}' đang ở trạng thái '${item.status}', thông tin đã bị khóa không được chỉnh sửa. Chỉ lệnh ở trạng thái 'Pending' mới được chỉnh sửa.`, 'warning')
                                : alert(`Lệnh đang ở trạng thái '${item.status}', không thể chỉnh sửa.`);
                            return;
                        }
                        try {
                            Model.openEditPrModal(item);
                            renderMainOnly();
                        } catch (err) {
                            View.showNotification ? View.showNotification(err.message, 'warning') : alert(err.message);
                        }
                    }
                    break;
                }

                case 'save-edit-pr': {
                    e.preventDefault();
                    const prId = actionEl.getAttribute('data-pr-id');
                    const prodSelect = document.getElementById('pr-form-product');
                    const qtyInput = document.getElementById('pr-form-quantity');

                    const productId = prodSelect ? parseInt(prodSelect.value, 10) : 0;
                    const quantityRequired = qtyInput ? parseInt(qtyInput.value, 10) : 0;
                    const status = 'Pending'; // Trạng thái trong trang chỉnh sửa luôn là Pending

                    if (!productId || productId <= 0) {
                        View.showNotification ? View.showNotification('Vui lòng chọn thành phẩm sản xuất.', 'warning') : alert('Vui lòng chọn thành phẩm.');
                        return;
                    }
                    if (!quantityRequired || quantityRequired <= 0) {
                        View.showNotification ? View.showNotification('Số lượng sản xuất phải lớn hơn 0.', 'warning') : alert('Số lượng phải lớn hơn 0.');
                        return;
                    }

                    actionEl.disabled = true;
                    try {
                        await Model.updateProductionRequestApi(prId, {
                            productId,
                            quantityRequired,
                            status
                        });
                        Model.closePrModal();
                        renderMainOnly();
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast('Cập nhật lệnh sản xuất thành công!', 'success');
                        }
                    } catch (err) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(err.message || 'Lỗi cập nhật lệnh sản xuất.', { type: 'error' });
                        } else {
                            alert(err.message);
                        }
                    } finally {
                        actionEl.disabled = false;
                    }
                    break;
                }

                // Tiếp nhận yêu cầu sản xuất (chức năng thay thế phân công nhân viên)
                case 'receive-pr': {
                    e.preventDefault();
                    e.stopPropagation();
                    const prId = actionEl.getAttribute('data-pr-id');
                    const prCode = actionEl.getAttribute('data-pr-code') || prId;
                    if (!prId) return;

                    const confirmed = await WhNotification.confirm(
                        `Bạn có muốn tiếp nhận xử lý lệnh sản xuất <strong>"${prCode}"</strong> không?<br/><br/>Hệ thống sẽ ghi nhận bạn là <strong>Nhân viên tiếp nhận</strong> và chuyển trạng thái sang <strong>Đang sản xuất (In Production)</strong>.`,
                        {
                            title: 'Tiếp nhận yêu cầu sản xuất',
                            confirmText: '📥 Tiếp nhận ngay',
                            cancelText: 'Hủy bỏ',
                            type: 'info'
                        }
                    );

                    if (confirmed) {
                        const origHtml = actionEl.innerHTML;
                        actionEl.disabled = true;
                        actionEl.innerHTML = 'Đang tiếp nhận...';
                        try {
                            const res = await Model.receiveProductionRequest(prId);
                            Model.closePrModal();
                            renderMainOnly();
                            if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                                WhNotification.toast(res.message || `Đã tiếp nhận lệnh sản xuất "${prCode}" thành công!`, 'success');
                            }
                        } catch (err) {
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(err.message || 'Lỗi khi tiếp nhận yêu cầu sản xuất!', { type: 'error' });
                            } else {
                                alert(err.message);
                            }
                        } finally {
                            actionEl.disabled = false;
                            actionEl.innerHTML = origHtml;
                        }
                    }
                    break;
                }

                case 'accept-pr-production': {
                    e.preventDefault();
                    const prId = actionEl.getAttribute('data-pr-id');
                    if (!prId) return;
                    actionEl.disabled = true;
                    try {
                        const res = await Model.receiveProductionRequest(prId);
                        Model.closePrModal();
                        renderMainOnly();
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(res.message || 'Nhân viên đã tiếp nhận yêu cầu sản xuất! Trạng thái đã chuyển sang In Production và dữ liệu đã được khóa cố định.', 'success');
                        }
                    } catch (err) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(err.message || 'Lỗi khi tiếp nhận yêu cầu sản xuất.', { type: 'error' });
                        } else {
                            alert(err.message);
                        }
                    } finally {
                        actionEl.disabled = false;
                    }
                    break;
                }

                case 'complete-pr-production': {
                    e.preventDefault();
                    const prId = actionEl.getAttribute('data-pr-id');
                    if (!prId) return;
                    if (!confirm('Bạn có chắc chắn muốn xác nhận HOÀN THÀNH lệnh sản xuất này?')) {
                        return;
                    }
                    actionEl.disabled = true;
                    try {
                        const res = await Model.updateProductionRequestStatusApi(prId, 'Completed');
                        Model.closePrModal();
                        renderMainOnly();
                        View.showNotification ? View.showNotification(res.message || 'Đã xác nhận hoàn thành lệnh sản xuất!', 'success') : null;
                    } catch (err) {
                        View.showNotification ? View.showNotification(err.message || 'Lỗi khi cập nhật trạng thái hoàn thành.', 'error') : alert(err.message);
                    } finally {
                        actionEl.disabled = false;
                    }
                    break;
                }

                case 'view-pr-item': {
                    e.preventDefault();
                    const prId = actionEl.getAttribute('data-pr-id');
                    try {
                        const detail = await Model.fetchProductionRequestDetailApi(prId);
                        Model.openDetailPrModal(detail);
                        renderMainOnly();
                    } catch (err) {
                        View.showNotification ? View.showNotification(err.message || 'Lỗi tải chi tiết lệnh sản xuất.', 'error') : alert(err.message);
                    }
                    break;
                }

                case 'delete-pr-item': {
                    e.preventDefault();
                    const prId = actionEl.getAttribute('data-pr-id');
                    const prCode = actionEl.getAttribute('data-pr-code') || prId;
                    const state = Model.getState();
                    const item = (state.productionRequests || []).find(r => String(r.requestId) === String(prId));
                    if (item && item.status !== 'Pending') {
                        View.showNotification
                            ? View.showNotification(`Lệnh sản xuất '${prCode}' đang ở trạng thái '${item.status}', không thể xóa. Chỉ lệnh ở trạng thái 'Pending' mới được phép xóa.`, 'warning')
                            : alert(`Lệnh đang ở trạng thái '${item.status}', không thể xóa.`);
                        return;
                    }
                    if (!confirm(`Bạn có chắc chắn muốn xóa lệnh sản xuất '${prCode}' không?`)) {
                        return;
                    }
                    try {
                        const res = await Model.deleteProductionRequestApi(prId);
                        renderMainOnly();
                        View.showNotification ? View.showNotification(res.message || `Đã xóa lệnh sản xuất '${prCode}'!`, 'success') : null;
                    } catch (err) {
                        View.showNotification ? View.showNotification(err.message || 'Không thể xóa lệnh sản xuất.', 'error') : alert(err.message);
                    }
                    break;
                }

                case 'filter-pr-unreceived':
                case 'filter-pr-unassigned': {
                    e.preventDefault();
                    if (Model.setPrReceiveFilter) {
                        Model.setPrReceiveFilter('Chưa tiếp nhận');
                    } else if (Model.setPrAssignmentFilter) {
                        Model.setPrAssignmentFilter('Chưa tiếp nhận');
                    }
                    Model.setPrTab('tat-ca');
                    renderMainOnly();
                    break;
                }

                // --- CÁC THAO TÁC TRÊN NHÀ CUNG CẤP (SUPPLIER) ---
                case 'open-create-supplier-modal': {
                    e.preventDefault();
                    Model.openCreateSupplierModal();
                    renderMainOnly();
                    break;
                }

                case 'close-supplier-modal': {
                    e.preventDefault();
                    Model.closeSupplierModal();
                    renderMainOnly();
                    break;
                }

                case 'view-supplier-detail': {
                    e.preventDefault();
                    const supId = parseInt(actionEl.getAttribute('data-supplier-id'), 10);
                    const list = Model.getState().suppliers?.length ? Model.getState().suppliers : Model.getSuppliersList();
                    const item = (list || []).find(s => (s.id || s.supplierId) === supId);
                    if (item) {
                        Model.openDetailSupplierModal(item);
                        renderMainOnly();
                    }
                    break;
                }

                case 'edit-supplier-item': {
                    e.preventDefault();
                    const supId = parseInt(actionEl.getAttribute('data-supplier-id'), 10);
                    const list = Model.getState().suppliers?.length ? Model.getState().suppliers : Model.getSuppliersList();
                    const item = (list || []).find(s => (s.id || s.supplierId) === supId);
                    if (item) {
                        Model.openEditSupplierModal(item);
                        renderMainOnly();
                    }
                    break;
                }

                case 'save-create-supplier': {
                    e.preventDefault();
                    const nameInput = document.getElementById('supplier-form-name');
                    const phoneInput = document.getElementById('supplier-form-phone');
                    const mailInput = document.getElementById('supplier-form-mail');
                    const addressInput = document.getElementById('supplier-form-address');
                    const costInput = document.getElementById('supplier-form-import-cost');

                    const supplierName = nameInput ? nameInput.value.trim() : '';
                    const phone = phoneInput ? phoneInput.value.trim() : '';
                    const mail = mailInput ? mailInput.value.trim() : '';
                    const address = addressInput ? addressInput.value.trim() : '';
                    
                    const costRaw = costInput ? costInput.value.trim() : '';
                    let importCost = 0;
                    if (costRaw !== '') {
                        const parsedCost = parseFloat(costRaw);
                        if (isNaN(parsedCost) || parsedCost < 0 || parsedCost > 100) {
                            const msg = 'Tỷ lệ chi phí nhập phải nằm trong khoảng từ 0% đến 100%.';
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(msg, { type: 'warning', title: 'Chi phí nhập không hợp lệ' });
                            } else {
                                alert(msg);
                            }
                            if (costInput) {
                                costInput.focus();
                                costInput.style.borderColor = '#ef4444';
                            }
                            return;
                        }
                        if (costRaw.includes('.')) {
                            const decimals = costRaw.split('.')[1] || '';
                            if (decimals.length > 1) {
                                const msg = 'Tỷ lệ chi phí nhập chỉ được có tối đa 1 chữ số thập phân (Ví dụ: 8.5%).';
                                if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                    WhNotification.alert(msg, { type: 'warning', title: 'Số chữ số thập phân không hợp lệ' });
                                } else {
                                    alert(msg);
                                }
                                if (costInput) {
                                    costInput.focus();
                                    costInput.style.borderColor = '#ef4444';
                                }
                                return;
                            }
                        }
                        importCost = Math.round(parsedCost * 10) / 10;
                    }

                    if (!supplierName) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast('Vui lòng nhập tên nhà cung cấp.', 'warning');
                        } else {
                            alert('Vui lòng nhập tên nhà cung cấp.');
                        }
                        if (nameInput) nameInput.focus();
                        return;
                    }

                    // Ràng buộc kiểm tra nhà cung cấp đã tồn tại (phía Client trước khi gọi API)
                    const existingSuppliers = (typeof Model.getSuppliersList === 'function' ? Model.getSuppliersList() : (Model.getState().suppliers || []));
                    const cleanName = supplierName.trim().toLowerCase();
                    const duplicate = existingSuppliers.find(s => (s.name || s.supplierName || '').trim().toLowerCase() === cleanName);
                    if (duplicate) {
                        const msg = `Nhà cung cấp "${supplierName}" đã tồn tại trong hệ thống (Mã: ${duplicate.code || 'NCC' + (duplicate.supplierId || duplicate.id)}). Không thể thêm mới!`;
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(msg, { type: 'warning', title: 'Nhà cung cấp đã tồn tại' });
                        } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(msg, 'warning');
                        } else {
                            alert(msg);
                        }
                        if (nameInput) {
                            nameInput.focus();
                            nameInput.style.borderColor = '#ef4444';
                        }
                        const hintEl = document.getElementById('supplier-name-dup-hint');
                        if (hintEl) {
                            hintEl.style.display = 'block';
                            hintEl.innerHTML = `⚠️ Nhà cung cấp "<strong>${escapeHtml(supplierName)}</strong>" đã tồn tại (Mã: <strong>${escapeHtml(duplicate.code || 'NCC' + (duplicate.supplierId || duplicate.id))}</strong>).`;
                        }
                        return;
                    }

                    // Kiểm tra trùng số điện thoại
                    if (phone) {
                        const cleanPhone = phone.trim();
                        const dupPhone = existingSuppliers.find(s => s.phone && s.phone.trim() === cleanPhone);
                        if (dupPhone) {
                            const msg = `Số điện thoại "${cleanPhone}" đã được đăng ký cho nhà cung cấp "${dupPhone.name || dupPhone.supplierName}".`;
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(msg, { type: 'warning', title: 'Trùng số điện thoại' });
                            } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                                WhNotification.toast(msg, 'warning');
                            } else {
                                alert(msg);
                            }
                            if (phoneInput) {
                                phoneInput.focus();
                                phoneInput.style.borderColor = '#ef4444';
                            }
                            return;
                        }
                    }

                    // Kiểm tra trùng email
                    if (mail) {
                        const cleanMail = mail.trim().toLowerCase();
                        const dupMail = existingSuppliers.find(s => s.mail && s.mail.trim().toLowerCase() === cleanMail);
                        if (dupMail) {
                            const msg = `Email "${mail}" đã được sử dụng bởi nhà cung cấp "${dupMail.name || dupMail.supplierName}".`;
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(msg, { type: 'warning', title: 'Trùng địa chỉ email' });
                            } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                                WhNotification.toast(msg, 'warning');
                            } else {
                                alert(msg);
                            }
                            if (mailInput) {
                                mailInput.focus();
                                mailInput.style.borderColor = '#ef4444';
                            }
                            return;
                        }
                    }

                    try {
                        await Model.createSupplierApi({ supplierName, phone, mail, address, importCost });
                        Model.closeSupplierModal();
                        renderMainOnly();
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(`Đã thêm mới nhà cung cấp "${supplierName}" thành công!`, 'success');
                        }
                    } catch (err) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(err.message || 'Không thể tạo nhà cung cấp.', { type: 'error', title: 'Không thể thêm nhà cung cấp' });
                        } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(err.message || 'Không thể tạo nhà cung cấp.', 'error');
                        } else {
                            alert(err.message);
                        }
                    }
                    break;
                }

                case 'save-edit-supplier': {
                    e.preventDefault();
                    const supId = parseInt(actionEl.getAttribute('data-supplier-id'), 10);
                    const nameInput = document.getElementById('supplier-form-name');
                    const phoneInput = document.getElementById('supplier-form-phone');
                    const mailInput = document.getElementById('supplier-form-mail');
                    const addressInput = document.getElementById('supplier-form-address');
                    const costInput = document.getElementById('supplier-form-import-cost');

                    const supplierName = nameInput ? nameInput.value.trim() : '';
                    const phone = phoneInput ? phoneInput.value.trim() : '';
                    const mail = mailInput ? mailInput.value.trim() : '';
                    const address = addressInput ? addressInput.value.trim() : '';
                    
                    const costRaw = costInput ? costInput.value.trim() : '';
                    let importCost = 0;
                    if (costRaw !== '') {
                        const parsedCost = parseFloat(costRaw);
                        if (isNaN(parsedCost) || parsedCost < 0 || parsedCost > 100) {
                            const msg = 'Tỷ lệ chi phí nhập phải nằm trong khoảng từ 0% đến 100%.';
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(msg, { type: 'warning', title: 'Chi phí nhập không hợp lệ' });
                            } else {
                                alert(msg);
                            }
                            if (costInput) {
                                costInput.focus();
                                costInput.style.borderColor = '#ef4444';
                            }
                            return;
                        }
                        if (costRaw.includes('.')) {
                            const decimals = costRaw.split('.')[1] || '';
                            if (decimals.length > 1) {
                                const msg = 'Tỷ lệ chi phí nhập chỉ được có tối đa 1 chữ số thập phân (Ví dụ: 8.5%).';
                                if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                    WhNotification.alert(msg, { type: 'warning', title: 'Số chữ số thập phân không hợp lệ' });
                                } else {
                                    alert(msg);
                                }
                                if (costInput) {
                                    costInput.focus();
                                    costInput.style.borderColor = '#ef4444';
                                }
                                return;
                            }
                        }
                        importCost = Math.round(parsedCost * 10) / 10;
                    }

                    if (!supplierName) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast('Vui lòng nhập tên nhà cung cấp.', 'warning');
                        } else {
                            alert('Vui lòng nhập tên nhà cung cấp.');
                        }
                        if (nameInput) nameInput.focus();
                        return;
                    }

                    // Ràng buộc kiểm tra trùng tên với nhà cung cấp khác
                    const existingSuppliers = (typeof Model.getSuppliersList === 'function' ? Model.getSuppliersList() : (Model.getState().suppliers || []));
                    const cleanName = supplierName.trim().toLowerCase();
                    const duplicate = existingSuppliers.find(s => ((s.id || s.supplierId) !== supId) && (s.name || s.supplierName || '').trim().toLowerCase() === cleanName);
                    if (duplicate) {
                        const msg = `Tên nhà cung cấp "${supplierName}" đã tồn tại trong hệ thống (Mã: ${duplicate.code || 'NCC' + (duplicate.supplierId || duplicate.id)}). Vui lòng chọn tên khác!`;
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(msg, { type: 'warning', title: 'Tên nhà cung cấp đã tồn tại' });
                        } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(msg, 'warning');
                        } else {
                            alert(msg);
                        }
                        if (nameInput) {
                            nameInput.focus();
                            nameInput.style.borderColor = '#ef4444';
                        }
                        const hintEl = document.getElementById('supplier-name-dup-hint');
                        if (hintEl) {
                            hintEl.style.display = 'block';
                            hintEl.innerHTML = `⚠️ Tên nhà cung cấp "<strong>${escapeHtml(supplierName)}</strong>" đã tồn tại (Mã: <strong>${escapeHtml(duplicate.code || 'NCC' + (duplicate.supplierId || duplicate.id))}</strong>).`;
                        }
                        return;
                    }

                    // Kiểm tra trùng số điện thoại
                    if (phone) {
                        const cleanPhone = phone.trim();
                        const dupPhone = existingSuppliers.find(s => ((s.id || s.supplierId) !== supId) && s.phone && s.phone.trim() === cleanPhone);
                        if (dupPhone) {
                            const msg = `Số điện thoại "${cleanPhone}" đã được đăng ký cho nhà cung cấp "${dupPhone.name || dupPhone.supplierName}".`;
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(msg, { type: 'warning', title: 'Trùng số điện thoại' });
                            } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                                WhNotification.toast(msg, 'warning');
                            } else {
                                alert(msg);
                            }
                            if (phoneInput) {
                                phoneInput.focus();
                                phoneInput.style.borderColor = '#ef4444';
                            }
                            return;
                        }
                    }

                    // Kiểm tra trùng email
                    if (mail) {
                        const cleanMail = mail.trim().toLowerCase();
                        const dupMail = existingSuppliers.find(s => ((s.id || s.supplierId) !== supId) && s.mail && s.mail.trim().toLowerCase() === cleanMail);
                        if (dupMail) {
                            const msg = `Email "${mail}" đã được sử dụng bởi nhà cung cấp "${dupMail.name || dupMail.supplierName}".`;
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(msg, { type: 'warning', title: 'Trùng địa chỉ email' });
                            } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                                WhNotification.toast(msg, 'warning');
                            } else {
                                alert(msg);
                            }
                            if (mailInput) {
                                mailInput.focus();
                                mailInput.style.borderColor = '#ef4444';
                            }
                            return;
                        }
                    }

                    try {
                        await Model.updateSupplierApi(supId, { supplierName, phone, mail, address, importCost });
                        Model.closeSupplierModal();
                        renderMainOnly();
                        if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(`Đã cập nhật thông tin nhà cung cấp "${supplierName}" thành công!`, 'success');
                        }
                    } catch (err) {
                        if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                            WhNotification.alert(err.message || 'Không thể cập nhật nhà cung cấp.', { type: 'error', title: 'Không thể cập nhật nhà cung cấp' });
                        } else if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                            WhNotification.toast(err.message || 'Không thể cập nhật nhà cung cấp.', 'error');
                        } else {
                            alert(err.message);
                        }
                    }
                    break;
                }

                case 'delete-supplier-item': {
                    e.preventDefault();
                    const supId = parseInt(actionEl.getAttribute('data-supplier-id'), 10);
                    const supName = actionEl.getAttribute('data-supplier-name') || 'nhà cung cấp';

                    let confirmed = false;
                    if (typeof WhNotification !== 'undefined' && WhNotification.confirm) {
                        confirmed = await WhNotification.confirm(`Bạn có chắc chắn muốn xóa nhà cung cấp "${supName}" không?\nLưu ý: Không thể xóa nhà cung cấp đã có chứng từ nhập kho liên kết.`, {
                            title: 'Xác nhận xóa nhà cung cấp',
                            confirmText: 'Xóa ngay',
                            type: 'danger'
                        });
                    } else {
                        confirmed = confirm(`Bạn có chắc chắn muốn xóa nhà cung cấp "${supName}" không?`);
                    }

                    if (confirmed) {
                        try {
                            await Model.deleteSupplierApi(supId);
                            renderMainOnly();
                            if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                                WhNotification.toast(`Đã xóa nhà cung cấp "${supName}" thành công!`, 'success');
                            }
                        } catch (err) {
                            if (typeof WhNotification !== 'undefined' && WhNotification.alert) {
                                WhNotification.alert(err.message || 'Không thể xóa nhà cung cấp do đã có chứng từ phát sinh.', {
                                    title: 'Không thể xóa nhà cung cấp',
                                    type: 'error'
                                });
                            } else {
                                alert(err.message);
                            }
                        }
                    }
                    break;
                }

                case 'refresh-datagrid': {
                    e.preventDefault();
                    actionEl.classList.add('spinning');
                    const curMenu = Model.getState().activeMenuId;
                    try {
                        if (curMenu === 'danh-muc-vthh') {
                            await Promise.all([Model.loadProducts(), Model.loadIngredients()]);
                        } else if (curMenu === 'xuat-kho') {
                            await Promise.all([Model.fetchMaterialReceiptsApi(), Model.fetchProductExportsApi()]);
                        } else if (curMenu === 'nhap-kho') {
                            await Promise.all([Model.fetchProductReceiptsApi(), Model.fetchImportReceiptsApi()]);
                        } else if (curMenu === 'yeu-cau-san-xuat') {
                            await Promise.all([
                                Model.fetchProductionRequestsApi(),
                                Model.fetchAssignableUsersApi(),
                                Model.loadProducts()
                            ]);
                        } else if (curMenu === 'nha-cung-cap') {
                            await Model.fetchSuppliersApi();
                        } else {
                            await Promise.allSettled([
                                Model.loadProducts(),
                                Model.loadIngredients(),
                                Model.fetchProductReceiptsApi(),
                                Model.fetchImportReceiptsApi(),
                                Model.fetchMaterialReceiptsApi(),
                                Model.fetchProductExportsApi(),
                                Model.fetchProductionRequestsApi(),
                                Model.fetchSuppliersApi()
                            ]);
                        }
                    } catch (err) {
                        console.error('Lỗi làm mới dữ liệu DataGrid:', err);
                    }
                    actionEl.classList.remove('spinning');
                    renderMainOnly();
                    if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                        WhNotification.toast('Đã làm mới danh sách dữ liệu thành công!', 'success');
                    }
                    break;
                }

                case 'toggle-all-rows': {
                    const checked = actionEl.checked;
                    const rowChecks = mainMount.querySelectorAll('.wh-row-checkbox');
                    rowChecks.forEach(function (cb) {
                        cb.checked = checked;
                    });
                    break;
                }

                case 'toggle-data-mode': {
                    e.preventDefault();
                    Model.toggleDataMode();
                    headerMount.innerHTML = View.renderHeader(Model.getState());
                    renderMainOnly();
                    break;
                }

                case 'change-report-period-type': {
                    e.preventDefault();
                    const val = actionEl.getAttribute('data-value');
                    Model.setReportPeriodType(val);
                    await Model.fetchReportDataApi();
                    renderMainOnly();
                    break;
                }

                case 'change-report-item-type': {
                    e.preventDefault();
                    const val = actionEl.getAttribute('data-value');
                    Model.setReportItemType(val);
                    await Model.fetchReportDataApi();
                    renderMainOnly();
                    break;
                }

                case 'refresh-report': {
                    e.preventDefault();
                    await Model.fetchReportDataApi();
                    renderMainOnly();
                    if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                        WhNotification.toast('Đã làm mới dữ liệu báo cáo thống kê kho!', 'success');
                    }
                    break;
                }

                case 'export-report-excel': {
                    e.preventDefault();
                    const cfg = Model.getReportDataConfig();
                    const items = cfg.items || [];
                    let csv = '\uFEFFSTT,Mã hàng,Tên nguyên vật liệu / sản phẩm,Phân loại,ĐVT,Tồn đầu kỳ,Nhập trong kỳ,Xuất trong kỳ,Tồn cuối kỳ,Đơn giá (đ),Thành tiền (đ),Trạng thái\n';
                    items.forEach((it, idx) => {
                        csv += `${idx + 1},"${it.code}","${(it.name || '').replace(/"/g, '""')}","${it.typeLabel}","${it.unit}",${it.openingStock},${it.inQty},${it.outQty},${it.closingStock},${it.unitPrice},${it.totalValue},"${it.stockStatusLabel}"\n`;
                    });
                    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    const periodTitle = (cfg.period?.title || 'Thong_Ke_Kho').replace(/[\/\s]/g, '_');
                    a.download = `Bao_Cao_Ton_Xuat_Luu_Kho_${periodTitle}.csv`;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);
                    if (typeof WhNotification !== 'undefined' && WhNotification.toast) {
                        WhNotification.toast('Đã xuất báo cáo CSV (tương thích Excel) thành công!', 'success');
                    }
                    break;
                }

                case 'print-report': {
                    e.preventDefault();
                    window.print();
                    break;
                }

                case 'view-stock-card': {
                    e.preventDefault();
                    const itemId = actionEl.getAttribute('data-item-id');
                    const itemType = actionEl.getAttribute('data-item-type');
                    if (itemId && itemType) {
                        const cardData = await Model.fetchStockCardApi(itemId, itemType);
                        if (cardData) {
                            let existing = document.getElementById('wh-stock-card-overlay');
                            if (existing) existing.remove();
                            const tempDiv = document.createElement('div');
                            tempDiv.innerHTML = View.renderStockCardModal(cardData);
                            const overlay = tempDiv.firstElementChild;
                            if (overlay) {
                                overlay.id = 'wh-stock-card-overlay';
                                if (mainMount) {
                                    mainMount.appendChild(overlay);
                                } else {
                                    document.body.appendChild(overlay);
                                }
                            }
                        }
                    }
                    break;
                }

                case 'close-stock-card-modal': {
                    e.preventDefault();
                    Model.closeStockCardModal();
                    const overlay = document.getElementById('wh-stock-card-overlay');
                    if (overlay) {
                        overlay.remove();
                    } else {
                        renderMainOnly();
                    }
                    break;
                }
            }
        });

        // Đóng Thẻ kho khi bấm vào vùng nền mờ overlay bên ngoài cửa sổ
        document.addEventListener('click', function (e) {
            if (e.target && e.target.id === 'wh-stock-card-overlay') {
                Model.closeStockCardModal();
                e.target.remove();
            }
        });

        // Đóng Phiếu Nhập / Xuất hoặc Modal VTHH / Lệnh SX / NCC / Thẻ kho khi bấm phím Escape
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                const openPrDropdown = document.querySelector('.wh-pr-dropdown-menu[style*="display: block"]');
                if (openPrDropdown) {
                    openPrDropdown.style.display = 'none';
                    document.querySelectorAll('.wh-pr-select-trigger').forEach(el => el.classList.remove('is-open'));
                    return;
                }
                if (Model.getReportDataConfig && Model.getReportDataConfig().stockCardModal) {
                    Model.closeStockCardModal();
                    const overlay = document.getElementById('wh-stock-card-overlay');
                    if (overlay) {
                        overlay.remove();
                    } else {
                        renderMainOnly();
                    }
                } else if (Model.getState().supplierModal) {
                    Model.closeSupplierModal();
                    renderMainOnly();
                } else if (Model.getState().productionRequestModal) {
                    Model.closePrModal();
                    renderMainOnly();
                } else if (Model.getState().vthhModal) {
                    Model.closeVthhModal();
                    renderMainOnly();
                } else if (Model.getState().activeVoucherModal) {
                    Model.closeVoucherModal();
                    renderMainOnly();
                }
            }
        });

        document.addEventListener('change', async function (e) {
            const target = e.target;
            if (!target) return;

            const action = target.getAttribute('data-action');
            if (action === 'change-report-period-val') {
                Model.setReportPeriodValue(target.value);
                await Model.fetchReportDataApi();
                renderMainOnly();
                return;
            }

            if (action === 'change-report-year') {
                Model.setReportYear(target.value);
                await Model.fetchReportDataApi();
                renderMainOnly();
                return;
            }

            if (target.id === 'pr-form-product') {
                const selectedOpt = target.options[target.selectedIndex];
                const unit = selectedOpt ? selectedOpt.getAttribute('data-unit') : 'Hộp';
                const unitInput = document.getElementById('pr-form-unit');
                if (unitInput) unitInput.value = unit || 'Hộp';
                return;
            }

            if (target.id === 'wh-param-days-select') {
                Model.setPendingDaysParam(target.value);
                return;
            }

            if (action === 'change-inbound-supplier') {
                Model.setInboundNewSupplier(target.value);
                const codeInput = document.getElementById('wh-inbound-supplier-code');
                const draft = Model.getVoucherData('inbound');
                if (codeInput) {
                    codeInput.value = draft ? (draft.supplierCode || '') : '';
                }
                updateVoucherModalDom();
                return;
            }

            if (action === 'change-inbound-item') {
                const rowId = target.getAttribute('data-row-id');
                const changeRes = Model.changeInboundNewItem(rowId, target.value);
                if (changeRes && changeRes.duplicate) {
                    WhNotification.alert(changeRes.message, { type: 'warning', title: 'Trùng nguyên vật liệu' });
                }
                updateVoucherModalDom();
                return;
            }

            if (action === 'change-inbound-item-qty') {
                const rowId = target.getAttribute('data-row-id');
                Model.changeInboundNewItemQty(rowId, target.value);
                return;
            }

            if (action === 'change-pr-item-product') {
                const rowId = target.getAttribute('data-row-id');
                if (rowId && Model.changePrItemProduct) {
                    Model.changePrItemProduct(rowId, target.value);
                    renderMainOnly();
                }
                return;
            }

            if (action === 'change-pr-item-qty') {
                const rowId = target.getAttribute('data-row-id');
                if (rowId && Model.changePrItemQty) {
                    Model.changePrItemQty(rowId, target.value);
                    renderMainOnly();
                }
                return;
            }

            const filterId = target.getAttribute('data-filter-id');
            if (filterId === 'pr-status-filter') {
                Model.setPrStatusFilter(target.value);
                renderMainOnly();
            } else if (filterId === 'pr-receive-filter' || filterId === 'pr-assign-filter') {
                if (Model.setPrReceiveFilter) {
                    Model.setPrReceiveFilter(target.value);
                } else if (Model.setPrAssignmentFilter) {
                    Model.setPrAssignmentFilter(target.value);
                }
                renderMainOnly();
            } else if (filterId === 'inbound-status-filter') {
                Model.setOperationStatusFilter('nhap-kho', target.value);
                renderMainOnly();
            } else if (filterId === 'outbound-status-filter') {
                Model.setOperationStatusFilter('xuat-kho', target.value);
                renderMainOnly();
            }
        });

        document.addEventListener('input', async function (e) {
            const target = e.target;
            if (!target) return;

            // Kiểm tra trùng lặp thời gian thực khi nhập thông tin nhà cung cấp
            if (target.id === 'supplier-form-name') {
                const query = target.value.trim().toLowerCase();
                const hintEl = document.getElementById('supplier-name-dup-hint');
                const saveBtn = document.querySelector('[data-action="save-create-supplier"], [data-action="save-edit-supplier"]');
                const isEdit = saveBtn && saveBtn.getAttribute('data-action') === 'save-edit-supplier';
                const currentSupId = isEdit ? parseInt(saveBtn.getAttribute('data-supplier-id'), 10) : 0;

                if (!query) {
                    target.style.borderColor = '#cbd5e1';
                    if (hintEl) {
                        hintEl.style.display = 'none';
                        hintEl.textContent = '';
                    }
                    return;
                }

                const existingSuppliers = (typeof Model.getSuppliersList === 'function' ? Model.getSuppliersList() : (Model.getState().suppliers || []));
                const dup = existingSuppliers.find(s => {
                    const sid = s.id || s.supplierId;
                    if (isEdit && sid === currentSupId) return false;
                    return (s.name || s.supplierName || '').trim().toLowerCase() === query;
                });

                if (dup) {
                    target.style.borderColor = '#ef4444';
                    if (hintEl) {
                        hintEl.style.display = 'block';
                        hintEl.innerHTML = `⚠️ Nhà cung cấp "<strong>${escapeHtml(dup.name || dup.supplierName)}</strong>" đã tồn tại (Mã: <strong>${escapeHtml(dup.code || 'NCC' + (dup.supplierId || dup.id))}</strong>). Không thể thêm trùng!`;
                    }
                } else {
                    target.style.borderColor = '#22c55e';
                    if (hintEl) {
                        hintEl.style.display = 'none';
                        hintEl.textContent = '';
                    }
                }
                return;
            }

            if (target.id === 'supplier-form-phone' || target.id === 'supplier-form-mail') {
                target.style.borderColor = '#cbd5e1';
            }

            if (target.id === 'supplier-form-import-cost') {
                const raw = target.value.trim();
                const hintEl = document.getElementById('supplier-cost-dup-hint');
                if (!raw) {
                    target.style.borderColor = '#cbd5e1';
                    if (hintEl) { hintEl.style.display = 'none'; hintEl.textContent = ''; }
                    return;
                }
                const val = parseFloat(raw);
                const hasMoreThan1Dec = raw.includes('.') && (raw.split('.')[1] || '').length > 1;
                if (isNaN(val) || val < 0 || val > 100 || hasMoreThan1Dec) {
                    target.style.borderColor = '#ef4444';
                    if (hintEl) {
                        hintEl.style.display = 'block';
                        hintEl.innerHTML = hasMoreThan1Dec
                            ? '⚠️ Tỷ lệ phí nhập chỉ được có tối đa 1 chữ số thập phân (Ví dụ: 8.5)'
                            : '⚠️ Tỷ lệ phí nhập chỉ được từ 0% đến 100%';
                    }
                } else {
                    target.style.borderColor = '#22c55e';
                    if (hintEl) { hintEl.style.display = 'none'; hintEl.textContent = ''; }
                }
                return;
            }

            // Thanh tìm kiếm nhanh thành phẩm trong modal Yêu cầu sản xuất
            if (target.id === 'pr-product-search-input') {
                const query = target.value.toLowerCase().trim();
                const items = document.querySelectorAll('.wh-product-picker-item');
                let visibleCount = 0;
                items.forEach(function (el) {
                    const code = (el.getAttribute('data-code') || '').toLowerCase();
                    const name = (el.getAttribute('data-name') || '').toLowerCase();
                    if (!query || code.includes(query) || name.includes(query)) {
                        el.style.display = 'flex';
                        visibleCount++;
                    } else {
                        el.style.display = 'none';
                    }
                });
                const noResult = document.getElementById('pr-product-no-result');
                if (noResult) {
                    noResult.style.display = visibleCount === 0 ? 'block' : 'none';
                }
                return;
            }

            const inputAction = target.getAttribute('data-action');
            if (inputAction === 'search-pr-product-list') {
                const rowId = target.getAttribute('data-row-id');
                const query = target.value.toLowerCase().trim();
                const listEl = document.getElementById(`pr-prod-list-${rowId}`);
                if (listEl) {
                    const items = listEl.querySelectorAll('.wh-pr-dropdown-item');
                    let visibleCount = 0;
                    items.forEach(el => {
                        const name = (el.getAttribute('data-pname') || '').toLowerCase();
                        const code = (el.getAttribute('data-pcode') || '').toLowerCase();
                        if (!query || name.includes(query) || code.includes(query)) {
                            el.style.display = 'flex';
                            visibleCount++;
                        } else {
                            el.style.display = 'none';
                        }
                    });
                    const noRes = listEl.querySelector('.wh-pr-dropdown-no-results');
                    if (noRes) {
                        noRes.style.display = visibleCount === 0 ? 'block' : 'none';
                    }
                }
                return;
            }

            if (inputAction === 'change-pr-item-qty') {
                const rowId = target.getAttribute('data-row-id');
                if (rowId && Model.changePrItemQty) {
                    Model.changePrItemQty(rowId, target.value);
                    const modalData = Model.getPrModalData ? Model.getPrModalData() : null;
                    if (modalData && modalData.item && Array.isArray(modalData.item.items)) {
                        const totalQty = modalData.item.items.reduce((s, it) => s + (parseInt(it.quantityRequired, 10) || 0), 0);
                        const tfootEl = document.querySelector('.wh-voucher-body tfoot td[style*="color:#0284c7"]');
                        if (tfootEl) {
                            tfootEl.textContent = totalQty.toLocaleString('vi-VN');
                        }
                    }
                }
                return;
            }

            if (inputAction === 'change-inbound-item-qty') {
                if (target.value && target.value.length > 6) {
                    target.value = target.value.slice(0, 6);
                }
                const rowId = target.getAttribute('data-row-id');
                const row = Model.changeInboundNewItemQty(rowId, target.value);
                if (row) {
                    const amtCell = document.querySelector(`.wh-cell-text[data-cell="amount"][data-row-id="${rowId}"]`);
                    if (amtCell) {
                        amtCell.textContent = Number(row.amount || 0).toLocaleString('vi-VN') + ' đ';
                    }
                    const draft = Model.getVoucherData('inbound');
                    if (draft && draft.items) {
                        const totalQty = draft.items.reduce((s, it) => s + (Number(it.quantity) || 0), 0);
                        const totalAmt = draft.items.reduce((s, it) => s + (Number(it.amount) || 0), 0);
                        const totalQtyEl = document.getElementById('wh-total-inbound-qty');
                        if (totalQtyEl) {
                            totalQtyEl.textContent = totalQty.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                        }
                        const totalAmtEl = document.getElementById('wh-total-inbound-amount');
                        if (totalAmtEl) {
                            totalAmtEl.textContent = totalAmt.toLocaleString('vi-VN') + ' đ';
                        }
                    }
                }
                return;
            }

            if (target.id === 'wh-edit-image-url') {
                const val = target.value.trim();
                const previewImg = document.getElementById('wh-vthh-img-preview');
                const brokenBox = document.getElementById('wh-vthh-img-broken');
                if (!val) {
                    if (previewImg) {
                        previewImg.src = '';
                        previewImg.style.display = 'none';
                    }
                    if (brokenBox) {
                        brokenBox.style.display = 'inline-flex';
                    }
                } else {
                    if (previewImg) {
                        previewImg.src = val;
                        previewImg.style.display = 'block';
                    }
                    if (brokenBox) {
                        brokenBox.style.display = 'none';
                    }
                }
                return;
            }

            if (target.id === 'wh-global-search') {
                Model.setSearchKeyword(target.value);
                renderMainOnly();
                return;
            }

            if (target.getAttribute('data-action') === 'search-report') {
                const cursorPos = target.selectionStart;
                Model.setReportSearch(target.value);
                await Model.fetchReportDataApi();
                renderMainOnly();
                const newInput = mainMount.querySelector('[data-action="search-report"]');
                if (newInput) {
                    newInput.focus();
                    try {
                        newInput.setSelectionRange(cursorPos, cursorPos);
                    } catch (_) {}
                }
                return;
            }

            const opSearchType = target.getAttribute('data-op-search');
            if (opSearchType) {
                const cursorPos = target.selectionStart;
                Model.setOperationSearch(opSearchType, target.value);
                renderMainOnly();
                const newInput = mainMount.querySelector(`[data-op-search="${opSearchType}"]`);
                if (newInput) {
                    newInput.focus();
                    try {
                        newInput.setSelectionRange(cursorPos, cursorPos);
                    } catch (_) {}
                }
            }
        });
    }

    return {
        init
    };
})(WarehouseModel, WarehouseComponents);

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        WarehouseController.init();
    });
} else {
    WarehouseController.init();
}
