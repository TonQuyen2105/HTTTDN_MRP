/**
 * PRODUCTIONS - HANDOVER JS
 * Quản lý nghiệp vụ Bàn giao thành phẩm:
 * 1. Chuyển tab "Chờ bàn giao" & "Phiếu bàn giao".
 * 2. Tìm kiếm, lọc trạng thái phiếu.
 * 3. Modal lập và gửi phiếu bàn giao thành phẩm từng phần với Idempotency Key.
 */

document.addEventListener('DOMContentLoaded', () => {
  initHandoverTabs();

  initHandoverModal();
});

/**
 * 1. Chuyển tab giữa "Chờ bàn giao" và "Phiếu bàn giao"
 */
function initHandoverTabs() {
  const tabBtnPending = document.getElementById('tabBtnPending');
  const tabBtnSlips = document.getElementById('tabBtnSlips');
  const panePending = document.getElementById('tabPending');
  const paneSlips = document.getElementById('tabSlips');
  const slipsFilterOptions = document.getElementById('slipsFilterOptions');

  function switchTab(targetTab) {
    if (targetTab === 'slips') {
      tabBtnSlips?.classList.add('active');
      tabBtnPending?.classList.remove('active');
      paneSlips?.classList.add('active');
      panePending?.classList.remove('active');
      if (slipsFilterOptions) slipsFilterOptions.style.display = 'flex';
    } else {
      tabBtnPending?.classList.add('active');
      tabBtnSlips?.classList.remove('active');
      panePending?.classList.add('active');
      paneSlips?.classList.remove('active');
      if (slipsFilterOptions) slipsFilterOptions.style.display = 'none';
    }

    // Cập nhật tham số tab trên URL không làm reload trang
    const url = new URL(window.location.href);
    url.searchParams.set('tab', targetTab);
    window.history.replaceState({}, '', url);

    // Kích hoạt tìm kiếm lại cho tab hiện tại
    applyFilters();
  }

  if (tabBtnPending) {
    tabBtnPending.addEventListener('click', () => switchTab('pending'));
  }

  if (tabBtnSlips) {
    tabBtnSlips.addEventListener('click', () => switchTab('slips'));
  }
}

/**
 * 2. Tìm kiếm và lọc trạng thái
 */
function applyFilters() {
  window.ProductionListSearch?.refresh('handoverSearchInput');
}

/**
 * 3. Modal Lập phiếu bàn giao thành phẩm (Hỗ trợ bàn giao từng phần)
 */
