/**
 * Areas/Warehouse/Views/Shared/components.js
 * ---------------------------------------------------------
 * [VIEW LAYER - REUSABLE UI COMPONENTS]
 * Tập hợp các Component tái sử dụng cho phân hệ Kho hàng:
 * - WarehouseIcons: Bộ icon SVG chuẩn thiết kế
 * - HeaderComponent: Thanh điều hướng trên cùng
 * - SidebarComponent: Thanh menu dọc bên trái (hỗ trợ submenu & thu gọn mượt mà)
 * - ParameterPopoverComponent: Hộp thoại cấu hình "Chọn tham số"
 * - WidgetCardComponent: Thẻ bảng dữ liệu tổng quan (tái sử dụng cho 4 bảng ở Tổng quan)
 * - WorkspaceComponent: Giao diện trang "Bàn làm việc" (Hero Banner, Promo Card, Donut & Bar Cards)
 * - TabbedDataGridComponent: Component bảng dữ liệu đa tab tái sử dụng cho cả "Nhập kho" và "Xuất kho"
 * - StockVoucherModalComponent: Component Phiếu Nhập Kho (PNK00005) & Phiếu Xuất Kho (PXK00002) tái sử dụng
 * - MainContentComponent: Điều phối hiển thị nội dung theo mục Sidebar đang chọn
 */

