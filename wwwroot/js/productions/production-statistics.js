/**
 * PRODUCTIONS - PRODUCTION STATISTICS JS
 * Xử lý tương tác Phiếu thống kê sản xuất & Danh sách phiếu
 */

document.addEventListener('DOMContentLoaded', () => {
  initStatsDetailTable();
  initStatsSubTable();
  initStatsFormActions();
  initStatsListSearch();
});

/**
 * Xử lý thêm, xoá dòng trong Bảng Chi tiết thống kê
 */
function initStatsDetailTable() {
  const tableBody = document.getElementById('statsDetailBody');
  const btnAdd = document.getElementById('btnAddStatsRow');
  const btnClear = document.getElementById('btnClearStatsRows');
  const totalCount = document.getElementById('totalStatsCount');

  function updateCount() {
    if (!tableBody) return;
    const rows = tableBody.querySelectorAll('tr');
    if (totalCount) totalCount.textContent = rows.length;
  }

  if (btnAdd && tableBody) {
    btnAdd.addEventListener('click', () => {
      const row = document.createElement('tr');
      row.className = 'page-fade-in';
      row.innerHTML = `
        <td><input type="text" class="form-input" placeholder="Mã NV..." style="height:26px; font-size:12px;"></td>
        <td><input type="text" class="form-input" placeholder="Tên nhân viên..." style="height:26px; font-size:12px;"></td>
        <td>Hộp</td>
        <td class="text-right"><input type="number" class="form-input text-right" placeholder="0" style="height:26px; font-size:12px;"></td>
        <td class="text-right"><input type="number" class="form-input text-right" placeholder="0" style="height:26px; font-size:12px; color:#2ea664;"></td>
        <td class="text-right"><input type="number" class="form-input text-right" placeholder="0" style="height:26px; font-size:12px; color:#e54848;"></td>
        <td><input type="text" class="form-input" placeholder="Lý do lỗi..." style="height:26px; font-size:12px;"></td>
        <td><input type="text" class="form-input" placeholder="Số lô..." style="height:26px; font-size:12px;"></td>
        <td><input type="text" class="form-input" placeholder="dd/mm/yyyy" style="height:26px; font-size:12px;"></td>
        <td class="text-center">
          <button type="button" class="row-act-btn btn-delete-row" title="Xóa dòng">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"></polyline>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            </svg>
          </button>
        </td>
      `;
      tableBody.appendChild(row);
      updateCount();
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
            updateCount();
          }, 200);
        }
      }
    });
  }

  if (btnClear && tableBody) {
    btnClear.addEventListener('click', () => {
      if (confirm('Bạn có chắc chắn muốn xóa tất cả chi tiết thống kê?')) {
        tableBody.innerHTML = '';
        updateCount();
      }
    });
  }
}

/**
 * Xử lý thêm, xóa trong bảng phụ phẩm
 */
function initStatsSubTable() {
  const tableBody = document.getElementById('statsSubBody');
  if (tableBody) {
    tableBody.addEventListener('click', (e) => {
      const deleteBtn = e.target.closest('.btn-delete-row');
      if (deleteBtn) {
        const row = deleteBtn.closest('tr');
        if (row) row.remove();
      }
    });
  }
}

/**
 * Xử lý các nút Hủy, Lưu trên form
 */
function initStatsFormActions() {
  const btnCancel = document.getElementById('btnCancelStats');
  const btnClose = document.getElementById('btnCloseStats');
  const btnSave = document.getElementById('btnSaveStats');

  function returnToList() {
    window.location.href = '/Productions/Home/ProductionStatisticsList';
  }

  if (btnCancel) btnCancel.addEventListener('click', returnToList);
  if (btnClose) btnClose.addEventListener('click', returnToList);

  if (btnSave) {
    btnSave.addEventListener('click', (e) => {
      e.preventDefault();
      alert('Chức năng lập phiếu thống kê sản xuất thủ công chưa được triển khai backend.\nSản lượng thực tế được ghi nhận thông qua nút "Đánh dấu" thành phẩm từ màn hình Đơn đặt hàng thuốc.');
    });
  }

  // Xóa tag pill ca làm việc nếu bấm nút x
  const tagRemoveBtn = document.querySelector('.tag-remove');
  if (tagRemoveBtn) {
    tagRemoveBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      const pill = tagRemoveBtn.closest('.tag-pill-sm');
      if (pill) pill.remove();
    });
  }
}

/**
 * Tìm kiếm trên trang danh sách phiếu thống kê
 */
function initStatsListSearch() {
  const searchInput = document.getElementById('statsSearchInput');
  const tableRows = document.querySelectorAll('#statsListBody tr');

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
