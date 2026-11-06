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
            const actionEl = e.target.closest('[data-action]');
            if (!actionEl) {
                return;
            }

            const action = actionEl.getAttribute('data-action');

            switch (action) {
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
                            await Model.fetchMaterialReceiptsApi();
                            renderMainOnly();
                        } else if (menuId === 'nhap-kho') {
                            await Model.fetchProductReceiptsApi();
                            await Model.fetchImportReceiptsApi();
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
                        } else if (pageType === 'xuat-kho' && tabId === 'lenh-xuat-kho') {
                            await Model.fetchMaterialReceiptsApi();
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
                    }
                    const mode = (docNo || receiptId) ? 'view' : 'create';
                    Model.openVoucherModal(vType, receiptId || docNo, mode);
                    renderMainOnly();
                    if (vType === 'inbound' && mode === 'view' && (receiptId || docNo)) {
                        await Model.fetchImportReceiptDetailApi(receiptId || docNo);
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
                    Model.addInboundNewItemRow();
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
                    const cardId = actionEl.getAttribute('data-card-id');
                    if (cardId) {
                        const cardSection = document.querySelector(`[data-card-id="${cardId}"]`);
                        const refreshBtn = cardSection ? cardSection.querySelector('.wh-card-action-btn[data-action="refresh-card"]') : null;
                        if (refreshBtn) {
                            refreshBtn.classList.add('spinning');
                        }
                        setTimeout(function () {
                            Model.refreshCardTimestamp(cardId);
                            renderMainOnly();
                        }, 300);
                    }
                    break;
                }

                case 'refresh-datagrid': {
                    e.preventDefault();
                    actionEl.classList.add('spinning');
                    const curMenu = Model.getState().activeMenuId;
                    if (curMenu === 'danh-muc-vthh') {
                        try {
                            await Promise.all([Model.loadProducts(), Model.loadIngredients()]);
                        } catch (err) {
                            console.error('Lỗi làm mới VTHH:', err);
                        }
                    } else if (curMenu === 'xuat-kho') {
                        try {
                            await Model.fetchMaterialReceiptsApi();
                        } catch (err) {
                            console.error('Lỗi làm mới xuất kho:', err);
                        }
                    } else if (curMenu === 'nhap-kho') {
                        try {
                            await Promise.all([Model.fetchProductReceiptsApi(), Model.fetchImportReceiptsApi()]);
                        } catch (err) {
                            console.error('Lỗi làm mới nhập kho:', err);
                        }
                    }
                    setTimeout(function () {
                        renderMainOnly();
                    }, 250);
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
            }
        });

        // Đóng Phiếu Nhập / Xuất hoặc Modal VTHH khi bấm phím Escape
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                if (Model.getState().vthhModal) {
                    Model.closeVthhModal();
                    renderMainOnly();
                } else if (Model.getState().activeVoucherModal) {
                    Model.closeVoucherModal();
                    renderMainOnly();
                }
            }
        });

        document.addEventListener('change', function (e) {
            const target = e.target;
            if (!target) return;

            if (target.id === 'wh-param-days-select') {
                Model.setPendingDaysParam(target.value);
                return;
            }

            const action = target.getAttribute('data-action');
            if (action === 'change-inbound-supplier') {
                Model.setInboundNewSupplier(target.value);
                const codeInput = document.getElementById('wh-inbound-supplier-code');
                const draft = Model.getVoucherData('inbound');
                if (codeInput) {
                    codeInput.value = draft ? (draft.supplierCode || '') : '';
                }
                return;
            }

            if (action === 'change-inbound-item') {
                const rowId = target.getAttribute('data-row-id');
                Model.changeInboundNewItem(rowId, target.value);
                updateVoucherModalDom();
                return;
            }

            if (action === 'change-inbound-item-qty') {
                const rowId = target.getAttribute('data-row-id');
                Model.changeInboundNewItemQty(rowId, target.value);
                return;
            }

            const filterId = target.getAttribute('data-filter-id');
            if (filterId === 'inbound-status-filter') {
                Model.setOperationStatusFilter('nhap-kho', target.value);
                renderMainOnly();
            } else if (filterId === 'outbound-status-filter') {
                Model.setOperationStatusFilter('xuat-kho', target.value);
                renderMainOnly();
            }
        });

        document.addEventListener('input', function (e) {
            const target = e.target;
            if (!target) return;

            const inputAction = target.getAttribute('data-action');
            if (inputAction === 'change-inbound-item-qty') {
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

document.addEventListener('DOMContentLoaded', function () {
    WarehouseController.init();
});