const WarehouseComponents = (function () {

    // 1. Bộ SVG Icons dùng chung
    const Icons = {
        appLauncher: `<svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
            <circle cx="5" cy="5" r="2"/><circle cx="12" cy="5" r="2"/><circle cx="19" cy="5" r="2"/>
            <circle cx="5" cy="12" r="2"/><circle cx="12" cy="12" r="2"/><circle cx="19" cy="12" r="2"/>
            <circle cx="5" cy="19" r="2"/><circle cx="12" cy="19" r="2"/><circle cx="19" cy="19" r="2"/>
        </svg>`,
        brandWarehouse: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 9.5L12 3l9 6.5V20a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V9.5z"/>
            <path d="M9 21V13h6v8"/>
        </svg>`,
        search: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
        </svg>`,
        settings: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
        </svg>`,
        splitLayout: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="18" height="18" rx="2"/>
            <line x1="12" y1="3" x2="12" y2="21"/>
            <line x1="12" y1="12" x2="21" y2="12"/>
        </svg>`,
        plus: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
        </svg>`,
        arrowRight: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/>
        </svg>`,
        keyboard: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="2" y="4" width="20" height="16" rx="2"/>
            <line x1="6" y1="8" x2="6" y2="8"/><line x1="10" y1="8" x2="10" y2="8"/><line x1="14" y1="8" x2="14" y2="8"/><line x1="18" y1="8" x2="18" y2="8"/>
            <line x1="6" y1="12" x2="6" y2="12"/><line x1="10" y1="12" x2="10" y2="12"/><line x1="14" y1="12" x2="14" y2="12"/><line x1="18" y1="12" x2="18" y2="12"/>
            <line x1="8" y1="16" x2="16" y2="16"/>
        </svg>`,
        closeX: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
        </svg>`,
        calendar: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
        </svg>`,
        pin: `<svg width="12" height="12" viewBox="0 0 24 24" fill="#6b7280" stroke="#4b5563" stroke-width="1.8" style="margin-right:3px;vertical-align:-1px;">
            <path d="M16 3l5 5-7 7-4 1-3 3-2-2 3-3 1-4 7-7z"/>
        </svg>`,
        trashRed: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="3 6 5 6 21 6"/>
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
            <line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/>
        </svg>`,
        pencil: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
            <path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/>
        </svg>`,
        checkBlue: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#2563eb" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="20 6 9 17 4 12"/>
        </svg>`,
        docFile: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
            <line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/>
        </svg>`,
        checkCircle: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
            <polyline points="22 4 12 14.01 9 11.01"/>
        </svg>`,
        folderDoc: `<svg width="17" height="17" viewBox="0 0 24 24" fill="#3b82f6" stroke="#2563eb" stroke-width="1.5">
            <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
        </svg>`,
        chat: `<svg width="17" height="17" viewBox="0 0 24 24" fill="#3b82f6" stroke="#2563eb" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"/>
        </svg>`,
        bell: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
            <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
        </svg>`,
        helpCircle: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>`,
        moreCircle: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <circle cx="12" cy="12" r="1"/><circle cx="16" cy="12" r="1"/><circle cx="8" cy="12" r="1"/>
        </svg>`,
        ideaBulb: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="#f59e0b" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
            <path d="M9 18h6"/><path d="M10 22h4"/>
            <path d="M12 2a7 7 0 0 0-4 12.7V17h8v-2.3A7 7 0 0 0 12 2z"/>
        </svg>`,
        refresh: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="23 4 23 10 17 10"/>
            <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/>
        </svg>`,
        chevronDown: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="6 9 12 15 18 9"/>
        </svg>`,
        chevronUp: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="18 15 12 9 6 15"/>
        </svg>`,
        chevronRight: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="9 18 15 12 9 6"/>
        </svg>`,
        collapseLeft: `<svg class="wh-collapse-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
            <line x1="9" y1="3" x2="9" y2="21"/>
            <polyline points="15 9 12 12 15 15"/>
        </svg>`,
        // Sidebar Navigation Icons
        workspace: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/>
            <polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/>
        </svg>`,
        overview: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 9.5L12 3l9 6.5V20a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V9.5z"/>
            <rect x="9" y="13" width="6" height="8"/>
        </svg>`,
        items: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
            <polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/>
        </svg>`,
        warehouse: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 8.35V20a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8.35A2 2 0 0 1 3.26 6.5l8-3.2a2 2 0 0 1 1.48 0l8 3.2A2 2 0 0 1 22 8.35Z"/>
            <path d="M6 18h12"/><path d="M6 14h12"/>
        </svg>`,
        operations: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="8" cy="20" r="2"/><circle cx="18" cy="20" r="2"/>
            <path d="M2 3h3l2.6 12.4a2 2 0 0 0 2 1.6h9.8"/>
            <rect x="9" y="6" width="10" height="7" rx="1"/>
        </svg>`,
        report: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21.21 15.89A10 10 0 1 1 8 2.83"/>
            <path d="M22 12A10 10 0 0 0 12 2v10z"/>
        </svg>`,
        category: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="7" height="7" rx="1"/>
            <rect x="14" y="3" width="7" height="7" rx="1"/>
            <rect x="14" y="14" width="7" height="7" rx="1"/>
            <rect x="3" y="14" width="7" height="7" rx="1"/>
        </svg>`,
        system: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
        </svg>`
    };

    // Định dạng số thập phân chuẩn Việt Nam (500 -> 500,00 | 1000 -> 1.000,00)
    function formatVnNumber(num) {
        const n = Number(num) || 0;
        return n.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    /**
     * 2. HeaderComponent (Thanh Header trên cùng)
     */
    function renderHeader(state) {
        const avatarSvg = `data:image/svg+xml;utf8,` + encodeURIComponent(
            `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
                <rect width="64" height="64" fill="#cbd5e1"/>
                <circle cx="32" cy="24" r="12" fill="#475569"/>
                <path d="M12 56c0-11 9-20 20-20s20 9 20 20" fill="#475569"/>
            </svg>`
        );

        return `
            <header class="wh-header">
                <div class="wh-header-left">
                    <button type="button" class="wh-app-launcher" title="Ứng dụng hệ thống MRP">
                        ${Icons.appLauncher}
                    </button>
                    <a href="#tong-quan" class="wh-brand" data-action="go-overview">
                        <span class="wh-brand-logo">${Icons.brandWarehouse}</span>
                        <span class="wh-brand-title">Kho hàng</span>
                    </a>
                    <div class="wh-search-box">
                        <span class="wh-search-icon">${Icons.search}</span>
                        <input
                            type="text"
                            id="wh-global-search"
                            class="wh-search-input"
                            placeholder="Tìm kiếm"
                            value="${escapeHtml(state.searchKeyword)}"
                        />
                    </div>
                </div>

                <div class="wh-header-right">
                    <button
                        type="button"
                        class="wh-data-mode-btn"
                        data-action="toggle-data-mode"
                        title="Chuyển đổi giữa dữ liệu mẫu theo ảnh và dữ liệu mở rộng Dược phẩm MRP"
                    >
                        ${state.usePharmaFullData ? 'Dữ liệu: MRP Dược phẩm' : 'Dữ liệu: Theo ảnh mẫu'}
                    </button>
                    <button type="button" class="wh-icon-btn" title="Thiết lập">
                        ${Icons.settings}
                    </button>
                    <span class="wh-header-divider"></span>
                    <button type="button" class="wh-icon-btn" title="Tài liệu kho">
                        ${Icons.folderDoc}
                        <span class="wh-badge-dot"></span>
                    </button>
                    <button type="button" class="wh-icon-btn" title="Trao đổi nội bộ">
                        ${Icons.chat}
                        <span class="wh-badge-danger">29</span>
                    </button>
                    <button type="button" class="wh-icon-btn" title="Thông báo">
                        ${Icons.bell}
                    </button>
                    <button type="button" class="wh-icon-btn" title="Trợ giúp">
                        ${Icons.helpCircle}
                    </button>
                    <button type="button" class="wh-icon-btn" title="Tiện ích khác">
                        ${Icons.moreCircle}
                    </button>
                    <button type="button" class="wh-icon-btn" title="Gợi ý tối ưu tồn kho">
                        ${Icons.ideaBulb}
                    </button>
                    <button type="button" class="wh-avatar-btn" title="Tài khoản Thủ kho Dược">
                        <img src="${avatarSvg}" alt="User Avatar" />
                    </button>
                </div>
            </header>
        `;
    }

    /**
     * 3. SidebarComponent (Thanh điều hướng bên trái)
     */
    function renderSidebarItem(item, activeMenuId) {
        const hasChildren = Array.isArray(item.children) && item.children.length > 0;
        const isActive = item.id === activeMenuId;

        let rightElement = '';
        if (item.badge) {
            rightElement = `<span class="wh-nav-badge">${item.badge}</span>`;
        } else if (hasChildren) {
            rightElement = `<span class="wh-nav-arrow wh-nav-arrow-down">${Icons.chevronDown}</span>`;
        } else if (item.hasArrowRight) {
            rightElement = `<span class="wh-nav-arrow">${Icons.chevronRight}</span>`;
        }

        const submenuHtml = hasChildren
            ? `<div class="wh-submenu ${item.expanded ? 'open' : ''}" data-submenu-for="${item.id}">
                ${item.children.map(child => `
                    <button
                        type="button"
                        class="wh-submenu-item ${child.id === activeMenuId ? 'active' : ''}"
                        data-action="select-menu"
                        data-menu-id="${child.id}"
                    >
                        ${escapeHtml(child.title)}
                    </button>
                `).join('')}
               </div>`
            : '';

        return `
            <div class="wh-nav-group">
                <button
                    type="button"
                    class="wh-nav-item ${isActive && !hasChildren ? 'active' : ''} ${item.expanded ? 'expanded' : ''}"
                    data-action="${hasChildren ? 'toggle-submenu' : 'select-menu'}"
                    data-menu-id="${item.id}"
                    title="${escapeHtml(item.title)}"
                >
                    <span class="wh-nav-item-left">
                        <span class="wh-nav-icon">${Icons[item.icon] || Icons.overview}</span>
                        <span class="wh-nav-label">${escapeHtml(item.title)}</span>
                    </span>
                    ${rightElement}
                </button>
                ${submenuHtml}
            </div>
        `;
    }

    function renderSidebar(menuItems, state) {
        const topItems = menuItems.filter(i => i.section === 'top');
        const middleItems = menuItems.filter(i => i.section === 'middle');
        const bottomItems = menuItems.filter(i => i.section === 'bottom');

        return `
            <aside class="wh-sidebar ${state.sidebarCollapsed ? 'collapsed' : ''}" id="wh-sidebar">
                <nav class="wh-sidebar-nav">
                    ${topItems.map(item => renderSidebarItem(item, state.activeMenuId)).join('')}
                    <div class="wh-sidebar-divider"></div>
                    ${middleItems.map(item => renderSidebarItem(item, state.activeMenuId)).join('')}
                    <div class="wh-sidebar-divider"></div>
                    ${bottomItems.map(item => renderSidebarItem(item, state.activeMenuId)).join('')}
                </nav>
                <div class="wh-sidebar-footer">
                    <button type="button" class="wh-collapse-btn" data-action="collapse-sidebar" title="Thu gọn / Mở rộng thanh menu">
                        ${Icons.collapseLeft}
                        <span>Thu gọn</span>
                    </button>
                </div>
            </aside>
        `;
    }

    /**
     * 4. ParameterPopoverComponent (Hộp thoại "Chọn tham số")
     */
    function renderParameterPopover(state) {
        const dayOptions = [5, 15, 30, 60, 90];
        return `
            <div class="wh-param-popover ${state.isParamPopoverOpen ? 'open' : ''}" id="wh-param-popover">
                <div class="wh-param-header">
                    <span class="wh-param-title">Chọn tham số</span>
                    <button type="button" class="wh-param-close" data-action="close-param-popover" title="Đóng">&times;</button>
                </div>
                <div class="wh-param-body">
                    <span>Số ngày trước hạn</span>
                    <select class="wh-param-select" id="wh-param-days-select">
                        ${dayOptions.map(d => `
                            <option value="${d}" ${Number(state.pendingDaysParam) === d ? 'selected' : ''}>${d}</option>
                        `).join('')}
                    </select>
                    <span>ngày</span>
                </div>
                <div class="wh-param-footer">
                    <button type="button" class="wh-btn wh-btn-cancel" data-action="close-param-popover">Hủy</button>
                    <button type="button" class="wh-btn wh-btn-apply" data-action="apply-param-popover">Áp dụng</button>
                </div>
            </div>
        `;
    }

    /**
     * 5. WidgetCardComponent (Component Thẻ Bảng Tổng Quan tái sử dụng)
     */
    function renderWidgetCard(cardConfig, state) {
        const headersHtml = cardConfig.columns.map(col => `
            <th class="${col.align === 'right' ? 'wh-text-right' : 'wh-text-left'}">
                ${escapeHtml(col.label)}
            </th>
        `).join('');

        const rowsHtml = cardConfig.rows.length > 0
            ? cardConfig.rows.map(row => `
                <tr>
                    ${cardConfig.columns.map(col => {
                        if (col.isProductCol) {
                            return `
                                <td class="wh-text-left">
                                    <div class="wh-product-cell">
                                        <span class="wh-product-dot" style="background-color: ${row.color || '#3b82f6'};"></span>
                                        <span class="wh-product-name">${escapeHtml(String(row[col.key] ?? ''))}</span>
                                    </div>
                                </td>
                            `;
                        }
                        return `
                            <td class="${col.align === 'right' ? 'wh-text-right' : 'wh-text-left'}">
                                ${escapeHtml(String(row[col.key] ?? ''))}
                            </td>
                        `;
                    }).join('')}
                </tr>
            `).join('')
            : `<tr><td colspan="${cardConfig.columns.length}" class="wh-empty-row">Không có dữ liệu phù hợp</td></tr>`;

        const settingsBtnHtml = cardConfig.hasSettings
            ? `<button type="button" class="wh-card-action-btn" data-action="toggle-param-popover" title="Chọn tham số">
                ${Icons.settings}
               </button>`
            : '';

        const popoverHtml = cardConfig.hasSettings
            ? renderParameterPopover(state)
            : '';

        return `
            <section class="wh-card" data-card-id="${cardConfig.id}">
                ${popoverHtml}
                <div>
                    <div class="wh-card-header">
                        <h2 class="wh-card-title">${escapeHtml(cardConfig.title)}</h2>
                        <div class="wh-card-actions">
                            <button
                                type="button"
                                class="wh-card-action-btn"
                                data-action="refresh-card"
                                data-card-id="${cardConfig.id}"
                                title="Làm mới dữ liệu"
                            >
                                ${Icons.refresh}
                            </button>
                            ${settingsBtnHtml}
                        </div>
                    </div>

                    <div class="wh-table-wrapper">
                        <table class="wh-table">
                            <thead>
                                <tr>${headersHtml}</tr>
                            </thead>
                            <tbody>
                                ${rowsHtml}
                            </tbody>
                        </table>
                    </div>
                </div>

                <div class="wh-card-footer">
                    <span>Số liệu tính đến: <strong style="font-weight:500;">${escapeHtml(cardConfig.lastUpdated)}</strong></span>
                    <a href="#reload" class="wh-reload-link" data-action="refresh-card" data-card-id="${cardConfig.id}">Tải lại</a>
                </div>
            </section>
        `;
    }

    /**
     * 6. WorkspaceComponent (Giao diện trang "Bàn làm việc")
     */
    function renderWorkspace(wsData) {
        const r = 60;
        const c = 2 * Math.PI * r;
        const greenLen = (56.96 / 100) * c;
        const purpleLen = (43.04 / 100) * c;

        const donutSvg = `
            <svg width="176" height="176" viewBox="0 0 160 160">
                <g transform="rotate(-90 80 80)">
                    <circle
                        cx="80" cy="80" r="${r}"
                        fill="transparent"
                        stroke="#84cc16"
                        stroke-width="32"
                        stroke-dasharray="${greenLen - 2} ${c}"
                        stroke-dashoffset="0"
                    />
                    <circle
                        cx="80" cy="80" r="${r}"
                        fill="transparent"
                        stroke="#6366f1"
                        stroke-width="32"
                        stroke-dasharray="${purpleLen - 2} ${c}"
                        stroke-dashoffset="-${greenLen}"
                    />
                </g>
            </svg>
        `;

        const bannerWaveArt = `
            <svg class="wh-ws-banner-art" viewBox="0 0 600 260" fill="none" xmlns="http://www.w3.org/2000/svg">
                <defs>
                    <linearGradient id="waveGrad1" x1="0" y1="0" x2="1" y2="1">
                        <stop offset="0%" stop-color="#93c5fd" stop-opacity="0.55"/>
                        <stop offset="50%" stop-color="#3b82f6" stop-opacity="0.25"/>
                        <stop offset="100%" stop-color="#1e3a8a" stop-opacity="0.7"/>
                    </linearGradient>
                    <linearGradient id="waveGrad2" x1="1" y1="0" x2="0" y2="1">
                        <stop offset="0%" stop-color="#e0e7ff" stop-opacity="0.65"/>
                        <stop offset="60%" stop-color="#60a5fa" stop-opacity="0.3"/>
                        <stop offset="100%" stop-color="#1d4ed8" stop-opacity="0.1"/>
                    </linearGradient>
                </defs>
                <path d="M120 -30 C 40 90, 190 220, 340 270 L 430 270 C 260 190, 140 80, 220 -30 Z" fill="url(#waveGrad1)"/>
                <path d="M290 -40 C 240 110, 380 210, 580 180 C 460 140, 370 50, 410 -40 Z" fill="url(#waveGrad2)"/>
                <ellipse cx="450" cy="145" rx="155" ry="95" transform="rotate(-18 450 145)" stroke="rgba(255,255,255,0.32)" stroke-width="18" fill="none"/>
                <ellipse cx="380" cy="175" rx="140" ry="80" transform="rotate(14 380 175)" stroke="rgba(147,197,253,0.28)" stroke-width="12" fill="none"/>
            </svg>
        `;

        const promoArt = `
            <svg class="wh-ws-promo-art" viewBox="0 0 180 180" fill="none" xmlns="http://www.w3.org/2000/svg">
                <circle cx="135" cy="125" r="56" stroke="rgba(99,102,241,0.22)" stroke-width="18"/>
                <ellipse cx="115" cy="140" rx="46" ry="24" transform="rotate(-30 115 140)" fill="rgba(147,197,253,0.45)"/>
                <circle cx="150" cy="70" r="24" fill="rgba(244,114,182,0.22)"/>
            </svg>
        `;

        return `
            <div class="wh-page-header">
                <h1 class="wh-page-title">Bàn làm việc</h1>
            </div>

            <div class="wh-ws-hero-grid">
                <div class="wh-ws-banner">
                    ${bannerWaveArt}
                    <div class="wh-ws-date">${escapeHtml(wsData.dateText)}</div>
                    <div class="wh-ws-greeting-box">
                        <h2 class="wh-ws-greeting-title">Xin chào ${escapeHtml(wsData.userName)}! 👋</h2>
                        <p class="wh-ws-greeting-sub">${escapeHtml(wsData.greetingSub)}</p>
                    </div>
                </div>

                <div class="wh-ws-promo">
                    ${promoArt}
                    <div>
                        <div class="wh-ws-promo-icon">${Icons.brandWarehouse}</div>
                        <p class="wh-ws-promo-text">${escapeHtml(wsData.promoText)}</p>
                    </div>
                    <button type="button" class="wh-ws-promo-btn" data-action="select-menu" data-menu-id="quan-ly-kho">
                        <span>Xem chi tiết</span>
                        ${Icons.arrowRight}
                    </button>
                </div>
            </div>

            <div class="wh-dashboard-grid">
                <section class="wh-card" data-card-id="${wsData.orderCard.id}">
                    <div>
                        <div class="wh-card-header">
                            <div class="wh-ws-card-header-left">
                                <h2 class="wh-card-title">${escapeHtml(wsData.orderCard.title)}</h2>
                                <button type="button" class="wh-card-action-btn" data-action="refresh-card" data-card-id="${wsData.orderCard.id}" title="Làm mới">
                                    ${Icons.refresh}
                                </button>
                            </div>
                        </div>

                        <div class="wh-ws-donut-layout">
                            <div class="wh-ws-donut-wrap">
                                ${donutSvg}
                            </div>
                            <div>
                                <div class="wh-ws-total-box">
                                    <div class="wh-ws-total-label">Cần thực hiện</div>
                                    <div class="wh-ws-total-value">${wsData.orderCard.totalPending}</div>
                                </div>
                                <div class="wh-ws-stat-list">
                                    ${wsData.orderCard.segments.map(seg => `
                                        <div class="wh-ws-stat-row">
                                            <div class="wh-ws-stat-left">
                                                <span class="wh-product-dot" style="background-color: ${seg.color};"></span>
                                                <span>${escapeHtml(seg.label)} <span class="wh-ws-stat-percent">(${escapeHtml(seg.percentText)})</span></span>
                                            </div>
                                            <span class="wh-ws-stat-count">${seg.count}</span>
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="wh-ws-card-footer-right">
                        <a href="#xuat-kho" class="wh-ws-more-link" data-action="select-menu" data-menu-id="xuat-kho">Xem thêm</a>
                    </div>
                </section>

                <section class="wh-card" data-card-id="${wsData.outboundCommandCard.id}">
                    <div>
                        <div class="wh-card-header">
                            <div class="wh-ws-card-header-left">
                                <h2 class="wh-card-title">${escapeHtml(wsData.outboundCommandCard.title)}</h2>
                                <button type="button" class="wh-card-action-btn" data-action="refresh-card" data-card-id="${wsData.outboundCommandCard.id}" title="Làm mới">
                                    ${Icons.refresh}
                                </button>
                            </div>
                        </div>

                        <div class="wh-ws-total-box">
                            <div class="wh-ws-total-label">Cần thực hiện</div>
                            <div class="wh-ws-total-value">${wsData.outboundCommandCard.totalPending}</div>
                        </div>

                        <div class="wh-ws-bar-container">
                            <div class="wh-ws-bar-track" title="Chưa thực hiện: 10 (100%)"></div>
                            <div class="wh-ws-bar-legend">
                                ${wsData.outboundCommandCard.items.map(item => `
                                    <div class="wh-ws-stat-row" title="${escapeHtml(item.fullLabel)}">
                                        <div class="wh-ws-stat-left">
                                            <span class="wh-product-dot" style="background-color: ${item.color};"></span>
                                            <span>${escapeHtml(item.label)}</span>
                                        </div>
                                        <span class="wh-ws-stat-count">${item.count}</span>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>

                    <div class="wh-ws-card-footer-right">
                        <a href="#xuat-kho" class="wh-ws-more-link" data-action="select-menu" data-menu-id="xuat-kho">Xem thêm</a>
                    </div>
                </section>
            </div>
        `;
    }

    /**
     * Sub-component tái sử dụng: Ô "Tham chiếu" chứa các Chip Tags (VD: DMH00028 ×, PNK00039 ×)
     */
    function renderReferenceTagInput(docType, tags) {
        const safeTags = Array.isArray(tags) ? tags : [];
        return `
            <div class="wh-tag-input-box">
                ${safeTags.map(tag => `
                    <span class="wh-ref-chip">
                        <span>${escapeHtml(tag)}</span>
                        <button
                            type="button"
                            class="wh-ref-chip-remove"
                            data-action="remove-ref-tag"
                            data-doc-type="${escapeHtml(docType)}"
                            data-tag="${escapeHtml(tag)}"
                            title="Gỡ tham chiếu"
                        >&times;</button>
                    </span>
                `).join('')}
                <button type="button" class="wh-input-addon-btn" title="Chọn chứng từ tham chiếu">${Icons.search}</button>
            </div>
        `;
    }

    /**
     * Sub-component tái sử dụng: Thanh phân trang đôi dưới chân bảng Đơn hàng & Lệnh kho
     */
    function renderTableSplitFooter(totalCount, rangeText) {
        return `
            <div class="wh-vtable-split-footer">
                <div>Tổng số: <strong>${totalCount}</strong></div>
                <div class="wh-dg-footer-right">
                    <span>Số dòng/trang</span>
                    <select class="wh-dg-page-size">
                        <option value="20" selected>20</option>
                        <option value="50">50</option>
                    </select>
                    <span class="wh-dg-range">${escapeHtml(rangeText)}</span>
                    <div class="wh-dg-pager">
                        <button type="button" class="wh-dg-page-btn" disabled>|&lt;</button>
                        <button type="button" class="wh-dg-page-btn" disabled>&lt;</button>
                        <button type="button" class="wh-dg-page-btn" disabled>&gt;</button>
                        <button type="button" class="wh-dg-page-btn" disabled>&gt;|</button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * 7A. OrderDetailModalComponent (Component Tái sử dụng cho cả "Đơn mua hàng DMH00006" & "Đơn đặt hàng DHTP00402")
     */
    function renderOrderDetailModal(order) {
        const totalReqQty = order.items.reduce((s, r) => s + (Number(r.reqQty) || 0), 0);
        const totalDoneQty = order.items.reduce((s, r) => s + (Number(r.doneQty) || 0), 0);
        const rangeText = order.items.length > 0 ? `1 - ${order.items.length}` : '0 - 0';

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window">
                    <!-- Thanh tiêu đề trên cùng -->
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left">
                            <h2 class="wh-voucher-title">${escapeHtml(order.title)}</h2>
                        </div>
                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" title="Phím tắt">${Icons.keyboard}</button>
                            <button type="button" class="wh-voucher-win-btn" title="Tùy chỉnh">${Icons.settings}</button>
                            <button type="button" class="wh-voucher-win-btn" title="Hướng dẫn">${Icons.helpCircle}</button>
                            <button type="button" class="wh-voucher-win-btn" data-action="close-voucher-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <!-- Thân Đơn hàng -->
                    <div class="wh-voucher-body">
                        <!-- Vùng Master -->
                        <div class="wh-voucher-master">
                            <div class="wh-voucher-master-left">
                                <!-- Dòng 1: Mã NCC / KH + Tên NCC / KH -->
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.partnerCodeLabel)}</label>
                                    <div class="wh-input-group">
                                        <select class="wh-select-control">
                                            ${order.partnerCodeOptions.map(opt => `
                                                <option value="${escapeHtml(opt)}" ${opt === order.partnerCode ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                            `).join('')}
                                        </select>
                                        <button type="button" class="wh-input-addon-btn" title="Thêm mới">${Icons.plus}</button>
                                    </div>
                                </div>
                                <div class="wh-form-group wh-col-span-2">
                                    <label class="wh-form-label">${escapeHtml(order.partnerNameLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(order.partnerName)}" />
                                </div>

                                <!-- Dòng 2: Người liên hệ/nhận + Số điện thoại + Đơn vị đề nghị mua/bán -->
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.contactPersonLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(order.contactPerson)}" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.phoneLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(order.phone)}" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.departmentLabel)}</label>
                                    <select class="wh-select-control">
                                        ${order.departmentOptions.map(opt => `
                                            <option value="${escapeHtml(opt)}" ${opt === order.department ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                        `).join('')}
                                    </select>
                                </div>

                                <!-- Dòng 3: Ngày giao hàng / Hạn giao hàng + Diễn giải -->
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.deliveryDateLabel)}</label>
                                    <div class="wh-input-group">
                                        <input type="text" class="wh-input-control" value="${escapeHtml(order.deliveryDate)}" />
                                        <button type="button" class="wh-input-addon-btn" title="Chọn ngày">${Icons.calendar}</button>
                                    </div>
                                </div>
                                <div class="wh-form-group wh-col-span-2">
                                    <label class="wh-form-label">Diễn giải</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(order.description)}" />
                                </div>

                                <!-- Dòng 4: Tham chiếu (chiếm trọn 3 cột) -->
                                <div class="wh-form-group wh-col-span-3">
                                    <label class="wh-form-label">Tham chiếu</label>
                                    ${renderReferenceTagInput(order.docType, order.referenceTags)}
                                </div>
                            </div>

                            <!-- Cột phải: Số đơn hàng + Ngày đơn hàng + Tình trạng nhập/xuất kho -->
                            <div class="wh-voucher-master-right">
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.orderNoLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(order.orderNo)}" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.orderDateLabel)}</label>
                                    <div class="wh-input-group">
                                        <input type="text" class="wh-input-control" value="${escapeHtml(order.orderDate)}" />
                                        <button type="button" class="wh-input-addon-btn" title="Chọn ngày">${Icons.calendar}</button>
                                    </div>
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(order.statusLabel)}</label>
                                    <select class="wh-select-control">
                                        ${order.statusOptions.map(opt => `
                                            <option value="${escapeHtml(opt)}" ${opt === order.statusValue ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                        `).join('')}
                                    </select>
                                </div>
                            </div>
                        </div>

                        <!-- Vùng Chi tiết Hàng hóa -->
                        <div class="wh-voucher-detail">
                            <h3 class="wh-voucher-section-title">Hàng hóa</h3>

                            <div class="wh-vtable-wrap">
                                <table class="wh-vtable">
                                    <thead>
                                        <tr>
                                            <th style="width: 40px;">#</th>
                                            <th style="width: 120px;">Mã hàng</th>
                                            <th>Tên hàng</th>
                                            <th style="width: 140px;">Mã quy cách</th>
                                            <th style="width: 150px;">${escapeHtml(order.warehouseColLabel)}</th>
                                            <th style="width: 115px;">Đơn vị tính</th>
                                            <th style="width: 130px;" class="wh-text-right">${escapeHtml(order.reqQtyColLabel)}</th>
                                            <th style="width: 130px;" class="wh-text-right">${escapeHtml(order.doneQtyColLabel)}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${order.items.map((row, idx) => `
                                            <tr>
                                                <td>${idx + 1}</td>
                                                <td>${escapeHtml(row.itemCode)}</td>
                                                <td>${escapeHtml(row.itemName)}</td>
                                                <td>${escapeHtml(row.specCode)}</td>
                                                <td>${escapeHtml(row.warehouse)}</td>
                                                <td>${escapeHtml(row.unit)}</td>
                                                <td class="wh-text-right">${formatVnNumber(row.reqQty)}</td>
                                                <td class="wh-text-right" style="color:#6b7280;">${formatVnNumber(row.doneQty)}</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                    <tfoot>
                                        <tr>
                                            <td colspan="6"></td>
                                            <td class="wh-text-right">${formatVnNumber(totalReqQty)}</td>
                                            <td class="wh-text-right">${formatVnNumber(totalDoneQty)}</td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>

                            ${renderTableSplitFooter(order.items.length, rangeText)}
                        </div>
                    </div>

                    <!-- Thanh hành động dưới đáy: [ Lập lệnh nhập/xuất kho ] [ Hoàn thành ] -->
                    <div class="wh-voucher-bottombar">
                        <button
                            type="button"
                            class="wh-btn wh-btn-apply wh-btn-icon-inline"
                            data-action="open-voucher-modal"
                            data-voucher-type="${escapeHtml(order.primaryActionTarget)}"
                        >
                            ${Icons.docFile}
                            <span>${escapeHtml(order.primaryActionLabel)}</span>
                        </button>
                        <button
                            type="button"
                            class="wh-btn wh-btn-cancel wh-btn-icon-inline"
                            data-action="close-voucher-modal"
                        >
                            ${Icons.checkCircle}
                            <span>Hoàn thành</span>
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * 7B. CommandDetailModalComponent (Component Tái sử dụng cho cả "Lệnh nhập kho LNK00039" & "Lệnh xuất kho LXK00088")
     */
    function renderCommandDetailModal(cmd) {
        const totalReqQty = cmd.items.reduce((s, r) => s + (Number(r.reqQty) || 0), 0);
        const totalActualQty = cmd.items.reduce((s, r) => s + (Number(r.actualQty) || 0), 0);
        const rangeText = cmd.items.length > 0 ? `1 - ${cmd.items.length}` : '0 - 0';

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window">
                    <!-- Thanh tiêu đề trên cùng kèm ô Chọn số đơn hàng -->
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left">
                            <h2 class="wh-voucher-title">${escapeHtml(cmd.title)}</h2>
                            <div class="wh-top-order-picker">
                                <select class="wh-top-order-select">
                                    ${cmd.topSearchOptions.map((opt, idx) => `
                                        <option value="${escapeHtml(opt)}" ${idx === 0 ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                    `).join('')}
                                </select>
                                <button type="button" class="wh-top-order-search-btn" title="Tìm kiếm đơn hàng">${Icons.search}</button>
                            </div>
                        </div>

                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" title="Phím tắt">${Icons.keyboard}</button>
                            <button type="button" class="wh-voucher-win-btn" title="Tùy chỉnh">${Icons.settings}</button>
                            <button type="button" class="wh-voucher-win-btn" title="Hướng dẫn">${Icons.helpCircle}</button>
                            <button type="button" class="wh-voucher-win-btn" data-action="close-voucher-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <!-- Thân Lệnh Nhập / Xuất Kho -->
                    <div class="wh-voucher-body">
                        <!-- Vùng Master (3 khu vực: Trái - Giữa - Nút phải) -->
                        <div class="wh-voucher-master has-far-right">
                            <div class="wh-voucher-master-left">
                                <!-- Dòng 1: Mã đối tượng + Tên đối tượng giao/nhận hàng -->
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Mã đối tượng</label>
                                    <div class="wh-input-group">
                                        <select class="wh-select-control">
                                            ${cmd.partnerCodeOptions.map(opt => `
                                                <option value="${escapeHtml(opt)}" ${opt === cmd.partnerCode ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                            `).join('')}
                                        </select>
                                        <button type="button" class="wh-input-addon-btn" title="Thêm đối tượng">${Icons.plus}</button>
                                    </div>
                                </div>
                                <div class="wh-form-group wh-col-span-2">
                                    <label class="wh-form-label">${escapeHtml(cmd.partnerNameLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(cmd.partnerName)}" />
                                </div>

                                <!-- Dòng 2: Kho nhập/xuất + Chi nhánh + Người giao/nhận -->
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(cmd.warehouseLabel)}</label>
                                    <select class="wh-select-control">
                                        ${cmd.warehouseOptions.map(opt => `
                                            <option value="${escapeHtml(opt)}" ${opt === cmd.warehouse ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                        `).join('')}
                                    </select>
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Chi nhánh</label>
                                    <select class="wh-select-control">
                                        ${cmd.branchOptions.map(opt => `
                                            <option value="${escapeHtml(opt)}" ${opt === cmd.branch ? 'selected' : ''}>${escapeHtml(opt)}</option>
                                        `).join('')}
                                    </select>
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(cmd.personLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(cmd.personName)}" />
                                </div>

                                <!-- Dòng 3: Diễn giải (2 cột) + Tham chiếu (1 cột) -->
                                <div class="wh-form-group wh-col-span-2">
                                    <label class="wh-form-label">Diễn giải</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(cmd.description)}" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Tham chiếu</label>
                                    ${renderReferenceTagInput(cmd.docType, cmd.referenceTags)}
                                </div>
                            </div>

                            <!-- Cột giữa: Số lệnh + Hạn nhập/xuất + Tình trạng thực hiện -->
                            <div class="wh-voucher-master-right">
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(cmd.commandNoLabel)}</label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(cmd.commandNo)}" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(cmd.dueDateLabel)}</label>
                                    <div class="wh-input-group">
                                        <input type="text" class="wh-input-control" value="${escapeHtml(cmd.dueDate)}" />
                                        <button type="button" class="wh-input-addon-btn" title="Chọn hạn">${Icons.calendar}</button>
                                    </div>
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Tình trạng thực hiện</label>
                                    <input type="text" class="wh-input-control wh-input-readonly" value="${escapeHtml(cmd.statusText)}" readonly />
                                </div>
                            </div>

                            <!-- Cột ngoài cùng bên phải: Nút Đọc phiếu giao hàng / Đề xuất lô FEFO -->
                            <div class="wh-voucher-master-far-right">
                                <button type="button" class="wh-doc-scan-btn">
                                    ${Icons.docFile}
                                    <span>${escapeHtml(cmd.topRightActionLabel)}</span>
                                </button>
                            </div>
                        </div>

                        <!-- Vùng Chi tiết Hàng hóa -->
                        <div class="wh-voucher-detail">
                            <h3 class="wh-voucher-section-title">Hàng hóa</h3>

                            <div class="wh-vtable-wrap">
                                <table class="wh-vtable">
                                    <thead>
                                        <tr>
                                            <th style="width: 38px;">#</th>
                                            <th style="width: 120px;">${Icons.pin}Mã hàng</th>
                                            <th>Tên hàng</th>
                                            <th style="width: 110px;">Mã quy cách</th>
                                            <th style="width: 90px;">ĐVT</th>
                                            <th style="width: 95px;">Vị trí</th>
                                            <th style="width: 115px;" class="wh-text-right">${escapeHtml(cmd.reqQtyColLabel)}</th>
                                            <th style="width: 115px;" class="wh-text-right">${escapeHtml(cmd.actualQtyColLabel)}</th>
                                            <th style="width: 105px; text-align:center;">Đánh dấu đủ</th>
                                            <th style="width: 100px;">Số lô</th>
                                            <th style="width: 110px;">Hạn sử dụng</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${cmd.items.map((row, idx) => `
                                            <tr>
                                                <td>${idx + 1}</td>
                                                <td>${escapeHtml(row.itemCode)}</td>
                                                <td>${escapeHtml(row.itemName)}</td>
                                                <td>${escapeHtml(row.specCode)}</td>
                                                <td>${escapeHtml(row.unit)}</td>
                                                <td>${escapeHtml(row.location)}</td>
                                                <td class="wh-text-right">${formatVnNumber(row.reqQty)}</td>
                                                <td class="wh-text-right">${formatVnNumber(row.actualQty)}</td>
                                                <td style="text-align:center;">
                                                    <input
                                                        type="checkbox"
                                                        class="wh-dg-checkbox"
                                                        data-action="toggle-cmd-row-full"
                                                        data-doc-type="${escapeHtml(cmd.docType)}"
                                                        data-row-id="${row.id}"
                                                        ${row.isFull ? 'checked' : ''}
                                                    />
                                                </td>
                                                <td>${escapeHtml(row.batchNo)}</td>
                                                <td>${escapeHtml(row.expiryDate)}</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                    <tfoot>
                                        <tr>
                                            <td colspan="6"></td>
                                            <td class="wh-text-right">${formatVnNumber(totalReqQty)}</td>
                                            <td class="wh-text-right">${formatVnNumber(totalActualQty)}</td>
                                            <td colspan="3"></td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>

                            ${renderTableSplitFooter(cmd.items.length, rangeText)}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * 7C. StockVoucherModalComponent (Component Tái sử dụng cho cả Phiếu Nhập Kho & Phiếu Xuất Kho)
     * - Inbound: "Phiếu nhập kho mua hàng PNK00005"
     * - Outbound: "Phiếu xuất kho bán hàng PXK00002"
     */
    function renderStockVoucherModal(voucher) {
        const isInbound = voucher.voucherType === 'inbound';
        const fullTitle = `Phiếu ${voucher.selectedSubType.toLowerCase()} ${voucher.voucherNo}`;
        const totalQuantity = voucher.items.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0);
        const rangeText = voucher.items.length > 0 ? `1 - ${voucher.items.length}` : '0 - 0';

        // Phần thông tin chung bên trái (Master Left Grid) khác nhau nhẹ giữa Phiếu Nhập và Phiếu Xuất đúng theo 2 ảnh mẫu
        const masterLeftFieldsHtml = isInbound
            ? `
                <!-- Dòng 1: Mã đối tượng + Tên đối tượng giao hàng -->
                <div class="wh-form-group">
                    <label class="wh-form-label">Mã đối tượng</label>
                    <div class="wh-input-group">
                        <select class="wh-select-control">
                            ${voucher.partnerCodeOptions.map(opt => `
                                <option value="${escapeHtml(opt)}" ${opt === voucher.partnerCode ? 'selected' : ''}>${escapeHtml(opt)}</option>
                            `).join('')}
                        </select>
                        <button type="button" class="wh-input-addon-btn" title="Thêm nhà cung cấp">${Icons.plus}</button>
                    </div>
                </div>
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Tên đối tượng giao hàng</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.partnerName)}" />
                </div>

                <!-- Dòng 2: Kho nhập * + Chi nhánh + Người giao -->
                <div class="wh-form-group">
                    <label class="wh-form-label">Kho nhập <span class="wh-req">*</span></label>
                    <select class="wh-select-control">
                        ${voucher.warehouseOptions.map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.warehouse ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Chi nhánh</label>
                    <select class="wh-select-control">
                        ${voucher.branchOptions.map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.branch ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Người giao</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.deliverer)}" />
                </div>

                <!-- Dòng 3: Tham chiếu + Diễn giải -->
                <div class="wh-form-group">
                    <label class="wh-form-label">Tham chiếu</label>
                    <div class="wh-input-group">
                        <input type="text" class="wh-input-control" value="${escapeHtml(voucher.reference)}" />
                        <button type="button" class="wh-input-addon-btn" title="Chọn chứng từ tham chiếu">${Icons.search}</button>
                    </div>
                </div>
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Diễn giải</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.description)}" />
                </div>
            `
            : `
                <!-- Dòng 1 (Phiếu Xuất): Mã đối tượng + Tên đối tượng nhận hàng + Kho xuất * -->
                <div class="wh-form-group">
                    <label class="wh-form-label">Mã đối tượng</label>
                    <select class="wh-select-control">
                        ${voucher.partnerCodeOptions.map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.partnerCode ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Tên đối tượng nhận hàng</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.partnerName)}" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Kho xuất <span class="wh-req">*</span></label>
                    <select class="wh-select-control">
                        ${voucher.warehouseOptions.map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.warehouse ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>

                <!-- Dòng 2 (Phiếu Xuất): Chi nhánh + Người nhận + Địa chỉ giao hàng -->
                <div class="wh-form-group">
                    <label class="wh-form-label">Chi nhánh</label>
                    <select class="wh-select-control">
                        ${voucher.branchOptions.map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.branch ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Người nhận</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.receiver)}" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Địa chỉ giao hàng</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.deliveryAddress)}" />
                </div>

                <!-- Dòng 3 (Phiếu Xuất): Diễn giải + Tham chiếu -->
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Diễn giải</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.description)}" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Tham chiếu</label>
                    <div class="wh-input-group">
                        <input type="text" class="wh-input-control" value="${escapeHtml(voucher.reference)}" />
                        <button type="button" class="wh-input-addon-btn" title="Chọn chứng từ tham chiếu">${Icons.search}</button>
                    </div>
                </div>
            `;

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window">
                    <!-- Thanh tiêu đề trên cùng của Phiếu -->
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left">
                            <h2 class="wh-voucher-title">${escapeHtml(fullTitle)}</h2>
                        </div>

                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" title="Phím tắt">${Icons.keyboard}</button>
                            <button type="button" class="wh-voucher-win-btn" title="Tùy chỉnh mẫu phiếu">${Icons.settings}</button>
                            <button type="button" class="wh-voucher-win-btn" title="Hướng dẫn">${Icons.helpCircle}</button>
                            <button type="button" class="wh-voucher-win-btn" data-action="close-voucher-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <!-- Thân phiếu (Master + Detail + Đính kèm) -->
                    <div class="wh-voucher-body">
                        <!-- Vùng Master -->
                        <div class="wh-voucher-master">
                            <div class="wh-voucher-master-left">
                                ${masterLeftFieldsHtml}
                            </div>

                            <div class="wh-voucher-master-right">
                                <div class="wh-form-group">
                                    <label class="wh-form-label">
                                        ${isInbound ? 'Số phiếu nhập kho' : 'Số phiếu xuất kho'} <span class="wh-req">*</span>
                                    </label>
                                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.voucherNo)}" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">
                                        ${isInbound ? 'Ngày nhập kho' : 'Ngày xuất kho'} <span class="wh-req">*</span>
                                    </label>
                                    <div class="wh-input-group">
                                        <input type="text" class="wh-input-control" value="${escapeHtml(voucher.voucherDate)}" />
                                        <button type="button" class="wh-input-addon-btn" title="Chọn ngày giờ">${Icons.calendar}</button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Vùng Chi tiết Hàng hóa -->
                        <div class="wh-voucher-detail">
                            <h3 class="wh-voucher-section-title">Hàng hóa</h3>

                            <div class="wh-vtable-wrap">
                                <table class="wh-vtable">
                                    <thead>
                                        <tr>
                                            <th style="width: 38px;">#</th>
                                            <th style="width: 155px;">${Icons.pin}Mã hàng</th>
                                            <th>Tên hàng</th>
                                            <th style="width: 115px;">Mã quy cách</th>
                                            <th style="width: 80px;">ĐVT</th>
                                            <th style="width: 105px;">Vị trí</th>
                                            <th style="width: 115px;" class="wh-text-right">${escapeHtml(voucher.quantityColumnLabel)}</th>
                                            <th style="width: 110px;">Số lô</th>
                                            <th style="width: 115px;">Hạn sử dụng</th>
                                            <th style="width: 40px;"></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${voucher.items.length > 0 ? voucher.items.map((row, idx) => `
                                            <tr>
                                                <td>${idx + 1}</td>
                                                <td>${escapeHtml(row.itemCode)}</td>
                                                <td>${escapeHtml(row.itemName)}</td>
                                                <td>${escapeHtml(row.specCode)}</td>
                                                <td>${escapeHtml(row.unit)}</td>
                                                <td>${escapeHtml(row.location)}</td>
                                                <td class="wh-text-right">${formatVnNumber(row.quantity)}</td>
                                                <td>${escapeHtml(row.batchNo)}</td>
                                                <td>${escapeHtml(row.expiryDate)}</td>
                                                <td style="text-align:center;">
                                                    <button
                                                        type="button"
                                                        class="wh-vtable-del-btn"
                                                        data-action="delete-voucher-row"
                                                        data-voucher-type="${voucher.voucherType}"
                                                        data-row-id="${row.id}"
                                                        title="Xóa dòng"
                                                    >
                                                        ${Icons.trashRed}
                                                    </button>
                                                </td>
                                            </tr>
                                        `).join('') : `
                                            <tr>
                                                <td colspan="10" class="wh-empty-row">Chưa có dòng hàng hóa nào. Nhấn "+ Thêm dòng" để bổ sung.</td>
                                            </tr>
                                        `}
                                    </tbody>
                                    <tfoot>
                                        <tr>
                                            <td colspan="6"></td>
                                            <td class="wh-text-right">${formatVnNumber(totalQuantity)}</td>
                                            <td colspan="3"></td>
                                        </tr>
                                    </tfoot>
                                </table>

                                <!-- Phân trang thu gọn ngay dưới chân bảng Hàng hóa -->
                                <div class="wh-vtable-pager-row">
                                    <span>Số dòng/trang</span>
                                    <select class="wh-dg-page-size">
                                        <option value="20" selected>20</option>
                                        <option value="50">50</option>
                                    </select>
                                    <span class="wh-dg-range">${rangeText}</span>
                                    <div class="wh-dg-pager">
                                        <button type="button" class="wh-dg-page-btn" disabled>|&lt;</button>
                                        <button type="button" class="wh-dg-page-btn" disabled>&lt;</button>
                                        <button type="button" class="wh-dg-page-btn" disabled>&gt;</button>
                                        <button type="button" class="wh-dg-page-btn" disabled>&gt;|</button>
                                    </div>
                                </div>
                            </div>

                            <!-- Cụm nút Thêm dòng / Xóa hết dòng / Thêm ghi chú + Đính kèm -->
                            <div class="wh-voucher-subfooter">
                                <div class="wh-voucher-total-count">
                                    Tổng số: <strong>${voucher.items.length}</strong>
                                </div>

                                <div class="wh-voucher-row-actions">
                                    <button
                                        type="button"
                                        class="wh-vbtn-outline wh-vbtn-blue"
                                        data-action="add-voucher-row"
                                        data-voucher-type="${voucher.voucherType}"
                                    >
                                        ${Icons.plus}
                                        <span>Thêm dòng</span>
                                    </button>
                                    <button
                                        type="button"
                                        class="wh-vbtn-outline wh-vbtn-red"
                                        data-action="clear-voucher-rows"
                                        data-voucher-type="${voucher.voucherType}"
                                    >
                                        ${Icons.trashRed}
                                        <span>Xóa hết dòng</span>
                                    </button>
                                    <button type="button" class="wh-vbtn-outline wh-vbtn-gray">
                                        ${Icons.pencil}
                                        <span>Thêm ghi chú</span>
                                    </button>
                                </div>

                                <div class="wh-voucher-attach-title">Đính kèm</div>
                                <label class="wh-voucher-dropzone">
                                    <input type="file" accept=".xls,.xlsx" style="display:none;" />
                                    <div class="wh-dropzone-main">
                                        <span class="wh-dropzone-link">Chọn tệp</span> hoặc kéo và thả tệp vào đây
                                    </div>
                                    <div class="wh-dropzone-sub">Định dạng .xls, .xlsx (tối đa 20MB)</div>
                                </label>
                            </div>
                        </div>
                    </div>

                    <!-- Thanh nút hành động cố định dưới đáy Phiếu -->
                    <div class="wh-voucher-bottombar">
                        <button type="button" class="wh-btn wh-btn-cancel" data-action="close-voucher-modal">Hủy</button>
                        <button type="button" class="wh-btn wh-btn-cancel" data-action="close-voucher-modal">Lưu và Thêm</button>
                        <button type="button" class="wh-btn wh-btn-apply" data-action="close-voucher-modal">Lưu</button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * 8. TabbedDataGridComponent
     * Component bảng dữ liệu đa tab tái sử dụng cho cả "Nhập kho" và "Xuất kho"
     */
    function renderTabbedDataGrid(config, activeVoucherHtml) {
        const tabsHtml = config.tabs.map(tab => `
            <button
                type="button"
                class="wh-tab-btn ${tab.id === config.activeTab ? 'active' : ''}"
                data-action="switch-op-tab"
                data-page-type="${config.pageType}"
                data-tab-id="${tab.id}"
            >
                ${escapeHtml(tab.label)}
            </button>
        `).join('');

        const filtersHtml = config.filters.map(f => `
            <div class="wh-dg-filter-pill">
                <span class="wh-dg-filter-prefix">${escapeHtml(f.prefix)}</span>
                <select
                    class="wh-dg-filter-select"
                    data-filter-id="${f.id}"
                    data-page-type="${config.pageType}"
                >
                    ${f.options.map(opt => `
                        <option value="${escapeHtml(opt)}" ${opt === f.value ? 'selected' : ''}>${escapeHtml(opt)}</option>
                    `).join('')}
                </select>
            </div>
        `).join('');

        const addBtnHtml = config.showAddButton
            ? `<button
                type="button"
                class="wh-dg-add-btn"
                ${config.canOpenVoucher ? `data-action="open-voucher-modal" data-voucher-type="${config.voucherType}"` : ''}
                title="${config.canOpenVoucher ? `Lập phiếu ${config.voucherType === 'inbound' ? 'nhập kho' : 'xuất kho'}` : 'Thêm mới'}"
               >
                ${Icons.plus}
                <span>${escapeHtml(config.addButtonLabel || 'Thêm')}</span>
               </button>`
            : '';

        const checkboxHeaderHtml = config.showCheckbox
            ? `<th class="wh-dg-checkbox-col">
                <input type="checkbox" class="wh-dg-checkbox" data-action="toggle-all-rows" />
               </th>`
            : '';

        const headersHtml = config.columns.map(col => `
            <th style="${col.width ? `width:${col.width};` : ''}">
                ${escapeHtml(col.label)}
            </th>
        `).join('');

        const totalColSpan = config.columns.length + (config.showCheckbox ? 1 : 0);

        const rowsHtml = config.rows.length > 0
            ? config.rows.map(row => {
                const checkboxCell = config.showCheckbox
                    ? `<td class="wh-dg-checkbox-col">
                        <input type="checkbox" class="wh-dg-checkbox wh-row-checkbox" />
                       </td>`
                    : '';

                const dataCells = config.columns.map(col => {
                    const val = String(row[col.key] ?? '');
                    if (col.isLink) {
                        if (config.canOpenVoucher) {
                            return `<td>
                                <a
                                    href="#${escapeHtml(val)}"
                                    class="wh-dg-link"
                                    data-action="open-voucher-modal"
                                    data-voucher-type="${config.voucherType}"
                                    title="Nhấn để mở chi tiết phiếu ${escapeHtml(val)}"
                                >${escapeHtml(val)}</a>
                            </td>`;
                        }
                        return `<td><a href="#${escapeHtml(val)}" class="wh-dg-link" onclick="return false;">${escapeHtml(val)}</a></td>`;
                    }
                    if (col.isStatusLink) {
                        const cls = row.statusType === 'success' ? 'wh-dg-status-success' : 'wh-dg-status-info';
                        return `<td><span class="${cls}">${escapeHtml(val)}</span></td>`;
                    }
                    if (col.isOrderStatus) {
                        const cls = row.orderStatusType === 'warning' ? 'wh-dg-status-warning' : 'wh-dg-status-success';
                        return `<td><span class="${cls}">${escapeHtml(val)}</span></td>`;
                    }
                    return `<td>${escapeHtml(val)}</td>`;
                }).join('');

                return `<tr>${checkboxCell}${dataCells}</tr>`;
            }).join('')
            : `<tr><td colspan="${totalColSpan}" class="wh-empty-row">Không có dữ liệu phù hợp</td></tr>`;

        return `
            <div class="wh-datagrid-page">
                <div class="wh-tabs-bar">
                    ${tabsHtml}
                </div>

                <div class="wh-datagrid-card">
                    <div class="wh-dg-toolbar">
                        <div class="wh-dg-toolbar-left">
                            <div class="wh-dg-search-box">
                                <span class="wh-dg-search-icon">${Icons.search}</span>
                                <input
                                    type="text"
                                    class="wh-dg-search-input"
                                    placeholder="Tìm kiếm"
                                    data-op-search="${config.pageType}"
                                    value="${escapeHtml(config.searchValue)}"
                                />
                            </div>
                            ${filtersHtml}
                        </div>

                        <div class="wh-dg-toolbar-right">
                            <button type="button" class="wh-dg-icon-btn" data-action="refresh-datagrid" title="Tải lại dữ liệu">
                                ${Icons.refresh}
                            </button>
                            <button type="button" class="wh-dg-icon-btn" title="Chế độ hiển thị chia khung">
                                ${Icons.splitLayout}
                            </button>
                            <button type="button" class="wh-dg-icon-btn" title="Tùy chỉnh cột">
                                ${Icons.settings}
                            </button>
                            ${addBtnHtml}
                        </div>
                    </div>

                    <div class="wh-dg-table-scroll">
                        <table class="wh-dg-table">
                            <thead>
                                <tr>
                                    ${checkboxHeaderHtml}
                                    ${headersHtml}
                                </tr>
                            </thead>
                            <tbody>
                                ${rowsHtml}
                            </tbody>
                        </table>
                    </div>

                    <div class="wh-dg-footer">
                        <div>Tổng số: <strong>${config.totalRecords}</strong></div>
                        <div class="wh-dg-footer-right">
                            <span>Số dòng/trang</span>
                            <select class="wh-dg-page-size">
                                <option value="20" selected>20</option>
                                <option value="50">50</option>
                                <option value="100">100</option>
                            </select>
                            <span class="wh-dg-range">${escapeHtml(config.rangeText)}</span>
                            <div class="wh-dg-pager">
                                <button type="button" class="wh-dg-page-btn" disabled title="Trang đầu">|&lt;</button>
                                <button type="button" class="wh-dg-page-btn" disabled title="Trang trước">&lt;</button>
                                <button type="button" class="wh-dg-page-btn" ${config.hasMorePages ? '' : 'disabled'} title="Trang sau">&gt;</button>
                                <button type="button" class="wh-dg-page-btn" ${config.hasMorePages ? '' : 'disabled'} title="Trang cuối">&gt;|</button>
                            </div>
                        </div>
                    </div>
                </div>

                ${activeVoucherHtml || ''}
            </div>
        `;
    }

    /**
     * Điều phối render Modal Chứng từ theo loại (Đơn hàng / Lệnh kho / Phiếu kho)
     */
    function renderDocumentModal(docData) {
        if (!docData) return '';
        if (docData.docType === 'purchase-order' || docData.docType === 'sales-order') {
            return renderOrderDetailModal(docData);
        }
        if (docData.docType === 'inbound-command' || docData.docType === 'outbound-command') {
            return renderCommandDetailModal(docData);
        }
        return renderStockVoucherModal(docData);
    }

    /**
     * 9. MainContentComponent (Vùng nội dung chính)
     */
    function renderMainContent(state, viewPayloads) {
        const activeVoucherHtml = state.activeVoucherModal
            ? renderDocumentModal(viewPayloads.activeVoucherData)
            : '';

        switch (state.activeMenuId) {
            case 'ban-lam-viec':
                return renderWorkspace(viewPayloads.workspaceData);

            case 'tong-quan':
                return `
                    <div class="wh-page-header">
                        <h1 class="wh-page-title">Tổng quan</h1>
                    </div>
                    <div class="wh-dashboard-grid">
                        ${viewPayloads.cardsConfig.map(card => renderWidgetCard(card, state)).join('')}
                    </div>
                `;

            case 'nhap-kho':
                return renderTabbedDataGrid(viewPayloads.inboundConfig, activeVoucherHtml);

            case 'xuat-kho':
                return renderTabbedDataGrid(viewPayloads.outboundConfig, activeVoucherHtml);

            default:
                return `<div class="wh-empty-maincontent" data-empty-section="${escapeHtml(state.activeMenuId)}"></div>`;
        }
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    return {
        Icons,
        renderHeader,
        renderSidebar,
        renderWidgetCard,
        renderParameterPopover,
        renderWorkspace,
        renderTabbedDataGrid,
        renderOrderDetailModal,
        renderCommandDetailModal,
        renderStockVoucherModal,
        renderDocumentModal,
        renderMainContent
    };
})();
