/**
 * PRODUCTIONS - PRODUCTION ORDER JS
 * Xử lý tương tác trang Lệnh sản xuất thuốc GMP (Danh sách & Lập lệnh mới)
 */

document.addEventListener('DOMContentLoaded', () => {
  initViewToggle();
  initOrderSearchAndFilter();
  initTabs();
  initTableActions();
  initFormButtons();
});

/**
 * Xử lý chuyển đổi giữa Chế độ xem: Danh sách Lệnh SX <-> Form Lập Lệnh mới
 */
function initViewToggle() {
  const tabList = document.getElementById('tabOrderList');
  const tabCreate = document.getElementById('tabOrderCreate');
  const viewList = document.getElementById('orderListView');
  const viewForm = document.getElementById('orderFormView');
  const btnOpenCreate = document.getElementById('btnOpenCreateFromList');
  const btnBackToList = document.getElementById('btnBackToList');

  function showList() {
    if (viewList) viewList.style.display = 'block';
    if (viewForm) viewForm.style.display = 'none';
    if (tabList) tabList.classList.add('active');
    if (tabCreate) tabCreate.classList.remove('active');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function showForm() {
    if (viewList) viewList.style.display = 'none';
    if (viewForm) viewForm.style.display = 'block';
    if (tabList) tabList.classList.remove('active');
    if (tabCreate) tabCreate.classList.add('active');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  if (tabList) tabList.addEventListener('click', showList);
  if (tabCreate) tabCreate.addEventListener('click', showForm);
  if (btnOpenCreate) btnOpenCreate.addEventListener('click', showForm);
  if (btnBackToList) btnBackToList.addEventListener('click', showList);

  const btnCancel = document.getElementById('btnCancelOrder');
  if (btnCancel) {
    btnCancel.addEventListener('click', (e) => {
      e.preventDefault();
      showList();
    });
  }
}

/**
 * Lọc và tìm kiếm danh sách Lệnh sản xuất (Table filter)
 */
function initOrderSearchAndFilter() {
  const searchInput = document.getElementById('orderSearchInput');
  const statusFilter = document.getElementById('statusFilterSelect');
  const tableBody = document.getElementById('orderListTableBody');

  if (!tableBody) return;

  function applyFilter() {
    const query = searchInput ? searchInput.value.toLowerCase().trim() : '';
    const status = statusFilter ? statusFilter.value : '';
    const rows = tableBody.querySelectorAll('tr');

    rows.forEach(row => {
      const rowStatus = row.getAttribute('data-status') || '';
      const rowName = row.getAttribute('data-name') || '';
      const rowCode = row.getAttribute('data-code') || '';
      const text = row.textContent.toLowerCase();

      const matchesQuery = !query || text.includes(query) || rowName.includes(query) || rowCode.includes(query);
      const matchesStatus = !status || rowStatus.toLowerCase() === status.toLowerCase();

      if (matchesQuery && matchesStatus) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  if (searchInput) searchInput.addEventListener('input', applyFilter);
  if (statusFilter) statusFilter.addEventListener('change', applyFilter);
}

/**
 * Xử lý chuyển đổi các Tab con trong Form (Chi tiết NVL, Công đoạn, Phụ phẩm, Đính kèm)
 */
function initTabs() {
  const tabs = document.querySelectorAll('.order-tab-link');

  tabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      tabs.forEach((t) => t.classList.remove('active'));
      tab.classList.add('active');
    });
  });
}

/**
 * Xử lý thêm, xoá dòng trong bảng nguyên vật liệu
 */
function initTableActions() {
  const tableBody = document.getElementById('bomTableBody');
  const btnAddRow = document.getElementById('btnAddRow');
  const btnClearRows = document.getElementById('btnClearRows');
  const totalRowsCount = document.getElementById('totalRowsCount');

  // Hàm cập nhật STT và Tổng số dòng
  function updateRowNumbers() {
    if (!tableBody) return;
    const rows = tableBody.querySelectorAll('tr');
    rows.forEach((row, index) => {
      const sttCell = row.querySelector('.col-stt');
      if (sttCell) sttCell.textContent = index + 1;
    });

    if (totalRowsCount) {
      totalRowsCount.textContent = rows.length;
    }
  }

  // Thêm dòng mới
  if (btnAddRow && tableBody) {
    btnAddRow.addEventListener('click', () => {
      const newIndex = tableBody.querySelectorAll('tr').length + 1;
      const newRow = document.createElement('tr');
      newRow.className = 'page-fade-in';
      newRow.innerHTML = `
        <td class="text-center col-stt">${newIndex}</td>
        <td><input type="text" class="form-input" placeholder="Mã NVL..." style="height:26px; font-size:12px;"></td>
        <td><input type="text" class="form-input" placeholder="Tên hoạt chất / tá dược..." style="height:26px; font-size:12px;"></td>
        <td>kg</td>
        <td class="text-center"><input type="checkbox"></td>
        <td class="text-right">1,0000</td>
        <td class="text-right">1,000</td>
        <td class="text-center">
          <div class="row-actions-cell">
            <button type="button" class="row-act-btn btn-delete-row" title="Xóa dòng">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"></polyline>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
              </svg>
            </button>
          </div>
        </td>
      `;

      tableBody.appendChild(newRow);
      updateRowNumbers();
    });
  }

  // Xoá 1 dòng
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

  // Xoá hết dòng
  if (btnClearRows && tableBody) {
    btnClearRows.addEventListener('click', () => {
      if (confirm('Bạn có chắc chắn muốn xóa hết các dòng nguyên vật liệu?')) {
        tableBody.innerHTML = '';
        updateRowNumbers();
      }
    });
  }
}

/**
 * Xử lý các nút Lưu / Submit Lệnh sản xuất
 */
function initFormButtons() {
  const btnSubmit = document.getElementById('btnSubmitOrder');

  if (btnSubmit) {
    btnSubmit.addEventListener('click', (e) => {
      e.preventDefault();
      alert('Chức năng tạo lệnh sản xuất thủ công chưa được triển khai.');
    });
  }
}
