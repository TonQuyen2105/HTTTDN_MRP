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

    function init() {
        headerMount = document.getElementById('wh-header-mount');
        sidebarMount = document.getElementById('wh-sidebar-mount');
        mainMount = document.getElementById('wh-main-mount');

        if (!headerMount || !sidebarMount || !mainMount) {
            return;
        }

        renderAll();
        bindEvents();
    }

    function getViewPayloads() {
        const state = Model.getState();
        return {
            workspaceData: Model.getWorkspaceData(),
            cardsConfig: Model.getDashboardCardsConfig(),
            inboundConfig: Model.getInboundGridConfig(),
            outboundConfig: Model.getOutboundGridConfig(),
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
        mainMount.innerHTML = View.renderMainContent(state, getViewPayloads());
    }

    function bindEvents() {
        document.addEventListener('click', function (e) {
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
                    }
                    break;
                }

                // --- CÁC SỰ KIỆN TRÊN ĐƠN HÀNG, LỆNH KHO & PHIẾU KHO ---
                case 'open-voucher-modal': {
                    e.preventDefault();
                    const vType = actionEl.getAttribute('data-voucher-type') || 'inbound';
                    // Nếu đang từ Đơn mua hàng bấm "Lập lệnh nhập kho" -> chuyển sang tab Lệnh nhập kho và mở Lệnh nhập kho
                    if (vType === 'inbound-command') {
                        Model.setOperationTab('nhap-kho', 'lenh-nhap-kho');
                    } else if (vType === 'outbound-command') {
                        Model.setOperationTab('xuat-kho', 'lenh-xuat-kho');
                    }
                    Model.openVoucherModal(vType);
                    renderMainOnly();
                    break;
                }

                case 'close-voucher-modal': {
                    e.preventDefault();
                    Model.closeVoucherModal();
                    renderMainOnly();
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
                    const docType = actionEl.getAttribute('data-doc-type');
                    const rowId = actionEl.getAttribute('data-row-id');
                    if (docType && rowId) {
                        Model.toggleCommandRowFull(docType, rowId, actionEl.checked);
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

        // Đóng Phiếu Nhập / Xuất khi bấm phím Escape
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && Model.getState().activeVoucherModal) {
                Model.closeVoucherModal();
                renderMainOnly();
            }
        });

        document.addEventListener('change', function (e) {
            const target = e.target;
            if (!target) return;

            if (target.id === 'wh-param-days-select') {
                Model.setPendingDaysParam(target.value);
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
