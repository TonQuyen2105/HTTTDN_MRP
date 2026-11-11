/**
 * PRODUCTIONS - MATERIAL REQUISITION JS
 * Xử lý tương tác Yêu cầu xuất vật tư (Thêm/xóa dòng, tìm kiếm, lưu phiếu)
 */

document.addEventListener('DOMContentLoaded', () => {
  initMRTable();
  initMRFormActions();

});

/**
 * Xử lý thêm, xóa dòng và tính toán thành tiền trong bảng chi tiết vật tư
 */
function initMRTable() {
  const tableBody = document.getElementById('mrTableBody');
  const btnAdd = document.getElementById('btnAddMRRow');
  const btnClear = document.getElementById('btnClearMRRows');
  const totalCount = document.getElementById('totalMRCount');
  if (!tableBody || !btnAdd) return; // Existing receipts remain read-only.
  const catalog = JSON.parse(document.getElementById('mrIngredientCatalog')?.textContent || '[]');
  const emptyRow = tableBody.querySelector('[data-mr-empty]');
  const emptyTemplate = emptyRow?.cloneNode(true);
  const number = value => Number(value).toLocaleString('vi-VN', { maximumFractionDigits: 4 });

  const requestSelect = document.getElementById('mrProductionRequest');
  let wip = null;
  let wipState = 'Chọn đơn sản xuất';
  let wipLoadVersion = 0;
  function showWip(row) {
    const id = row.dataset.ingredientId;
    row.querySelector('[data-material-wip]').textContent = id && wip !== null ? number(wip[id] ?? 0) : '—';
    row.querySelector('[data-wip-status]').textContent = !id ? 'Chọn nguyên liệu' : wipState;
  }
  async function loadWip() {
    const version = ++wipLoadVersion;
    const requestId = Number(requestSelect?.value || 0);
    wip = null;
    wipState = requestId > 0 ? 'Đang tải WIP…' : 'Chọn đơn sản xuất';
    tableBody.querySelectorAll('[data-mr-line]').forEach(showWip);
    if (!requestId) return;
    try {
      const url = new URL(requestSelect.dataset.wipUrl, window.location.origin);
      url.searchParams.set('requestId', requestId);
      const response = await fetch(url, { headers: { Accept: 'application/json' }, cache: 'no-store' });
      if (!response.ok) throw new Error('Không tải được WIP');
      const result = await response.json();
      if (version !== wipLoadVersion) return;
      wip = Object.fromEntries(result.items.map(item => [String(item.ingredientId), item.quantity]));
      wipState = 'Tồn theo đơn đã chọn';
    } catch {
      if (version !== wipLoadVersion) return;
      wipState = 'Không tải được WIP; chọn lại đơn để thử lại';
    }
    tableBody.querySelectorAll('[data-mr-line]').forEach(showWip);
  }
  requestSelect?.addEventListener('change', loadWip);
  loadWip();

  function updateRows() {
    const rows = [...tableBody.querySelectorAll('[data-mr-line]')];
    rows.forEach((row, index) => row.querySelector('[data-stt]').textContent = index + 1);
    if (totalCount) totalCount.textContent = rows.length;
    if (!rows.length && !tableBody.querySelector('[data-mr-empty]') && emptyTemplate)
      tableBody.appendChild(emptyTemplate.cloneNode(true));
  }

  if (!catalog.length) {
    btnAdd.disabled = true;
    btnAdd.title = 'Chưa tải được danh mục nguyên vật liệu. Vui lòng tải lại trang.';
  }
  btnAdd.addEventListener('click', () => {
    tableBody.querySelector('[data-mr-empty]')?.remove();
    const row = document.createElement('tr');
    row.dataset.mrLine = '';
    row.className = 'page-fade-in';
    row.innerHTML = `
      <td class="text-center" data-stt></td>
      <td><input type="text" class="form-input mr-material-code" list="mrIngredientCodes" autocomplete="off" placeholder="Nhập hoặc chọn mã NVL" aria-label="Mã nguyên vật liệu" style="min-width:180px;">
          <small data-code-error role="status" style="display:block;color:#b91c1c;max-width:200px;"></small></td>
      <td data-material-name>—</td>
      <td data-material-unit>—</td>
      <td><input type="number" step="any" min="0" class="form-input text-right val-req" aria-label="Số lượng yêu cầu xuất" placeholder="Nhập số lượng" style="min-width:120px;"><small data-quantity-message role="status" style="display:block;color:#b91c1c;"></small></td>
      <td class="text-right" data-material-wip>—</td>
      <td class="text-right" data-material-stock>—</td>
      <td class="text-center" data-wip-status>Chọn đơn sản xuất</td>
      <td class="text-center">Chưa lập phiếu</td>
      <td class="text-center"><button type="button" class="row-act-btn btn-delete-row" aria-label="Xóa dòng nguyên liệu" style="width:auto;">Xóa</button></td>`;
    const code = row.querySelector('.mr-material-code');
    const quantity = row.querySelector('.val-req');
    const quantityMessage = row.querySelector('[data-quantity-message]');
    quantity.addEventListener('input', () => {
      if (Number(quantity.value) < 0) {
        quantity.value = '0';
        quantityMessage.textContent = 'Số lượng không được âm. Hãy nhập số lớn hơn 0 để yêu cầu xuất.';
      } else quantityMessage.textContent = '';
    });
    quantity.addEventListener('keydown', event => {
      if (event.key === '-') { event.preventDefault(); quantityMessage.textContent = 'Số lượng không được âm.'; }
    });
    function selectMaterial(finalize) {
      const value = code.value.trim().toUpperCase();
      const ingredient = catalog.find(i => i.code.toUpperCase() === value || String(i.id) === value);
      const duplicate = ingredient && [...tableBody.querySelectorAll('[data-mr-line]')]
        .some(other => other !== row && other.dataset.ingredientId === String(ingredient.id));
      const error = duplicate ? 'Nguyên liệu này đã được chọn ở dòng khác.' : (!ingredient && value && finalize ? 'Hãy chọn mã có trong danh sách.' : '');
      row.querySelector('[data-code-error]').textContent = error;
      code.setCustomValidity(error);
      const selected = ingredient && !duplicate ? ingredient : null;
      row.dataset.ingredientId = selected ? String(selected.id) : '';
      row.querySelector('[data-material-name]').textContent = selected?.name || '—';
      row.querySelector('[data-material-unit]').textContent = selected?.unit || '—';
      row.querySelector('[data-material-stock]').textContent = selected?.stock != null ? number(selected.stock) : '—';
      if (selected && finalize) code.value = selected.code;
      showWip(row);
    }
    code.addEventListener('input', () => selectMaterial(false));
    code.addEventListener('change', () => selectMaterial(true));
    row.querySelector('.btn-delete-row').addEventListener('click', () => { row.remove(); updateRows(); });
    tableBody.appendChild(row);
    showWip(row);
    updateRows();
    code.focus();
  });
  btnClear?.addEventListener('click', () => {
    if (!tableBody.querySelector('[data-mr-line]')) return;
    if (confirm('Bạn có chắc chắn muốn xóa tất cả nguyên vật liệu trong danh sách yêu cầu?')) {
      tableBody.querySelectorAll('[data-mr-line]').forEach(row => row.remove());
      updateRows();
    }
  });
}

