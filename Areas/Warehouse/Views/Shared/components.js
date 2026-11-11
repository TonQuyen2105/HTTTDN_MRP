/**
 * Areas/Warehouse/Views/Shared/components.js
 * ---------------------------------------------------------
 * [VIEW LAYER - REUSABLE UI COMPONENTS]
 * Tập hợp các Component tái sử dụng cho phân hệ Kho hàng:
 * - WarehouseIcons: Bộ icon SVG chuẩn thiết kế
 * - WhNotification: Hệ thống thông báo dùng chung (Toast & Dialog Modal)
 * - HeaderComponent: Thanh điều hướng trên cùng
 * - SidebarComponent: Thanh menu dọc bên trái (hỗ trợ submenu & thu gọn mượt mà)
 * - ParameterPopoverComponent: Hộp thoại cấu hình "Chọn tham số"
 * - WidgetCardComponent: Thẻ bảng dữ liệu tổng quan (tái sử dụng cho 4 bảng ở Tổng quan)
 * - WorkspaceComponent: Giao diện trang "Bàn làm việc" (Hero Banner, Promo Card, Donut & Bar Cards)
 * - TabbedDataGridComponent: Component bảng dữ liệu đa tab tái sử dụng cho cả "Nhập kho" và "Xuất kho"
 * - StockVoucherModalComponent: Component Phiếu Nhập Kho (PNK00005) & Phiếu Xuất Kho (PXK00002) tái sử dụng
 * - MainContentComponent: Điều phối hiển thị nội dung theo mục Sidebar đang chọn
 */