function initHandoverModal() {
  const backdrop = document.getElementById('handoverModalBackdrop');
  const btnClose = document.getElementById('btnCloseModal');
  const btnCancel = document.getElementById('btnCancelModal');
  const form = document.getElementById('handoverCreateForm');
  if (!form) return;
  const quantityInput = document.getElementById('modalQuantityInput');
  const noteInput = document.getElementById('modalNoteInput');
  const charCounter = document.getElementById('noteCharCounter');
  const submitBtn = document.getElementById('btnSubmitHandover');
  const submitBtnText = document.getElementById('submitBtnText');

  let currentAvailable = 0;
  const productionSelect = document.getElementById('modalProductionLog');
  const productionHint = document.getElementById('modalProductionHint');
  function selectProduction() {
    const option = productionSelect.selectedOptions[0];
    currentAvailable = Number(option?.dataset.available || 0);
    quantityInput.max = currentAvailable;
    quantityInput.value = currentAvailable || '';
    submitBtn.disabled = currentAvailable <= 0;
    if (currentAvailable > 0) productionHint.textContent = `Hạn sử dụng: ${option.dataset.expiry}. Còn có thể bàn giao: ${currentAvailable.toLocaleString('vi-VN')}. Có thể nhập số lượng nhỏ hơn để giao một phần.`;
  }
  productionSelect.addEventListener('change', selectProduction);

  function closeModal() {
    if (backdrop) backdrop.classList.remove('show');
    if (form) form.reset();
    currentAvailable = 0;
  }

  if (btnClose) btnClose.addEventListener('click', closeModal);
  if (btnCancel) btnCancel.addEventListener('click', closeModal);

  if (backdrop) {
    backdrop.addEventListener('click', (e) => {
      if (e.target === backdrop) closeModal();
    });
  }

  // Bắt sự kiện bấm nút "Lập bàn giao" trên từng dòng bảng Chờ bàn giao
  document.addEventListener('click', (e) => {
    const btn = e.target.closest('.btn-open-handover-modal');
    if (!btn || btn.classList.contains('disabled')) return;

    const reqId = parseInt(btn.getAttribute('data-reqid') || '0', 10);
    const prodId = parseInt(btn.getAttribute('data-prodid') || '0', 10);
    const reqCode = btn.getAttribute('data-reqcode') || '—';
    const prodName = btn.getAttribute('data-prodname') || '—';
    const unit = btn.getAttribute('data-unit') || 'Hộp';
    const available = parseInt(btn.getAttribute('data-available') || '0', 10);
    const produced = parseInt(btn.getAttribute('data-produced') || '0', 10);
    const received = parseInt(btn.getAttribute('data-received') || '0', 10);
    const pending = parseInt(btn.getAttribute('data-pending') || '0', 10);

    currentAvailable = available;

    // Gán dữ liệu vào modal
    document.getElementById('modalRequestId').value = reqId;
    document.getElementById('modalProductId').value = prodId;
    document.getElementById('modalRequestCode').textContent = reqCode;
    document.getElementById('modalProductName').textContent = prodName;
    document.getElementById('modalUnit').textContent = unit;
    document.getElementById('modalTotalProduced').textContent = `${produced.toLocaleString('vi-VN')} ${unit}`;
    document.getElementById('modalWarehouseReceived').textContent = `${received.toLocaleString('vi-VN')} ${unit}`;
    document.getElementById('modalPendingHandover').textContent = `${pending.toLocaleString('vi-VN')} ${unit}`;
    document.getElementById('modalAvailableQty').textContent = `${available.toLocaleString('vi-VN')} ${unit}`;

    // Tạo mã Idempotency Key mới cho phiên thao tác này
    const ik = (window.crypto && crypto.randomUUID) ? crypto.randomUUID() : 'ik_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);
    document.getElementById('modalIdempotencyKey').value = ik;

    // Cài đặt giới hạn input số lượng
    if (quantityInput) {
      quantityInput.max = available;
      quantityInput.value = available; // Mặc định điền toàn bộ, người dùng có thể chỉnh sửa giảm đi
    }

    if (noteInput) {
      noteInput.value = '';
    }
    if (charCounter) {
      charCounter.textContent = '0/255 ký tự';
    }

    productionSelect.replaceChildren();
    const choices = JSON.parse(btn.dataset.options || '[]');
    for (const item of choices) {
      const option = new Option(`GN-${item.LogId} · ${item.ProducedDate} · HSD: ${item.ExpiryDate} · Còn ${item.Available.toLocaleString('vi-VN')} ${unit}`, item.LogId);
      option.dataset.available = item.Available;
      option.dataset.expiry = item.ExpiryDate;
      productionSelect.add(option);
    }
    productionHint.textContent = btn.dataset.warning || 'Chưa có lần sản xuất còn số lượng để bàn giao. Dữ liệu cũ cần được đối soát.';
    selectProduction();

    // Mở modal
    if (backdrop) backdrop.classList.add('show');
    if (quantityInput) {
      setTimeout(() => quantityInput.focus(), 100);
    }
  });

  // Nút điền nhanh tỷ lệ phần trăm (25%, 50%, 100%)
  const quickPctBtns = document.querySelectorAll('.btn-quick-pct');
  quickPctBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      if (currentAvailable <= 0 || !quantityInput) return;
      const pct = parseInt(btn.getAttribute('data-pct') || '100', 10);
      let calculated = Math.round((currentAvailable * pct) / 100);
      if (calculated <= 0 && currentAvailable > 0) calculated = 1;
      if (calculated > currentAvailable) calculated = currentAvailable;
      quantityInput.value = calculated;
      quantityInput.focus();
    });
  });

  // Đếm ký tự ghi chú
  if (noteInput && charCounter) {
    noteInput.addEventListener('input', () => {
      charCounter.textContent = `${noteInput.value.length}/255 ký tự`;
    });
  }

  // Gửi Form lập phiếu bàn giao
  if (form) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const reqId = parseInt(document.getElementById('modalRequestId').value || '0', 10);
      const prodId = parseInt(document.getElementById('modalProductId').value || '0', 10);
      const qty = Number(quantityInput.value);
      const note = noteInput ? noteInput.value.trim() : '';
      const ik = document.getElementById('modalIdempotencyKey').value;

      if (reqId <= 0 || prodId <= 0) {
        alert('Thông tin đơn hàng hoặc sản phẩm không hợp lệ.');
        return;
      }

      if (!Number.isInteger(qty) || qty <= 0 || !productionSelect.value) {
        alert('Vui lòng nhập số lượng bàn giao hợp lệ (phải là số nguyên dương lớn hơn 0).');
        quantityInput.focus();
        return;
      }

      if (qty > currentAvailable) {
        alert(`Số lượng bàn giao (${qty.toLocaleString('vi-VN')}) vượt quá số lượng có thể bàn giao hiện tại (${currentAvailable.toLocaleString('vi-VN')})!`);
        quantityInput.focus();
        return;
      }

      // Lấy Antiforgery Token
      const tokenElement = form.querySelector('input[name="__RequestVerificationToken"]');
      const token = tokenElement ? tokenElement.value : '';

      // Khóa nút submit
      if (submitBtn) submitBtn.disabled = true;
      if (submitBtnText) submitBtnText.textContent = 'Đang gửi phiếu...';

      try {
        const payload = {
          ProductionLogId: Number(productionSelect.value),
          RequestId: reqId,
          ProductId: prodId,
          Quantity: qty,
          Note: note,
          IdempotencyKey: ik
        };

        const res = await fetch('/Productions/Home/CreateHandoverSlip', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
          },
          body: JSON.stringify(payload)
        });

        const data = await res.json();

        if (res.ok && data.success) {
          alert(`Thành công: ${data.message}`);
          closeModal();
          // Chuyển sang Tab "Phiếu bàn giao" để người dùng xem ngay phiếu mới gửi
          window.location.href = '/Productions/Home/HandoverList?tab=slips';
        } else {
          alert(`Không thể lập phiếu: ${data.message || 'Lỗi không xác định từ máy chủ.'}`);
          if (submitBtn) submitBtn.disabled = false;
          if (submitBtnText) submitBtnText.textContent = 'Gửi phiếu bàn giao';
        }
      } catch (err) {
        console.error('Lỗi khi gửi phiếu bàn giao:', err);
        alert('Lỗi kết nối mạng hoặc máy chủ không phản hồi. Vui lòng thử lại.');
        if (submitBtn) submitBtn.disabled = false;
        if (submitBtnText) submitBtnText.textContent = 'Gửi phiếu bàn giao';
      }
    });
  }
}
