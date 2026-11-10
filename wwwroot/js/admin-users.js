/*!
 * Admin › Quản lý tài khoản
 * - Bảng, lọc, tìm kiếm thời gian thực, phân trang: AJAX (thay HTML partial, không reload trang).
 * - Form Thêm/Sửa/Reset mật khẩu: validate client bằng Constraint Validation API (checkValidity/reportValidity),
 *   dùng CHUNG pattern + thông báo với server (render từ UserAccountRules), và gắn lỗi ModelState của server
 *   trở lại đúng ô nhập qua setCustomValidity.
 * Không dùng mật khẩu ở bất kỳ nơi nào ngoài body của request POST; không log, không lưu storage.
 */
(() => {
    'use strict';

    const page = document.getElementById('userPage');
    if (!page) return;

    // ── Cấu hình từ data-* (Razor sinh URL, không hard-code đường dẫn ở JS) ──
    const d = page.dataset;
    const URLS = {
        index: d.urlIndex,
        create: d.urlCreate,
        update: d.urlUpdate,
        setStatus: d.urlSetStatus,
        resetPassword: d.urlResetPassword,
        del: d.urlDelete,
        bulkStatus: d.urlBulkStatus,
        bulkDelete: d.urlBulkDelete,
        login: d.urlLogin
    };
    const MSG_PASSWORD = d.msgPassword;

    const $ = (sel, root = document) => root.querySelector(sel);
    const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

    const host = $('#userTableHost');
    const searchInput = $('#filterSearch');
    const roleSelect = $('#filterRole');
    const statusSelect = $('#filterStatus');
    const clearFiltersBtn = $('#clearFiltersBtn');

    const state = { page: 1 };

    // ═════════════════════════════════════════════════════════════════════
    //  Tiện ích
    // ═════════════════════════════════════════════════════════════════════
    const escapeHtml = (value) => String(value ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#039;');

    function toast(icon, title) {
        Swal.fire({
            toast: true, position: 'top-end', icon, title,
            showConfirmButton: false, timer: 3200, timerProgressBar: true,
            customClass: { popup: 'adm-swal-toast' },
            didOpen: (el) => {
                el.addEventListener('mouseenter', Swal.stopTimer);
                el.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
    }

    const antiForgeryToken = () => $('input[name="__RequestVerificationToken"]')?.value ?? '';

    let sessionExpiredShown = false;
    function sessionExpired() {
        if (sessionExpiredShown) return;
        sessionExpiredShown = true;
        Swal.fire({
            icon: 'warning',
            title: 'Phiên đăng nhập đã hết hạn',
            text: 'Vui lòng đăng nhập lại để tiếp tục.',
            confirmButtonText: 'Đăng nhập',
            allowOutsideClick: false
        }).then(() => { window.location.href = URLS.login; });
    }

    /** Gắn spinner + disable nút (chống double-click). Khôi phục nội dung gốc khi tắt. */
    function setLoading(btn, on, text = 'Đang xử lý...') {
        if (!btn) return;
        if (on) {
            if (btn.dataset.loading === '1') return;
            btn.dataset.loading = '1';
            btn._html = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = `<i class="fa fa-spinner fa-spin${text ? ' me-2' : ''}" aria-hidden="true"></i>${escapeHtml(text)}`;
        } else {
            if (btn.dataset.loading !== '1') return;
            delete btn.dataset.loading;
            btn.disabled = false;
            if (btn._html !== undefined) btn.innerHTML = btn._html;
        }
    }

    /**
     * Gọi API ghi (JSON). KHÔNG throw: luôn trả { ok, status, data, handled }.
     * 401 → phiên hết hạn (handled = true, caller không cần hiện thêm thông báo).
     */
    async function api(url, body) {
        let res;
        try {
            const headers = { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': antiForgeryToken() };
            if (body !== undefined) headers['Content-Type'] = 'application/json';
            res = await fetch(url, {
                method: 'POST', credentials: 'same-origin', headers,
                body: body === undefined ? undefined : JSON.stringify(body)
            });
        } catch {
            return { ok: false, status: 0, data: { message: 'Không thể kết nối máy chủ. Vui lòng kiểm tra mạng và thử lại.' } };
        }

        if (res.status === 401) { sessionExpired(); return { ok: false, status: 401, handled: true, data: {} }; }

        let data = null;
        try { data = await res.json(); } catch { /* phản hồi không phải JSON */ }

        if (!data) {
            const message = res.status === 403
                ? 'Bạn không có quyền thực hiện thao tác này.'
                : 'Phản hồi không hợp lệ từ máy chủ.';
            return { ok: false, status: res.status, data: { message } };
        }
        return { ok: res.ok && data.success !== false, status: res.status, data };
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Bảng: tải bằng AJAX (lọc / tìm kiếm / phân trang / làm mới sau thao tác)
    // ═════════════════════════════════════════════════════════════════════
    let tableController = null;

    function currentFilters() {
        return {
            search: searchInput.value.trim(),
            role: roleSelect.value,
            status: statusSelect.value
        };
    }

    function buildQuery(pageNo) {
        const f = currentFilters();
        const p = new URLSearchParams();
        if (f.search) p.set('search', f.search);
        if (f.role) p.set('role', f.role);
        if (f.status) p.set('status', f.status);
        if (pageNo > 1) p.set('page', String(pageNo));
        return p.toString();
    }

    function syncFilterUi() {
        const f = currentFilters();
        clearFiltersBtn.hidden = !(f.search || f.role || f.status);
    }

    async function loadTable(pageNo = state.page) {
        syncFilterUi();
        tableController?.abort();
        const controller = (tableController = new AbortController());

        host.classList.add('is-loading');
        host.setAttribute('aria-busy', 'true');

        const qs = buildQuery(pageNo);
        try {
            const res = await fetch(URLS.index + (qs ? `?${qs}` : ''), {
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                signal: controller.signal
            });
            if (res.status === 401) { sessionExpired(); return; }
            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            host.innerHTML = await res.text();

            const root = $('#userTableRoot', host);
            state.page = Number(root?.dataset.page) || 1;

            // Địa chỉ URL phản ánh bộ lọc hiện tại (F5 / chia sẻ link vẫn đúng) nhưng không thêm lịch sử.
            const finalQs = buildQuery(state.page);
            history.replaceState(null, '', window.location.pathname + (finalQs ? `?${finalQs}` : ''));

            updateBulkUi();
        } catch (err) {
            if (err.name === 'AbortError') return;
            toast('error', 'Không thể tải danh sách tài khoản. Vui lòng thử lại.');
        } finally {
            if (tableController === controller) {
                host.classList.remove('is-loading');
                host.removeAttribute('aria-busy');
            }
        }
    }

    // Tìm kiếm thời gian thực: debounce 350ms; bỏ qua khi đang gõ dấu (IME/Telex).
    let searchTimer = 0;
    const scheduleSearch = () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(() => loadTable(1), 350);
    };
    searchInput.addEventListener('input', (e) => { if (!e.isComposing) scheduleSearch(); });
    searchInput.addEventListener('compositionend', scheduleSearch);
    searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') { e.preventDefault(); window.clearTimeout(searchTimer); loadTable(1); }
    });
    roleSelect.addEventListener('change', () => loadTable(1));
    statusSelect.addEventListener('change', () => loadTable(1));
    $('#userFilterForm').addEventListener('submit', (e) => { e.preventDefault(); loadTable(1); });

    function clearFilters() {
        searchInput.value = '';
        roleSelect.value = '';
        statusSelect.value = '';
        loadTable(1);
    }

    // Phân trang (event delegation vì HTML bảng bị thay thế liên tục)
    host.addEventListener('click', (e) => {
        const link = e.target.closest('a[data-page]');
        if (!link) return;
        e.preventDefault();
        loadTable(Number(link.dataset.page) || 1);
        $('.adm-table-wrapper', host)?.scrollTo({ top: 0, behavior: 'smooth' });
    });

    // ═════════════════════════════════════════════════════════════════════
    //  Chọn nhiều dòng + thanh thao tác hàng loạt
    // ═════════════════════════════════════════════════════════════════════
    const bulkBar = $('#bulkBar');
    const bulkButtons = ['#bulkLockBtn', '#bulkUnlockBtn', '#bulkDeleteBtn'].map((s) => $(s));

    const rowBoxes = () => $$('.row-checkbox:not(:disabled)', host);
    const selectedIds = () => $$('.row-checkbox:checked', host).map((cb) => Number(cb.value));

    function updateBulkUi() {
        const boxes = rowBoxes();
        const count = selectedIds().length;
        const selectAll = $('#selectAllRows', host);

        if (selectAll) {
            selectAll.disabled = boxes.length === 0;
            selectAll.checked = boxes.length > 0 && count === boxes.length;
            selectAll.indeterminate = count > 0 && count < boxes.length;
        }

        bulkBar.hidden = count === 0;
        bulkBar.classList.toggle('d-flex', count > 0);
        $('#bulkCount').textContent = String(count);
        $('#bulkLabel').textContent = `Đã chọn ${count} tài khoản`;
        bulkButtons.forEach((b) => { b.disabled = count === 0; });
    }

    function clearSelection() {
        $$('.row-checkbox', host).forEach((cb) => { cb.checked = false; });
        updateBulkUi();
    }

    host.addEventListener('change', (e) => {
        if (e.target.id === 'selectAllRows') {
            rowBoxes().forEach((cb) => { cb.checked = e.target.checked; });
            updateBulkUi();
        } else if (e.target.matches('.row-checkbox')) {
            updateBulkUi();
        }
    });
    $('#bulkClearBtn').addEventListener('click', clearSelection);

    async function confirmDialog(options) {
        const result = await Swal.fire({
            icon: 'warning', showCancelButton: true, cancelButtonText: 'Hủy',
            confirmButtonColor: '#6366f1', cancelButtonColor: '#94a3b8', focusCancel: true, ...options
        });
        return result.isConfirmed;
    }

    /** Chạy 1 thao tác API rồi thông báo + làm mới bảng (không reload trang). */
    async function runAction(btn, loadingText, request, fallbackSuccess) {
        setLoading(btn, true, loadingText);
        const res = await api(...request);
        setLoading(btn, false);
        if (res.handled) return false;

        if (!res.ok) {
            toast('error', res.data.message || 'Thao tác không thành công.');
            return false;
        }
        toast('success', res.data.message || fallbackSuccess);
        await loadTable(state.page);
        return true;
    }

    async function bulkSetStatus(status, label, btn) {
        const ids = selectedIds();
        if (!ids.length) return;
        const ok = await confirmDialog({
            title: `${label} ${ids.length} tài khoản?`,
            text: status === 'Active'
                ? 'Các tài khoản đã chọn sẽ được phép đăng nhập trở lại.'
                : 'Các tài khoản đã chọn sẽ không thể đăng nhập cho tới khi được mở khóa.',
            confirmButtonText: label
        });
        if (ok) await runAction(btn, 'Đang xử lý...', [URLS.bulkStatus, { ids, status }], 'Đã cập nhật trạng thái.');
    }

    $('#bulkLockBtn').addEventListener('click', (e) => bulkSetStatus('Blocked', 'Khóa', e.currentTarget));
    $('#bulkUnlockBtn').addEventListener('click', (e) => bulkSetStatus('Active', 'Mở khóa', e.currentTarget));
    $('#bulkDeleteBtn').addEventListener('click', async (e) => {
        const btn = e.currentTarget;
        const ids = selectedIds();
        if (!ids.length) return;

        const names = $$('.row-checkbox:checked', host).map((cb) => cb.closest('tr').dataset.username);
        const list = names.slice(0, 5).map((n) => `<li><strong>${escapeHtml(n)}</strong></li>`).join('')
            + (names.length > 5 ? `<li>… và ${names.length - 5} tài khoản khác</li>` : '');

        const ok = await confirmDialog({
            title: `Xóa vĩnh viễn ${ids.length} tài khoản?`,
            html: `<ul class="text-start mb-2 ps-4">${list}</ul><small class="text-danger">Hành động này không thể hoàn tác.</small>`,
            confirmButtonText: 'Xóa', confirmButtonColor: '#ef4444'
        });
        if (ok) await runAction(btn, 'Đang xóa...', [URLS.bulkDelete, { ids }], 'Đã xóa các tài khoản đã chọn.');
    });

    // ═════════════════════════════════════════════════════════════════════
    //  Validation: đồng bộ client ⇄ server
    // ═════════════════════════════════════════════════════════════════════
    const fieldEls = (form) => Array.from(form.elements).filter((el) => el.name && el.type !== 'hidden' && el.tagName !== 'BUTTON');

    function feedbackOf(el) {
        return (el.closest('.mb-3, .col-sm-6') ?? el.parentElement).querySelector('.invalid-feedback');
    }

    function renderFieldState(el) {
        if (el.disabled || el.closest('[hidden]')) return;
        const fb = feedbackOf(el);
        if (el.checkValidity()) {
            el.classList.remove('is-invalid');
            if (el.value) el.classList.add('is-valid'); else el.classList.remove('is-valid');
            if (fb) fb.textContent = '';
        } else {
            el.classList.add('is-invalid');
            el.classList.remove('is-valid');
            // Lỗi server (customError) → dùng đúng thông điệp server; lỗi ràng buộc HTML → dùng data-msg dùng chung.
            if (fb) fb.textContent = el.validity.customError ? el.validationMessage : (el.dataset.msg || el.validationMessage);
        }
    }

    function resetFormState(form) {
        fieldEls(form).forEach((el) => {
            el.setCustomValidity('');
            el.classList.remove('is-invalid', 'is-valid');
            const fb = feedbackOf(el);
            if (fb) fb.textContent = '';
        });
    }

    /** Quy tắc mật khẩu giống server: tối thiểu 8 ký tự, không được chỉ gồm khoảng trắng (không trim để lưu). */
    function applyPasswordRules(el) {
        if (el.name !== 'password' && el.name !== 'newPassword') return;
        if (!el.value) return;
        if (!el.value.trim() || el.value.length < 8) {
            el.setCustomValidity(MSG_PASSWORD);
        }
    }

    function attachRealtimeValidation(form, extra) {
        form.addEventListener('input', (e) => {
            const el = e.target;
            if (!el.name || el.type === 'hidden') return;
            el.setCustomValidity('');              // xóa lỗi server cũ ngay khi người dùng sửa
            applyPasswordRules(el);
            extra?.(el);
            renderFieldState(el);
        });
        form.addEventListener('change', (e) => {
            if (e.target.tagName === 'SELECT') renderFieldState(e.target);
        });
    }

    /** Gắn lỗi ModelState/ServiceResult của server { errors: { field: msg } } vào đúng ô nhập. */
    function applyServerErrors(form, data) {
        const errors = data?.errors;
        if (!errors) return false;
        let applied = false;
        for (const [key, message] of Object.entries(errors)) {
            const el = form.elements[key];
            if (!el || typeof el.setCustomValidity !== 'function' || el.type === 'hidden') continue;
            el.setCustomValidity(message);
            renderFieldState(el);
            applied = true;
        }
        if (applied) form.reportValidity();
        return applied;
    }

    /** Validate toàn form trước khi gửi. Trả về true nếu hợp lệ; nếu không thì hiện lỗi + focus ô lỗi đầu tiên. */
    function validateBeforeSubmit(form, extra) {
        fieldEls(form).forEach((el) => {
            if (el.disabled) return;
            el.setCustomValidity('');
            applyPasswordRules(el);
            extra?.(el);
        });
        if (form.checkValidity()) return true;
        fieldEls(form).forEach(renderFieldState);
        form.reportValidity();
        return false;
    }

    // Hiện/ẩn mật khẩu
    document.addEventListener('click', (e) => {
        const toggle = e.target.closest('[data-toggle-password]');
        if (!toggle) return;
        const input = toggle.parentElement.querySelector('input');
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        toggle.querySelector('i').className = show ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye';
    });

    // ═════════════════════════════════════════════════════════════════════
    //  Modal Thêm / Sửa
    // ═════════════════════════════════════════════════════════════════════
    const userModalEl = $('#userModal');
    const userModal = bootstrap.Modal.getOrCreateInstance(userModalEl);
    const userForm = $('#userForm');
    const userSubmit = $('#userFormSubmit');
    const passwordGroup = $('#passwordGroup');
    const notice = $('#protectedNotice');
    let userMode = 'create';

    attachRealtimeValidation(userForm);

    function setPasswordVisible(visible) {
        passwordGroup.hidden = !visible;
        const input = userForm.elements.password;
        input.disabled = !visible;     // control bị disable không tham gia validation
        input.required = visible;
        input.type = 'password';
    }

    function openUserModal(mode, row) {
        userMode = mode;
        userForm.reset();
        resetFormState(userForm);
        notice.classList.add('d-none');

        const f = userForm.elements;
        const title = $('#userModalLabel');
        const submitIcon = '<i class="fa-solid fa-floppy-disk me-1" aria-hidden="true"></i>';

        if (mode === 'create') {
            title.innerHTML = '<i class="fa-solid fa-user-plus text-primary me-2" aria-hidden="true"></i>Tạo tài khoản mới';
            userSubmit.innerHTML = `${submitIcon}Tạo tài khoản`;
            f.id.value = '0';
            f.username.disabled = false;
            f.role.disabled = false;
            f.status.disabled = false;
            f.status.value = 'Active';
            setPasswordVisible(true);
        } else {
            title.innerHTML = '<i class="fa-solid fa-pen-to-square text-primary me-2" aria-hidden="true"></i>Sửa tài khoản';
            userSubmit.innerHTML = `${submitIcon}Lưu thay đổi`;
            f.id.value = row.id;
            f.username.value = row.username;
            f.username.disabled = true;          // username không đổi được (định danh đăng nhập)
            f.fullName.value = row.fullname || '';
            f.phone.value = row.phone || '';
            f.role.value = row.role;
            f.status.value = row.status;
            setPasswordVisible(false);

            const isSelf = row.self === 'true';
            const isLastAdmin = row.lastAdmin === 'true';
            if (isSelf || isLastAdmin) {
                // UI chỉ để hướng dẫn; server vẫn là nơi cưỡng chế quy tắc.
                f.role.disabled = true;
                f.status.disabled = true;
                notice.querySelector('span').textContent = isSelf
                    ? 'Bạn không thể đổi vai trò hoặc khóa chính tài khoản đang đăng nhập.'
                    : 'Đây là Admin đang hoạt động duy nhất — không thể đổi vai trò hoặc khóa. Hãy tạo thêm Admin khác trước.';
                notice.classList.remove('d-none');
            } else {
                f.role.disabled = false;
                f.status.disabled = false;
            }
        }
        userModal.show();
    }

    userModalEl.addEventListener('shown.bs.modal', () => {
        (userMode === 'create' ? userForm.elements.username : userForm.elements.fullName).focus();
    });

    userForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (userSubmit.dataset.loading === '1') return;
        if (!validateBeforeSubmit(userForm)) return;

        const f = userForm.elements;
        const payload = {
            fullName: f.fullName.value.trim(),
            phone: f.phone.value.trim() || null
        };
        if (userMode === 'create') {
            payload.username = f.username.value.trim();
            payload.password = f.password.value;          // không trim mật khẩu
            payload.role = f.role.value;
            payload.status = f.status.value;
        } else {
            payload.id = Number(f.id.value);
            // select bị disable (chính mình / Admin cuối) vẫn gửi giá trị hiện tại để server đối chiếu
            payload.role = f.role.value;
            payload.status = f.status.value;
        }

        setLoading(userSubmit, true, 'Đang lưu...');
        let res;
        try {
            res = await api(userMode === 'create' ? URLS.create : URLS.update, payload);
        } finally {
            setLoading(userSubmit, false);
        }
        if (res.handled) return;

        if (!res.ok) {
            if (applyServerErrors(userForm, res.data)) return;
            toast('error', res.data.message || 'Không thể lưu tài khoản.');
            return;
        }

        userModal.hide();
        toast('success', res.data.message || 'Đã lưu tài khoản.');
        await loadTable(state.page);
    });

    // ═════════════════════════════════════════════════════════════════════
    //  Modal Đặt lại mật khẩu
    // ═════════════════════════════════════════════════════════════════════
    const resetModalEl = $('#resetModal');
    const resetModal = bootstrap.Modal.getOrCreateInstance(resetModalEl);
    const resetForm = $('#resetForm');
    const resetSubmit = $('#resetFormSubmit');

    const confirmMatchRule = (el) => {
        const f = resetForm.elements;
        if (el.name === 'confirm' || el.name === 'newPassword') {
            if (f.confirm.value && f.confirm.value !== f.newPassword.value) {
                f.confirm.setCustomValidity('Mật khẩu nhập lại không khớp.');
            } else {
                f.confirm.setCustomValidity('');
            }
            if (el.name === 'newPassword') renderFieldState(f.confirm);
        }
    };
    attachRealtimeValidation(resetForm, confirmMatchRule);

    function openResetModal(row) {
        resetForm.reset();
        resetFormState(resetForm);
        $$('input[type="text"]', resetForm).forEach((i) => { i.type = 'password'; });
        resetForm.elements.id.value = row.id;
        $('#resetUsername').textContent = row.username;
        resetModal.show();
    }

    resetModalEl.addEventListener('shown.bs.modal', () => resetForm.elements.newPassword.focus());

    resetForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (resetSubmit.dataset.loading === '1') return;
        if (!validateBeforeSubmit(resetForm, confirmMatchRule)) return;

        const f = resetForm.elements;
        setLoading(resetSubmit, true, 'Đang đặt lại...');
        let res;
        try {
            res = await api(URLS.resetPassword, { id: Number(f.id.value), newPassword: f.newPassword.value });
        } finally {
            setLoading(resetSubmit, false);
        }
        if (res.handled) return;

        if (!res.ok) {
            if (applyServerErrors(resetForm, res.data)) return;
            toast('error', res.data.message || 'Không thể đặt lại mật khẩu.');
            return;
        }

        resetForm.reset();                     // xóa mật khẩu khỏi DOM ngay sau khi gửi
        resetModal.hide();
        toast('success', res.data.message || 'Đã đặt lại mật khẩu.');
    });

    // ═════════════════════════════════════════════════════════════════════
    //  Hành động từng dòng (event delegation)
    // ═════════════════════════════════════════════════════════════════════
    async function toggleStatus(row, btn) {
        const next = row.status === 'Active' ? 'Blocked' : 'Active';
        const ok = await confirmDialog({
            title: next === 'Active' ? 'Mở khóa tài khoản?' : 'Khóa tài khoản?',
            html: `Tài khoản <strong>${escapeHtml(row.username)}</strong> sẽ ${next === 'Active' ? 'được phép đăng nhập trở lại' : 'không thể đăng nhập'}.`,
            confirmButtonText: next === 'Active' ? 'Mở khóa' : 'Khóa'
        });
        if (ok) await runAction(btn, '', [URLS.setStatus, { id: Number(row.id), status: next }], 'Đã cập nhật trạng thái.');
    }

    async function deleteAccount(row, btn) {
        const ok = await confirmDialog({
            title: 'Xóa tài khoản?',
            html: `Bạn sắp xóa vĩnh viễn <strong>${escapeHtml(row.username)}</strong>.<br><small class="text-danger">Hành động này không thể hoàn tác.</small>`,
            confirmButtonText: 'Xóa', confirmButtonColor: '#ef4444'
        });
        if (ok) await runAction(btn, '', [`${URLS.del}/${encodeURIComponent(row.id)}`], 'Đã xóa tài khoản.');
    }

    document.addEventListener('click', (e) => {
        const btn = e.target.closest('[data-action]');
        if (!btn || btn.disabled) return;

        const row = btn.closest('tr')?.dataset;
        switch (btn.dataset.action) {
            case 'create': openUserModal('create'); break;
            case 'edit': if (row) openUserModal('edit', row); break;
            case 'reset': if (row) openResetModal(row); break;
            case 'status': if (row) toggleStatus(row, btn); break;
            case 'delete': if (row) deleteAccount(row, btn); break;
            case 'clear-filters': clearFilters(); break;
        }
    });

    // ═════════════════════════════════════════════════════════════════════
    //  Khởi tạo
    // ═════════════════════════════════════════════════════════════════════
    state.page = Number($('#userTableRoot', host)?.dataset.page) || 1;
    syncFilterUi();
    updateBulkUi();

    if (d.flashError) toast('error', d.flashError);
    if (d.flashSuccess) toast('success', d.flashSuccess);
})();
