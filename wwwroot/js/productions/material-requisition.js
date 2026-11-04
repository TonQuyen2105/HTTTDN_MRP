/**
 * PRODUCTIONS - MATERIAL REQUISITION JS
 * Xử lý tương tác Yêu cầu xuất vật tư (Thêm/xóa dòng, tìm kiếm, lưu phiếu)
 */

document.addEventListener('DOMContentLoaded', () => {
  initMRTable();
  initMRFormActions();
  initMRListSearch();
});

/**
 * Xử lý thêm, xóa dòng và tính toán thành tiền trong bảng chi tiết vật tư
 */
function initMRTable() {
  const tableBody = document.getElementById('mrTableBody');
  const btnAdd = document.getElementById('btnAddMRRow');
  const btnClear = document.getElementById('btnClearMRRows');
  const totalCount = document.getElementById('totalMRCount');

  function updateTotals() {
    if (!tableBody) return;
    const rows = tableBody.querySelectorAll('tr');
    if (totalCount) totalCount.textContent = rows.length;
  }

  if (btnAdd && tableBody) {
    btnAdd.addEventListener('click', () => {
      const row = document.createElement('tr');
      row.className = 'page-fade-in';
      const idx = tableBody.querySelectorAll('tr').length + 1;
      row.innerHTML = `
        <td class="text-center">${idx}</td>
        <td><input type="text" class="form-input" placeholder="Mã NVL..." style="height:26px; font-size:12px; width:100px;"></td>
        <td><input type="text" class="form-input" placeholder="Tên nguyên vật liệu..." style="height:26px; font-size:12px;"></td>
        <td>kg</td>
        <td class="text-right">50,000</td>
        <td class="text-right"><input type="text" class="form-input text-right val-req" value="50,000" style="height:26px; font-size:12px; font-weight:600; width:100px;"></td>
        <td class="text-center">
          <button type="button" class="row-act-btn btn-delete-row" title="Xóa dòng" style="border:none; background:transparent;">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"></polyline>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            </svg>
          </button>
        </td>
      `;
      tableBody.appendChild(row);
      updateTotals();
    });
  }

  if (tableBody) {
    tableBody.addEventListener('click', (e) => {
      const deleteBtn = e.target.closest('.btn-delete-row');
      if (deleteBtn) {
        const row = deleteBtn.closest('tr');
        if (row) {
          row.style.opacity = '0';
          row.style.transform = 'translateX(20px)';
          row.style.transition = 'all 0.2s ease';
          setTimeout(() => {
            row.remove();
            updateTotals();
          }, 200);
        }
      }
    });
  }

  if (btnClear && tableBody) {
    btnClear.addEventListener('click', () => {
      if (confirm('Bạn có chắc chắn muốn xóa tất cả nguyên vật liệu trong danh sách yêu cầu?')) {
        tableBody.innerHTML = '';
        updateTotals();
      }
    });
  }
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
      alert('Đã lập Phiếu yêu cầu xuất vật tư thành công!');
      returnToList();
    });
  }

  if (btnSaveAndNew) {
    btnSaveAndNew.addEventListener('click', (e) => {
      e.preventDefault();
      alert('Đã lưu thành công! Đang mở phiếu yêu cầu xuất vật tư mới...');
      location.reload();
    });
  }
}

/**
 * Tìm kiếm trên trang danh sách yêu cầu xuất vật tư
 */
function initMRListSearch() {
  const searchInput = document.getElementById('mrSearchInput');
  const tableRows = document.querySelectorAll('#mrListBody tr');

  if (searchInput && tableRows.length) {
    searchInput.addEventListener('input', (e) => {
      const keyword = e.target.value.toLowerCase().trim();
      tableRows.forEach((row) => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(keyword) ? '' : 'none';
      });
    });
  }
}