/**
 * Xử lý lưu, hủy form yêu cầu xuất vật tư
 */
function initMRFormActions() {
  const btnCancel = document.getElementById('btnCancelMR');
  const btnClose = document.getElementById('btnCloseMR');
  const btnSave = document.getElementById('btnSaveMR');
  const btnSaveAndNew = document.getElementById('btnSaveAndNewMR');

  function returnToList() {
    window.location.href = '/Productions/Home/MaterialRequisitionList';
  }

  if (btnCancel) btnCancel.addEventListener('click', returnToList);
  if (btnClose) btnClose.addEventListener('click', returnToList);

  if (btnSave) {
    btnSave.addEventListener('click', (e) => {
      e.preventDefault();
      const quantities = [...document.querySelectorAll('#mrTableBody .val-req')];
      if (quantities.some(input => !Number.isFinite(Number(input.value)) || Number(input.value) <= 0)) {
        alert('Số lượng yêu cầu xuất phải lớn hơn 0.');
        return;
      }
      alert('Chức năng lập phiếu xuất vật tư thủ công chưa được triển khai backend.\nPhiếu yêu cầu xuất vật tư được sinh tự động từ chức năng "Tính nhu cầu nguyên vật liệu (MRP)".');
    });
  }

  if (btnSaveAndNew) {
    btnSaveAndNew.addEventListener('click', (e) => {
      e.preventDefault();
      alert('Chức năng lập phiếu xuất vật tư thủ công chưa được triển khai backend.');
    });
  }

  // --- XỬ LÝ NHẬP KHO BỔ SUNG & XUẤT KHO VỀ KHO SẢN XUẤT (WIP) ---
  function getVerificationToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
  }

  const receiptIdInput = document.getElementById('currentReceiptId');
  const expReceiptId = receiptIdInput ? parseInt(receiptIdInput.value, 10) : 0;

  // 2. Nút Xuất kho gửi về kho sản xuất (WIP)
  const btnExportToWip = document.getElementById('btnExportToWip');
  if (btnExportToWip) {
    btnExportToWip.addEventListener('click', async () => {
      if (!expReceiptId || expReceiptId <= 0) return;
      if (!confirm('Xác nhận xuất toàn bộ nguyên vật liệu từ kho thực tế và chuyển về kho sản xuất (WIP) cho lệnh sản xuất này?')) return;

      btnExportToWip.disabled = true;
      btnExportToWip.innerHTML = 'Đang xử lý xuất kho...';

      try {
        const response = await fetch('/Productions/Home/ExportMaterialsToWip', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getVerificationToken()
          },
          body: JSON.stringify({ expReceiptId: expReceiptId })
        });

        if (response.status === 401) {
          alert('Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.');
          window.location.href = '/Account/Login';
          return;
        }

        const res = await response.json();
        if (!response.ok || !res.success) {
          alert(res.message || 'Lỗi khi xuất kho.');
          btnExportToWip.disabled = false;
          btnExportToWip.innerHTML = 'Xuất kho gửi về kho sản xuất (WIP)';
          return;
        }

        alert('✅ ' + res.message);
        if (res.requestId && confirm('Bạn có muốn chuyển sang trang Tính nhu cầu NVL (MRP) ngay bây giờ để kiểm tra số lượng thiếu đã về 0 không?')) {
          window.location.href = `/Productions/Home/MaterialRequirement?requestId=${res.requestId}`;
        } else {
          window.location.reload();
        }
      } catch (err) {
        alert('Lỗi kết nối: ' + err.message);
        btnExportToWip.disabled = false;
        btnExportToWip.innerHTML = 'Xuất kho gửi về kho sản xuất (WIP)';
      }
    });
  }
}

// List search is initialized independently by list-search.js.