// =========================================================================
// THÀNH PHẦN THÔNG BÁO DÙNG CHUNG TOÀN HỆ THỐNG (TOAST & DIALOG MODAL)
// =========================================================================
window.WhNotification = (function () {
    const NOTIF_ICONS = {
        success: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#16a34a" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>`,
        error: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#dc2626" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`,
        warning: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#d97706" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
        info: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0284c7" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>`
    };

    function escapeText(str) {
        return String(str || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function ensureToastContainer() {
        let container = document.getElementById('wh-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'wh-toast-container';
            container.className = 'wh-toast-container';
            document.body.appendChild(container);
        }
        return container;
    }

    function toast(message, type = 'info', title = '', duration = 3500) {
        if (type === 'danger') type = 'error';
        const container = ensureToastContainer();
        const toastEl = document.createElement('div');
        toastEl.className = `wh-toast wh-toast-${type}`;

        const defaultTitle = type === 'success' ? 'Thành công' : (type === 'error' ? 'Lỗi thao tác' : (type === 'warning' ? 'Cảnh báo' : 'Thông báo'));
        const displayTitle = title || defaultTitle;

        toastEl.innerHTML = `
            <div class="wh-toast-icon">${NOTIF_ICONS[type] || NOTIF_ICONS.info}</div>
            <div class="wh-toast-content">
                <div class="wh-toast-title">${escapeText(displayTitle)}</div>
                <div class="wh-toast-message">${escapeText(message)}</div>
            </div>
            <button type="button" class="wh-toast-close" title="Đóng">&times;</button>
            <div class="wh-toast-progress" style="animation-duration: ${duration}ms;"></div>
        `;

        const closeBtn = toastEl.querySelector('.wh-toast-close');
        let timer = null;

        const dismiss = () => {
            if (timer) clearTimeout(timer);
            toastEl.classList.add('is-hiding');
            setTimeout(() => {
                if (toastEl.parentNode) toastEl.parentNode.removeChild(toastEl);
            }, 300);
        };

        closeBtn.onclick = dismiss;
        timer = setTimeout(dismiss, duration);

        container.appendChild(toastEl);
    }

    function alert(message, options = {}) {
        return new Promise(resolve => {
            const type = (options.type === 'danger') ? 'error' : (options.type || 'info');
            const title = options.title || (type === 'error' ? 'Không thể thực hiện' : (type === 'warning' ? 'Cảnh báo' : 'Thông báo'));
            const confirmText = options.confirmText || 'Đã hiểu';

            const overlay = document.createElement('div');
            overlay.className = 'wh-dialog-overlay';
            overlay.innerHTML = `
                <div class="wh-dialog-box wh-dialog-${type}">
                    <div class="wh-dialog-icon-wrap">
                        ${NOTIF_ICONS[type] || NOTIF_ICONS.info}
                    </div>
                    <div class="wh-dialog-body">
                        <h3 class="wh-dialog-title">${escapeText(title)}</h3>
                        <div class="wh-dialog-message">${String(message || '').replace(/\n/g, '<br/>')}</div>
                    </div>
                    <div class="wh-dialog-footer">
                        <button type="button" class="wh-dialog-btn wh-dialog-btn-primary">${escapeText(confirmText)}</button>
                    </div>
                </div>
            `;

            const btn = overlay.querySelector('.wh-dialog-btn-primary');
            const cleanup = () => {
                overlay.classList.add('is-closing');
                setTimeout(() => {
                    if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
                    resolve();
                }, 200);
            };

            btn.onclick = cleanup;
            overlay.onkeydown = (e) => { if (e.key === 'Escape' || e.key === 'Enter') cleanup(); };
            document.body.appendChild(overlay);
            setTimeout(() => btn.focus(), 50);
        });
    }

    function confirm(message, options = {}) {
        return new Promise(resolve => {
            const type = (options.type === 'danger') ? 'error' : (options.type || 'warning');
            const title = options.title || 'Xác nhận thao tác';
            const confirmText = options.confirmText || 'Đồng ý';
            const cancelText = options.cancelText || 'Hủy bỏ';
            const isDanger = options.isDanger || type === 'error' || type === 'warning';

            const overlay = document.createElement('div');
            overlay.className = 'wh-dialog-overlay';
            overlay.innerHTML = `
                <div class="wh-dialog-box wh-dialog-${type}">
                    <div class="wh-dialog-icon-wrap">
                        ${NOTIF_ICONS[type] || NOTIF_ICONS.warning}
                    </div>
                    <div class="wh-dialog-body">
                        <h3 class="wh-dialog-title">${escapeText(title)}</h3>
                        <div class="wh-dialog-message">${String(message || '').replace(/\n/g, '<br/>')}</div>
                    </div>
                    <div class="wh-dialog-footer">
                        <button type="button" class="wh-dialog-btn wh-dialog-btn-cancel">${escapeText(cancelText)}</button>
                        <button type="button" class="wh-dialog-btn ${isDanger ? 'wh-dialog-btn-danger' : 'wh-dialog-btn-primary'}">${escapeText(confirmText)}</button>
                    </div>
                </div>
            `;

            const btnConfirm = overlay.querySelector('.wh-dialog-btn-primary, .wh-dialog-btn-danger');
            const btnCancel = overlay.querySelector('.wh-dialog-btn-cancel');

            const finish = (result) => {
                overlay.classList.add('is-closing');
                setTimeout(() => {
                    if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
                    resolve(result);
                }, 200);
            };

            btnConfirm.onclick = () => finish(true);
            btnCancel.onclick = () => finish(false);
            overlay.onclick = (e) => {
                if (e.target === overlay) finish(false);
            };

            document.body.appendChild(overlay);
            setTimeout(() => btnConfirm.focus(), 50);
        });
    }

    function prompt(message, defaultValue = '', options = {}) {
        return new Promise(resolve => {
            const type = options.type || 'info';
            const title = options.title || 'Nhập thông tin';
            const confirmText = options.confirmText || 'Xác nhận';
            const cancelText = options.cancelText || 'Hủy bỏ';

            const overlay = document.createElement('div');
            overlay.className = 'wh-dialog-overlay';
            overlay.innerHTML = `
                <div class="wh-dialog-box wh-dialog-${type}">
                    <div class="wh-dialog-icon-wrap">
                        ${NOTIF_ICONS[type] || NOTIF_ICONS.info}
                    </div>
                    <div class="wh-dialog-body">
                        <h3 class="wh-dialog-title">${escapeText(title)}</h3>
                        <div class="wh-dialog-message">${String(message || '').replace(/\n/g, '<br/>')}</div>
                        <input type="text" class="wh-dialog-input" value="${escapeText(defaultValue)}" />
                    </div>
                    <div class="wh-dialog-footer">
                        <button type="button" class="wh-dialog-btn wh-dialog-btn-cancel">${escapeText(cancelText)}</button>
                        <button type="button" class="wh-dialog-btn wh-dialog-btn-primary">${escapeText(confirmText)}</button>
                    </div>
                </div>
            `;

            const input = overlay.querySelector('.wh-dialog-input');
            const btnConfirm = overlay.querySelector('.wh-dialog-btn-primary');
            const btnCancel = overlay.querySelector('.wh-dialog-btn-cancel');

            const finish = (val) => {
                overlay.classList.add('is-closing');
                setTimeout(() => {
                    if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
                    resolve(val);
                }, 200);
            };

            btnConfirm.onclick = () => finish(input.value);
            btnCancel.onclick = () => finish(null);
            input.onkeydown = (e) => {
                if (e.key === 'Enter') finish(input.value);
                if (e.key === 'Escape') finish(null);
            };

            document.body.appendChild(overlay);
            setTimeout(() => { input.focus(); input.select(); }, 50);
        });
    }

    // Ghi đè an toàn alert & confirm mặc định của trình duyệt
    window.alert = (msg) => alert(msg);
    window.confirm = (msg) => confirm(msg);

    return {
        toast,
        alert,
        confirm,
        prompt,
        success: (msg, title) => toast(msg, 'success', title),
        error: (msg, title) => toast(msg, 'error', title),
        warning: (msg, title) => toast(msg, 'warning', title),
        info: (msg, title) => toast(msg, 'info', title)
    };
})();

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
        penToSquare: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
        </svg>`,
        lock: `<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
            <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
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
        brokenImage: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#94a3b8" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="18" height="18" rx="2" stroke="#94a3b8"/>
            <circle cx="8.5" cy="8.5" r="1.5" fill="#94a3b8"/>
            <path d="m21 15-5-5L5 21"/>
            <path d="M12 3l-1.5 4.5 3 4-2.5 4 2 5.5" stroke="#ef4444" stroke-width="1.8"/>
        </svg>`,
        warehouse: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 8.35V20a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8.35A2 2 0 0 1 3.26 6.5l8-3.2a2 2 0 0 1 1.48 0l8 3.2A2 2 0 0 1 22 8.35Z"/>
            <path d="M6 18h12"/><path d="M6 14h12"/>
        </svg>`,
        production: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M2 20a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8l-7 5V8l-7 5V4a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z"/>
            <path d="M17 18h1"/><path d="M12 18h1"/><path d="M7 18h1"/>
        </svg>`,
        supplier: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 21h18"/><path d="M9 8h1"/><path d="M9 12h1"/><path d="M9 16h1"/><path d="M14 8h1"/><path d="M14 12h1"/><path d="M14 16h1"/>
            <path d="M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16"/>
        </svg>`,
        userCheck: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><polyline points="16 11 18 13 22 9"/>
        </svg>`,
        userPlus: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><line x1="19" y1="8" x2="19" y2="14"/><line x1="22" y1="11" x2="16" y2="11"/>
        </svg>`,
        userClock: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><circle cx="18" cy="12" r="3"/><polyline points="18 11 18 12 19 12"/>
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

    // Định dạng tiền tệ VNĐ (25000 -> 25.000 đ | 12500000 -> 12.500.000 đ)
    function formatVnCurrency(num) {
        const n = Number(num) || 0;
        return n.toLocaleString('vi-VN') + ' đ';
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
                </div>

                <div class="wh-header-right">
                    <div class="wh-user-profile-menu-container">
                        <button
                            type="button"
                            class="wh-avatar-btn ${state.isUserDropdownOpen ? 'active' : ''}"
                            data-action="toggle-user-dropdown"
                            title="Tài khoản cá nhân: ${escapeHtml(state.currentUser?.fullName || 'Tài khoản Kho')}"
                        >
                            <img src="${avatarSvg}" alt="User Avatar" />
                        </button>
                        ${renderUserDropdown(state)}
                    </div>
                </div>
            </header>
        `;
    }

    /**
     * Component Dropdown Thông tin nhân viên trên Header
     */
    function renderUserDropdown(state) {
        if (!state.isUserDropdownOpen) return '';

        const user = state.currentUser || {
            userId: '-',
            username: 'warehouse',
            fullName: 'Tài khoản Kho',
            role: 'Warehouse',
            phone: '',
            status: 'Active'
        };

        const avatarSvg = `data:image/svg+xml;utf8,` + encodeURIComponent(
            `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
                <rect width="64" height="64" fill="#cbd5e1"/>
                <circle cx="32" cy="24" r="12" fill="#475569"/>
                <path d="M12 56c0-11 9-20 20-20s20 9 20 20" fill="#475569"/>
            </svg>`
        );

        return `
            <div class="wh-user-dropdown" id="wh-user-dropdown">
                <div class="wh-user-dropdown-header">
                    <div class="wh-user-dropdown-avatar">
                        <img src="${avatarSvg}" alt="Avatar" />
                    </div>
                    <div class="wh-user-dropdown-info">
                        <div class="wh-user-dropdown-name">${escapeHtml(user.fullName)}</div>
                        <div class="wh-user-dropdown-code">
                            <span class="wh-badge-code">ID: ${escapeHtml(user.userId)}</span>
                            <span class="wh-user-dropdown-username">@${escapeHtml(user.username)}</span>
                        </div>
                        <div class="wh-user-dropdown-role">${escapeHtml(user.role || 'Thủ kho')} • ${escapeHtml(user.status || 'Active')}</div>
                    </div>
                </div>
                <div class="wh-user-dropdown-divider"></div>
                <div class="wh-user-dropdown-actions">
                    <button type="button" class="wh-user-dropdown-item" data-action="view-employee-profile">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                        <span>Xem chi tiết</span>
                    </button>
                    <button type="button" class="wh-user-dropdown-item wh-user-logout-btn" data-action="logout-user">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path><polyline points="16 17 21 12 16 7"></polyline><line x1="21" y1="12" x2="9" y2="12"></line></svg>
                        <span>Đăng xuất</span>
                    </button>
                </div>
            </div>
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
                        <a href="javascript:void(0)" onclick="return false;" class="wh-ws-more-link" data-action="select-menu" data-menu-id="xuat-kho">Xem thêm</a>
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
                        <a href="javascript:void(0)" onclick="return false;" class="wh-ws-more-link" data-action="select-menu" data-menu-id="xuat-kho">Xem thêm</a>
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
     * 7B. CommandDetailModalComponent (Component Tái sử dụng cho cả "Lệnh nhập kho LNK00018" & "Lệnh xuất kho LXK00088")
     */
    function renderCommandDetailModal(cmd) {
        const isOutbound = cmd.docType === 'outbound-command';
        const itemCodeHeader = isOutbound ? 'Mã NVL' : 'Mã SP';
        const itemNameHeader = isOutbound ? 'Tên nguyên vật liệu' : 'Tên sản phẩm';
        const sectionTitle = isOutbound ? 'Danh sách nguyên vật liệu xuất kho' : 'Sản phẩm';
        const totalReqQty = cmd.items.reduce((s, r) => s + (Number(r.reqQty ?? r.quantity) || 0), 0);
        const totalActualQty = cmd.items.reduce((s, r) => s + (Number(r.actualQty ?? r.quantity) || 0), 0);
        const totalActualDisplay = (!isOutbound && totalReqQty > 0)
            ? `${formatVnNumber(totalActualQty)}/${formatVnNumber(totalReqQty)}`
            : formatVnNumber(totalActualQty);
        const totalAmount = cmd.items.reduce((s, r) => {
            const qty = Number(r.quantity ?? r.reqQty) || 0;
            const price = Number(r.price ?? r.unitCost) || 0;
            return s + (r.amount !== undefined ? Number(r.amount) : (qty * price));
        }, 0);
        const rangeText = cmd.items.length > 0 ? `1 - ${cmd.items.length}` : '0 - 0';
        const isPending = isOutbound
            ? (cmd.rawStatus === 'Pending' || cmd.statusText === 'Chờ xuất kho')
            : (cmd.rawStatus === 'Pending' || cmd.rawStatus === 'In Production' || cmd.rawStatus === 'Completed' || cmd.statusText === 'Chờ nhập kho');
        const isApproved = cmd.rawStatus === 'Approved' || cmd.statusText === 'Đã xuất kho' || cmd.statusText === 'Đã nhập kho' || (isOutbound && cmd.rawStatus === 'Completed');
        const isRejected = cmd.rawStatus === 'Rejected' || cmd.statusText === 'Từ chối';

        // Kiểm tra điều kiện tồn kho & hạn sử dụng cho Lệnh xuất NVL
        const hasIssue = isOutbound && cmd.items.some(r => {
            const status = r.conditionStatus || '';
            if (status === 'Thiếu số lượng đạt chuẩn' || status === 'Tồn kho hết hạn' || status === 'Hàng tồn đã hết hạn' || status === 'Thiếu tồn kho') {
                return true;
            }
            if (r.isOutOfStock) return true;
            const qty = Number(r.reqQty ?? r.quantity) || 0;
            const stock = Number(r.stockQty ?? 0);
            const expiredQty = Number(r.expiredStockQty ?? 0);
            return (stock < qty) || (expiredQty >= stock) || (expiredQty > 0 && (stock - expiredQty < qty));
        });

        const canApprove = isOutbound
            ? (cmd.canApprove !== undefined ? Boolean(cmd.canApprove) : (!hasIssue && isPending))
            : isPending;

        let blockReason = cmd.approvalBlockReason || '';
        if (!blockReason && isOutbound && hasIssue) {
            const badItem = cmd.items.find(r => {
                const s = r.conditionStatus || '';
                return s === 'Thiếu số lượng đạt chuẩn' || s === 'Tồn kho hết hạn' || s === 'Hàng tồn đã hết hạn' || s === 'Thiếu tồn kho';
            });
            if (badItem) {
                if (badItem.conditionStatus === 'Tồn kho hết hạn' || badItem.conditionStatus === 'Hàng tồn đã hết hạn') {
                    blockReason = `Hàng tồn kho của '${badItem.itemName || badItem.ingrName}' đã hết hạn sử dụng (Tồn kho hết hạn).`;
                } else if (badItem.conditionStatus === 'Thiếu số lượng đạt chuẩn') {
                    blockReason = `Nguyên vật liệu '${badItem.itemName || badItem.ingrName}' thiếu số lượng đạt chuẩn.`;
                } else {
                    blockReason = `Không đủ tồn kho cho nguyên vật liệu '${badItem.itemName || badItem.ingrName}'.`;
                }
            } else {
                blockReason = 'Có nguyên vật liệu không đủ điều kiện xuất kho do thiếu tồn kho hoặc hết hạn!';
            }
        }

        const rawReqCode = cmd.requestCode || cmd.productionRequestCode;
        const prodReqCodeDisplay = (rawReqCode && String(rawReqCode).trim())
            ? String(rawReqCode).trim()
            : (cmd.requestId ? `LSX${String(cmd.requestId).padStart(5, '0')}` : '-');
        const hasProdReq = prodReqCodeDisplay !== '-';

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window" style="max-width:1060px;">
                    <!-- Thanh tiêu đề trên cùng -->
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                            <h2 class="wh-voucher-title">${escapeHtml(cmd.title)}</h2>
                            <span class="wh-vthh-mode-badge ${isApproved ? 'is-view' : isRejected ? '' : 'is-edit'}" style="${isApproved ? 'background:#dcfce7;color:#15803d;border:1px solid #bbf7d0;' : isRejected ? 'background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;' : ''}">
                                ${escapeHtml(cmd.statusText || 'Chờ xuất kho')}
                            </span>
                        </div>

                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" data-action="close-voucher-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <!-- Thân Lệnh Nhập / Xuất Kho -->
                    <div class="wh-voucher-body">
                        <!-- Vùng Master (Trái - Phải ngăn nhau bằng đường gạch mờ) -->
                        <div class="wh-voucher-master">
                            <!-- Khối bên trái -->
                            <div class="wh-voucher-master-left wh-outbound-master-left">
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Mã nhân viên</label>
                                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(cmd.employeeCode || 'NV0005')}" readonly disabled />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Tên nhân viên yêu cầu</label>
                                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(cmd.employeeName || 'Nguyễn Tiến Huy')}" readonly disabled />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Bộ phận nhận</label>
                                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(cmd.department || 'Phân xưởng Viên nén GMP')}" readonly disabled />
                                </div>
                                ${cmd.assignedToName ? `
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Người được phân công</label>
                                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(cmd.assignedToName + (cmd.assignedToEmployeeCode ? ' (' + cmd.assignedToEmployeeCode + ')' : ''))}" readonly disabled />
                                </div>
                                ` : ''}
                                <div class="wh-form-group wh-col-span-2">
                                    <label class="wh-form-label">Diễn giải / Mục đích xuất</label>
                                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(cmd.description || '')}" readonly />
                                </div>
                            </div>

                            <!-- Khối bên phải -->
                            <div class="wh-voucher-master-right">
                                <div class="wh-form-group">
                                    <label class="wh-form-label">${escapeHtml(cmd.commandNoLabel || (isOutbound ? 'Số phiếu xuất kho' : 'Số phiếu nhập kho'))}</label>
                                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(cmd.commandNo)}" readonly disabled style="font-weight: 600;" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Mã yêu cầu sản xuất</label>
                                    <div class="wh-input-group">
                                        <input
                                            type="text"
                                            class="wh-input-control is-readonly"
                                            value="${escapeHtml(prodReqCodeDisplay)}"
                                            readonly
                                            disabled
                                            style="font-weight: 600; color: ${hasProdReq ? '#1d4ed8' : '#6b7280'};"
                                        />
                                        ${hasProdReq ? `
                                            <span class="wh-input-addon-btn" title="Mã yêu cầu sản xuất: ${escapeHtml(prodReqCodeDisplay)}" style="cursor:default;color:#2563eb;">
                                                ${Icons.docFile}
                                            </span>
                                        ` : ''}
                                    </div>
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label">Trạng thái thực hiện</label>
                                    <input type="text" class="wh-input-control wh-input-readonly" value="${escapeHtml(cmd.statusText)}" readonly disabled />
                                </div>
                            </div>
                        </div>

                        ${isOutbound && isPending && !canApprove ? `
                            <div style="background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:10px 14px;margin-bottom:14px;display:flex;align-items:center;gap:10px;color:#991b1b;font-size:12.5px;font-weight:500;">
                                <div style="flex-shrink:0;">
                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#dc2626" stroke-width="2.2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
                                </div>
                                <div>
                                    <strong style="color:#b91c1c;">Cảnh báo an toàn kho:</strong> Phiếu này hiện <strong>không thể duyệt xuất kho</strong> do: ${escapeHtml(blockReason.replace(/\.$/, ''))}. Vui lòng kiểm tra lại số lượng tồn kho và lô hạn sử dụng của nguyên vật liệu.
                                </div>
                            </div>
                        ` : ''}

                        <!-- Vùng Chi tiết Hàng hóa / Sản phẩm / Nguyên vật liệu -->
                        <div class="wh-voucher-detail">
                            <h3 class="wh-voucher-section-title">${escapeHtml(sectionTitle)}</h3>

                            <div class="wh-vtable-wrap">
                                <table class="wh-vtable">
                                    <thead>
                                        <tr>
                                            <th style="width: 36px;">#</th>
                                            <th style="width: 95px;">${Icons.pin}${escapeHtml(itemCodeHeader)}</th>
                                            <th>${escapeHtml(itemNameHeader)}</th>
                                            <th style="width: 60px; text-align:center;">ĐVT</th>
                                            <th style="width: 95px;" class="wh-text-right">${escapeHtml(cmd.reqQtyColLabel || (isOutbound ? 'SL Yêu cầu' : 'SL nhập'))}</th>
                                            ${isOutbound ? `
                                                <th style="width: 110px;" class="wh-text-right">SL Tồn kho</th>
                                                <th style="width: 100px;" class="wh-text-right">Đơn giá</th>
                                                <th style="width: 115px;" class="wh-text-right">Thành tiền</th>
                                                <th style="width: 160px; text-align:center;">Kiểm tra điều kiện</th>
                                            ` : `
                                                <th style="width: 120px;" class="wh-text-right">${escapeHtml(cmd.actualQtyColLabel || 'Thực nhập')}</th>
                                                <th style="width: 110px;">Hạn sử dụng</th>
                                            `}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${cmd.items.map((row, idx) => {
                                            const qty = Number(row.reqQty ?? row.quantity) || 0;
                                            const stock = Number(row.stockQty ?? 0);
                                            const unitCost = Number(row.unitCost ?? row.price) || 0;
                                            const amt = row.amount !== undefined ? Number(row.amount) : (qty * unitCost);
                                            const expiredStock = Number(row.expiredStockQty ?? 0);
                                            const validStock = Number(row.validStockQty ?? Math.max(0, stock - expiredStock));

                                            // Xác định trạng thái điều kiện xuất kho
                                            let condStatus = row.conditionStatus;
                                            if (!condStatus) {
                                                if (stock <= 0 && expiredStock === 0) condStatus = 'Thiếu tồn kho';
                                                else if (validStock <= 0) condStatus = 'Tồn kho hết hạn';
                                                else if (validStock < qty) condStatus = 'Thiếu số lượng đạt chuẩn';
                                                else condStatus = 'Đủ điều kiện';
                                            }

                                            const isOut = condStatus === 'Thiếu tồn kho' || stock < qty || Boolean(row.isOutOfStock);
                                            const shortage = isOut ? Math.max(0, qty - stock) : 0;
                                            const isExpiredAll = condStatus === 'Tồn kho hết hạn' || condStatus === 'Hàng tồn đã hết hạn';
                                            const isPartialExpired = condStatus === 'Thiếu số lượng đạt chuẩn';

                                            let condBadgeHtml = '';
                                            if (isExpiredAll) {
                                                condBadgeHtml = `
                                                    <span style="display:inline-flex;position:relative;align-items:center;gap:3px;font-size:11px;padding:3px 8px;border-radius:12px;background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;white-space:nowrap;font-weight:600;">
                                                        Tồn kho hết hạn
                                                    </span>
                                                `;
                                            } else if (isPartialExpired) {
                                                condBadgeHtml = `
                                                    <span style="display:inline-flex;position:relative;align-items:center;gap:3px;font-size:11px;padding:3px 8px;border-radius:12px;background:#fef3c7;color:#b45309;border:1px solid #fde68a;white-space:nowrap;font-weight:600;">
                                                        Thiếu số lượng đạt chuẩn
                                                    </span>
                                                `;
                                            } else if (isOut) {
                                                condBadgeHtml = `
                                                    <span style="display:inline-flex;position:relative;align-items:center;gap:3px;font-size:11px;padding:3px 8px;border-radius:12px;background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;white-space:nowrap;font-weight:600;">
                                                        ⚠️ Thiếu tồn kho
                                                    </span>
                                                `;
                                            } else {
                                                condBadgeHtml = `
                                                    <span style="display:inline-flex;position:relative;align-items:center;gap:3px;font-size:11px;padding:3px 8px;border-radius:12px;background:#dcfce7;color:#15803d;border:1px solid #bbf7d0;white-space:nowrap;font-weight:600;">
                                                        ✓ Đủ điều kiện
                                                    </span>
                                                `;
                                            }

                                            const rowBg = isOut || isExpiredAll
                                                ? 'background:#fff5f5;'
                                                : (isPartialExpired ? 'background:#fffbeb;' : '');

                                            return `
                                                <tr style="${rowBg}">
                                                    <td>${idx + 1}</td>
                                                    <td><strong>${escapeHtml(row.itemCode || row.ingrCode || '')}</strong></td>
                                                    <td>
                                                        <div style="font-weight:500;">${escapeHtml(row.itemName || row.ingrName || '')}</div>
                                                        ${isOutbound && row.note ? `<div style="font-size:11px;color:#64748b;margin-top:2px;">${escapeHtml(row.note)}</div>` : ''}
                                                    </td>
                                                    <td style="text-align:center;">${escapeHtml(row.unit || 'Hộp')}</td>
                                                    <td class="wh-text-right" style="font-weight:600;">${formatVnNumber(qty)}</td>
                                                    ${isOutbound ? `
                                                        <td class="wh-text-right" style="font-weight:700; color:${isOut || isExpiredAll ? '#dc2626' : (isPartialExpired ? '#d97706' : '#15803d')};">
                                                            ${formatVnNumber(stock)}
                                                            ${isOut ? `<div style="font-size:11px;color:#dc2626;font-weight:500;">(Thiếu ${formatVnNumber(shortage)} ${escapeHtml(row.unit || 'kg')})</div>` : ''}
                                                        </td>
                                                        <td class="wh-text-right">${unitCost > 0 ? unitCost.toLocaleString('vi-VN') + ' đ' : '-'}</td>
                                                        <td class="wh-text-right" style="font-weight:600;color:#2563eb;">${amt > 0 ? amt.toLocaleString('vi-VN') + ' đ' : '-'}</td>
                                                        <td style="text-align:center;">
                                                            ${condBadgeHtml}
                                                        </td>
                                                    ` : `
                                                        <td class="wh-text-right" data-cell="actual-qty" style="font-weight:700; color:${(row.actualQty >= qty) ? '#15803d' : '#2563eb'};">
                                                            ${row.actualQtyDisplay || (row.actualQty !== undefined && qty ? `${formatVnNumber(row.actualQty)}/${formatVnNumber(qty)}` : formatVnNumber(row.actualQty ?? qty))}
                                                        </td>
                                                        <td>${escapeHtml(row.expiryDate || '')}</td>
                                                    `}
                                                </tr>
                                            `;
                                        }).join('')}
                                    </tbody>
                                    <tfoot>
                                        <tr>
                                            <td colspan="4" style="text-align:right;font-weight:600;">Tổng cộng:</td>
                                            <td class="wh-text-right" style="font-weight:700;color:#0f172a;">${formatVnNumber(totalReqQty)}</td>
                                            ${isOutbound ? `
                                                <td colspan="2"></td>
                                                <td class="wh-text-right" style="font-weight:700;color:#2563eb;">${totalAmount > 0 ? totalAmount.toLocaleString('vi-VN') + ' đ' : '-'}</td>
                                                <td></td>
                                            ` : `
                                                <td class="wh-text-right" data-cell="total-actual-qty" style="font-weight:700;color:#0f172a;">${totalActualDisplay}</td>
                                                <td colspan="1"></td>
                                            `}
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>

                            ${renderTableSplitFooter(cmd.items.length, rangeText)}
                        </div>

                        <!-- Footer chứa các nút hành động -->
                        <div class="wh-voucher-footer" style="display:flex;justify-content:space-between;align-items:center;padding:12px 18px;border-top:1px solid #e2e8f0;background:#f8fafc;margin-top:16px;border-radius:0 0 8px 8px;">
                            <div class="wh-modal-status-hint">
                                ${isOutbound ? (
                                    isApproved
                                        ? `<span style="color:#16a34a;font-size:12px;font-weight:500;">✓ Phiếu này đã được duyệt xuất kho và chuyển giao vật tư vào xưởng sản xuất (WIP).</span>`
                                        : isRejected
                                            ? `<span style="color:#ef4444;font-size:12px;font-weight:500;">✗ Lệnh xuất kho này đã bị từ chối cấp phát.</span>`
                                            : (!canApprove
                                                ? `<span style="color:#dc2626;font-size:12px;font-weight:600;">⚠️ Không thể duyệt xuất: ${escapeHtml(blockReason)}</span>`
                                                : `<span style="color:#16a34a;font-size:12px;font-weight:500;">✓ Tất cả nguyên vật liệu đều đủ tồn kho và hạn sử dụng hợp lệ. Sẵn sàng duyệt xuất kho.</span>`
                                            )
                                ) : (
                                    isApproved
                                        ? `<span style="color:#16a34a;font-size:12px;font-weight:500;">✓ Lệnh này đã được kiểm đếm và duyệt nhập kho thành phẩm. Tồn kho thành phẩm đã được cập nhật.</span>`
                                        : isRejected
                                            ? `<span style="color:#ef4444;font-size:12px;font-weight:500;">✗ Lệnh nhập kho thành phẩm này đã bị từ chối tiếp nhận.</span>`
                                            : `<span style="color:#0284c7;font-size:12px;font-weight:500;">ℹ️ Kiểm tra đủ số lượng thực nhận và hạn sử dụng trước khi duyệt nhập kho thành phẩm.</span>`
                                )}
                            </div>
                            <div style="display:flex;gap:8px;">
                                ${isPending ? (
                                    isOutbound ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm ${canApprove ? 'wh-btn-approve-outbound' : 'wh-btn-approve-disabled'}"
                                            style="height:32px;padding:0 14px;font-size:12px;font-weight:600;${!canApprove ? 'opacity:0.6;cursor:not-allowed;background:#94a3b8;border-color:#94a3b8;color:#fff;' : ''}"
                                            data-action="approve-material-modal"
                                            data-receipt-id="${escapeHtml(String(cmd.receiptId || ''))}"
                                            data-order-no="${escapeHtml(cmd.commandNo || '')}"
                                            data-can-approve="${canApprove ? 'true' : 'false'}"
                                            data-block-reason="${escapeHtml(blockReason)}"
                                            ${!canApprove ? 'disabled' : ''}
                                        >
                                            ${Icons.checkBlue || ''} Duyệt xuất kho
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            style="height:32px;padding:0 12px;font-size:12px;"
                                            data-action="reject-material-modal"
                                            data-receipt-id="${escapeHtml(String(cmd.receiptId || ''))}"
                                            data-order-no="${escapeHtml(cmd.commandNo || '')}"
                                        >
                                            ${Icons.closeX || ''} Từ chối
                                        </button>
                                    ` : `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound"
                                            style="height:32px;padding:0 14px;font-size:12px;font-weight:600;"
                                            data-action="approve-product-modal"
                                            data-receipt-id="${escapeHtml(String(cmd.receiptId || ''))}"
                                            data-order-no="${escapeHtml(cmd.commandNo || '')}"
                                        >
                                            ${Icons.checkBlue || ''} Duyệt nhập kho
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            style="height:32px;padding:0 12px;font-size:12px;"
                                            data-action="reject-product-modal"
                                            data-receipt-id="${escapeHtml(String(cmd.receiptId || ''))}"
                                            data-order-no="${escapeHtml(cmd.commandNo || '')}"
                                        >
                                            ${Icons.closeX || ''} Từ chối
                                        </button>
                                    `
                                ) : ''}
                                <button
                                    type="button"
                                    class="wh-btn-sec"
                                    style="height:32px;padding:0 16px;border-radius:4px;font-size:12px;"
                                    data-action="close-voucher-modal"
                                >
                                    Đóng
                                </button>
                            </div>
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
        const isOutboundProduct = voucher.voucherType === 'outbound';
        const isCreate = voucher.mode === 'create';
        const suppliersList = (typeof WarehouseModel !== 'undefined' && WarehouseModel.getSuppliersList) ? WarehouseModel.getSuppliersList() : [];
        const ingredientsList = (typeof WarehouseModel !== 'undefined' && WarehouseModel.getIngredientsList) ? WarehouseModel.getIngredientsList() : [];

        let fullTitle = `Phiếu ${voucher.selectedSubType ? voucher.selectedSubType.toLowerCase() : (isInbound ? 'nhập kho' : 'xuất kho')} ${voucher.voucherNo}`;
        if (isInbound && isCreate) {
            fullTitle = `Lập phiếu nhập kho nguyên vật liệu - ${voucher.voucherNo}`;
        } else if (isOutboundProduct) {
            fullTitle = `Phiếu xuất kho thành phẩm - ${voucher.voucherNo}`;
        }

        const totalQuantity = voucher.items.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0);
        const totalAmount = voucher.items.reduce((sum, item) => {
            const qty = Number(item.quantity) || 0;
            const price = Number(item.price) || 0;
            return sum + (item.amount !== undefined ? Number(item.amount) : (qty * price));
        }, 0);
        const rangeText = voucher.items.length > 0 ? `1 - ${voucher.items.length}` : '0 - 0';

        // Phần thông tin chung bên trái (Master Left Grid)
        let masterLeftFieldsHtml = '';
        if (isInbound) {
            if (isCreate) {
                // Phiếu Nhập - Chế độ tạo mới: Tên NCC là dropdown có danh sách xổ xuống, Mã NCC tự động đổi theo tên NCC
                masterLeftFieldsHtml = `
                    <!-- Dòng 1: Mã nhân viên + Tên nhân viên (Tự động theo người đang đăng nhập) -->
                    <div class="wh-form-group">
                        <label class="wh-form-label">Mã nhân viên</label>
                        <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.employeeCode || 'NV0005')}" readonly disabled />
                    </div>
                    <div class="wh-form-group wh-col-span-2">
                        <label class="wh-form-label">Tên nhân viên</label>
                        <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.employeeName || 'Nguyễn Tiến Huy')}" readonly disabled />
                    </div>

                    <!-- Dòng 2: Mã nhà cung cấp + Tên nhà cung cấp (Dropdown) -->
                    <div class="wh-form-group">
                        <label class="wh-form-label">Mã nhà cung cấp</label>
                        <input
                            type="text"
                            class="wh-input-control is-readonly"
                            id="wh-inbound-supplier-code"
                            value="${escapeHtml(voucher.supplierCode || '')}"
                            placeholder="Mã NCC tự động"
                            readonly
                            disabled
                            style="font-weight: 600;"
                        />
                    </div>
                    <div class="wh-form-group wh-col-span-2">
                        <label class="wh-form-label">Tên nhà cung cấp <span class="wh-req">*</span></label>
                        <select class="wh-select-control" data-action="change-inbound-supplier" style="width: 100%;">
                            <option value="">-- Chọn nhà cung cấp --</option>
                            ${suppliersList.map(s => `
                                <option value="${escapeHtml(s.name)}" ${s.name === voucher.supplierName ? 'selected' : ''}>
                                    ${escapeHtml(s.name)} (${escapeHtml(s.code)}) - Phí nhập: ${s.importCost !== undefined && s.importCost !== null ? s.importCost : 0}%
                                </option>
                            `).join('')}
                        </select>
                    </div>
                `;
            } else {
                // Phiếu Nhập - Chế độ xem chi tiết: Khóa tất cả trường
                masterLeftFieldsHtml = `
                    <!-- Dòng 1: Mã nhân viên + Tên nhân viên -->
                    <div class="wh-form-group">
                        <label class="wh-form-label">Mã nhân viên</label>
                        <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.employeeCode || 'NV0005')}" readonly disabled />
                    </div>
                    <div class="wh-form-group wh-col-span-2">
                        <label class="wh-form-label">Tên nhân viên</label>
                        <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.employeeName || 'Nguyễn Tiến Huy')}" readonly disabled />
                    </div>

                    <!-- Dòng 2: Mã nhà cung cấp + Tên nhà cung cấp -->
                    <div class="wh-form-group">
                        <label class="wh-form-label">Mã nhà cung cấp</label>
                        <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.supplierCode || 'NCC0006')}" readonly disabled />
                    </div>
                    <div class="wh-form-group wh-col-span-2">
                        <label class="wh-form-label">Tên nhà cung cấp</label>
                        <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.supplierName || 'CÔNG TY CỔ PHẦN CÔNG NGHỆ IAT VIỆT NAM')}" readonly disabled />
                    </div>
                `;
            }
        } else if (isOutboundProduct) {
            // Phiếu Xuất kho thành phẩm (Xem chi tiết - Khóa tất cả trường)
            masterLeftFieldsHtml = `
                <!-- Dòng 1: Mã nhân viên + Tên nhân viên lập phiếu -->
                <div class="wh-form-group">
                    <label class="wh-form-label">Mã nhân viên</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.employeeCode || 'NV0005')}" readonly disabled />
                </div>
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Tên nhân viên lập phiếu</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.employeeName || voucher.creatorName || 'DS. Phan Thanh Hà')}" readonly disabled />
                </div>

                <!-- Dòng 2: Khách hàng / Đại lý + SĐT -->
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Khách hàng / Đại lý nhận hàng</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.customer || voucher.partnerName || '')}" readonly disabled style="font-weight:600;" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Số điện thoại</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.customerPhone || 'Chưa cập nhật')}" readonly disabled />
                </div>

                <!-- Dòng 3: Địa chỉ nhận hàng + Diễn giải -->
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Địa chỉ giao hàng</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.shippingAddress || voucher.deliveryAddress || '')}" readonly disabled />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Diễn giải / Lý do xuất</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.description || 'Xuất bán hàng dược phẩm')}" readonly disabled />
                </div>
            `;
        } else {
            // Phiếu Xuất khác
            masterLeftFieldsHtml = `
                <div class="wh-form-group">
                    <label class="wh-form-label">Mã đối tượng</label>
                    <select class="wh-select-control">
                        ${(voucher.partnerCodeOptions || []).map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.partnerCode ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Tên đối tượng nhận hàng</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.partnerName || '')}" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Kho xuất <span class="wh-req">*</span></label>
                    <select class="wh-select-control">
                        ${(voucher.warehouseOptions || []).map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.warehouse ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Chi nhánh</label>
                    <select class="wh-select-control">
                        ${(voucher.branchOptions || []).map(opt => `
                            <option value="${escapeHtml(opt)}" ${opt === voucher.branch ? 'selected' : ''}>${escapeHtml(opt)}</option>
                        `).join('')}
                    </select>
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Người nhận</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.receiver || '')}" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Địa chỉ giao hàng</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.deliveryAddress || '')}" />
                </div>
                <div class="wh-form-group wh-col-span-2">
                    <label class="wh-form-label">Diễn giải</label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.description || '')}" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Tham chiếu</label>
                    <div class="wh-input-group">
                        <input type="text" class="wh-input-control" value="${escapeHtml(voucher.reference || '')}" />
                        <button type="button" class="wh-input-addon-btn" title="Chọn chứng từ tham chiếu">${Icons.search}</button>
                    </div>
                </div>
            `;
        }

        const voucherReqCode = voucher.requestCode || voucher.productionRequestCode || (voucher.requestId ? `LSX${String(voucher.requestId).padStart(5, '0')}` : null);

        const masterRightFieldsHtml = isInbound
            ? `
                <div class="wh-form-group">
                    <label class="wh-form-label">
                        Số phiếu nhập kho <span class="wh-req">*</span>
                    </label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.voucherNo)}" readonly disabled style="font-weight: 600;" />
                </div>
                ${voucherReqCode ? `
                <div class="wh-form-group">
                    <label class="wh-form-label">Mã yêu cầu sản xuất</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucherReqCode)}" readonly disabled style="font-weight: 600; color: #1d4ed8;" />
                </div>
                ` : ''}
                <div class="wh-form-group">
                    <label class="wh-form-label">
                        Ngày nhập kho <span class="wh-req">*</span>
                    </label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.voucherDate)}" readonly disabled />
                </div>
            `
            : isOutboundProduct
            ? `
                <div class="wh-form-group">
                    <label class="wh-form-label">
                        Số phiếu xuất kho <span class="wh-req">*</span>
                    </label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.voucherNo)}" readonly disabled style="font-weight: 600;" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Đơn bán hàng liên kết</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.orderCode || 'Đơn bán hàng lẻ')}" readonly disabled style="font-weight: 600; color: #1d4ed8;" />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">
                        Ngày lập phiếu <span class="wh-req">*</span>
                    </label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.voucherDate)}" readonly disabled />
                </div>
                <div class="wh-form-group">
                    <label class="wh-form-label">Kho xuất</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucher.warehouse || 'Kho thành phẩm GMP')}" readonly disabled />
                </div>
            `
            : `
                <div class="wh-form-group">
                    <label class="wh-form-label">
                        Số phiếu xuất kho <span class="wh-req">*</span>
                    </label>
                    <input type="text" class="wh-input-control" value="${escapeHtml(voucher.voucherNo)}" />
                </div>
                ${voucherReqCode ? `
                <div class="wh-form-group">
                    <label class="wh-form-label">Mã yêu cầu sản xuất</label>
                    <input type="text" class="wh-input-control is-readonly" value="${escapeHtml(voucherReqCode)}" readonly disabled style="font-weight: 600; color: #1d4ed8;" />
                </div>
                ` : ''}
                <div class="wh-form-group">
                    <label class="wh-form-label">
                        Ngày xuất kho <span class="wh-req">*</span>
                    </label>
                    <div class="wh-input-group">
                        <input type="text" class="wh-input-control" value="${escapeHtml(voucher.voucherDate)}" />
                        <button type="button" class="wh-input-addon-btn" title="Chọn ngày giờ">${Icons.calendar}</button>
                    </div>
                </div>
            `;

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window" style="${isOutboundProduct ? 'max-width: 1080px;' : ''}">
                    <!-- Thanh tiêu đề trên cùng của Phiếu -->
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                            <h2 class="wh-voucher-title">${escapeHtml(fullTitle)}</h2>
                            ${isOutboundProduct ? `
                                <span class="wh-vthh-mode-badge ${voucher.status === 'Đã xuất kho' ? 'is-view' : (voucher.status === 'Từ chối' || String(voucher.status || '').toLowerCase() === 'cancel' || voucher.status === 'Cancelled' || voucher.status === 'Canceled') ? '' : 'is-edit'}" style="${voucher.status === 'Đã xuất kho' ? 'background:#dcfce7;color:#15803d;border:1px solid #bbf7d0;' : (voucher.status === 'Từ chối' || String(voucher.status || '').toLowerCase() === 'cancel' || voucher.status === 'Cancelled' || voucher.status === 'Canceled') ? 'background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;' : 'background:#fef3c7;color:#b45309;border:1px solid #fde68a;'}">
                                    ${escapeHtml(voucher.status || 'Chờ xuất kho')}
                                </span>
                            ` : ''}
                        </div>

                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" data-action="close-voucher-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <!-- Thân phiếu (Master + Detail) -->
                    <div class="wh-voucher-body">
                        <!-- Vùng Master -->
                        <div class="wh-voucher-master">
                            <div class="wh-voucher-master-left">
                                ${masterLeftFieldsHtml}
                            </div>

                            <div class="wh-voucher-master-right">
                                ${masterRightFieldsHtml}
                            </div>
                        </div>

                        ${isOutboundProduct && (voucher.status === 'Chờ xuất kho' || voucher.status === 'Pending') && (voucher.hasStockIssue || !voucher.canApprove) ? `
                            <div style="background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:10px 14px;margin-bottom:14px;display:flex;align-items:center;gap:10px;color:#991b1b;font-size:12.5px;font-weight:500;">
                                <div style="flex-shrink:0;">
                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#dc2626" stroke-width="2.2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
                                </div>
                                <div>
                                    <strong style="color:#b91c1c;">Cảnh báo an toàn tồn kho:</strong> Phiếu này hiện <strong>không thể duyệt xuất kho</strong> do: ${escapeHtml(voucher.approvalBlockReason || 'Có sản phẩm không đủ tồn kho để xuất')}. Vui lòng kiểm tra lại tồn kho thành phẩm trước khi xuất hàng.
                                </div>
                            </div>
                        ` : ''}

                        <!-- Vùng Chi tiết Hàng hóa / Nguyên vật liệu -->
                        <div class="wh-voucher-detail">
                            <h3 class="wh-voucher-section-title">${isInbound ? 'Nguyên vật liệu' : isOutboundProduct ? 'Thành phẩm xuất kho' : 'Hàng hóa'}</h3>

                            <div class="wh-vtable-wrap">
                                ${isInbound ? (isCreate ? `
                                    <!-- BẢNG TẠO MỚI NGUYÊN VẬT LIỆU (Có dropdown chọn NVL, thêm dòng, xóa từng dòng) -->
                                    <table class="wh-vtable">
                                        <thead>
                                            <tr>
                                                <th style="width: 45px; text-align: center;">#</th>
                                                <th style="width: 140px;">${Icons.pin}Mã NVL</th>
                                                <th>Tên nguyên vật liệu <span class="wh-req">*</span></th>
                                                <th style="width: 75px; text-align: center;">ĐVT</th>
                                                <th style="width: 115px;" class="wh-text-right">SL nhập <span class="wh-req">*</span></th>
                                                <th style="width: 135px;" class="wh-text-right">Giá nguyên liệu</th>
                                                <th style="width: 150px;" class="wh-text-right">Thành tiền</th>
                                                <th style="width: 120px; text-align: center;">Hạn sử dụng</th>
                                                <th style="width: 50px; text-align: center;">Xóa</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${voucher.items.length > 0 ? voucher.items.map((row, idx) => {
                                                const qty = Number(row.quantity) || 0;
                                                const price = Number(row.price) || 0;
                                                const amount = (row.amount !== undefined && row.amount !== null) ? Number(row.amount) : (qty * price);
                                                const usedCodesInOtherRows = new Set(
                                                    voucher.items
                                                        .filter(other => other.id !== row.id && other.itemCode)
                                                        .map(other => other.itemCode)
                                                );
                                                return `
                                                    <tr>
                                                        <td style="text-align: center;">${idx + 1}</td>
                                                        <td>
                                                            <input
                                                                type="text"
                                                                class="wh-input-control is-readonly wh-table-input"
                                                                value="${escapeHtml(row.itemCode || '')}"
                                                                placeholder="Tự động"
                                                                readonly
                                                                disabled
                                                                style="font-weight: 600;"
                                                            />
                                                        </td>
                                                        <td>
                                                            <select
                                                                class="wh-select-control wh-table-select"
                                                                data-action="change-inbound-item"
                                                                data-row-id="${row.id}"
                                                            >
                                                                <option value="">-- Chọn nguyên vật liệu --</option>
                                                                ${ingredientsList.map(ingr => {
                                                                    const isUsedElsewhere = usedCodesInOtherRows.has(ingr.ingrCode);
                                                                    return `
                                                                        <option
                                                                            value="${escapeHtml(ingr.ingrCode)}"
                                                                            ${row.itemCode === ingr.ingrCode ? 'selected' : ''}
                                                                            ${isUsedElsewhere ? 'disabled style="color:#9ca3af;background:#f8fafc;"' : ''}
                                                                        >
                                                                            ${escapeHtml(ingr.ingrName)} (${escapeHtml(ingr.ingrCode)})${isUsedElsewhere ? ' — Đã thêm trước đó' : ''}
                                                                        </option>
                                                                    `;
                                                                }).join('')}
                                                            </select>
                                                        </td>
                                                        <td style="text-align: center;">
                                                            <span class="wh-cell-text" data-cell="unit" data-row-id="${row.id}">${escapeHtml(row.unit || '-')}</span>
                                                        </td>
                                                        <td class="wh-text-right">
                                                            <input
                                                                type="number"
                                                                min="1"
                                                                max="999999"
                                                                maxlength="6"
                                                                oninput="if(this.value.length > 6) this.value = this.value.slice(0, 6);"
                                                                step="any"
                                                                class="wh-input-control wh-table-input wh-text-right wh-qty-input"
                                                                data-action="change-inbound-item-qty"
                                                                data-row-id="${row.id}"
                                                                value="${row.quantity ? row.quantity : ''}"
                                                                placeholder="0"
                                                            />
                                                        </td>
                                                        <td class="wh-text-right">
                                                            <span class="wh-cell-text wh-text-right" data-cell="price" data-row-id="${row.id}">${row.price ? formatVnCurrency(row.price) : '-'}</span>
                                                        </td>
                                                        <td class="wh-text-right">
                                                            <span class="wh-cell-text wh-text-right" data-cell="amount" data-row-id="${row.id}" style="font-weight: 600; color: #1e3a8a;">
                                                                ${formatVnCurrency(amount)}
                                                            </span>
                                                        </td>
                                                        <td style="text-align: center;">
                                                            <span class="wh-cell-text" data-cell="expiryDate" data-row-id="${row.id}">${escapeHtml(row.expiryDate || '-')}</span>
                                                        </td>
                                                        <td style="text-align: center;">
                                                            <button
                                                                type="button"
                                                                class="wh-vtable-del-btn"
                                                                data-action="delete-inbound-item-row"
                                                                data-row-id="${row.id}"
                                                                title="Xóa dòng này"
                                                            >
                                                                ${Icons.trashRed}
                                                            </button>
                                                        </td>
                                                    </tr>
                                                `;
                                            }).join('') : `
                                                <tr>
                                                    <td colspan="9" class="wh-empty-row">Chưa có dòng nguyên vật liệu nào. Nhấn "+ Thêm dòng mới" bên dưới để bổ sung.</td>
                                                </tr>
                                            `}
                                        </tbody>
                                        <tfoot>
                                            <tr>
                                                <td colspan="4" style="text-align: right; font-weight: 700; color: #374151;">Tổng cộng:</td>
                                                <td class="wh-text-right">
                                                    <strong id="wh-total-inbound-qty">${formatVnNumber(totalQuantity)}</strong>
                                                </td>
                                                <td></td>
                                                <td class="wh-text-right">
                                                    <strong id="wh-total-inbound-amount" style="color: #1d4ed8; font-size: 13.5px;">${formatVnCurrency(totalAmount)}</strong>
                                                </td>
                                                <td></td>
                                                <td></td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                ` : `
                                    <!-- BẢNG XEM CHI TIẾT NGUYÊN VẬT LIỆU (Chỉ đọc, không chỉnh sửa) -->
                                    <table class="wh-vtable">
                                        <thead>
                                            <tr>
                                                <th style="width: 45px; text-align: center;">#</th>
                                                <th style="width: 170px;">${Icons.pin}Mã NVL</th>
                                                <th>Tên nguyên vật liệu</th>
                                                <th style="width: 80px; text-align: center;">ĐVT</th>
                                                <th style="width: 120px;" class="wh-text-right">SL nhập</th>
                                                <th style="width: 140px;" class="wh-text-right">Giá nguyên liệu</th>
                                                <th style="width: 160px;" class="wh-text-right">Thành tiền</th>
                                                <th style="width: 130px; text-align: center;">Hạn sử dụng</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${voucher.items.length > 0 ? voucher.items.map((row, idx) => {
                                                const qty = Number(row.quantity) || 0;
                                                const price = Number(row.price) || 0;
                                                const amount = (row.amount !== undefined && row.amount !== null) ? Number(row.amount) : (qty * price);
                                                return `
                                                    <tr>
                                                        <td style="text-align: center;">${idx + 1}</td>
                                                        <td><strong>${escapeHtml(row.itemCode)}</strong></td>
                                                        <td>${escapeHtml(row.itemName)}</td>
                                                        <td style="text-align: center;">${escapeHtml(row.unit)}</td>
                                                        <td class="wh-text-right">${formatVnNumber(qty)}</td>
                                                        <td class="wh-text-right">${formatVnCurrency(price)}</td>
                                                        <td class="wh-text-right" style="font-weight: 600; color: #1e3a8a;">${formatVnCurrency(amount)}</td>
                                                        <td style="text-align: center;">${escapeHtml(row.expiryDate || '')}</td>
                                                    </tr>
                                                `;
                                            }).join('') : `
                                                <tr>
                                                    <td colspan="8" class="wh-empty-row">Chưa có dòng nguyên vật liệu nào.</td>
                                                </tr>
                                            `}
                                        </tbody>
                                        <tfoot>
                                            <tr>
                                                <td colspan="4" style="text-align: right; font-weight: 700; color: #374151;">Tổng cộng:</td>
                                                <td class="wh-text-right" style="font-weight: 700;">${formatVnNumber(totalQuantity)}</td>
                                                <td></td>
                                                <td class="wh-text-right" style="font-weight: 700; color: #1d4ed8; font-size: 13.5px;">${formatVnCurrency(totalAmount)}</td>
                                                <td></td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                `) : isOutboundProduct ? `
                                    <!-- Bảng chi tiết Thành phẩm xuất kho -->
                                    <table class="wh-vtable">
                                        <thead>
                                            <tr>
                                                <th style="width: 45px; text-align: center;">#</th>
                                                <th style="width: 120px;">${Icons.pin}Mã SP</th>
                                                <th>Tên sản phẩm thành phẩm</th>
                                                <th style="width: 75px; text-align: center;">ĐVT</th>
                                                <th style="width: 105px;" class="wh-text-right">SL xuất</th>
                                                <th style="width: 120px;" class="wh-text-right">Tồn kho hiện có</th>
                                                <th style="width: 130px;" class="wh-text-right">Đơn giá (VNĐ)</th>
                                                <th style="width: 145px;" class="wh-text-right">Thành tiền (VNĐ)</th>
                                                <th style="width: 130px; text-align: center;">Kiểm tra tồn</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${voucher.items.length > 0 ? voucher.items.map((row, idx) => {
                                                const qty = Number(row.quantity) || 0;
                                                const price = Number(row.price) || 0;
                                                const amount = (row.amount !== undefined && row.amount !== null) ? Number(row.amount) : (qty * price);
                                                const stockQty = row.stockQty !== undefined ? Number(row.stockQty) : null;
                                                const condStatus = row.conditionStatus || '';
                                                const isExpiredAll = condStatus === 'Tồn kho hết hạn' || condStatus === 'Hàng tồn đã hết hạn';
                                                const isExpiredPartial = condStatus === 'Thiếu số lượng đạt chuẩn';
                                                const isOutOfStock = row.isOutOfStock || (!isExpiredAll && !isExpiredPartial && stockQty !== null && stockQty < qty);
                                                const hasIssue = isExpiredAll || isExpiredPartial || isOutOfStock || row.isItemEligible === false;
                                                const badgeStyle = isExpiredPartial
                                                    ? 'background:#fef3c7; color:#b45309;'
                                                    : (isExpiredAll || isOutOfStock || hasIssue)
                                                        ? 'background:#fee2e2; color:#b91c1c;'
                                                        : 'background:#dcfce7; color:#15803d;';
                                                const badgeLabel = isExpiredAll
                                                    ? '✕ Tồn kho hết hạn'
                                                    : isExpiredPartial
                                                        ? '⚠️ Thiếu số lượng đạt chuẩn'
                                                        : isOutOfStock
                                                            ? `⚠️ Thiếu ${stockQty !== null ? formatVnNumber(qty - stockQty) : ''}`
                                                            : (condStatus ? `✓ ${condStatus}` : '✓ Đủ tồn kho');
                                                return `
                                                    <tr>
                                                        <td style="text-align: center;">${idx + 1}</td>
                                                        <td><strong>${escapeHtml(row.itemCode || '')}</strong></td>
                                                        <td><span style="font-weight: 500;">${escapeHtml(row.itemName || '')}</span></td>
                                                        <td style="text-align: center;">${escapeHtml(row.unit || 'Hộp')}</td>
                                                        <td class="wh-text-right"><strong>${formatVnNumber(qty)}</strong></td>
                                                        <td class="wh-text-right" style="color: ${hasIssue ? '#dc2626' : '#15803d'}; font-weight: 600;">
                                                            ${stockQty !== null ? formatVnNumber(stockQty) : '-'}
                                                        </td>
                                                        <td class="wh-text-right">${formatVnCurrency(price)}</td>
                                                        <td class="wh-text-right" style="font-weight: 600; color: #1e3a8a;">${formatVnCurrency(amount)}</td>
                                                        <td style="text-align: center;">
                                                            <span style="display:inline-flex; align-items:center; gap:3px; padding:2px 8px; border-radius:12px; ${badgeStyle} font-size:11.5px; font-weight:600; white-space:nowrap;">
                                                                ${escapeHtml(badgeLabel)}
                                                            </span>
                                                        </td>
                                                    </tr>
                                                `;
                                            }).join('') : `
                                                <tr>
                                                    <td colspan="9" class="wh-empty-row">Chưa có dòng sản phẩm nào.</td>
                                                </tr>
                                            `}
                                        </tbody>
                                        <tfoot>
                                            <tr>
                                                <td colspan="4" style="text-align: right; font-weight: 700; color: #374151;">Tổng cộng:</td>
                                                <td class="wh-text-right" style="font-weight: 700;">${formatVnNumber(totalQuantity)}</td>
                                                <td></td>
                                                <td></td>
                                                <td class="wh-text-right" style="font-weight: 700; color: #1d4ed8; font-size: 13.5px;">${formatVnCurrency(totalAmount)}</td>
                                                <td></td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                ` : `
                                    <!-- Bảng chi tiết Hàng hóa (Phiếu Xuất) -->
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
                                `}

                                <!-- Phân trang thu gọn ngay dưới chân bảng -->
                                <div class="wh-vtable-pager-row">
                                    <span>Số dòng/trang</span>
                                    <select class="wh-dg-page-size" ${isInbound ? 'disabled' : ''}>
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

                            ${isInbound ? (isCreate ? `
                                <!-- Phiếu Nhập Tạo Mới: Cụm nút Thêm dòng mới & Xóa tất cả dòng -->
                                <div class="wh-voucher-subfooter" style="padding-top: 10px; margin-bottom: 0; display: flex; justify-content: space-between; align-items: center;">
                                    <div class="wh-voucher-total-count" style="margin-bottom: 0;">
                                        Tổng số dòng: <strong>${voucher.items.length}</strong>
                                    </div>
                                    <div class="wh-voucher-row-actions" style="margin-bottom: 0;">
                                        <button
                                            type="button"
                                            class="wh-vbtn-outline wh-vbtn-blue"
                                            data-action="add-inbound-item-row"
                                            title="Thêm một dòng nguyên vật liệu mới"
                                        >
                                            ${Icons.plus}
                                            <span>Thêm dòng mới</span>
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-vbtn-outline wh-vbtn-red"
                                            data-action="clear-inbound-item-rows"
                                            title="Xóa tất cả các dòng hiện có"
                                        >
                                            ${Icons.trashRed}
                                            <span>Xóa tất cả dòng</span>
                                        </button>
                                    </div>
                                </div>
                            ` : `
                                <!-- Phiếu Nhập Xem Chi Tiết: Chỉ hiển thị tổng số dòng -->
                                <div class="wh-voucher-subfooter" style="padding-top: 10px; margin-bottom: 0;">
                                    <div class="wh-voucher-total-count">
                                        Tổng số dòng: <strong>${voucher.items.length}</strong>
                                    </div>
                                </div>
                            `) : isOutboundProduct ? `
                                <!-- Phiếu Xuất kho thành phẩm: Chỉ hiển thị tổng số dòng -->
                                <div class="wh-voucher-subfooter" style="padding-top: 10px; margin-bottom: 0;">
                                    <div class="wh-voucher-total-count">
                                        Tổng số mặt hàng: <strong>${voucher.items.length}</strong>
                                    </div>
                                </div>
                            ` : `
                                <!-- Cụm nút Thêm dòng / Xóa hết dòng / Thêm ghi chú + Đính kèm (Phiếu Xuất) -->
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
                            `}
                        </div>
                    </div>

                    <!-- Thanh nút hành động cố định dưới đáy Phiếu -->
                    <div class="wh-voucher-bottombar">
                        ${isInbound ? (isCreate ? `
                            <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
                                <div>
                                    <button type="button" class="wh-btn wh-btn-cancel" data-action="close-voucher-modal">Hủy</button>
                                </div>
                                <div style="display: flex; gap: 8px;">
                                    <button
                                        type="button"
                                        class="wh-btn wh-btn-secondary"
                                        data-action="save-draft-inbound-voucher"
                                        title="Lưu lại bản nháp để tiếp tục chỉnh sửa sau"
                                    >
                                        Lưu nháp
                                    </button>
                                    <button
                                        type="button"
                                        class="wh-btn wh-btn-apply"
                                        data-action="save-inbound-voucher"
                                        title="Lưu và ghi sổ phiếu nhập kho"
                                    >
                                        Lưu
                                    </button>
                                </div>
                            </div>
                        ` : `
                            <div style="display:flex; justify-content:space-between; align-items:center; width:100%;">
                                <div style="color: #64748b; font-size: 12.5px;">
                                    ${(voucher.isDraft || voucher.status === 'Lưu nháp')
                                        ? `<span style="color:#0284c7;font-weight:500;">ℹ️ Phiếu này đang lưu nháp trên giao diện (chưa lưu CSDL). Nhấn "Nhập" để lưu vào CSDL với trạng thái Approve hoặc "Không nhập" để lưu với trạng thái Reject.</span>`
                                        : (voucher.status === 'Approve' || voucher.status === 'Approved' || voucher.status === 'Đã nhập kho')
                                            ? `<span style="color:#16a34a;font-weight:600;">✓ Phiếu này đã ở trạng thái Approve và cộng tồn kho nguyên vật liệu.</span>`
                                            : (voucher.status === 'Reject' || voucher.status === 'Rejected' || voucher.status === 'Từ chối')
                                                ? `<span style="color:#ef4444;font-weight:600;">✕ Phiếu nhập này đã ở trạng thái Reject (không nhập kho).</span>`
                                                : `<span style="color:#0284c7;font-weight:500;">ℹ️ Kiểm tra số lượng thực nhận và hạn sử dụng trước khi nhập kho.</span>`
                                    }
                                </div>
                                <div style="display:flex; gap:8px;">
                                    ${(voucher.isDraft || voucher.status === 'Lưu nháp' || voucher.status === 'Chờ nhập kho' || voucher.status === 'Pending') ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound"
                                            style="height:32px;padding:0 14px;font-size:12px;font-weight:600;"
                                            data-action="approve-import-modal"
                                            data-receipt-id="${escapeHtml(String(voucher.receiptId || voucher.ipReceiptId || voucher.id || ''))}"
                                            data-voucher-no="${escapeHtml(voucher.voucherNo || '')}"
                                        >
                                            ${Icons.checkBlue || ''} Nhập
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            style="height:32px;padding:0 14px;font-size:12px;font-weight:600;"
                                            data-action="reject-import-modal"
                                            data-receipt-id="${escapeHtml(String(voucher.receiptId || voucher.ipReceiptId || voucher.id || ''))}"
                                            data-voucher-no="${escapeHtml(voucher.voucherNo || '')}"
                                        >
                                            ${Icons.closeX || ''} Không nhập
                                        </button>
                                    ` : ''}
                                    <button type="button" class="wh-btn wh-btn-cancel" data-action="close-voucher-modal">Đóng</button>
                                </div>
                            </div>
                        `) : isOutboundProduct ? `
                            <div style="display:flex; justify-content:space-between; align-items:center; width:100%;">
                                <div style="color: #64748b; font-size: 12.5px;">
                                    ${(voucher.status === 'Đã xuất kho' || voucher.status === 'Approved' || voucher.status === 'Completed')
                                        ? `<span style="color:#16a34a;font-weight:600;">✓ Phiếu xuất kho này đã được duyệt và trừ số lượng trong kho thành phẩm.</span>`
                                        : (voucher.status === 'Từ chối' || voucher.status === 'Rejected')
                                            ? `<span style="color:#ef4444;font-weight:600;">✕ Phiếu xuất kho này đã bị từ chối xuất bán.</span>`
                                            : (String(voucher.status || '').toLowerCase() === 'cancel' || voucher.status === 'Cancelled' || voucher.status === 'Canceled' || voucher.status === 'Đã hủy')
                                                ? `<span style="color:#dc2626;font-weight:600;">✕ Đơn hàng liên kết đã ở trạng thái Cancel nên phiếu xuất kho này đã bị hủy (đã hoàn lại tồn kho nếu đã xuất trước đó).</span>`
                                                : (voucher.canApprove !== false && !voucher.hasStockIssue)
                                                    ? `<span style="color:#16a34a;font-weight:600;">✓ Tất cả sản phẩm đều đủ tồn kho thực tế. Sẵn sàng duyệt xuất kho bán hàng.</span>`
                                                    : `<span style="color:#dc2626;font-weight:600;">⚠️ Không thể duyệt xuất: ${escapeHtml(voucher.approvalBlockReason || 'Có sản phẩm vượt quá tồn kho hiện tại!')}</span>`
                                    }
                                </div>
                                <div style="display:flex; gap:8px;">
                                    ${(voucher.status === 'Chờ xuất kho' || voucher.status === 'Pending') ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound ${voucher.canApprove !== false && !voucher.hasStockIssue ? '' : 'is-disabled'}"
                                            style="height:32px;padding:0 14px;font-size:12px;font-weight:600;${voucher.canApprove !== false && !voucher.hasStockIssue ? '' : 'opacity:0.65;cursor:not-allowed;background:#94a3b8;color:#fff;border-color:#94a3b8;'}"
                                            data-action="approve-product-export-modal"
                                            data-receipt-id="${escapeHtml(String(voucher.exportReceiptId || voucher.id || ''))}"
                                            data-voucher-no="${escapeHtml(voucher.voucherNo || '')}"
                                            data-can-approve="${voucher.canApprove !== false && !voucher.hasStockIssue ? 'true' : 'false'}"
                                            data-block-reason="${escapeHtml(voucher.approvalBlockReason || '')}"
                                        >
                                            ${Icons.checkBlue || ''} Duyệt xuất kho
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            style="height:32px;padding:0 14px;font-size:12px;font-weight:600;"
                                            data-action="reject-product-export-modal"
                                            data-receipt-id="${escapeHtml(String(voucher.exportReceiptId || voucher.id || ''))}"
                                            data-voucher-no="${escapeHtml(voucher.voucherNo || '')}"
                                        >
                                            ${Icons.closeX || ''} Từ chối
                                        </button>
                                    ` : ''}
                                    <button
                                        type="button"
                                        class="wh-btn wh-btn-cancel"
                                        style="height:32px;padding:0 16px;border-radius:4px;font-size:12px;"
                                        data-action="close-voucher-modal"
                                    >
                                        Đóng
                                    </button>
                                </div>
                            </div>
                        ` : `
                            <button type="button" class="wh-btn wh-btn-cancel" data-action="close-voucher-modal">Hủy</button>
                            <button type="button" class="wh-btn wh-btn-cancel" data-action="close-voucher-modal">Lưu và Thêm</button>
                            <button type="button" class="wh-btn wh-btn-apply" data-action="close-voucher-modal">Lưu</button>
                        `}
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
                ${f.prefix ? `<span class="wh-dg-filter-prefix">${escapeHtml(f.prefix)}</span>` : ''}
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

        let addBtnAction = '';
        let addBtnTitle = config.addButtonLabel || 'Thêm mới';
        if (config.canOpenVoucher) {
            addBtnAction = `data-action="open-voucher-modal" data-voucher-type="${config.voucherType}"`;
            addBtnTitle = config.addButtonLabel || `Lập phiếu ${config.voucherType === 'inbound' ? 'nhập kho' : 'xuất kho'}`;
        } else if (config.pageType === 'danh-muc-vthh') {
            const isProd = config.activeTab === 'san-pham';
            addBtnAction = `data-action="create-vthh-item" data-item-type="${isProd ? 'product' : 'ingredient'}"`;
            addBtnTitle = isProd ? 'Thêm mới sản phẩm' : 'Thêm mới nguyên vật liệu';
        } else if (config.pageType === 'yeu-cau-san-xuat') {
            addBtnAction = 'data-action="open-create-pr-modal"';
            addBtnTitle = 'Lập yêu cầu sản xuất mới';
        } else if (config.pageType === 'nha-cung-cap') {
            addBtnAction = 'data-action="open-create-supplier-modal"';
            addBtnTitle = 'Thêm mới nhà cung cấp';
        }

        const addBtnHtml = config.showAddButton
            ? `<button
                type="button"
                class="wh-dg-add-btn"
                ${addBtnAction}
                title="${escapeHtml(addBtnTitle)}"
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
            <th style="${col.width ? `width:${col.width};` : ''}${col.align ? `text-align:${col.align};` : ''}">
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
                    if (col.isApprovalButtons) {
                        const isInbound = config.pageType === 'nhap-kho';
                        const isApproved = isInbound
                            ? (row.status === 'Đã nhập kho')
                            : (row.orderStatus === 'Đã xuất kho' || row.status === 'Đã xuất kho');
                        const isRejected = isInbound
                            ? (row.status === 'Chờ nhập kho')
                            : (row.orderStatus === 'Chờ xuất kho' || row.status === 'Chờ xuất kho');
                        const approveAction = isInbound ? 'approve-inbound-cmd' : 'approve-outbound-cmd';
                        const rejectAction = isInbound ? 'reject-inbound-cmd' : 'reject-outbound-cmd';
                        const approveTitle = isInbound ? 'Duyệt nhập kho' : 'Duyệt xuất kho';
                        const rejectTitle = isInbound ? 'Từ chối nhập kho' : 'Từ chối xuất kho';
                        const docId = row.orderNo || row.voucherNo || '';
                        return `
                            <td>
                                <div class="wh-approval-btns">
                                    <button
                                        type="button"
                                        class="wh-btn-approve ${isApproved ? 'is-active' : ''}"
                                        data-action="${approveAction}"
                                        data-order-no="${escapeHtml(docId)}"
                                        title="${approveTitle}"
                                    >Duyệt</button>
                                    <button
                                        type="button"
                                        class="wh-btn-reject ${isRejected ? 'is-active' : ''}"
                                        data-action="${rejectAction}"
                                        data-order-no="${escapeHtml(docId)}"
                                        title="${rejectTitle}"
                                    >Từ chối</button>
                                </div>
                            </td>
                        `;
                    }
                    if (col.isLink) {
                        if (config.pageType === 'danh-muc-vthh') {
                            const isProd = config.activeTab === 'san-pham';
                            const itemType = isProd ? 'product' : 'ingredient';
                            const itemId = row.id ?? val;
                            return `<td>
                                <a
                                    href="javascript:void(0)"
                                    onclick="return false;"
                                    class="wh-dg-link"
                                    data-action="view-vthh-item"
                                    data-item-type="${itemType}"
                                    data-item-id="${escapeHtml(String(itemId))}"
                                    title="Xem thông tin chi tiết ${escapeHtml(val)}"
                                >${escapeHtml(val)}</a>
                            </td>`;
                        }
                        if (config.canOpenVoucher) {
                            return `<td>
                                <a
                                    href="javascript:void(0)"
                                    onclick="return false;"
                                    class="wh-dg-link"
                                    data-action="open-voucher-modal"
                                    data-voucher-type="${config.voucherType}"
                                    data-doc-no="${escapeHtml(val)}"
                                    data-receipt-id="${escapeHtml(String(row.expReceiptId || row.id || ''))}"
                                    title="Nhấn để mở chi tiết phiếu ${escapeHtml(val)}"
                                >${escapeHtml(val)}</a>
                            </td>`;
                        }
                        return `<td><a href="javascript:void(0)" class="wh-dg-link" onclick="return false;">${escapeHtml(val)}</a></td>`;
                    }
                    if (col.isPrLink) {
                        return `<td>
                            <a
                                href="javascript:void(0)"
                                onclick="return false;"
                                class="wh-dg-link wh-pr-code-link"
                                data-action="view-pr-item"
                                data-pr-id="${escapeHtml(String(row.requestId || ''))}"
                                title="Xem chi tiết lệnh sản xuất ${escapeHtml(val)}"
                            >
                                <strong>${escapeHtml(val)}</strong>
                            </a>
                        </td>`;
                    }
                    if (col.isNumber) {
                        const numFormatted = formatVnNumber(row[col.key]);
                        return `<td style="text-align:right;font-weight:600;color:#1e293b;">${escapeHtml(numFormatted)}</td>`;
                    }
                    if (col.isPrAssigned) {
                        const empCode = row.assignedToEmployeeCode ? ` (${row.assignedToEmployeeCode})` : '';

                        // Nhân viên tiếp nhận lấy từ assignedto
                        if (row.assignedTo && row.assignedToName) {
                            return `
                                <td>
                                    <div class="wh-pr-assigned-badge is-locked" style="background:#f0fdf4;color:#166534;border:1px solid #bbf7d0;display:inline-flex;align-items:center;gap:6px;padding:3px 8px;border-radius:4px;font-size:12px;" title="Nhân viên tiếp nhận (lấy từ assignedto)">
                                        <span style="font-size:12px;">👤</span>
                                        <span class="wh-pr-assigned-name" style="font-weight:600;">${escapeHtml(row.assignedToName + empCode)}</span>
                                    </div>
                                </td>
                            `;
                        }

                        // Chưa có nhân viên tiếp nhận (assignedto trống): Không có nút tiếp nhận
                        return `
                            <td style="color:#94a3b8;font-size:13px;font-style:italic;text-align:center;">
                                -
                            </td>
                        `;
                    }
                    if (col.isPrStatus) {
                        const st = row.status || 'Pending';
                        let cls = 'wh-pr-badge-pending';
                        let label = 'Chờ xử lý';
                        if (st === 'In Production' || st === 'In Progress' || st === 'Đang sản xuất') {
                            cls = 'wh-pr-badge-progress';
                            label = 'Đang sản xuất';
                        } else if (st === 'Completed' || st === 'Đã hoàn thành') {
                            cls = 'wh-pr-badge-completed';
                            label = 'Đã hoàn thành';
                        } else if (st === 'Cancelled' || st === 'Từ chối' || st === 'Đã hủy') {
                            cls = 'wh-pr-badge-cancelled';
                            label = 'Đã hủy';
                        }
                        return `
                            <td style="text-align:center;">
                                <span class="wh-pr-status-badge ${cls}">${escapeHtml(label)}</span>
                            </td>
                        `;
                    }
                    if (col.isPrActions) {
                        const prId = escapeHtml(String(row.requestId));
                        const prCode = escapeHtml(String(row.requestCode || row.orderNo || ''));
                        const isPending = (row.status || 'Pending') === 'Pending';

                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    ${isPending ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-icon wh-btn-view-voucher"
                                            data-action="view-pr-item"
                                            data-pr-id="${prId}"
                                            title="Xem chi tiết lệnh ${prCode}"
                                        >
                                            ${Icons.docFile || Icons.pencil}
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-icon wh-btn-edit-row"
                                            data-action="edit-pr-item"
                                            data-pr-id="${prId}"
                                            title="Chỉnh sửa lệnh ${prCode}"
                                        >
                                            ${Icons.penToSquare || Icons.pencil}
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-icon wh-btn-delete-row"
                                            data-action="delete-pr-item"
                                            data-pr-id="${prId}"
                                            data-pr-code="${prCode}"
                                            title="Xóa lệnh ${prCode}"
                                        >
                                            ${Icons.trashRed}
                                        </button>
                                    ` : `
                                        <button
                                            type="button"
                                            class="wh-btn-action-icon wh-btn-view-voucher"
                                            data-action="view-pr-item"
                                            data-pr-id="${prId}"
                                            title="Xem chi tiết lệnh ${prCode}"
                                        >
                                            ${Icons.docFile || Icons.pencil}
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-icon"
                                            disabled
                                            style="opacity:0.35;cursor:not-allowed;"
                                            title="Lệnh '${escapeHtml(row.status)}': các thông tin đã bị khóa, không được chỉnh sửa"
                                        >
                                            🔒
                                        </button>
                                    `}
                                </div>
                            </td>
                        `;
                    }
                    if (col.isSupplierCode) {
                        return `<td>
                            <a
                                href="javascript:void(0)"
                                onclick="return false;"
                                class="wh-dg-link"
                                data-action="view-supplier-detail"
                                data-supplier-id="${escapeHtml(String(row.supplierId || row.id || ''))}"
                                title="Xem chi tiết nhà cung cấp ${escapeHtml(val)}"
                            >
                                <strong style="color:#2563eb;">${escapeHtml(val)}</strong>
                            </a>
                        </td>`;
                    }
                    if (col.isSupplierActions) {
                        const supId = escapeHtml(String(row.supplierId || row.id || ''));
                        const supName = escapeHtml(String(row.supplierName || row.name || ''));
                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-view-voucher"
                                        data-action="view-supplier-detail"
                                        data-supplier-id="${supId}"
                                        title="Xem chi tiết nhà cung cấp ${supName}"
                                    >
                                        ${Icons.docFile || Icons.pencil}
                                    </button>
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-edit-row"
                                        data-action="edit-supplier-item"
                                        data-supplier-id="${supId}"
                                        title="Chỉnh sửa thông tin ${supName}"
                                    >
                                        ${Icons.penToSquare || Icons.pencil}
                                    </button>
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-delete-row"
                                        data-action="delete-supplier-item"
                                        data-supplier-id="${supId}"
                                        data-supplier-name="${supName}"
                                        title="Xóa nhà cung cấp ${supName}"
                                    >
                                        ${Icons.trashRed}
                                    </button>
                                </div>
                            </td>
                        `;
                    }
                    if (col.isStatusLink) {
                        const cls = row.statusType === 'success' ? 'wh-dg-status-success' : 'wh-dg-status-info';
                        return `<td><span class="${cls}">${escapeHtml(val)}</span></td>`;
                    }
                    if (col.isBadge) {
                        let cls = 'wh-dg-status-warning';
                        const currentStatus = val || row.status || row.orderStatus;
                        if (currentStatus === 'Approve' || currentStatus === 'Approved' || currentStatus === 'Đã xuất kho' || currentStatus === 'Đã nhập kho' || currentStatus === 'Completed' || row.orderStatusType === 'success' || row.statusType === 'success') {
                            cls = 'wh-dg-status-success';
                        } else if (currentStatus === 'Reject' || currentStatus === 'Rejected' || currentStatus === 'Từ chối' || String(currentStatus || '').toLowerCase() === 'cancel' || currentStatus === 'Cancelled' || currentStatus === 'Canceled' || currentStatus === 'Đã hủy' || row.orderStatusType === 'danger' || row.statusType === 'danger') {
                            cls = 'wh-dg-status-danger';
                        } else if (currentStatus === 'Lưu nháp' || currentStatus === 'Nháp' || row.isDraft) {
                            cls = 'wh-dg-status-draft';
                        } else if (currentStatus === 'Chờ xuất kho' || currentStatus === 'Chờ nhập kho' || currentStatus === 'Pending' || row.orderStatusType === 'warning' || row.statusType === 'warning') {
                            cls = 'wh-dg-status-warning';
                        }
                        const isApprovedState = currentStatus === 'Approved' || currentStatus === 'Approve';
                        const extraStyle = isApprovedState ? 'background:#dcfce7; color:#15803d; border:1px solid #bbf7d0; border-radius:4px; padding:2px 8px; font-weight:600; display:inline-flex;' : '';
                        return `<td><span class="${cls}" style="${extraStyle}">${escapeHtml(val)}</span></td>`;
                    }
                    if (col.isOrderStatus) {
                        let cls = 'wh-dg-status-warning';
                        const currentStatus = val || row.status || row.orderStatus;
                        if (currentStatus === 'Đã xuất kho' || currentStatus === 'Đã nhập kho' || currentStatus === 'Approved' || currentStatus === 'Approve' || currentStatus === 'Completed' || row.orderStatusType === 'success' || row.statusType === 'success') {
                            cls = 'wh-dg-status-success';
                        } else if (currentStatus === 'Từ chối' || currentStatus === 'Rejected' || String(currentStatus || '').toLowerCase() === 'cancel' || currentStatus === 'Cancelled' || currentStatus === 'Canceled' || currentStatus === 'Đã hủy' || row.orderStatusType === 'danger' || row.statusType === 'danger') {
                            cls = 'wh-dg-status-danger';
                        } else if (currentStatus === 'Chờ xuất kho' || currentStatus === 'Chờ nhập kho' || currentStatus === 'Pending' || row.orderStatusType === 'warning' || row.statusType === 'warning') {
                            cls = 'wh-dg-status-warning';
                        }
                        const isApprovedState = currentStatus === 'Approved' || currentStatus === 'Approve';
                        const extraStyle = isApprovedState ? 'background:#dcfce7; color:#15803d; border:1px solid #bbf7d0; border-radius:4px; padding:2px 8px; font-weight:600; display:inline-flex;' : '';
                        return `<td><span class="${cls}" style="${extraStyle}">${escapeHtml(val)}</span></td>`;
                    }
                    if (col.isImage) {
                        const imgSrc = (row[col.key] || '').trim();
                        const altText = row.productName || 'Ảnh sản phẩm';
                        return `
                            <td class="wh-dg-img-cell" style="text-align:center;">
                                <div class="wh-product-img-box">
                                    ${imgSrc
                                        ? `<img src="${escapeHtml(imgSrc)}" alt="${escapeHtml(altText)}" class="wh-product-thumbnail" onerror="this.style.display='none';if(this.nextElementSibling)this.nextElementSibling.style.display='inline-flex';" /><span class="wh-product-img-broken" style="display:none;" title="Không thể tải ảnh">${Icons.brokenImage}</span>`
                                        : `<span class="wh-product-img-broken" title="Chưa có ảnh sản phẩm">${Icons.brokenImage}</span>`
                                    }
                                </div>
                            </td>
                        `;
                    }
                    if (col.isActionDelete) {
                        const isProd = config.activeTab === 'san-pham';
                        const itemType = isProd ? 'product' : 'ingredient';
                        const itemId = row.id ?? (isProd ? row.productCode : row.ingrCode);
                        const itemName = row.productName || row.ingrName || '';
                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-view-voucher"
                                        data-action="view-vthh-item"
                                        data-item-type="${itemType}"
                                        data-item-id="${escapeHtml(String(itemId))}"
                                        data-item-name="${escapeHtml(itemName)}"
                                        title="Xem chi tiết ${escapeHtml(itemName)}"
                                    >
                                        ${Icons.docFile || Icons.pencil}
                                    </button>
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-edit-row"
                                        data-action="edit-vthh-item"
                                        data-item-type="${itemType}"
                                        data-item-id="${escapeHtml(String(itemId))}"
                                        data-item-name="${escapeHtml(itemName)}"
                                        title="Chỉnh sửa ${escapeHtml(itemName)}"
                                    >
                                        ${Icons.penToSquare || Icons.pencil}
                                    </button>
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-delete-row"
                                        data-action="delete-vthh-row"
                                        data-item-type="${itemType}"
                                        data-item-id="${escapeHtml(String(itemId))}"
                                        data-item-name="${escapeHtml(itemName)}"
                                        title="Xóa ${escapeHtml(itemName)}"
                                    >
                                        ${Icons.trashRed}
                                    </button>
                                </div>
                            </td>
                        `;
                    }
                    if (col.isActionInboundProduct) {
                        const status = row.orderStatus || row.status || '';
                        const isPending = status === 'Chờ nhập kho' || status === 'Pending' || status === 'In Production' || status === 'Completed';
                        const isApproved = status === 'Đã nhập kho' || status === 'Approved' || status === 'Approve';
                        const isRejected = status === 'Từ chối' || status === 'Rejected' || status === 'Reject';
                        const orderNo = row.orderNo || row.receiptCode || '';
                        const receiptId = row.productReceiptId || row.id || '';

                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    ${isPending ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound"
                                            data-action="approve-product-receipt"
                                            data-receipt-id="${escapeHtml(String(receiptId))}"
                                            data-order-no="${escapeHtml(orderNo)}"
                                            title="Duyệt nhận thành phẩm cho lệnh ${escapeHtml(orderNo)}"
                                        >
                                            ${Icons.checkBlue || ''} Duyệt
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            data-action="reject-product-receipt"
                                            data-receipt-id="${escapeHtml(String(receiptId))}"
                                            data-order-no="${escapeHtml(orderNo)}"
                                            title="Từ chối nhận thành phẩm"
                                        >
                                            ${Icons.closeX || ''} Từ chối
                                        </button>
                                    ` : isApproved ? `
                                        <span class="wh-action-status-badge is-approved" title="Đã duyệt nhận thành phẩm">
                                            ✓ Đã nhập
                                        </span>
                                    ` : isRejected ? `
                                        <span class="wh-action-status-badge is-rejected" title="Đã từ chối nhận hàng">
                                            ✕ Từ chối
                                        </span>
                                    ` : ''}
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-view-voucher"
                                        data-action="open-voucher-modal"
                                        data-voucher-type="inbound-command"
                                        data-doc-no="${escapeHtml(orderNo)}"
                                        data-receipt-id="${escapeHtml(String(receiptId))}"
                                        title="Xem chi tiết ${escapeHtml(orderNo)}"
                                    >
                                        ${Icons.docFile || Icons.pencil}
                                    </button>
                                </div>
                            </td>
                        `;
                    }
                    if (col.isActionImportReceipt) {
                        const status = row.orderStatus || row.status || '';
                        const isDraft = Boolean(row.isDraft) || status === 'Lưu nháp' || status === 'Nháp';
                        const isApproved = status === 'Approve' || status === 'Approved' || status === 'Đã nhập kho';
                        const isRejected = status === 'Reject' || status === 'Rejected' || status === 'Từ chối';
                        const isPending = isDraft || (!isApproved && !isRejected);
                        const voucherNo = row.voucherNo || row.orderNo || '';
                        const receiptId = row.ipReceiptId || row.id || '';

                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    ${isPending ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound"
                                            data-action="approve-import-receipt"
                                            data-receipt-id="${escapeHtml(String(receiptId))}"
                                            data-voucher-no="${escapeHtml(voucherNo)}"
                                            title="Nhập kho phiếu ${escapeHtml(voucherNo)}"
                                        >
                                            ${Icons.checkBlue || ''} Nhập
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            data-action="reject-import-receipt"
                                            data-receipt-id="${escapeHtml(String(receiptId))}"
                                            data-voucher-no="${escapeHtml(voucherNo)}"
                                            title="Không nhập phiếu ${escapeHtml(voucherNo)}"
                                        >
                                            ${Icons.closeX || ''} Không nhập
                                        </button>
                                    ` : isApproved ? `
                                        <span class="wh-action-status-badge is-approved" title="Đã nhập kho (Approve)">
                                            ✓ Đã nhập
                                        </span>
                                    ` : isRejected ? `
                                        <span class="wh-action-status-badge is-rejected" title="Không nhập kho (Reject)">
                                            ✕ Không nhập
                                        </span>
                                    ` : ''}
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-view-voucher"
                                        data-action="open-voucher-modal"
                                        data-voucher-type="inbound"
                                        data-doc-no="${escapeHtml(voucherNo)}"
                                        data-receipt-id="${escapeHtml(String(receiptId))}"
                                        title="Xem chi tiết ${escapeHtml(voucherNo)}"
                                    >
                                        ${Icons.docFile || Icons.pencil}
                                    </button>
                                </div>
                            </td>
                        `;
                    }
                    if (col.isActionOutbound) {
                        const status = row.orderStatus || row.status || '';
                        const isPending = status === 'Chờ xuất kho' || status === 'Pending';
                        const isApproved = status === 'Đã xuất kho' || status === 'Approved' || status === 'Completed';
                        const isRejected = status === 'Từ chối' || status === 'Rejected' || status === 'Reject';
                        const orderNo = row.orderNo || row.expReceiptCode || '';
                        const receiptId = row.expReceiptId || row.id || '';
                        const canApprove = row.canApprove !== false;
                        const blockReason = row.approvalBlockReason || '';

                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    ${isPending ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound ${canApprove ? '' : 'is-disabled'}"
                                            style="${canApprove ? '' : 'opacity:0.65; cursor:not-allowed; background:#94a3b8; color:#fff; border-color:#94a3b8;'}"
                                            data-action="approve-material-receipt"
                                            data-can-approve="${canApprove ? 'true' : 'false'}"
                                            data-block-reason="${escapeHtml(blockReason)}"
                                            data-receipt-id="${escapeHtml(String(receiptId))}"
                                            data-order-no="${escapeHtml(orderNo)}"
                                            title="${canApprove ? `Duyệt xuất kho cho phiếu ${escapeHtml(orderNo)}` : `Không thể duyệt: ${escapeHtml(blockReason)}`}"
                                        >
                                            ${Icons.checkBlue || ''} Duyệt
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            data-action="reject-material-receipt"
                                            data-receipt-id="${escapeHtml(String(receiptId))}"
                                            data-order-no="${escapeHtml(orderNo)}"
                                            title="Từ chối cấp phát vật tư"
                                        >
                                            ${Icons.closeX || ''} Từ chối
                                        </button>
                                    ` : isApproved ? `
                                        <span class="wh-action-status-badge is-approved" title="Đã duyệt xuất kho">
                                            ✓ Đã xuất
                                        </span>
                                    ` : isRejected ? `
                                        <span class="wh-action-status-badge is-rejected" title="Đã từ chối cấp phát">
                                            ✕ Từ chối
                                        </span>
                                    ` : ''}
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-view-voucher"
                                        data-action="open-voucher-modal"
                                        data-voucher-type="outbound-command"
                                        data-doc-no="${escapeHtml(orderNo)}"
                                        data-receipt-id="${escapeHtml(String(receiptId))}"
                                        title="Xem chi tiết ${escapeHtml(orderNo)}"
                                    >
                                        ${Icons.docFile || Icons.pencil}
                                    </button>
                                </div>
                            </td>
                        `;
                    }
                    if (col.isActionProductExport) {
                        const status = row.status || row.orderStatus || '';
                        const lowerStatus = String(status).trim().toLowerCase();
                        const isPending = status === 'Chờ xuất kho' || status === 'Pending';
                        const isApproved = status === 'Đã xuất kho' || status === 'Approved' || status === 'Completed';
                        const isRejected = status === 'Từ chối' || status === 'Rejected' || status === 'Reject';
                        const isCancelled = lowerStatus === 'cancel' || lowerStatus === 'cancelled' || lowerStatus === 'canceled' || status === 'Đã hủy';
                        const voucherNo = row.voucherNo || row.orderNo || '';
                        const exportReceiptId = row.exportReceiptId || row.id || '';
                        const canApprove = row.canApprove !== false && !row.hasStockIssue;
                        const blockReason = row.approvalBlockReason || (row.hasStockIssue ? 'Có sản phẩm không đủ tồn kho để xuất' : '');

                        return `
                            <td class="wh-dg-action-cell" style="text-align:center;">
                                <div class="wh-dg-action-group">
                                    ${isPending ? `
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-approve-outbound ${canApprove ? '' : 'is-disabled'}"
                                            style="${canApprove ? '' : 'opacity:0.65; cursor:not-allowed; background:#94a3b8; color:#fff; border-color:#94a3b8;'}"
                                            data-action="approve-product-export"
                                            data-can-approve="${canApprove ? 'true' : 'false'}"
                                            data-block-reason="${escapeHtml(blockReason)}"
                                            data-receipt-id="${escapeHtml(String(exportReceiptId))}"
                                            data-voucher-no="${escapeHtml(voucherNo)}"
                                            title="${canApprove ? `Duyệt xuất kho cho phiếu ${escapeHtml(voucherNo)}` : `Không thể duyệt: ${escapeHtml(blockReason)}`}"
                                        >
                                            ${Icons.checkBlue || ''} Duyệt
                                        </button>
                                        <button
                                            type="button"
                                            class="wh-btn-action-sm wh-btn-reject-outbound"
                                            data-action="reject-product-export"
                                            data-receipt-id="${escapeHtml(String(exportReceiptId))}"
                                            data-voucher-no="${escapeHtml(voucherNo)}"
                                            title="Từ chối xuất kho thành phẩm"
                                        >
                                            ${Icons.closeX || ''} Từ chối
                                        </button>
                                    ` : isApproved ? `
                                        <span class="wh-action-status-badge is-approved" title="Đã duyệt xuất kho">
                                            ✓ Đã xuất
                                        </span>
                                    ` : isRejected ? `
                                        <span class="wh-action-status-badge is-rejected" title="Đã từ chối xuất kho">
                                            ✕ Từ chối
                                        </span>
                                    ` : isCancelled ? `
                                        <span class="wh-action-status-badge is-rejected" title="Đơn hàng liên kết đã bị hủy">
                                            ✕ Cancel
                                        </span>
                                    ` : ''}
                                    <button
                                        type="button"
                                        class="wh-btn-action-icon wh-btn-view-voucher"
                                        data-action="open-voucher-modal"
                                        data-voucher-type="outbound"
                                        data-doc-no="${escapeHtml(voucherNo)}"
                                        data-receipt-id="${escapeHtml(String(exportReceiptId))}"
                                        title="Xem chi tiết phiếu ${escapeHtml(voucherNo)}"
                                    >
                                        ${Icons.docFile || Icons.pencil}
                                    </button>
                                </div>
                            </td>
                        `;
                    }
                    const alignStyle = col.align ? `text-align:${col.align};` : '';
                    return `<td style="${alignStyle}">${escapeHtml(val)}</td>`;
                }).join('');

                return `<tr>${checkboxCell}${dataCells}</tr>`;
            }).join('')
            : `<tr><td colspan="${totalColSpan}" class="wh-empty-row">Không có dữ liệu phù hợp</td></tr>`;

        const summaryStatsHtml = config.summaryStats ? `
            <div class="wh-pr-stats-bar">
                <div class="wh-pr-stat-card is-total ${config.activeTab === 'tat-ca' ? 'is-active' : ''}" data-action="switch-op-tab" data-page-type="yeu-cau-san-xuat" data-tab-id="tat-ca" title="Xem tất cả yêu cầu">
                    <div class="wh-pr-stat-info">
                        <span class="wh-pr-stat-label">Tổng yêu cầu</span>
                        <span class="wh-pr-stat-val">${config.summaryStats.total}</span>
                    </div>
                </div>
                <div class="wh-pr-stat-card is-pending ${config.activeTab === 'cho-xu-ly' ? 'is-active' : ''}" data-action="switch-op-tab" data-page-type="yeu-cau-san-xuat" data-tab-id="cho-xu-ly" title="Xem các yêu cầu chờ xử lý">
                    <div class="wh-pr-stat-info">
                        <span class="wh-pr-stat-label">Chờ xử lý</span>
                        <span class="wh-pr-stat-val text-warning">${config.summaryStats.pending}</span>
                    </div>
                </div>
                <div class="wh-pr-stat-card is-progress ${config.activeTab === 'dang-san-xuat' ? 'is-active' : ''}" data-action="switch-op-tab" data-page-type="yeu-cau-san-xuat" data-tab-id="dang-san-xuat" title="Xem các yêu cầu đang sản xuất">
                    <div class="wh-pr-stat-info">
                        <span class="wh-pr-stat-label">Đang sản xuất</span>
                        <span class="wh-pr-stat-val text-primary">${config.summaryStats.inProduction}</span>
                    </div>
                </div>
                <div class="wh-pr-stat-card is-completed ${config.activeTab === 'hoan-thanh' ? 'is-active' : ''}" data-action="switch-op-tab" data-page-type="yeu-cau-san-xuat" data-tab-id="hoan-thanh" title="Xem các yêu cầu đã hoàn thành">
                    <div class="wh-pr-stat-info">
                        <span class="wh-pr-stat-label">Đã hoàn thành</span>
                        <span class="wh-pr-stat-val text-success">${config.summaryStats.completed}</span>
                    </div>
                </div>
                <div class="wh-pr-stat-card is-unassigned" data-action="filter-pr-unreceived" title="Lọc các yêu cầu chưa tiếp nhận">
                    <div class="wh-pr-stat-info">
                        <span class="wh-pr-stat-label">Chưa tiếp nhận</span>
                        <span class="wh-pr-stat-val text-danger">${config.summaryStats.unreceived !== undefined ? config.summaryStats.unreceived : config.summaryStats.unassigned}</span>
                    </div>
                </div>
            </div>
        ` : '';

        return `
            <div class="wh-datagrid-page">
                <div class="wh-tabs-bar">
                    ${tabsHtml}
                </div>
                ${summaryStatsHtml}

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
     * Sub-component: Modal xem, thêm mới & chỉnh sửa chi tiết Danh mục VTHH (Sản phẩm / Nguyên vật liệu)
     */
    function renderVthhDetailModal(modalData) {
        if (!modalData || !modalData.item) return '';

        const { mode, itemType, item } = modalData;
        const isCreate = mode === 'create';
        const isEdit = mode === 'edit';
        const canEdit = isEdit || isCreate;
        const isProduct = itemType === 'product';

        const itemCode = isProduct ? item.productCode : item.ingrCode;
        const itemName = isProduct ? item.productName : item.ingrName;
        const typeLabel = isProduct ? 'sản phẩm' : 'nguyên vật liệu';
        const actualItemId = item.id ?? item.productId ?? item.ingrId ?? modalData.itemId ?? '';

        let titleText = '';
        let badgeClass = 'is-view';
        let badgeLabel = 'Chỉ xem (Đã khóa)';
        if (isCreate) {
            titleText = `Thêm mới ${typeLabel}`;
            badgeClass = 'is-create';
            badgeLabel = 'Thêm mới';
        } else if (isEdit) {
            titleText = `Chỉnh sửa ${typeLabel}: ${itemCode}`;
            badgeClass = 'is-edit';
            badgeLabel = 'Chế độ chỉnh sửa';
        } else {
            titleText = isProduct
                ? `Thông tin chi tiết sản phẩm: ${itemCode}`
                : `Thông tin chi tiết nguyên vật liệu: ${itemCode} - ${itemName}`;
            badgeClass = 'is-view';
            badgeLabel = 'Chỉ xem';
        }

        const isWideModal = !canEdit;
        const isApprovedProductReceipt = (r) => {
            if (!r) return false;
            const code = String(r.voucherNo || r.orderNo || r.receiptCode || '').trim().toUpperCase();
            const source = String(r.source || '').trim().toLowerCase();
            const note = String(r.note || '').trim().toLowerCase();
            if (code.startsWith('HOAN-') ||
                source.includes('hoàn hàng') ||
                source.includes('hoàn lại') ||
                note.includes('hoàn lại số lượng đã xuất') ||
                note.includes('hoàn lại kho')) {
                return false;
            }
            if (r.isDisposed) return true;
            const st = String(r.rawStatus || r.status || '').trim().toLowerCase();
            return st === 'approved' || st === 'approve' || st === 'đã nhập kho' || st === 'đã hủy';
        };
        const rawReceipts = isProduct
            ? (item.productReceipts || item.importReceipts || []).filter(isApprovedProductReceipt)
            : (item.importReceipts || []);
        const receipts = Array.isArray(rawReceipts) ? rawReceipts : [];
        const totalImportQty = receipts.reduce((sum, r) => sum + (Number(r.quantity) || 0), 0);
        const totalImportAmount = receipts.reduce((sum, r) => sum + (Number(r.amount) || ((Number(r.quantity) || 0) * (Number(r.cost) || 0))), 0);

        const distinctReceiptCosts = [];
        const seenReceiptCosts = new Set();
        receipts.forEach(r => {
            const c = Number(r.cost) || 0;
            if (c > 0 && !seenReceiptCosts.has(c)) {
                seenReceiptCosts.add(c);
                distinctReceiptCosts.push({
                    cost: c,
                    voucherNo: r.voucherNo || r.orderNo || '',
                    importDate: r.importDate || ''
                });
            }
        });
        const currentIngrCost = Number(item.cost) || 0;

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window wh-vthh-modal-window ${isWideModal ? 'wh-vthh-wide' : ''}">
                    <!-- Thanh tiêu đề Modal -->
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                            <h2 class="wh-voucher-title">${escapeHtml(titleText)}</h2>
                            <span class="wh-vthh-mode-badge ${badgeClass}">
                                ${escapeHtml(badgeLabel)}
                            </span>
                        </div>
                        <div class="wh-voucher-topbar-right">
                            <button
                                type="button"
                                class="wh-voucher-win-btn"
                                data-action="close-vthh-modal"
                                title="Đóng (Esc)"
                            >
                                ${Icons.closeX}
                            </button>
                        </div>
                    </div>

                    <!-- Thân Form chi tiết -->
                    <div class="wh-voucher-body wh-vthh-modal-body">
                        <form id="wh-vthh-form" onsubmit="return false;">
                            ${!canEdit ? (isProduct ? `
                                <!-- Giao diện Xem chi tiết Thành phẩm (kèm chi tiết các lệnh/phiếu nhập kho thành phẩm) -->
                                <div class="wh-vthh-info-card">
                                    <div class="wh-vthh-info-head">
                                        <div style="display:flex;align-items:center;gap:14px;">
                                            ${item.imageUrl ? `
                                                <div style="width:52px;height:52px;border-radius:8px;border:1px solid #cbd5e1;background:#fff;overflow:hidden;display:flex;align-items:center;justify-content:center;flex-shrink:0;">
                                                    <img src="${escapeHtml(item.imageUrl)}" alt="${escapeHtml(itemName)}" style="width:100%;height:100%;object-fit:cover;" onerror="this.style.display='none';" />
                                                </div>
                                            ` : ''}
                                            <div>
                                                <div style="display:flex;align-items:center;gap:10px;margin-bottom:4px;">
                                                    <span class="wh-vthh-info-code">${escapeHtml(itemCode)}</span>
                                                    <span class="wh-vthh-info-name">${escapeHtml(itemName)}</span>
                                                </div>
                                                ${item.shelfLifeMonths || item.expiredDate ? `
                                                    <div style="font-size:12px;color:#64748b;">
                                                        Hạn sử dụng: <strong>${item.shelfLifeMonths ? `${item.shelfLifeMonths} tháng` : ''}</strong> ${item.expiredDate ? `(${escapeHtml(item.expiredDate)})` : ''}
                                                    </div>
                                                ` : ''}
                                            </div>
                                        </div>
                                    </div>
                                    <div class="wh-vthh-stats-grid">
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Đơn vị tính (ĐVT)</div>
                                            <div class="wh-vthh-stat-value">${escapeHtml(item.unit || 'Hộp')}</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Số lượng tồn kho</div>
                                            <div class="wh-vthh-stat-value is-primary">${item.quantityFormatted || item.quantity || 0} ${escapeHtml(item.unit || 'Hộp')}</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Giá bán hiện hành</div>
                                            <div class="wh-vthh-stat-value">${item.costFormatted || (item.cost ? Number(item.cost).toLocaleString('vi-VN') : '0')} đ</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Giá vốn hiện hành</div>
                                            <div class="wh-vthh-stat-value" style="color:#0284c7;font-weight:700;">${item.materialCostFormatted || (item.materialCost ? Number(item.materialCost).toLocaleString('vi-VN') : '0')} đ</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Số lệnh nhập đã lập</div>
                                            <div class="wh-vthh-stat-value is-success">${receipts.length} lệnh/phiếu</div>
                                        </div>
                                    </div>
                                    ${item.description ? `
                                        <div style="margin-top:12px;padding-top:10px;border-top:1px dashed #e2e8f0;font-size:12.5px;color:#64748b;">
                                            <strong>Mô tả / Tiêu chuẩn chất lượng:</strong> ${escapeHtml(item.description)}
                                        </div>
                                    ` : ''}
                                </div>

                                <!-- Bảng chi tiết từng lệnh / phiếu nhập kho thành phẩm của sản phẩm -->
                                <div class="wh-vthh-receipts-section">
                                    <div class="wh-vthh-receipts-title">
                                        <h3>
                                            ${Icons.boxCheck || '📦'} Chi tiết từng lệnh / phiếu nhập kho thành phẩm của sản phẩm
                                        </h3>
                                        <span style="font-size:12px;color:#64748b;">
                                            Tổng cộng: <strong>${receipts.length}</strong> lần nhập kho
                                        </span>
                                    </div>

                                    ${receipts.length > 0 ? `
                                        <div class="wh-vthh-table-wrapper">
                                            <table class="wh-vthh-table">
                                                <thead>
                                                    <tr>
                                                        <th style="width:40px;text-align:center;">#</th>
                                                        <th style="width:130px;">Số lệnh nhập</th>
                                                        <th style="width:150px;">Ngày nhập kho</th>
                                                        <th>Người lập / Phân xưởng</th>
                                                        <th style="width:105px;text-align:right;">SL nhập</th>
                                                        <th style="width:115px;text-align:right;">Giá vốn</th>
                                                        <th style="width:130px;text-align:right;">Thành tiền</th>
                                                        <th style="width:115px;text-align:center;">Hạn sử dụng</th>
                                                        <th style="width:115px;text-align:center;">Trạng thái</th>
                                                        <th style="width:110px;text-align:center;">Thao tác</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    ${receipts.map((r, idx) => {
                                                        const isDisposed = Boolean(r.isDisposed) || r.status === 'Đã hủy';
                                                        const isApprove = !isDisposed && (r.status === 'Approve' || r.status === 'Approved' || r.status === 'Đã nhập kho');
                                                        const isReject = !isDisposed && (r.status === 'Reject' || r.status === 'Rejected' || r.status === 'Từ chối');
                                                        const isDraft = !isDisposed && (r.status === 'Lưu nháp' || r.status === 'Nháp' || r.isDraft);
                                                        const statusBadgeCls = isDisposed ? 'wh-dg-status-danger' : isApprove ? 'wh-dg-status-success' : isReject ? 'wh-dg-status-danger' : isDraft ? 'wh-dg-status-draft' : 'wh-dg-status-warning';
                                                        const statusText = isDisposed ? 'Đã hủy' : isApprove ? (r.status === 'Approve' ? 'Approve' : 'Approved') : isReject ? 'Reject' : isDraft ? 'Lưu nháp' : (r.status || 'Chờ nhập kho');

                                                        return `
                                                            <tr>
                                                                <td style="text-align:center;color:#64748b;">${idx + 1}</td>
                                                                <td style="font-weight:600;color:#0f172a;">
                                                                    ${escapeHtml(r.voucherNo || r.orderNo || '---')}
                                                                </td>
                                                                <td style="color:#64748b;">${escapeHtml(r.importDate || '---')}</td>
                                                                <td>
                                                                    <div style="font-weight:600;color:#1e293b;">${escapeHtml(r.creatorName || r.source || 'Phân xưởng sản xuất')}</div>
                                                                    ${r.employeeCode ? `<div style="font-size:11px;color:#94a3b8;">${escapeHtml(r.employeeCode)} • ${escapeHtml(r.source || 'Phân xưởng')}</div>` : ''}
                                                                </td>
                                                                <td style="text-align:right;font-weight:700;color:#0f172a;">
                                                                    ${escapeHtml(String(r.quantityFormatted || r.quantity))} <span style="font-size:11px;font-weight:normal;color:#64748b;">${escapeHtml(r.unit || item.unit || 'Hộp')}</span>
                                                                </td>
                                                                <td style="text-align:right;color:#475569;">
                                                                    ${escapeHtml(String(r.costFormatted || (r.cost ? Number(r.cost).toLocaleString('vi-VN') : '0')))} đ
                                                                </td>
                                                                <td style="text-align:right;font-weight:600;color:#0284c7;">
                                                                    ${escapeHtml(String(r.amountFormatted || (r.amount ? Number(r.amount).toLocaleString('vi-VN') : '0')))} đ
                                                                </td>
                                                                <td style="text-align:center;">
                                                                    <span class="wh-vthh-expiry-tag" style="${r.isExpired && !isDisposed ? 'background:#fee2e2;color:#b91c1c;border:1px solid #fca5a5;font-weight:600;' : ''}">${escapeHtml(r.expiredDate || item.expiredDate || '31/12/2028')}</span>
                                                                </td>
                                                                <td style="text-align:center;">
                                                                    <span class="${statusBadgeCls}">${escapeHtml(statusText)}</span>
                                                                </td>
                                                                <td style="text-align:center;">
                                                                    ${isDisposed ? `
                                                                        <span class="wh-action-status-badge is-rejected" style="background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;padding:3px 8px;border-radius:12px;font-size:11px;font-weight:600;display:inline-block;">Đã hủy SP</span>
                                                                    ` : (r.isExpired ? `
                                                                        <button
                                                                            type="button"
                                                                            class="wh-btn-action-sm"
                                                                            style="background:#fee2e2;color:#b91c1c;border:1px solid #fca5a5;border-radius:5px;padding:4px 10px;font-size:11.5px;font-weight:600;cursor:pointer;display:inline-flex;align-items:center;gap:4px;box-shadow:0 1px 2px rgba(0,0,0,0.05);transition:all 0.15s ease;"
                                                                            data-action="dispose-expired-product-batch"
                                                                            data-product-id="${actualItemId}"
                                                                            data-receipt-id="${r.productReceiptId}"
                                                                            data-voucher-no="${escapeHtml(r.voucherNo || '')}"
                                                                            data-qty="${r.quantity}"
                                                                            data-unit="${escapeHtml(r.unit || item.unit || 'Hộp')}"
                                                                            title="Hủy thành phẩm hết hạn sử dụng"
                                                                        >
                                                                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
                                                                            Hủy SP
                                                                        </button>
                                                                    ` : `<span style="color:#94a3b8;font-size:13px;">-</span>`)}
                                                                </td>
                                                            </tr>
                                                        `;
                                                    }).join('')}
                                                </tbody>
                                                <tfoot>
                                                    <tr>
                                                        <td colspan="4" style="text-align:right;font-weight:700;color:#1e293b;">
                                                            Tổng cộng (${receipts.length} lệnh/phiếu):
                                                        </td>
                                                        <td style="text-align:right;color:#0f172a;">
                                                            ${totalImportQty.toLocaleString('vi-VN')} <span style="font-size:11px;color:#64748b;">${escapeHtml(item.unit || 'Hộp')}</span>
                                                        </td>
                                                        <td></td>
                                                        <td style="text-align:right;color:#0284c7;">
                                                            ${totalImportAmount.toLocaleString('vi-VN')} đ
                                                        </td>
                                                        <td colspan="3"></td>
                                                    </tr>
                                                </tfoot>
                                            </table>
                                        </div>
                                    ` : `
                                        <div style="text-align:center;padding:36px 16px;background:#f8fafc;border:1px dashed #cbd5e1;border-radius:8px;margin-top:6px;">
                                            <div style="font-size:32px;margin-bottom:8px;">📦</div>
                                            <div style="font-weight:600;color:#334155;font-size:14.5px;">Chưa có phiếu nhập kho nào ở trạng thái Approved cho sản phẩm này</div>
                                            <div style="font-size:12.5px;color:#64748b;margin-top:4px;">Khi phiếu nhập kho thành phẩm có chứa ${escapeHtml(itemCode)} được duyệt (Approved), thông tin chi tiết số lượng, giá vốn và hạn sử dụng theo từng đợt nhập sẽ hiển thị tại đây.</div>
                                        </div>
                                    `}
                                </div>
                            ` : `
                                <!-- Giao diện Xem chi tiết Nguyên vật liệu (kèm chi tiết các phiếu nhập) -->
                                <div class="wh-vthh-info-card">
                                    <div class="wh-vthh-info-head">
                                        <div style="display:flex;align-items:center;gap:12px;">
                                            <span class="wh-vthh-info-code">${escapeHtml(itemCode)}</span>
                                            <span class="wh-vthh-info-name">${escapeHtml(itemName)}</span>
                                        </div>
                                    </div>
                                    <div class="wh-vthh-stats-grid">
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Đơn vị tính (ĐVT)</div>
                                            <div class="wh-vthh-stat-value">${escapeHtml(item.unit || 'kg')}</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Số lượng tồn kho</div>
                                            <div class="wh-vthh-stat-value is-primary">${item.quantityFormatted || item.quantity || 0} ${escapeHtml(item.unit || 'kg')}</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Đơn giá hiện hành</div>
                                            <div class="wh-vthh-stat-value">${item.priceFormatted || (item.price ? Number(item.price).toLocaleString('vi-VN') : '0')} đ</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Giá nhập</div>
                                            <div class="wh-vthh-stat-value" style="color:#0284c7;font-weight:700;">${item.costFormatted || (item.cost ? Number(item.cost).toLocaleString('vi-VN') : '0')} đ</div>
                                        </div>
                                        <div class="wh-vthh-stat-item">
                                            <div class="wh-vthh-stat-label">Số phiếu nhập đã lập</div>
                                            <div class="wh-vthh-stat-value is-success">${receipts.length} phiếu</div>
                                        </div>
                                    </div>
                                    ${item.description ? `
                                        <div style="margin-top:12px;padding-top:10px;border-top:1px dashed #e2e8f0;font-size:12.5px;color:#64748b;">
                                            <strong>Tiêu chuẩn kỹ thuật:</strong> ${escapeHtml(item.description)}
                                        </div>
                                    ` : ''}
                                </div>

                                <!-- Bảng chi tiết từng phiếu nhập của nguyên vật liệu -->
                                <div class="wh-vthh-receipts-section">
                                    <div class="wh-vthh-receipts-title">
                                        <h3>
                                            ${Icons.boxCheck || '📦'} Chi tiết từng phiếu nhập kho của nguyên vật liệu
                                        </h3>
                                        <span style="font-size:12px;color:#64748b;">
                                            Tổng cộng: <strong>${receipts.length}</strong> lần nhập kho
                                        </span>
                                    </div>

                                    ${receipts.length > 0 ? `
                                        <div class="wh-vthh-table-wrapper">
                                            <table class="wh-vthh-table">
                                                <thead>
                                                    <tr>
                                                        <th style="width:40px;text-align:center;">#</th>
                                                        <th style="width:130px;">Số phiếu nhập</th>
                                                        <th style="width:150px;">Ngày nhập kho</th>
                                                        <th>Nhà cung cấp</th>
                                                        <th style="width:105px;text-align:right;">SL nhập</th>
                                                        <th style="width:115px;text-align:right;">Đơn giá nhập</th>
                                                        <th style="width:130px;text-align:right;">Thành tiền</th>
                                                        <th style="width:115px;text-align:center;">Hạn sử dụng</th>
                                                        <th style="width:115px;text-align:center;">Trạng thái</th>
                                                        <th style="width:110px;text-align:center;">Thao tác</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    ${receipts.map((r, idx) => {
                                                        const isApprove = r.status === 'Approve' || r.status === 'Approved' || r.status === 'Đã nhập kho';
                                                        const isReject = r.status === 'Reject' || r.status === 'Rejected' || r.status === 'Từ chối';
                                                        const isDraft = r.status === 'Lưu nháp' || r.status === 'Nháp' || r.isDraft;
                                                        const statusBadgeCls = isApprove ? 'wh-dg-status-success' : isReject ? 'wh-dg-status-danger' : isDraft ? 'wh-dg-status-draft' : 'wh-dg-status-warning';
                                                        const statusText = isApprove ? (r.status === 'Approve' ? 'Approve' : 'Approved') : isReject ? 'Reject' : isDraft ? 'Lưu nháp' : (r.status || 'Chờ nhập kho');

                                                        return `
                                                            <tr>
                                                                <td style="text-align:center;color:#64748b;">${idx + 1}</td>
                                                                <td style="font-weight:600;color:#0f172a;">
                                                                    ${escapeHtml(r.voucherNo || '---')}
                                                                </td>
                                                                <td style="color:#64748b;">${escapeHtml(r.importDate || '---')}</td>
                                                                <td>
                                                                    <div style="font-weight:600;color:#1e293b;">${escapeHtml(r.supplierName || '---')}</div>
                                                                    ${r.supplierCode ? `<div style="font-size:11px;color:#94a3b8;">${escapeHtml(r.supplierCode)}</div>` : ''}
                                                                </td>
                                                                <td style="text-align:right;font-weight:700;color:#0f172a;">
                                                                    ${escapeHtml(String(r.quantityFormatted || r.quantity))} <span style="font-size:11px;font-weight:normal;color:#64748b;">${escapeHtml(r.unit || item.unit || 'kg')}</span>
                                                                </td>
                                                                <td style="text-align:right;color:#475569;">
                                                                    ${escapeHtml(String(r.costFormatted || (r.cost ? Number(r.cost).toLocaleString('vi-VN') : '0')))} đ
                                                                </td>
                                                                <td style="text-align:right;font-weight:600;color:#0284c7;">
                                                                    ${escapeHtml(String(r.amountFormatted || (r.amount ? Number(r.amount).toLocaleString('vi-VN') : '0')))} đ
                                                                </td>
                                                                <td style="text-align:center;">
                                                                    <span class="wh-vthh-expiry-tag" style="${r.isExpired && !r.isDisposed ? 'background:#fee2e2;color:#b91c1c;border:1px solid #fca5a5;font-weight:600;' : ''}">${escapeHtml(r.expiredDate || '31/12/2028')}</span>
                                                                </td>
                                                                <td style="text-align:center;">
                                                                    <span class="${statusBadgeCls}">${escapeHtml(statusText)}</span>
                                                                </td>
                                                                <td style="text-align:center;">
                                                                    ${r.isDisposed ? `
                                                                        <span class="wh-action-status-badge is-rejected" style="background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;padding:3px 8px;border-radius:12px;font-size:11px;font-weight:600;display:inline-block;">Đã hủy NVL</span>
                                                                    ` : (r.isExpired ? `
                                                                        <button
                                                                            type="button"
                                                                            class="wh-btn-action-sm"
                                                                            style="background:#fee2e2;color:#b91c1c;border:1px solid #fca5a5;border-radius:5px;padding:4px 10px;font-size:11.5px;font-weight:600;cursor:pointer;display:inline-flex;align-items:center;gap:4px;box-shadow:0 1px 2px rgba(0,0,0,0.05);transition:all 0.15s ease;"
                                                                            data-action="dispose-expired-batch"
                                                                            data-ingr-id="${actualItemId}"
                                                                            data-receipt-id="${r.ipReceiptId}"
                                                                            data-voucher-no="${escapeHtml(r.voucherNo || '')}"
                                                                            data-qty="${r.quantity}"
                                                                            data-unit="${escapeHtml(r.unit || item.unit || 'kg')}"
                                                                            title="Hủy nguyên vật liệu hết hạn sử dụng"
                                                                        >
                                                                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
                                                                            Hủy NVL
                                                                        </button>
                                                                    ` : `<span style="color:#94a3b8;font-size:13px;">-</span>`)}
                                                                </td>
                                                            </tr>
                                                        `;
                                                    }).join('')}
                                                </tbody>
                                                <tfoot>
                                                    <tr>
                                                        <td colspan="4" style="text-align:right;font-weight:700;color:#1e293b;">
                                                            Tổng cộng (${receipts.length} phiếu):
                                                        </td>
                                                        <td style="text-align:right;color:#0f172a;">
                                                            ${totalImportQty.toLocaleString('vi-VN')} <span style="font-size:11px;color:#64748b;">${escapeHtml(item.unit || 'kg')}</span>
                                                        </td>
                                                        <td></td>
                                                        <td style="text-align:right;color:#0284c7;">
                                                            ${totalImportAmount.toLocaleString('vi-VN')} đ
                                                        </td>
                                                        <td colspan="3"></td>
                                                    </tr>
                                                </tfoot>
                                            </table>
                                        </div>
                                    ` : `
                                        <div style="text-align:center;padding:36px 16px;background:#f8fafc;border:1px dashed #cbd5e1;border-radius:8px;margin-top:6px;">
                                            <div style="font-size:32px;margin-bottom:8px;">📦</div>
                                            <div style="font-weight:600;color:#334155;font-size:14.5px;">Chưa có phiếu nhập kho nào cho nguyên vật liệu này</div>
                                            <div style="font-size:12.5px;color:#64748b;margin-top:4px;">Khi lập và duyệt phiếu nhập có chứa ${escapeHtml(itemCode)}, thông tin chi tiết số lượng, đơn giá và hạn sử dụng theo từng phiếu sẽ hiển thị tại đây.</div>
                                        </div>
                                    `}
                                </div>
                            `) : (isProduct ? `
                                <!-- Form Thêm mới hoặc Sửa thông tin Sản phẩm -->
                                ${isCreate ? `
                                    <div style="background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 6px; padding: 10px 14px; font-size: 13px; color: #166534; display: flex; align-items: flex-start; gap: 8px; margin-bottom: 14px;">
                                        <span style="font-size: 16px; line-height: 1;">💡</span>
                                        <div>
                                            <strong>Quy tắc tạo mới thành phẩm:</strong> Nhập <strong>Tên sản phẩm</strong> và <strong>Đơn vị tính (ĐVT)</strong>. Trường <strong>Giá vốn</strong> được khóa cố định (0 đ) và sẽ tự động lấy giá cao nhất từ các <strong>Phiếu nhập kho thành phẩm</strong>.
                                        </div>
                                    </div>
                                ` : ''}
                                ${isEdit ? `
                                    <div style="background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 10px 14px; font-size: 13px; color: #1e40af; display: flex; align-items: flex-start; gap: 8px; margin-bottom: 14px;">
                                        <span style="font-size: 16px; line-height: 1;">ℹ️</span>
                                        <div>
                                            <strong>Lưu ý:</strong> Trường <strong>Giá vốn</strong> được khóa cố định và tự động lấy theo <strong>giá cao nhất từ các phiếu nhập kho thành phẩm</strong> của sản phẩm này.
                                        </div>
                                    </div>
                                ` : ''}

                                <!-- Khối ảnh và thông tin chung cho Sản phẩm -->
                                <div class="wh-vthh-product-head">
                                    <div class="wh-vthh-img-section">
                                        <div class="wh-vthh-img-preview-box">
                                            <img
                                                id="wh-vthh-img-preview"
                                                src="${escapeHtml(item.imageUrl || '')}"
                                                alt="${escapeHtml(itemName || 'Sản phẩm')}"
                                                class="wh-vthh-preview-img"
                                                style="${item.imageUrl ? '' : 'display:none;'}"
                                                onerror="this.style.display='none';var fb=document.getElementById('wh-vthh-img-broken');if(fb)fb.style.display='inline-flex';"
                                                onload="if(this.src && this.style.display==='none'){this.style.display='block';var fb=document.getElementById('wh-vthh-img-broken');if(fb)fb.style.display='none';}"
                                            />
                                            <div id="wh-vthh-img-broken" class="wh-vthh-img-broken" style="${item.imageUrl ? 'display:none;' : 'display:inline-flex;'}" title="Chưa có ảnh hoặc liên kết ảnh bị lỗi">
                                                ${Icons.brokenImage}
                                            </div>
                                        </div>
                                        <div class="wh-vthh-img-controls">
                                            <label class="wh-form-label" style="font-size:11px;margin-bottom:3px;">
                                                Ảnh sản phẩm (URL):
                                            </label>
                                            <input
                                                type="text"
                                                name="imageUrl"
                                                id="wh-edit-image-url"
                                                class="wh-input-control"
                                                value="${escapeHtml(item.imageUrl || '')}"
                                                placeholder="Dán link ảnh sản phẩm..."
                                            />
                                        </div>
                                    </div>

                                    <div class="wh-vthh-main-fields">
                                        <div class="wh-form-group">
                                            <label class="wh-form-label">
                                                Mã sản phẩm <span class="wh-lock-hint" title="${isCreate ? 'Mã được hệ thống tự động sinh' : 'Mã không được phép sửa'}">${Icons.lock} ${isCreate ? 'Tự động' : 'Khóa'}</span>
                                            </label>
                                            <input
                                                type="text"
                                                name="productCode"
                                                class="wh-input-control is-readonly"
                                                value="${escapeHtml(item.productCode || '')}"
                                                readonly
                                                tabindex="-1"
                                                style="font-weight: 600;"
                                            />
                                        </div>

                                        <div class="wh-form-group">
                                            <label class="wh-form-label">Tên sản phẩm <span class="wh-req">*</span></label>
                                            <input
                                                type="text"
                                                name="productName"
                                                class="wh-input-control"
                                                value="${escapeHtml(item.productName || '')}"
                                                placeholder="VD: Viên nén Panadol Extra 500mg..."
                                                required
                                                ${isCreate ? 'autofocus' : ''}
                                            />
                                        </div>

                                        <div class="wh-form-group">
                                            <label class="wh-form-label">
                                                Đơn vị tính (ĐVT) ${isCreate ? `<span class="wh-req">*</span>` : `<span class="wh-lock-hint" title="Đơn vị tính cố định">${Icons.lock} Khóa</span>`}
                                            </label>
                                            ${isCreate ? `
                                                <input
                                                    type="text"
                                                    name="unit"
                                                    class="wh-input-control"
                                                    value="${escapeHtml(item.unit || 'Hộp')}"
                                                    placeholder="VD: Hộp, Chai, Vỉ, Tuýp, Lọ, Gói..."
                                                    required
                                                />
                                            ` : `
                                                <input
                                                    type="text"
                                                    name="unit"
                                                    class="wh-input-control is-readonly"
                                                    value="${escapeHtml(item.unit || 'Hộp')}"
                                                    readonly
                                                    tabindex="-1"
                                                    title="Đơn vị tính thành phẩm cố định sau khi tạo"
                                                />
                                            `}
                                        </div>
                                    </div>
                                </div>

                                <div class="wh-vthh-grid-fields">
                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Số lượng tồn kho <span class="wh-lock-hint" title="${isCreate ? 'Mặc định 0 cho sản phẩm mới' : 'Cập nhật qua phiếu xuất/nhập'}">${Icons.lock} Khóa</span>
                                        </label>
                                        <input
                                            type="number"
                                            name="quantity"
                                            class="wh-input-control is-readonly"
                                            value="${isCreate ? 0 : (item.quantity !== undefined ? item.quantity : 0)}"
                                            readonly
                                            tabindex="-1"
                                            style="font-weight: 600;"
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Giá bán (VNĐ)
                                        </label>
                                        <input
                                            type="number"
                                            name="cost"
                                            class="wh-input-control"
                                            min="0"
                                            step="1000"
                                            placeholder="VD: 50000"
                                            value="${item.cost !== undefined && item.cost !== null ? item.cost : ''}"
                                            style="font-weight: 600; color: #1e293b;"
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Giá vốn (VNĐ) <span class="wh-lock-hint" title="Giá vốn tự động lấy giá cao nhất từ các phiếu nhập kho thành phẩm">${Icons.lock} Khóa (Lấy từ phiếu nhập)</span>
                                        </label>
                                        ${isCreate ? `
                                            <input
                                                type="text"
                                                name="materialCost"
                                                class="wh-input-control is-readonly"
                                                value="0 đ"
                                                readonly
                                                tabindex="-1"
                                                title="Giá vốn sản phẩm được khóa và sẽ tự động lấy từ các phiếu nhập kho thành phẩm"
                                                style="font-weight: 600; color: #64748b;"
                                            />
                                        ` : `
                                            <input
                                                type="text"
                                                name="materialCost"
                                                class="wh-input-control is-readonly"
                                                value="${item.materialCostFormatted ? `${item.materialCostFormatted} đ` : (item.materialCost ? `${Number(item.materialCost).toLocaleString('vi-VN')} đ` : '0 đ')}"
                                                readonly
                                                tabindex="-1"
                                                style="font-weight: 600; color: #1e293b;"
                                            />
                                        `}
                                    </div>

                                    <div class="wh-form-group wh-col-span-full">
                                        <label class="wh-form-label">
                                            Mô tả / Tiêu chuẩn chất lượng ${isCreate ? `<span class="wh-lock-hint" title="Cập nhật từ phiếu nhập">${Icons.lock} Khóa</span>` : ''}
                                        </label>
                                        ${isCreate ? `
                                            <textarea
                                                name="description"
                                                rows="3"
                                                class="wh-input-control is-readonly"
                                                readonly
                                                tabindex="-1"
                                                placeholder="(Bỏ trống - Cập nhật từ phiếu nhập thành phẩm)"
                                                style="resize: none;"
                                            ></textarea>
                                        ` : `
                                            <textarea
                                                name="description"
                                                rows="3"
                                                class="wh-input-control"
                                                placeholder="Nhập ghi chú quy chuẩn, GMP, hoạt chất..."
                                            >${escapeHtml(item.description || '')}</textarea>
                                        `}
                                    </div>
                                </div>
                            ` : `
                                <!-- Form Thêm mới hoặc Sửa thông tin Nguyên vật liệu -->
                                <div class="wh-vthh-grid-fields">
                                    ${isCreate ? `
                                        <div style="grid-column: 1 / -1; background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 6px; padding: 10px 14px; font-size: 13px; color: #166534; display: flex; align-items: flex-start; gap: 8px;">
                                            <span style="font-size: 16px; line-height: 1;">💡</span>
                                            <div>
                                                <strong>Quy tắc thêm mới nguyên vật liệu:</strong> Nhập <strong>Tên nguyên vật liệu</strong> và <strong>Đơn giá</strong> (VNĐ) gốc lớn hơn 0 để khởi tạo mã mới trong hệ thống. Các thông tin còn lại (ĐVT, số lượng tồn kho = 0, giá nhập = 0 đ, mô tả) sẽ được khóa cố định và tự động cập nhật khi lập và duyệt <strong>Phiếu nhập kho</strong>.
                                            </div>
                                        </div>
                                    ` : ''}

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Mã nguyên vật liệu <span class="wh-lock-hint" title="${isCreate ? 'Mã được hệ thống tự động sinh' : 'Mã không được phép sửa'}">${Icons.lock} ${isCreate ? 'Tự động' : 'Khóa'}</span>
                                        </label>
                                        <input
                                            type="text"
                                            name="ingrCode"
                                            class="wh-input-control is-readonly"
                                            value="${escapeHtml(item.ingrCode || '')}"
                                            readonly
                                            tabindex="-1"
                                            style="font-weight: 600;"
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">Tên nguyên vật liệu <span class="wh-req">*</span></label>
                                        <input
                                            type="text"
                                            name="ingrName"
                                            class="wh-input-control"
                                            value="${escapeHtml(item.ingrName || '')}"
                                            placeholder="VD: Paracetamol BP tinh khiết..."
                                            required
                                            ${isCreate ? 'autofocus' : ''}
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Đơn vị tính (ĐVT) <span class="wh-lock-hint" title="${isCreate ? 'Cập nhật từ phiếu nhập' : 'Đơn vị tính cố định'}">${Icons.lock} Khóa</span>
                                        </label>
                                        <input
                                            type="text"
                                            name="unit"
                                            class="wh-input-control is-readonly"
                                            value="${escapeHtml(item.unit || '')}"
                                            readonly
                                            tabindex="-1"
                                            placeholder="(Bỏ trống - Cập nhật từ phiếu nhập)"
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Số lượng tồn kho <span class="wh-lock-hint" title="${isCreate ? 'Mặc định 0 cho NVL mới' : 'Cập nhật qua phiếu xuất/nhập'}">${Icons.lock} Khóa</span>
                                        </label>
                                        <input
                                            type="number"
                                            name="quantity"
                                            class="wh-input-control is-readonly"
                                            value="${isCreate ? 0 : (item.quantity !== undefined ? item.quantity : 0)}"
                                            readonly
                                            tabindex="-1"
                                            style="font-weight: 600;"
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Đơn giá (VNĐ) <span class="wh-req">*</span>
                                        </label>
                                        <input
                                            type="number"
                                            name="price"
                                            min="0"
                                            step="100"
                                            class="wh-input-control"
                                            value="${item.price !== undefined ? item.price : 0}"
                                            placeholder="VD: 50000"
                                            required
                                        />
                                    </div>

                                    <div class="wh-form-group">
                                        <label class="wh-form-label">
                                            Giá nhập (VNĐ) ${isCreate ? `<span class="wh-lock-hint" title="Cập nhật từ phiếu nhập kho">${Icons.lock} Khóa (Cập nhật từ phiếu nhập)</span>` : (isEdit && distinctReceiptCosts.length > 0 ? `<span class="wh-hint" style="font-size:11px;color:#0284c7;font-weight:normal;">(Chọn từ phiếu nhập)</span>` : '')}
                                        </label>
                                        ${isCreate ? `
                                            <input
                                                type="text"
                                                name="cost"
                                                class="wh-input-control is-readonly"
                                                value="0 đ"
                                                readonly
                                                tabindex="-1"
                                                title="Giá nhập nguyên vật liệu được khóa và sẽ tự động cập nhật từ các phiếu nhập kho"
                                                style="font-weight: 600; color: #64748b;"
                                            />
                                        ` : (distinctReceiptCosts.length > 0 ? `
                                            <select name="cost" class="wh-select-control" style="font-weight: 600; color: #1e293b;">
                                                ${!distinctReceiptCosts.some(rc => rc.cost === currentIngrCost) && currentIngrCost > 0 ? `
                                                    <option value="${currentIngrCost}" selected>${currentIngrCost.toLocaleString('vi-VN')} đ (Giá hiện tại)</option>
                                                ` : ''}
                                                ${distinctReceiptCosts.map(rc => `
                                                    <option value="${rc.cost}" ${rc.cost === currentIngrCost ? 'selected' : ''}>
                                                        ${rc.cost.toLocaleString('vi-VN')} đ (${rc.voucherNo ? `từ ${rc.voucherNo}` : 'Phiếu nhập'}${rc.importDate ? ` - ${rc.importDate.split(' ')[0]}` : ''})
                                                    </option>
                                                `).join('')}
                                            </select>
                                        ` : `
                                            <input
                                                type="number"
                                                name="cost"
                                                min="0"
                                                step="100"
                                                class="wh-input-control"
                                                value="${item.cost !== undefined && item.cost !== null ? item.cost : 0}"
                                                placeholder="Chưa có phiếu nhập - nhập giá (VD: 50000)"
                                                style="font-weight: 600; color: #1e293b;"
                                            />
                                        `)}
                                    </div>

                                    <div class="wh-form-group wh-col-span-full">
                                        <label class="wh-form-label">
                                            Mô tả / Tiêu chuẩn kỹ thuật ${isCreate ? `<span class="wh-lock-hint" title="Cập nhật từ phiếu nhập">${Icons.lock} Khóa</span>` : ''}
                                        </label>
                                        ${isCreate ? `
                                            <textarea
                                                name="description"
                                                rows="3"
                                                class="wh-input-control is-readonly"
                                                readonly
                                                tabindex="-1"
                                                placeholder="(Bỏ trống - Cập nhật từ phiếu nhập của nguyên vật liệu)"
                                                style="resize: none;"
                                            ></textarea>
                                        ` : `
                                            <textarea
                                                name="description"
                                                rows="3"
                                                class="wh-input-control"
                                                placeholder="Độ tinh khiết, tiêu chuẩn Dược điển BP/USP..."
                                            >${escapeHtml(item.description || '')}</textarea>
                                        `}
                                    </div>
                                </div>
                            `)}
                        </form>
                    </div>

                    <!-- Chân Modal (Thao tác) -->
                    <div class="wh-voucher-bottombar wh-vthh-bottombar">
                        <div class="wh-vthh-bottombar-left">
                            ${isCreate ? `
                                <span class="wh-vthh-lock-notice">
                                    ${itemType === 'ingredient'
                                        ? '* Điền Tên nguyên vật liệu và Đơn giá (> 0) để tạo mới. Giá nhập và các thông tin còn lại sẽ cập nhật từ phiếu nhập kho.'
                                        : '* Điền Tên sản phẩm và Đơn vị tính (ĐVT) để tạo mới. Trường Giá vốn được khóa và sẽ cập nhật từ phiếu nhập kho thành phẩm.'}
                                </span>
                            ` : (isEdit ? `
                                <span class="wh-vthh-lock-notice">
                                    ${Icons.lock} Mã ${typeLabel} là định danh cố định không thể chỉnh sửa.
                                </span>
                            ` : '')}
                        </div>

                        <div class="wh-vthh-bottombar-right">
                            ${isCreate ? `
                                <button
                                    type="button"
                                    class="wh-btn wh-btn-cancel"
                                    data-action="close-vthh-modal"
                                >
                                    Hủy
                                </button>
                                <button
                                    type="button"
                                    class="wh-btn wh-btn-apply"
                                    data-action="save-new-vthh-item"
                                    data-item-type="${itemType}"
                                    style="display:inline-flex;align-items:center;gap:6px;"
                                >
                                    ${Icons.plus}
                                    <span>Thêm mới</span>
                                </button>
                            ` : (!isEdit ? `
                                <button
                                    type="button"
                                    class="wh-btn wh-btn-cancel"
                                    data-action="close-vthh-modal"
                                >
                                    Đóng
                                </button>
                                <button
                                    type="button"
                                    class="wh-btn wh-btn-apply"
                                    data-action="edit-vthh-item"
                                    data-item-type="${itemType}"
                                    data-item-id="${escapeHtml(String(actualItemId))}"
                                    style="display:inline-flex;align-items:center;gap:6px;"
                                >
                                    ${Icons.penToSquare || Icons.pencil}
                                    <span>Chỉnh sửa</span>
                                </button>
                            ` : `
                                <button
                                    type="button"
                                    class="wh-btn wh-btn-cancel"
                                    data-action="close-vthh-modal"
                                >
                                    Hủy
                                </button>
                                <button
                                    type="button"
                                    class="wh-btn wh-btn-apply"
                                    data-action="save-vthh-item"
                                    data-item-type="${itemType}"
                                    data-item-id="${escapeHtml(String(actualItemId))}"
                                    style="display:inline-flex;align-items:center;gap:6px;"
                                >
                                    ${Icons.checkBlue}
                                    <span>Lưu thay đổi</span>
                                </button>
                            `)}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Sub-component: Modal Lập, Chỉnh sửa, Phân công và Xem chi tiết Yêu cầu sản xuất
     */
    /**
     * Component Modal Yêu cầu sản xuất (Lập mới nhiều sản phẩm, Chỉnh sửa, Xem chi tiết)
     */
    function renderProductionRequestModal(modalData) {
        if (!modalData || !modalData.item) return '';

        const { mode, item, products = [] } = modalData;
        const isCreate = mode === 'create';
        const isEdit = mode === 'edit';
        const isDetail = mode === 'detail';

        const prId = item.requestId || 0;
        const prCode = item.requestCode || `LSX${String(prId).padStart(5, '0')}`;
        const prodName = item.productName || '';
        const qty = item.quantityRequired || 0;
        const unit = item.unit || 'Hộp';
        const status = item.status || 'Pending';

        if (isCreate) {
            const items = item.items || [];
            const totalQty = items.reduce((s, it) => s + (parseInt(it.quantityRequired, 10) || 0), 0);

            return `
                <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                    <div class="wh-voucher-window" style="max-width:760px;width:95%;">
                        <div class="wh-voucher-topbar">
                            <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                                <h2 class="wh-voucher-title">Lập yêu cầu sản xuất mới</h2>
                                <span class="wh-vthh-mode-badge is-create">Tạo mới</span>
                            </div>
                            <div class="wh-voucher-topbar-right">
                                <button type="button" class="wh-voucher-win-btn" data-action="close-pr-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                            </div>
                        </div>
                        <div class="wh-voucher-body" style="padding:20px;">
                            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px;">
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Mã lệnh sản xuất <span class="wh-required">*</span></label>
                                    <input type="text" id="pr-form-code" class="wh-input-control is-readonly" value="${escapeHtml(prCode)}" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#f8fafc;color:#64748b;border-radius:4px;padding:0 10px;cursor:not-allowed;" />
                                    <div style="font-size:11px;color:#94a3b8;margin-top:4px;">Mã lệnh được hệ thống tự động sinh và khóa cố định.</div>
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Trạng thái</label>
                                    <input type="text" id="pr-form-status" class="wh-input-control is-readonly" value="Pending" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#fef3c7;color:#b45309;font-weight:600;border-radius:4px;padding:0 10px;cursor:not-allowed;" />
                                    <div style="font-size:11px;color:#94a3b8;margin-top:4px;">Trạng thái luôn mặc định là Pending (khóa, chỉ thay đổi khi nhân viên tiếp nhận).</div>
                                </div>
                            </div>

                            <!-- Danh sách thành phẩm cần sản xuất (cho phép nhiều sản phẩm cùng lúc) -->
                            <div style="margin-top:16px;">
                                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;">
                                    <label class="wh-form-label" style="font-weight:700;font-size:14px;color:#0f172a;margin:0;">
                                        Danh sách thành phẩm yêu cầu sản xuất <span class="wh-required">*</span>
                                    </label>
                                    <button type="button" class="wh-btn-secondary" data-action="add-pr-item-row" style="font-size:12px;padding:5px 12px;height:32px;display:flex;align-items:center;gap:6px;color:#2563eb;border-color:#bfdbfe;font-weight:600;">
                                        <span>+ Thêm thành phẩm</span>
                                    </button>
                                </div>
                                <div style="border:1px solid #e2e8f0;border-radius:6px;background:#fff;position:relative;min-height:160px;">
                                    <table class="wh-vtable" style="width:100%;border-collapse:collapse;">
                                        <thead>
                                            <tr style="background:#f8fafc;border-bottom:1px solid #e2e8f0;font-size:12px;color:#475569;">
                                                <th style="width:35px;text-align:center;padding:8px 6px;">#</th>
                                                <th style="width:120px;text-align:center;padding:8px 6px;">Mã sản phẩm</th>
                                                <th style="min-width:270px;text-align:left;padding:8px 10px;">Tên sản phẩm <span class="wh-required">*</span></th>
                                                <th style="width:75px;text-align:center;padding:8px 6px;">ĐVT</th>
                                                <th style="width:120px;text-align:right;padding:8px 10px;">Số lượng SX <span class="wh-required">*</span></th>
                                                <th style="width:45px;text-align:center;padding:8px 6px;">Xóa</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${items.length === 0 ? `
                                                <tr>
                                                    <td colspan="6" style="text-align:center;padding:32px 16px;color:#94a3b8;font-size:13px;background:#fafbfc;">
                                                        <div style="font-size:24px;margin-bottom:6px;">📦</div>
                                                        Danh sách thành phẩm yêu cầu hiện đang trống.<br/>
                                                        Nhấn nút <strong style="color:#2563eb;">"+ Thêm thành phẩm"</strong> ở trên để thêm dòng yêu cầu mới.
                                                    </td>
                                                </tr>
                                            ` : items.map((row, idx) => `
                                                <tr style="border-bottom:1px solid #f1f5f9;">
                                                    <td style="text-align:center;color:#64748b;font-size:12px;padding:8px 6px;">${idx + 1}</td>
                                                    <td style="text-align:center;padding:8px 6px;">
                                                        ${row.productCode ? `
                                                            <span style="display:inline-block;padding:3px 8px;border-radius:4px;background:#eff6ff;color:#1d4ed8;font-weight:700;font-size:12px;border:1px solid #bfdbfe;">
                                                                ${escapeHtml(row.productCode)}
                                                            </span>
                                                        ` : `
                                                            <span style="color:#cbd5e1;font-size:12px;font-style:italic;">--</span>
                                                        `}
                                                    </td>
                                                    <td style="padding:8px 10px;position:relative;">
                                                        <div class="wh-pr-searchable-select" data-row-id="${row.id}">
                                                            <div class="wh-pr-select-trigger" data-action="toggle-pr-product-dropdown" data-row-id="${row.id}">
                                                                <span class="wh-pr-selected-text ${row.productId ? 'has-val' : 'is-placeholder'}">
                                                                    ${row.productName ? escapeHtml(row.productName) : '-- Chọn tên sản phẩm --'}
                                                                </span>
                                                                <span class="wh-pr-dropdown-arrow">▾</span>
                                                            </div>
                                                            <div class="wh-pr-dropdown-menu" id="pr-prod-dropdown-${row.id}" style="display:none;">
                                                                <div class="wh-pr-dropdown-search-wrap">
                                                                    <input
                                                                        type="text"
                                                                        class="wh-pr-product-search-input"
                                                                        data-action="search-pr-product-list"
                                                                        data-row-id="${row.id}"
                                                                        placeholder="🔍 Tìm tên hoặc mã sản phẩm..."
                                                                        autocomplete="off"
                                                                    />
                                                                </div>
                                                                <div class="wh-pr-dropdown-list" id="pr-prod-list-${row.id}">
                                                                    ${products.map(p => {
                                                                        const pid = p.id || p.productId;
                                                                        const pcode = p.productCode || '';
                                                                        const pname = p.productName || '';
                                                                        const punit = p.unit || 'Hộp';
                                                                        const isSelected = String(pid) === String(row.productId);
                                                                        return `
                                                                            <div
                                                                                class="wh-pr-dropdown-item ${isSelected ? 'is-selected' : ''}"
                                                                                data-action="select-pr-product-item"
                                                                                data-row-id="${row.id}"
                                                                                data-pid="${pid}"
                                                                                data-pcode="${escapeHtml(pcode)}"
                                                                                data-pname="${escapeHtml(pname)}"
                                                                                data-punit="${escapeHtml(punit)}"
                                                                            >
                                                                                <span class="wh-pr-item-name">${escapeHtml(pname)}</span>
                                                                                <span class="wh-pr-item-badge">${escapeHtml(pcode)}</span>
                                                                            </div>
                                                                        `;
                                                                    }).join('')}
                                                                    <div class="wh-pr-dropdown-no-results" style="display:none;padding:12px;text-align:center;color:#94a3b8;font-size:12px;">
                                                                        Không tìm thấy sản phẩm phù hợp
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </td>
                                                    <td style="text-align:center;color:#475569;font-size:12px;padding:8px 6px;">
                                                        ${escapeHtml(row.unit || '--')}
                                                    </td>
                                                    <td style="text-align:right;padding:8px 10px;">
                                                        <input type="number" min="1" step="1" class="wh-input-control wh-pr-qty-input" data-action="change-pr-item-qty" data-row-id="${row.id}" value="${row.quantityRequired || 1}" style="width:105px;height:34px;text-align:right;font-weight:600;padding:0 8px;" />
                                                    </td>
                                                    <td style="text-align:center;padding:8px 6px;">
                                                        <button type="button" class="wh-vtable-del-btn" data-action="remove-pr-item-row" data-row-id="${row.id}" title="Xóa dòng này" style="color:#ef4444;background:none;border:none;cursor:pointer;font-size:14px;">
                                                            ${Icons.trash || '🗑'}
                                                        </button>
                                                    </td>
                                                </tr>
                                            `).join('')}
                                        </tbody>
                                        <tfoot>
                                            <tr style="background:#f8fafc;border-top:1px solid #e2e8f0;font-weight:600;font-size:12px;color:#334155;">
                                                <td colspan="4" style="padding:10px 12px;text-align:right;">Tổng số lượng yêu cầu (${items.length} loại sản phẩm):</td>
                                                <td style="padding:10px 12px;text-align:right;color:#0284c7;font-size:14px;font-weight:700;">
                                                    ${totalQty.toLocaleString('vi-VN')}
                                                </td>
                                                <td></td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                </div>
                                <div style="font-size:11px;color:#64748b;margin-top:6px;">
                                    💡 Bạn có thể thêm nhiều thành phẩm cần sản xuất cùng lúc. Khi tạo yêu cầu, nhân viên sẽ trực tiếp tiếp nhận để tiến hành sản xuất.
                                </div>
                            </div>
                        </div>
                        <div class="wh-voucher-footer" style="display:flex;justify-content:flex-end;gap:8px;padding:12px 20px;border-top:1px solid #e5e7eb;background:#f9fafb;">
                            <button type="button" class="wh-btn-secondary" data-action="close-pr-modal">Hủy</button>
                            <button type="button" class="wh-btn-primary" data-action="save-create-pr">
                                Tạo yêu cầu
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        if (isEdit) {
            const title = `Chỉnh sửa yêu cầu sản xuất: ${prCode}`;
            let selectedProd = products.find(p => (p.id || p.productId) === item.productId) || products[0];
            const currentProdId = selectedProd ? (selectedProd.id || selectedProd.productId) : (item.productId || '');
            const currentProdCode = selectedProd ? (selectedProd.productCode || item.productCode || '') : (item.productCode || '');
            const currentUnit = selectedProd ? (selectedProd.unit || item.unit || 'Hộp') : (item.unit || 'Hộp');

            return `
                <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                    <div class="wh-voucher-window" style="max-width:640px;">
                        <div class="wh-voucher-topbar">
                            <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                                <h2 class="wh-voucher-title">${escapeHtml(title)}</h2>
                                <span class="wh-vthh-mode-badge is-edit">Chỉnh sửa</span>
                            </div>
                            <div class="wh-voucher-topbar-right">
                                <button type="button" class="wh-voucher-win-btn" data-action="close-pr-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                            </div>
                        </div>
                        <div class="wh-voucher-body" style="padding:20px;">
                            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px;">
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Mã lệnh sản xuất</label>
                                    <input type="text" id="pr-form-code" class="wh-input-control is-readonly" value="${escapeHtml(prCode)}" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#f8fafc;color:#64748b;border-radius:4px;padding:0 10px;cursor:not-allowed;" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Trạng thái</label>
                                    <input type="text" id="pr-form-status" class="wh-input-control is-readonly" value="Pending" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#fef3c7;color:#b45309;font-weight:600;border-radius:4px;padding:0 10px;cursor:not-allowed;" />
                                </div>
                            </div>

                            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px;">
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Mã thành phẩm</label>
                                    <input type="text" id="pr-form-product-code" class="wh-input-control is-readonly" value="${escapeHtml(currentProdCode)}" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#f8fafc;color:#1e293b;font-weight:600;border-radius:4px;padding:0 10px;cursor:not-allowed;" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Tên thành phẩm <span class="wh-required">*</span></label>
                                    <select id="pr-form-product" class="wh-select-control" style="width:100%;height:36px;">
                                        ${products.map(p => {
                                            const pid = p.id || p.productId;
                                            return `<option value="${pid}" ${pid == currentProdId ? 'selected' : ''}>[${escapeHtml(p.productCode || 'SP')}] ${escapeHtml(p.productName)}</option>`;
                                        }).join('')}
                                    </select>
                                </div>
                            </div>

                            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px;">
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Số lượng cần sản xuất <span class="wh-required">*</span></label>
                                    <input type="number" id="pr-form-quantity" class="wh-input-control" min="1" step="1" value="${qty}" style="width:100%;height:36px;border:1px solid #cbd5e1;border-radius:4px;padding:0 10px;" />
                                </div>
                                <div class="wh-form-group">
                                    <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Đơn vị tính</label>
                                    <input type="text" id="pr-form-unit" class="wh-input-control is-readonly" value="${escapeHtml(currentUnit)}" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#f8fafc;border-radius:4px;padding:0 10px;" />
                                </div>
                            </div>
                        </div>
                        <div class="wh-voucher-footer" style="display:flex;justify-content:flex-end;gap:8px;padding:12px 20px;border-top:1px solid #e5e7eb;background:#f9fafb;">
                            <button type="button" class="wh-btn-secondary" data-action="close-pr-modal">Hủy</button>
                            <button type="button" class="wh-btn-primary" data-action="save-edit-pr" data-pr-id="${prId}">
                                Lưu thay đổi
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        // isDetail mode
        const isPending = status === 'Pending';
        const isInProduction = (status === 'In Production' || status === 'In Progress');
        const isCompleted = (status === 'Completed');

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window" style="max-width:800px;">
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                            <h2 class="wh-voucher-title">Chi tiết yêu cầu sản xuất: ${escapeHtml(prCode)}</h2>
                            <span class="wh-vthh-mode-badge is-view">Chỉ xem</span>
                        </div>
                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" data-action="close-pr-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <div class="wh-voucher-body" style="padding:20px;">
                        ${isInProduction ? `
                            <div style="background:#eff6ff;border:1px solid #bfdbfe;border-radius:6px;padding:12px 16px;margin-bottom:16px;display:flex;align-items:center;gap:10px;color:#1e40af;font-size:13px;">
                                <span style="font-size:18px;">⚙️</span>
                                <div><strong>Lệnh đang sản xuất (In Production):</strong> Yêu cầu đã được nhân viên tiếp nhận và tiến hành quy trình sản xuất. Các thông tin được khóa cố định.</div>
                            </div>
                        ` : isCompleted ? `
                            <div style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:6px;padding:12px 16px;margin-bottom:16px;display:flex;align-items:center;gap:10px;color:#166534;font-size:13px;">
                                <span style="font-size:18px;">✅</span>
                                <div><strong>Lệnh sản xuất đã hoàn thành (Completed):</strong> Các quy trình sản xuất và nhập kho thành phẩm đã hoàn tất.</div>
                            </div>
                        ` : ''}

                        <!-- Master Card -->
                        <div style="display:grid;grid-template-columns:1fr 1fr;gap:20px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;padding:16px;">
                            <div>
                                <div style="font-size:12px;color:#64748b;margin-bottom:2px;">Mã lệnh & Trạng thái</div>
                                <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px;">
                                    <span style="font-size:16px;font-weight:700;color:#0f172a;">${escapeHtml(prCode)}</span>
                                    <span class="wh-pr-status-badge ${status === 'In Production' || status === 'In Progress' ? 'wh-pr-badge-progress' : status === 'Completed' ? 'wh-pr-badge-completed' : 'wh-pr-badge-pending'}">${escapeHtml(status)}</span>
                                </div>
                                <div style="font-size:13px;color:#334155;line-height:1.7;">
                                    <div>Ngày lập: <strong>${escapeHtml(item.createdDate || '-')}</strong></div>
                                    <div>Người lập lệnh: <strong>${escapeHtml(item.managerName || 'Quản lý sản xuất')}</strong> ${item.managerEmployeeCode ? `(${escapeHtml(item.managerEmployeeCode)})` : ''}</div>
                                </div>
                            </div>
                            <div>
                                <div style="font-size:12px;color:#64748b;margin-bottom:2px;">Thành phẩm sản xuất</div>
                                <div style="font-size:15px;font-weight:600;color:#1e293b;margin-bottom:4px;">
                                    ${escapeHtml(item.productCode ? '[' + item.productCode + '] ' : '')}${escapeHtml(prodName)}
                                </div>
                                <div style="font-size:14px;color:#0284c7;font-weight:600;margin-bottom:8px;">
                                    Số lượng yêu cầu: ${formatVnNumber(qty)} ${escapeHtml(unit)}
                                </div>
                                <div style="font-size:13px;color:#334155;">
                                    Nhân viên tiếp nhận:
                                    ${item.assignedToName ? `
                                        <span class="wh-pr-assigned-badge" style="display:inline-flex;margin-left:6px;background:#f0fdf4;color:#166534;border:1px solid #bbf7d0;">
                                            <span>👤</span> <strong>${escapeHtml(item.assignedToName)}</strong> ${item.assignedToEmployeeCode ? `(${escapeHtml(item.assignedToEmployeeCode)})` : ''}
                                        </span>
                                    ` : `
                                        <span style="color:#d97706;font-style:italic;margin-left:6px;">Chưa có nhân viên tiếp nhận</span>
                                    `}
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="wh-voucher-footer" style="display:flex;justify-content:flex-end;gap:8px;padding:12px 20px;border-top:1px solid #e5e7eb;background:#f9fafb;">
                        ${isPending ? `
                            <button type="button" class="wh-btn-secondary" data-action="edit-pr-item" data-pr-id="${prId}">
                                ${Icons.penToSquare || ''} Chỉnh sửa
                            </button>
                        ` : ''}
                        <button type="button" class="wh-btn-secondary" data-action="close-pr-modal">Đóng</button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Sub-component: Modal Thêm mới, Chỉnh sửa và Xem chi tiết Nhà cung cấp
     */
    function renderSupplierModal(modalData) {
        if (!modalData || !modalData.item) return '';

        const { mode, item } = modalData;
        const isCreate = mode === 'create';
        const isEdit = mode === 'edit';
        const isDetail = mode === 'detail';

        const supId = item.supplierId || item.id || 0;
        const supCode = item.code || (isCreate ? 'Tự động sinh (NCC...)' : `NCC${String(supId).padStart(4, '0')}`);
        const supName = item.supplierName || item.name || '';
        const phone = item.phone || '';
        const mail = item.mail || '';
        const address = item.address || '';
        const importCost = (item.importCost !== undefined && item.importCost !== null) ? item.importCost : 0;

        const title = isCreate
            ? 'Thêm mới nhà cung cấp'
            : isEdit
                ? `Chỉnh sửa nhà cung cấp: ${supCode}`
                : `Chi tiết nhà cung cấp: ${supCode}`;

        const badgeHtml = isCreate
            ? `<span class="wh-vthh-mode-badge is-create">Tạo mới</span>`
            : isEdit
                ? `<span class="wh-vthh-mode-badge is-edit">Chỉnh sửa</span>`
                : `<span class="wh-vthh-mode-badge is-view">Chỉ xem</span>`;

        return `
            <div class="wh-voucher-overlay" id="wh-voucher-overlay">
                <div class="wh-voucher-window" style="max-width:640px;">
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left" style="display:flex;align-items:center;gap:10px;">
                            <h2 class="wh-voucher-title">${escapeHtml(title)}</h2>
                            ${badgeHtml}
                        </div>
                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-win-btn" data-action="close-supplier-modal" title="Đóng (Esc)">${Icons.closeX}</button>
                        </div>
                    </div>

                    <div class="wh-voucher-body" style="padding:20px;">
                        <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px;">
                            <div class="wh-form-group">
                                <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Mã nhà cung cấp</label>
                                <input type="text" id="supplier-form-code" class="wh-input-control is-readonly" value="${escapeHtml(supCode)}" readonly disabled style="width:100%;height:36px;border:1px solid #e2e8f0;background:#f8fafc;color:#64748b;font-weight:600;border-radius:4px;padding:0 10px;cursor:not-allowed;" />
                                <div style="font-size:11px;color:#94a3b8;margin-top:4px;">${isCreate ? 'Hệ thống tự động sinh mã sau khi lưu.' : 'Mã định danh duy nhất trong hệ thống.'}</div>
                            </div>
                            <div class="wh-form-group">
                                <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Số điện thoại</label>
                                <input type="text" id="supplier-form-phone" class="wh-input-control" value="${escapeHtml(phone)}" ${isDetail ? 'readonly disabled style="background:#f8fafc;cursor:not-allowed;"' : ''} placeholder="VD: 024 3829 0486..." style="width:100%;height:36px;border:1px solid #cbd5e1;border-radius:4px;padding:0 10px;" />
                            </div>
                        </div>

                        <div class="wh-form-group" style="margin-bottom:16px;">
                            <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Tên nhà cung cấp <span class="wh-required">*</span></label>
                            <input type="text" id="supplier-form-name" class="wh-input-control" value="${escapeHtml(supName)}" ${isDetail ? 'readonly disabled style="background:#f8fafc;cursor:not-allowed;"' : ''} placeholder="Nhập tên đầy đủ của đơn vị cung cấp..." style="width:100%;height:36px;border:1px solid #cbd5e1;border-radius:4px;padding:0 10px;" />
                            <div id="supplier-name-dup-hint" style="display:none;font-size:12px;color:#dc2626;margin-top:4px;font-weight:500;"></div>
                        </div>

                        <div class="wh-form-group" style="margin-bottom:16px;">
                            <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Email liên hệ</label>
                            <input type="email" id="supplier-form-mail" class="wh-input-control" value="${escapeHtml(mail)}" ${isDetail ? 'readonly disabled style="background:#f8fafc;cursor:not-allowed;"' : ''} placeholder="VD: supply@cpc1.com.vn..." style="width:100%;height:36px;border:1px solid #cbd5e1;border-radius:4px;padding:0 10px;" />
                        </div>

                        <div class="wh-form-group" style="margin-bottom:16px;">
                            <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">
                                Tỷ lệ chi phí nhập (%) <span style="font-size:12px;color:#64748b;font-weight:normal;">(import_cost %)</span>
                            </label>
                            <div style="display:flex;align-items:center;gap:8px;">
                                <input type="number" id="supplier-form-import-cost" class="wh-input-control" value="${importCost}" min="0" max="100" step="0.1" ${isDetail ? 'readonly disabled style="background:#f8fafc;cursor:not-allowed;"' : ''} placeholder="VD: 10" style="width:140px;height:36px;border:1px solid #cbd5e1;border-radius:4px;padding:0 10px;" />
                                <span style="font-weight:600;color:#64748b;">%</span>
                            </div>
                            <div style="font-size:11px;color:#64748b;margin-top:4px;">Chỉ được nhập từ 0% đến 100% và tối đa 1 chữ số thập phân (Ví dụ: 8.5%).</div>
                            <div id="supplier-cost-dup-hint" style="display:none;font-size:12px;color:#dc2626;margin-top:4px;font-weight:500;"></div>
                        </div>

                        <div class="wh-form-group" style="margin-bottom:16px;">
                            <label class="wh-form-label" style="font-weight:600;margin-bottom:6px;display:block;">Địa chỉ</label>
                            <textarea id="supplier-form-address" class="wh-input-control" rows="3" ${isDetail ? 'readonly disabled style="background:#f8fafc;cursor:not-allowed;"' : ''} placeholder="VD: 356A Giải Phóng, Thanh Xuân, Hà Nội..." style="width:100%;border:1px solid #cbd5e1;border-radius:4px;padding:8px 10px;resize:vertical;font-family:inherit;">${escapeHtml(address)}</textarea>
                        </div>
                    </div>

                    <div class="wh-voucher-footer" style="display:flex;justify-content:${isDetail ? 'space-between' : 'flex-end'};gap:8px;padding:12px 20px;border-top:1px solid #e5e7eb;background:#f9fafb;">
                        ${isDetail ? `
                            <button type="button" class="wh-btn-secondary" style="color:#2563eb;border-color:#bfdbfe;" data-action="edit-supplier-item" data-supplier-id="${supId}">
                                Chuyển sang chỉnh sửa
                            </button>
                            <button type="button" class="wh-btn-primary" data-action="close-supplier-modal">Đóng</button>
                        ` : `
                            <button type="button" class="wh-btn-secondary" data-action="close-supplier-modal">Hủy</button>
                            <button type="button" class="wh-btn-primary" data-action="${isCreate ? 'save-create-supplier' : 'save-edit-supplier'}" data-supplier-id="${supId}">
                                ${isCreate ? 'Thêm nhà cung cấp' : 'Lưu thay đổi'}
                            </button>
                        `}
                    </div>
                </div>
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
     * Component hiển thị Thẻ kho chi tiết (Stock Card Modal)
     */
    function renderStockCardModal(data) {
        if (!data) return '';
        const transactions = data.transactions || [];
        const isIngr = (data.itemCode || '').startsWith('NVL');

        return `
            <div class="wh-voucher-overlay" id="wh-stock-card-overlay" style="z-index: 9999;">
                <div class="wh-voucher-window" style="max-width: 960px; width: 95%;">
                    <div class="wh-voucher-topbar">
                        <div class="wh-voucher-topbar-left" style="display:flex; flex-direction:column; gap:4px;">
                            <div style="display:flex; align-items:center; gap:8px;">
                                <h2 class="wh-voucher-title" style="margin:0;">Thẻ kho: [${escapeHtml(data.itemCode)}] ${escapeHtml(data.itemName)}</h2>
                                <span class="wh-report-badge is-${isIngr ? 'ingredient' : 'product'}">
                                    ${isIngr ? 'Nguyên vật liệu' : 'Thành phẩm'}
                                </span>
                            </div>
                            <div style="font-size:12px; color:#64748b;">
                                Đơn vị tính: <strong>${escapeHtml(data.unit || '')}</strong> &bull; 
                                Tồn kho hiện tại: <strong style="color:#0284c7;">${(Number(data.currentStock) || 0).toLocaleString('vi-VN')} ${escapeHtml(data.unit || '')}</strong> &bull; 
                                Tổng số phát sinh: <strong>${transactions.length} giao dịch</strong>
                            </div>
                        </div>
                        <div class="wh-voucher-topbar-right">
                            <button type="button" class="wh-voucher-icon-btn" data-action="close-stock-card-modal" title="Đóng">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                            </button>
                        </div>
                    </div>

                    <div style="max-height: 60vh; overflow-y: auto; padding: 16px;">
                        <table class="wh-report-table" style="width: 100%; border-collapse: separate; border-spacing: 0;">
                            <thead>
                                <tr>
                                    <th style="width: 40px; text-align: center;">STT</th>
                                    <th style="width: 130px;">Ngày chứng từ</th>
                                    <th style="width: 110px;">Số chứng từ</th>
                                    <th>Loại giao dịch</th>
                                    <th style="width: 100px; text-align: right;">SL Nhập</th>
                                    <th style="width: 100px; text-align: right;">SL Xuất</th>
                                    <th style="width: 110px; text-align: right;">Đơn giá</th>
                                    <th style="width: 110px; text-align: center;">Trạng thái</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${transactions.length === 0 ? `
                                    <tr>
                                        <td colspan="8" style="text-align: center; padding: 30px; color: #94a3b8;">
                                            Chưa ghi nhận giao dịch nhập/xuất kho nào cho mặt hàng này.
                                        </td>
                                    </tr>
                                ` : transactions.map((t, idx) => `
                                    <tr>
                                        <td style="text-align: center; color: #64748b;">${idx + 1}</td>
                                        <td>${t.transDate ? new Date(t.transDate).toLocaleString('vi-VN') : '-'}</td>
                                        <td><strong style="color: #0284c7;">${escapeHtml(t.voucherNo || '-')}</strong></td>
                                        <td>${escapeHtml(t.transType || '-')}</td>
                                        <td style="text-align: right; color: ${t.inQty > 0 ? '#0284c7' : '#94a3b8'}; font-weight: ${t.inQty > 0 ? '600' : 'normal'};">
                                            ${t.inQty > 0 ? `+${Number(t.inQty).toLocaleString('vi-VN')}` : '-'}
                                        </td>
                                        <td style="text-align: right; color: ${t.outQty > 0 ? '#d97706' : '#94a3b8'}; font-weight: ${t.outQty > 0 ? '600' : 'normal'};">
                                            ${t.outQty > 0 ? `-${Number(t.outQty).toLocaleString('vi-VN')}` : '-'}
                                        </td>
                                        <td style="text-align: right; color: #475569;">
                                            ${t.cost ? `${Number(t.cost).toLocaleString('vi-VN')} đ` : '-'}
                                        </td>
                                        <td style="text-align: center;">
                                            <span class="wh-action-status-badge ${t.status === 'Completed' || t.status === 'Approve' || t.status === 'Approved' || t.status === 'Đã xuất kho' || t.status === 'Đã nhập kho' ? 'is-approved' : (t.status === 'Rejected' || t.status === 'Reject' || t.status === 'Từ chối' || t.status === 'Đã hủy' ? 'is-rejected' : '')}" style="${t.status === 'Approved' || t.status === 'Approve' || t.status === 'Đã xuất kho' || t.status === 'Đã nhập kho' || t.status === 'Completed' ? 'background:#dcfce7; color:#15803d; border:1px solid #bbf7d0; font-weight:600; padding:3px 8px; border-radius:4px; display:inline-flex;' : (t.status === 'Đã hủy' || t.status === 'Rejected' || t.status === 'Reject' || t.status === 'Từ chối' ? 'background:#fee2e2; color:#b91c1c; border:1px solid #fecaca; font-weight:600; padding:3px 8px; border-radius:4px; display:inline-flex;' : '')}">
                                                ${escapeHtml(t.status || 'Hoàn thành')}
                                            </span>
                                        </td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>

                    <div class="wh-voucher-footer" style="padding: 12px 20px; display: flex; justify-content: flex-end; border-top: 1px solid #e2e8f0; background: #f8fafc;">
                        <button type="button" class="wh-btn-secondary" data-action="close-stock-card-modal">Đóng</button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Component hiển thị Trang Báo cáo thống kê Nhập - Xuất - Tồn & Lưu kho
     */
    function renderReportPage(config) {
        if (!config) {
            return `
                <div class="wh-report-container" style="padding: 24px; text-align: center;">
                    <div style="font-size: 16px; color: #64748b;">Đang tải dữ liệu báo cáo thống kê...</div>
                </div>
            `;
        }

        const period = config.period || {};
        const summary = config.summary || {};
        const chartData = config.chartData || [];
        const items = config.items || [];
        const filters = config.filters || {};
        const availableYears = config.availableYears || [2026, 2025, 2024];
        const stockCardModalHtml = config.stockCardModal ? renderStockCardModal(config.stockCardModal) : '';

        // Tính toán các tổng cột bảng
        const totalOpeningStock = items.reduce((s, it) => s + (Number(it.openingStock) || 0), 0);
        const totalInQty = items.reduce((s, it) => s + (Number(it.inQty) || 0), 0);
        const totalOutQty = items.reduce((s, it) => s + (Number(it.outQty) || 0), 0);
        const totalWipQty = items.reduce((s, it) => s + (Number(it.wipQty) || 0), 0);
        const totalClosingStock = items.reduce((s, it) => s + (Number(it.closingStock) || 0), 0);
        const grandTotalValue = items.reduce((s, it) => s + (Number(it.totalValue) || 0), 0);

        // Chuẩn bị biểu đồ SVG
        const maxBarVal = Math.max(...chartData.map(c => Math.max(c.inQty || 0, c.outQty || 0)), 100);
        const chartHeight = 150;
        const chartWidth = 700;
        const barGroupWidth = chartData.length > 0 ? (chartWidth - 60) / chartData.length : 50;

        return `
            <div class="wh-report-container">
                <!-- Tiêu đề trang & các nút thao tác nhanh -->
                <div class="wh-report-header">
                    <div>
                        <div class="wh-report-title-row">
                            <h1 class="wh-report-main-title">Báo cáo Thống kê Kho</h1>
                            <span class="wh-report-period-pill">${escapeHtml(period.title || 'Tháng 11/2026')}</span>
                        </div>
                        <p class="wh-report-subtitle">
                            Thống kê số lượng tồn kho, xuất kho, lưu kho theo tháng, quý, năm về vật liệu và sản phẩm
                            ${period.startDate && period.endDate ? ` (Từ ngày <strong>${period.startDate}</strong> đến <strong>${period.endDate}</strong>)` : ''}
                        </p>
                    </div>

                    <div class="wh-report-actions">
                        <button type="button" class="wh-btn-secondary" data-action="export-report-excel" title="Tải xuống tệp CSV (mở trực tiếp bằng Microsoft Excel)">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right:5px;"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                            Xuất Excel
                        </button>
                        <button type="button" class="wh-btn-secondary" data-action="print-report" title="In hoặc lưu file PDF">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right:5px;"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
                            In báo cáo
                        </button>
                        <button type="button" class="wh-btn-primary" data-action="refresh-report" title="Cập nhật số liệu mới nhất">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right:5px;"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
                            Làm mới
                        </button>
                    </div>
                </div>

                <!-- Thanh bộ lọc thống kê theo Thời gian & Đối tượng -->
                <div class="wh-report-filter-bar">
                    <!-- Phân loại thời gian: Tháng / Quý / Năm -->
                    <div class="wh-report-filter-item">
                        <label class="wh-report-filter-label">Kỳ báo cáo:</label>
                        <div class="wh-report-segmented-group">
                            <button type="button" class="wh-report-seg-btn ${filters.periodType === 'month' ? 'is-active' : ''}" data-action="change-report-period-type" data-value="month">
                                Theo Tháng
                            </button>
                            <button type="button" class="wh-report-seg-btn ${filters.periodType === 'quarter' ? 'is-active' : ''}" data-action="change-report-period-type" data-value="quarter">
                                Theo Quý
                            </button>
                            <button type="button" class="wh-report-seg-btn ${filters.periodType === 'year' ? 'is-active' : ''}" data-action="change-report-period-type" data-value="year">
                                Theo Năm
                            </button>
                        </div>
                    </div>

                    <!-- Chọn Tháng / Quý -->
                    ${filters.periodType === 'month' ? `
                        <div class="wh-report-filter-item">
                            <label class="wh-report-filter-label">Chọn Tháng:</label>
                            <select class="wh-report-select" data-action="change-report-period-val">
                                ${Array.from({ length: 12 }, (_, i) => i + 1).map(m => `
                                    <option value="${m}" ${Number(filters.periodValue) === m ? 'selected' : ''}>Tháng ${m}</option>
                                `).join('')}
                            </select>
                        </div>
                    ` : ''}

                    ${filters.periodType === 'quarter' ? `
                        <div class="wh-report-filter-item">
                            <label class="wh-report-filter-label">Chọn Quý:</label>
                            <select class="wh-report-select" data-action="change-report-period-val">
                                ${[1, 2, 3, 4].map(q => `
                                    <option value="${q}" ${Number(filters.periodValue) === q ? 'selected' : ''}>Quý ${q} (Q${q})</option>
                                `).join('')}
                            </select>
                        </div>
                    ` : ''}

                    <!-- Chọn Năm -->
                    <div class="wh-report-filter-item">
                        <label class="wh-report-filter-label">Năm:</label>
                        <select class="wh-report-select" data-action="change-report-year">
                            ${availableYears.map(y => `
                                <option value="${y}" ${Number(filters.year) === y ? 'selected' : ''}>${y}</option>
                            `).join('')}
                        </select>
                    </div>

                    <div style="width: 1px; height: 32px; background: #e2e8f0; margin: 0 4px;"></div>

                    <!-- Lọc phân loại: Tất cả / Vật liệu / Sản phẩm -->
                    <div class="wh-report-filter-item">
                        <label class="wh-report-filter-label">Phân loại:</label>
                        <div class="wh-report-segmented-group">
                            <button type="button" class="wh-report-seg-btn ${filters.itemType === 'all' ? 'is-active' : ''}" data-action="change-report-item-type" data-value="all">
                                Tất cả (${summary.totalItems || 0})
                            </button>
                            <button type="button" class="wh-report-seg-btn ${filters.itemType === 'ingredient' ? 'is-active' : ''}" data-action="change-report-item-type" data-value="ingredient">
                                Vật liệu (${summary.ingredientCount || 0})
                            </button>
                            <button type="button" class="wh-report-seg-btn ${filters.itemType === 'product' ? 'is-active' : ''}" data-action="change-report-item-type" data-value="product">
                                Sản phẩm (${summary.productCount || 0})
                            </button>
                        </div>
                    </div>

                    <!-- Tìm kiếm theo mã hoặc tên -->
                    <div class="wh-report-filter-item" style="margin-left: auto;">
                        <div class="wh-report-search-box">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                            <input type="text" class="wh-report-search-input" data-action="search-report" placeholder="Tìm theo mã hoặc tên..." value="${escapeHtml(filters.search || '')}">
                        </div>
                    </div>
                </div>

                <!-- 3 Thẻ KPI Tóm tắt Chỉ số Kho -->
                <div class="wh-report-kpi-grid">
                    <!-- KPI 1: Tồn kho hiện tại -->
                    <div class="wh-report-kpi-card is-stock">
                        <div class="wh-report-kpi-icon-box" style="background:#e0f2fe; color:#0284c7;">
                            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path><polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline><line x1="12" y1="22.08" x2="12" y2="12"></line></svg>
                        </div>
                        <div class="wh-report-kpi-content">
                            <div class="wh-report-kpi-title">Tổng số lượng tồn kho</div>
                            <div class="wh-report-kpi-number" style="color:#0284c7;">
                                ${(summary.totalStockQty || 0).toLocaleString('vi-VN')}
                            </div>
                            <div class="wh-report-kpi-sub">
                                Ước tính giá trị: <strong>${(summary.totalStockValue || 0).toLocaleString('vi-VN')} đ</strong>
                            </div>
                        </div>
                    </div>

                    <!-- KPI 2: Nhập kho / Lưu kho trong kỳ -->
                    <div class="wh-report-kpi-card is-inbound">
                        <div class="wh-report-kpi-icon-box" style="background:#dcfce7; color:#16a34a;">
                            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line><line x1="4" y1="21" x2="20" y2="21"></line></svg>
                        </div>
                        <div class="wh-report-kpi-content">
                            <div class="wh-report-kpi-title">Nhập / Lưu kho trong kỳ</div>
                            <div class="wh-report-kpi-number" style="color:#16a34a;">
                                +${(summary.totalInQty || 0).toLocaleString('vi-VN')}
                            </div>
                            <div class="wh-report-kpi-sub">
                                NVL từ nhà cung cấp & Thành phẩm từ xưởng
                            </div>
                        </div>
                    </div>

                    <!-- KPI 3: Xuất kho trong kỳ -->
                    <div class="wh-report-kpi-card is-outbound">
                        <div class="wh-report-kpi-icon-box" style="background:#fef3c7; color:#d97706;">
                            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="17 14 12 9 7 14"></polyline><line x1="12" y1="9" x2="12" y2="21"></line><line x1="4" y1="3" x2="20" y2="3"></line></svg>
                        </div>
                        <div class="wh-report-kpi-content">
                            <div class="wh-report-kpi-title">Xuất kho trong kỳ</div>
                            <div class="wh-report-kpi-number" style="color:#d97706;">
                                -${(summary.totalOutQty || 0).toLocaleString('vi-VN')}
                            </div>
                            <div class="wh-report-kpi-sub">
                                NVL xuất sản xuất & Thành phẩm xuất bán hàng
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Biểu đồ Trực quan Tương quan Nhập vs Xuất kho -->
                <div class="wh-report-chart-box">
                    <div class="wh-report-chart-header">
                        <div style="font-size: 15px; font-weight: 600; color: #1e293b; display: flex; align-items: center; gap: 8px;">
                            <span>Tương quan Nhập / Lưu kho và Xuất kho (${escapeHtml(period.title || '')})</span>
                        </div>
                        <div class="wh-report-chart-legend">
                            <span class="wh-legend-item"><span class="wh-legend-dot" style="background:#0284c7;"></span> Nhập / Lưu kho</span>
                            <span class="wh-legend-item"><span class="wh-legend-dot" style="background:#d97706;"></span> Xuất kho</span>
                        </div>
                    </div>

                    <div style="padding: 16px 20px 8px 20px; overflow-x: auto;">
                        <svg viewBox="0 0 ${chartWidth} ${chartHeight + 35}" style="width: 100%; max-height: 200px; display: block;">
                            <!-- Đường lưới kẻ ngang -->
                            <line x1="30" y1="20" x2="${chartWidth}" y2="20" stroke="#f1f5f9" stroke-width="1" />
                            <line x1="30" y1="65" x2="${chartWidth}" y2="65" stroke="#f1f5f9" stroke-width="1" />
                            <line x1="30" y1="110" x2="${chartWidth}" y2="110" stroke="#f1f5f9" stroke-width="1" />
                            <line x1="30" y1="${chartHeight}" x2="${chartWidth}" y2="${chartHeight}" stroke="#cbd5e1" stroke-width="1" />

                            <!-- Các cột bar chart -->
                            ${chartData.map((d, idx) => {
                                const xCenter = 50 + idx * barGroupWidth + (barGroupWidth / 2);
                                const barW = Math.max(12, Math.min(24, barGroupWidth * 0.3));
                                const inH = maxBarVal > 0 ? ((d.inQty || 0) / maxBarVal) * (chartHeight - 30) : 0;
                                const outH = maxBarVal > 0 ? ((d.outQty || 0) / maxBarVal) * (chartHeight - 30) : 0;
                                const inY = chartHeight - inH;
                                const outY = chartHeight - outH;

                                return `
                                    <g class="wh-chart-group">
                                        <!-- Cột Nhập (Xanh) -->
                                        <rect x="${xCenter - barW - 2}" y="${inY}" width="${barW}" height="${inH}" rx="3" fill="#0284c7" opacity="0.9">
                                            <title>${d.label} - Nhập/Lưu kho: ${(d.inQty || 0).toLocaleString('vi-VN')}</title>
                                        </rect>
                                        <!-- Cột Xuất (Cam) -->
                                        <rect x="${xCenter + 2}" y="${outY}" width="${barW}" height="${outH}" rx="3" fill="#d97706" opacity="0.9">
                                            <title>${d.label} - Xuất kho: ${(d.outQty || 0).toLocaleString('vi-VN')}</title>
                                        </rect>
                                        <!-- Nhãn trục X -->
                                        <text x="${xCenter}" y="${chartHeight + 18}" text-anchor="middle" font-size="11" fill="#64748b" font-weight="500">${escapeHtml(d.label)}</text>
                                        <!-- Giá trị trên cột (nếu có số liệu) -->
                                        ${d.inQty > 0 ? `<text x="${xCenter - barW/2 - 2}" y="${inY - 4}" text-anchor="middle" font-size="9" fill="#0284c7" font-weight="600">${Number(d.inQty).toLocaleString('vi-VN')}</text>` : ''}
                                        ${d.outQty > 0 ? `<text x="${xCenter + barW/2 + 2}" y="${outY - 4}" text-anchor="middle" font-size="9" fill="#d97706" font-weight="600">${Number(d.outQty).toLocaleString('vi-VN')}</text>` : ''}
                                    </g>
                                `;
                            }).join('')}
                        </svg>
                    </div>
                </div>

                <!-- Bảng chi tiết Thống kê số lượng Tồn, Xuất, Lưu kho -->
                <div class="wh-report-table-card">
                    <div class="wh-report-table-header">
                        <div style="font-size: 15px; font-weight: 600; color: #1e293b;">
                            Bảng chi tiết Nhập - Xuất - Tồn & Lưu kho (${items.length} mặt hàng)
                        </div>
                        <div style="font-size: 12px; color: #64748b;">
                            Nhấp vào <strong>Mã hàng</strong> hoặc nút <strong>Thẻ kho</strong> để xem toàn bộ lịch sử chứng từ
                        </div>
                    </div>

                    <div class="wh-report-table-wrapper">
                        <table class="wh-report-table">
                            <thead>
                                <tr>
                                    <th style="width: 40px; text-align: center;">STT</th>
                                    <th style="width: 90px;">Mã hàng</th>
                                    <th style="min-width: 200px;">Tên nguyên vật liệu / sản phẩm</th>
                                    <th style="width: 120px;">Phân loại</th>
                                    <th style="width: 60px; text-align: center;">ĐVT</th>
                                    <th style="width: 95px; text-align: right;">Tồn đầu kỳ</th>
                                    <th style="width: 105px; text-align: right; background: #f0fdf4;">Nhập/Lưu kho</th>
                                    <th style="width: 95px; text-align: right; background: #fffbeb;">Xuất kho</th>
                                    <th style="width: 105px; text-align: right; background: #f0f9ff; font-weight: 700;">Tồn cuối kỳ</th>
                                    <th style="width: 100px; text-align: right;">Đơn giá</th>
                                    <th style="width: 130px; text-align: right;">Thành tiền tồn</th>
                                    <th style="width: 95px; text-align: center;">Trạng thái</th>
                                    <th style="width: 85px; text-align: center;">Thẻ kho</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${items.length === 0 ? `
                                    <tr>
                                        <td colspan="13" style="text-align: center; padding: 40px; color: #94a3b8;">
                                            Không tìm thấy dữ liệu thống kê phù hợp với điều kiện tìm kiếm.
                                        </td>
                                    </tr>
                                ` : items.map((it, idx) => {
                                    const isIngr = it.type === 'ingredient';
                                    return `
                                        <tr>
                                            <td style="text-align: center; color: #64748b;">${idx + 1}</td>
                                            <td>
                                                <a href="javascript:void(0)" class="wh-report-code-link" data-action="view-stock-card" data-item-id="${it.id}" data-item-type="${it.type}">
                                                    ${escapeHtml(it.code)}
                                                </a>
                                            </td>
                                            <td>
                                                <strong style="color: #1e293b;">${escapeHtml(it.name)}</strong>
                                            </td>
                                            <td>
                                                <span class="wh-report-badge is-${isIngr ? 'ingredient' : 'product'}">
                                                    ${escapeHtml(it.typeLabel)}
                                                </span>
                                            </td>
                                            <td style="text-align: center; color: #64748b;">${escapeHtml(it.unit)}</td>
                                            <td style="text-align: right;">${(Number(it.openingStock) || 0).toLocaleString('vi-VN')}</td>
                                            <td style="text-align: right; background: #f0fdf4; color: ${it.inQty > 0 ? '#16a34a' : '#64748b'}; font-weight: ${it.inQty > 0 ? '600' : 'normal'};">
                                                ${it.inQty > 0 ? `+${(Number(it.inQty) || 0).toLocaleString('vi-VN')}` : '0'}
                                            </td>
                                            <td style="text-align: right; background: #fffbeb; color: ${it.outQty > 0 ? '#d97706' : '#64748b'}; font-weight: ${it.outQty > 0 ? '600' : 'normal'};">
                                                ${it.outQty > 0 ? `-${(Number(it.outQty) || 0).toLocaleString('vi-VN')}` : '0'}
                                            </td>
                                            <td style="text-align: right; background: #f0f9ff; font-weight: 700; color: #0284c7;">
                                                ${(Number(it.closingStock) || 0).toLocaleString('vi-VN')}
                                            </td>
                                            <td style="text-align: right; color: #475569;">
                                                ${(Number(it.unitPrice) || 0).toLocaleString('vi-VN')} đ
                                            </td>
                                            <td style="text-align: right; font-weight: 600; color: #1e293b;">
                                                ${(Number(it.totalValue) || 0).toLocaleString('vi-VN')} đ
                                            </td>
                                            <td style="text-align: center;">
                                                <span class="wh-report-status-pill is-${it.stockStatus}">
                                                    ${escapeHtml(it.stockStatusLabel)}
                                                </span>
                                            </td>
                                            <td style="text-align: center;">
                                                <button type="button" class="wh-report-stockcard-btn" data-action="view-stock-card" data-item-id="${it.id}" data-item-type="${it.type}" title="Xem lịch sử nhập xuất thẻ kho">
                                                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>
                                                    Thẻ kho
                                                </button>
                                            </td>
                                        </tr>
                                    `;
                                }).join('')}
                            </tbody>
                            <tfoot>
                                <tr style="background: #f8fafc; font-weight: 700; border-top: 2px solid #cbd5e1;">
                                    <td colspan="5" style="text-align: right; padding: 12px 16px; color: #1e293b;">
                                        TỔNG CỘNG (${items.length} mặt hàng):
                                    </td>
                                    <td style="text-align: right; color: #1e293b;">${totalOpeningStock.toLocaleString('vi-VN')}</td>
                                    <td style="text-align: right; background: #dcfce7; color: #16a34a;">+${totalInQty.toLocaleString('vi-VN')}</td>
                                    <td style="text-align: right; background: #fef3c7; color: #d97706;">-${totalOutQty.toLocaleString('vi-VN')}</td>
                                    <td style="text-align: right; background: #e0f2fe; color: #0284c7;">${totalClosingStock.toLocaleString('vi-VN')}</td>
                                    <td style="text-align: right; color: #64748b;">-</td>
                                    <td style="text-align: right; color: #0f172a; font-size: 13px;">${grandTotalValue.toLocaleString('vi-VN')} đ</td>
                                    <td colspan="2"></td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </div>

                <!-- Modal Thẻ kho nếu có -->
                ${stockCardModalHtml}
            </div>
        `;
    }

    /**
     * 9. MainContentComponent (Vùng nội dung chính)
     */
    function renderMainContent(state, viewPayloads) {
        const activeVoucherHtml = state.activeVoucherModal
            ? renderDocumentModal(viewPayloads.activeVoucherData)
            : '';

        switch (state.activeMenuId) {
            case 'tong-quan':
                return `
                    <div class="wh-page-header">
                        <h1 class="wh-page-title">Tổng quan</h1>
                    </div>
                    <div class="wh-dashboard-grid">
                        ${viewPayloads.cardsConfig.map(card => renderWidgetCard(card, state)).join('')}
                    </div>
                `;

            case 'danh-muc-vthh': {
                const modalHtml = viewPayloads.vthhModalData
                    ? renderVthhDetailModal(viewPayloads.vthhModalData)
                    : '';
                return renderTabbedDataGrid(viewPayloads.vthhConfig, modalHtml);
            }

            case 'yeu-cau-san-xuat': {
                const prModalHtml = viewPayloads.prModalData
                    ? renderProductionRequestModal(viewPayloads.prModalData)
                    : '';
                return renderTabbedDataGrid(viewPayloads.prConfig, prModalHtml);
            }

            case 'nha-cung-cap': {
                const supplierModalHtml = viewPayloads.supplierModalData
                    ? renderSupplierModal(viewPayloads.supplierModalData)
                    : '';
                return renderTabbedDataGrid(viewPayloads.supplierConfig, supplierModalHtml);
            }

            case 'nhap-kho':
                return renderTabbedDataGrid(viewPayloads.inboundConfig, activeVoucherHtml);

            case 'xuat-kho':
                return renderTabbedDataGrid(viewPayloads.outboundConfig, activeVoucherHtml);

            case 'bao-cao':
                return renderReportPage(viewPayloads.reportConfig);

            case 'thong-tin-nhan-vien':
                return renderEmployeeProfile(
                    viewPayloads.currentUser || state.currentUser,
                    viewPayloads.warehouseTasks || (typeof WarehouseModel !== 'undefined' && WarehouseModel.getWarehouseKeeperTasks ? WarehouseModel.getWarehouseKeeperTasks() : [])
                );

            default:
                return `<div class="wh-empty-maincontent" data-empty-section="${escapeHtml(state.activeMenuId)}"></div>`;
        }
    }

    /**
     * 8b. Component Trang Thông tin cá nhân & Bảng công việc của thủ kho
     * - Chỉ lấy các cột từ bảng account: userid, username, fullname, phone, role, status, password
     * - Bỏ các bảng Chuyên môn & Chứng chỉ Dược, Phân quyền & Phạm vi Quản lý kho, Thống kê hoạt động & Đánh giá KPIs
     * - Thay bằng Bảng công việc của thủ kho cần thực hiện
     */
    function renderEmployeeProfile(user, tasks) {
        user = user || {
            userId: '-',
            username: 'warehouse',
            fullName: 'Tài khoản Kho',
            role: 'Warehouse',
            phone: '',
            status: 'Active'
        };

        tasks = Array.isArray(tasks) && tasks.length > 0
            ? tasks
            : (typeof WarehouseModel !== 'undefined' && WarehouseModel.getWarehouseKeeperTasks ? WarehouseModel.getWarehouseKeeperTasks() : []);

        const avatarLargeSvg = `data:image/svg+xml;utf8,` + encodeURIComponent(
            `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <defs>
                    <linearGradient id="avatarGrad" x1="0%" y1="0%" x2="100%" y2="100%">
                        <stop offset="0%" stop-color="#0284c7" />
                        <stop offset="100%" stop-color="#0369a1" />
                    </linearGradient>
                </defs>
                <circle cx="50" cy="50" r="48" fill="url(#avatarGrad)"/>
                <circle cx="50" cy="38" r="18" fill="#ffffff"/>
                <path d="M22 84c0-16 12-28 28-28s28 12 28 28" fill="#ffffff"/>
                <rect x="47" y="68" width="6" height="14" fill="#0284c7" rx="1"/>
                <rect x="43" y="72" width="14" height="6" fill="#0284c7" rx="1"/>
            </svg>`
        );

        return `
            <div class="wh-employee-profile-page">
                <!-- Toolbar & Breadcrumb -->
                <div class="wh-profile-toolbar">
                    <div class="wh-profile-breadcrumb-wrap">
                        <button type="button" class="wh-btn wh-btn-secondary wh-btn-back" data-action="back-from-profile">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
                            <span>Quay lại</span>
                        </button>
                        <div class="wh-profile-breadcrumb">
                            <span>Quản lý hệ thống</span>
                            <span class="wh-breadcrumb-sep">/</span>
                            <span>Tài khoản người dùng</span>
                            <span class="wh-breadcrumb-sep">/</span>
                            <span class="wh-breadcrumb-current">Thông tin tài khoản & Bảng công việc</span>
                        </div>
                    </div>
                    <div class="wh-profile-actions">
                        <button type="button" class="wh-btn wh-btn-secondary" onclick="window.print()" title="In thông tin">
                            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
                            <span>In thông tin</span>
                        </button>
                    </div>
                </div>

                <!-- Hero Card Banner (Chỉ chứa thông tin có trong bảng account) -->
                <div class="wh-profile-hero">
                    <div class="wh-profile-hero-main">
                        <div class="wh-profile-hero-avatar">
                            <img src="${avatarLargeSvg}" alt="Avatar" />
                            <span class="wh-profile-avatar-status" title="Trạng thái: ${escapeHtml(user.status || 'Active')}"></span>
                        </div>
                        <div class="wh-profile-hero-info">
                            <div class="wh-profile-hero-topline">
                                <h2 class="wh-profile-hero-name">${escapeHtml(user.fullName)}</h2>
                                <span class="wh-badge-code wh-badge-code-lg">ID: ${escapeHtml(user.userId)}</span>
                                <span class="wh-profile-hero-status-pill is-${(user.status || 'Active').toLowerCase()}">
                                    <span class="wh-pulse-dot"></span>
                                    ${escapeHtml(user.status || 'Active')}
                                </span>
                            </div>
                            <div class="wh-profile-hero-title">
                                <span class="wh-profile-hero-username">@${escapeHtml(user.username)}</span>
                                <span class="wh-profile-hero-dot">•</span>
                                <span class="wh-profile-hero-role">Vai trò: ${escapeHtml(user.role || 'Employee')}</span>
                                ${user.phone ? `
                                    <span class="wh-profile-hero-dot">•</span>
                                    <span class="wh-profile-hero-phone">SĐT: ${escapeHtml(user.phone)}</span>
                                ` : ''}
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Khối 1: Thông tin tài khoản & Cá nhân (Chỉ lấy các cột trong bảng account, không thêm dữ liệu ảo) -->
                <div class="wh-profile-card">
                    <div class="wh-profile-card-header">
                        <div class="wh-profile-card-header-icon is-blue">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                        </div>
                        <div>
                            <h3 class="wh-profile-card-title">Thông tin tài khoản & Cá nhân</h3>
                            <p class="wh-profile-card-subtitle">Các trường dữ liệu thực tế từ bảng <code>account</code> trong cơ sở dữ liệu (không có dữ liệu ảo)</p>
                        </div>
                    </div>
                    <div class="wh-profile-card-body">
                        <div class="wh-account-columns-grid">
                            <div class="wh-account-col-item">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">userid</span>
                                    Mã người dùng (User ID)
                                </div>
                                <div class="wh-account-col-val">
                                    <span class="wh-badge-code">ID: ${escapeHtml(user.userId)}</span>
                                </div>
                            </div>
                            <div class="wh-account-col-item">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">username</span>
                                    Tên đăng nhập
                                </div>
                                <div class="wh-account-col-val">
                                    <strong>${escapeHtml(user.username)}</strong>
                                </div>
                            </div>
                            <div class="wh-account-col-item">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">fullname</span>
                                    Họ và tên
                                </div>
                                <div class="wh-account-col-val">
                                    <strong>${escapeHtml(user.fullName)}</strong>
                                </div>
                            </div>
                            <div class="wh-account-col-item">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">phone</span>
                                    Số điện thoại
                                </div>
                                <div class="wh-account-col-val">
                                    ${user.phone ? `
                                        <a href="tel:${escapeHtml(user.phone)}" class="wh-profile-link">
                                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
                                            ${escapeHtml(user.phone)}
                                        </a>
                                    ` : '<span style="color:#94a3b8; font-style:italic;">Chưa cập nhật</span>'}
                                </div>
                            </div>
                            <div class="wh-account-col-item">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">role</span>
                                    Vai trò hệ thống
                                </div>
                                <div class="wh-account-col-val">
                                    <span class="wh-role-tag">${escapeHtml(user.role || 'Employee')}</span>
                                </div>
                            </div>
                            <div class="wh-account-col-item">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">status</span>
                                    Trạng thái tài khoản
                                </div>
                                <div class="wh-account-col-val">
                                    <span class="wh-status-badge is-${(user.status || 'Active').toLowerCase()}">
                                        <span class="wh-dot"></span>
                                        ${escapeHtml(user.status || 'Active')}
                                    </span>
                                </div>
                            </div>
                            <div class="wh-account-col-item wh-account-col-full">
                                <div class="wh-account-col-label">
                                    <span class="wh-col-db-tag">password</span>
                                    Mật khẩu
                                </div>
                                <div class="wh-account-col-val" style="color: #64748b; font-size: 13px;">
                                    <code>••••••••••••</code> 
                                    <span style="font-size: 12px; color: #94a3b8; margin-left: 8px;">(Được bảo mật và mã hóa trong cơ sở dữ liệu)</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Khối 2: Bảng công việc của thủ kho cần thực hiện -->
                <div class="wh-profile-card">
                    <div class="wh-profile-card-header">
                        <div class="wh-profile-card-header-icon is-green">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 11l3 3L22 4"></path><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path></svg>
                        </div>
                        <div style="flex:1;">
                            <div style="display: flex; align-items: center; justify-content: space-between;">
                                <h3 class="wh-profile-card-title">Bảng công việc của thủ kho cần thực hiện</h3>
                                <span class="wh-badge-code">${tasks.length} nhiệm vụ chính</span>
                            </div>
                            <p class="wh-profile-card-subtitle">Quy trình, chứng từ liên quan và danh mục trách nhiệm vận hành kho hàng</p>
                        </div>
                    </div>
                    <div class="wh-profile-card-body" style="padding: 0;">
                        <div class="wh-task-table-wrapper">
                            <table class="wh-task-table">
                                <thead>
                                    <tr>
                                        <th style="width: 50px; text-align: center;">STT</th>
                                        <th style="min-width: 280px;">Nhiệm vụ / Công việc</th>
                                        <th style="min-width: 250px;">Quy trình & Chứng từ áp dụng</th>
                                        <th style="min-width: 170px;">Chu kỳ thực hiện</th>
                                        <th style="width: 110px; text-align: center;">Mức ưu tiên</th>
                                        <th style="width: 140px; text-align: center;">Phân hệ quản lý</th>
                                        <th style="width: 130px; text-align: center;">Thao tác</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${tasks.map((task, idx) => `
                                        <tr>
                                            <td style="text-align: center; font-weight: 700; color: #64748b;">${idx + 1}</td>
                                            <td>
                                                <div class="wh-task-title">${escapeHtml(task.title)}</div>
                                                <div class="wh-task-desc">${escapeHtml(task.description)}</div>
                                            </td>
                                            <td>
                                                <div class="wh-task-docs-wrap">
                                                    ${task.documents.map(doc => `
                                                        <span class="wh-task-doc-pill">
                                                            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg>
                                                            ${escapeHtml(doc)}
                                                        </span>
                                                    `).join('')}
                                                </div>
                                            </td>
                                            <td>
                                                <div class="wh-task-freq">
                                                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
                                                    <span>${escapeHtml(task.frequency)}</span>
                                                </div>
                                            </td>
                                            <td style="text-align: center;">
                                                <span class="wh-priority-badge is-${task.priorityClass}">
                                                    ${escapeHtml(task.priority)}
                                                </span>
                                            </td>
                                            <td style="text-align: center;">
                                                <span class="wh-module-tag">
                                                    ${escapeHtml(task.module)}
                                                </span>
                                            </td>
                                            <td style="text-align: center;">
                                                <button type="button" class="wh-task-action-btn" data-action="select-menu" data-menu-id="${escapeHtml(task.menuId)}" title="Mở phân hệ ${escapeHtml(task.module)}">
                                                    <span>Mở phân hệ</span>
                                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
                                                </button>
                                            </td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        `;
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
        renderVthhDetailModal,
        renderProductionRequestModal,
        renderSupplierModal,
        renderStockCardModal,
        renderReportPage,
        renderUserDropdown,
        renderEmployeeProfile,
        renderMainContent
    };
})();
