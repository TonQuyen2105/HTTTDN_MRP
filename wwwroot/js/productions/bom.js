/**
 * PRODUCTIONS - BOM JS
 * Tương tác form Thêm & Sửa Định mức nguyên vật liệu (BOM)
 */

document.addEventListener('DOMContentLoaded', () => {
  initBomMaterialsTable();
  initBomByProductsTable();
  initBomFormActions();
  initBomListFilter();
});

/**
 * Xử lý thêm, xoá dòng trong Danh sách nguyên vật liệu của BOM
 */
function initBomMaterialsTable() {
  const tableBody = document.getElementById('bomMaterialsBody');
  const btnAdd = document.getElementById('btnAddMaterialRow');
  const btnClear = document.getElementById('btnClearMaterialRows');
  const totalCount = document.getElementById('totalMaterialsCount');

  function updateRowNumbers() {
    if (!tableBody) return;
    const rows = tableBody.querySelectorAll('tr');
    rows.forEach((row, idx) => {
      const stt = row.querySelector('.col-stt');
      if (stt) stt.textContent = idx + 1;
    });
    if (totalCount) totalCount.textContent = rows.length;
  }

  if (btnAdd && tableBody) {
    btnAdd.addEventListener('click', () => {
      const newIdx = tableBody.querySelectorAll('tr').length + 1;
      const row = document.createElement('tr');
      row.className = 'page-fade-in';
      row.innerHTML = `
        <td class="text-center col-stt">${newIdx}</td>
        <td><input type="text" class="form-input" placeholder="Mã NVL..." style="height:26px; font-size:12px;"></td>
        <td><input type="text" class="form-input" placeholder="Tên nguyên vật liệu..." style="height:26px; font-size:12px;"></td>
        <td>Nguyên vật liệu</td>
        <td>ml</td>
        <td class="text-center"><input type="checkbox"></td>
        <td><input type="text" class="form-input text-right" value="100,0000" style="height:26px; font-size:12px;"></td>
        <td>Tỷ lệ cố định</td>
        <td class="text-right">0,0000</td>
        <td class="text-right">1,00</td>
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
      updateRowNumbers();
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
            updateRowNumbers();
          }, 200);
        }
      }
    });
  }

  if (btnClear && tableBody) {
    btnClear.addEventListener('click', () => {
      if (confirm('Bạn có chắc chắn muốn xóa tất cả nguyên vật liệu?')) {
        tableBody.innerHTML = '';
        updateRowNumbers();
      }
    });
  }
}

/**
 * Xử lý thêm, xoá dòng trong Bảng Phụ phẩm
 */
function initBomByProductsTable() {
  const tableBody = document.getElementById('byProductsBody');
  const btnAdd = document.getElementById('btnAddByProductRow');
  const btnClear = document.getElementById('btnClearByProductRows');

  if (btnAdd && tableBody) {
    btnAdd.addEventListener('click', () => {
      const newIdx = tableBody.querySelectorAll('tr').length + 1;
      const row = document.createElement('tr');
      row.className = 'page-fade-in';
      row.innerHTML = `
        <td class="text-center col-stt">${newIdx}</td>
        <td><input type="text" class="form-input" placeholder="Mã phụ phẩm..." style="height:26px; font-size:12px;"></td>
        <td><input type="text" class="form-input" placeholder="Tên phụ phẩm..." style="height:26px; font-size:12px;"></td>
        <td>kg</td>
        <td><input type="text" class="form-input text-right" value="1,0000" style="height:26px; font-size:12px;"></td>
        <td>Công đoạn đóng gói</td>
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
    });
  }

  if (tableBody) {
    tableBody.addEventListener('click', (e) => {
      const deleteBtn = e.target.closest('.btn-delete-row');
      if (deleteBtn) {
        const row = deleteBtn.closest('tr');
        if (row) row.remove();
      }
    });
  }

  if (btnClear && tableBody) {
    btnClear.addEventListener('click', () => {
      if (confirm('Bạn có chắc chắn muốn xóa hết dòng phụ phẩm?')) {
        tableBody.innerHTML = '';
      }
    });
  }
}

/**
 * Xử lý các nút Lưu / Hủy trên form
 */
function initBomFormActions() {
  const btnCancel = document.getElementById('btnCancelBom');
  const btnClose = document.getElementById('btnCloseBom');
  const btnSave = document.getElementById('btnSaveBom');

  function returnToList() {
    window.location.href = '/Productions/Home/BomList';
  }

  if (btnCancel) btnCancel.addEventListener('click', returnToList);
  if (btnClose) btnClose.addEventListener('click', returnToList);

  if (btnSave) {
    btnSave.addEventListener('click', (e) => {
      e.preventDefault();
      alert('Đã lưu Định mức nguyên vật liệu (BOM) thành công!');
      returnToList();
    });
  }
}

/**
 * Lọc tìm kiếm trên trang Danh sách BOM
 */
function initBomListFilter() {
  const searchInput = document.getElementById('bomSearchInput');
  const tableRows = document.querySelectorAll('#bomListTableBody tr');

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
