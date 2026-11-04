/**
 * JAVASCRIPT CHO TRANG TÍNH NHU CẦU NGUYÊN VẬT LIỆU (MRP)
 * Quản lý tính toán nhu cầu, kiểm tra tồn kho, đối chiếu BOM và lập phiếu gửi kho
 */

document.addEventListener('DOMContentLoaded', function () {
  // 1. Khởi tạo các phần tử DOM
  const selectRequest = document.getElementById('selectProductionRequest');
  const inputOrderCode = document.getElementById('inputOrderCode');
  const selectProduct = document.getElementById('selectProduct');
  const inputTargetQuantity = document.getElementById('inputTargetQuantity');
  const spanProductUnit = document.getElementById('spanProductUnit');
  const selectBom = document.getElementById('selectBom');
  const inputStandardOutput = document.getElementById('inputStandardOutput');
  const bomUnitLabel = document.getElementById('bomUnitLabel');
  const inputRequiredDate = document.getElementById('inputRequiredDate');
  const inputNote = document.getElementById('inputNote');

  // Nút hành động
  const btnCalculate = document.getElementById('btnCalculate');
  const btnRefreshStock = document.getElementById('btnRefreshStock');
  const btnViewBom = document.getElementById('btnViewBom');
  const btnCreateSlip = document.getElementById('btnCreateSlip');

  // Thẻ thông báo
  const noBomAlert = document.getElementById('noBomAlert');
  const trialAlert = document.getElementById('trialAlert');
  const existingSlipAlert = document.getElementById('existingSlipAlert');
  const existingSlipLink = document.getElementById('existingSlipLink');
  const outdatedAlert = document.getElementById('outdatedAlert');

  // Thẻ KPI
  const kpiTotalTypes = document.getElementById('kpiTotalTypes');
  const kpiSufficient = document.getElementById('kpiSufficient');
  const kpiShortage = document.getElementById('kpiShortage');
  const kpiConclusionBadge = document.getElementById('kpiConclusionBadge');

  // Bảng kết quả & Bộ lọc
  const tableBody = document.getElementById('mrpTableBody');
  const timestampText = document.getElementById('stockCheckedTimestamp');
  const filterAllBtn = document.getElementById('filterAll');
  const filterShortageOnlyBtn = document.getElementById('filterShortageOnly');
  const tableContainer = document.getElementById('mrpTableContainer');

  // Token bảo vệ Form (Anti-forgery)
  function getVerificationToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
  }

  let isOutdated = false;
  let currentFilter = 'all';

  // 2. Hàm đánh dấu dữ liệu đầu vào đã thay đổi
  function markOutdated() {
    isOutdated = true;
    if (outdatedAlert) outdatedAlert.style.display = 'flex';
    if (tableContainer) tableContainer.classList.add('mrp-outdated-watermark');
    if (btnCreateSlip) {
      btnCreateSlip.disabled = true;
      btnCreateSlip.title = 'Vui lòng bấm "Tính nhu cầu" để cập nhật dữ liệu trước khi lập phiếu';
    }
  }

  function clearOutdated() {
    isOutdated = false;
    if (outdatedAlert) outdatedAlert.style.display = 'none';
    if (tableContainer) tableContainer.classList.remove('mrp-outdated-watermark');
  }

  // 3. Sự kiện thay đổi Yêu cầu sản xuất nguồn
  if (selectRequest) {
    selectRequest.addEventListener('change', async function () {
      const requestId = parseInt(this.value, 10);
      markOutdated();

      if (!requestId || isNaN(requestId) || requestId <= 0) {
        // Chế độ tính thử độc lập
        if (inputOrderCode) inputOrderCode.value = '';
        if (trialAlert) trialAlert.style.display = 'flex';
        if (existingSlipAlert) existingSlipAlert.style.display = 'none';
        return;
      }

      if (trialAlert) trialAlert.style.display = 'none';

      try {
        const response = await fetch(`/Productions/Home/GetRequestDetails?requestId=${requestId}`);
        if (!response.ok) throw new Error('Không thể tải thông tin yêu cầu.');
        const data = await response.json();

        // Điền sản phẩm & số lượng
        if (selectProduct) {
          selectProduct.value = data.productId;
        }
        if (inputTargetQuantity) {
          inputTargetQuantity.value = data.quantityRequired;
        }
        if (spanProductUnit) {
          spanProductUnit.textContent = data.unit || 'Hộp';
          if (bomUnitLabel) bomUnitLabel.textContent = data.unit || 'Hộp';
        }

        // Lệnh sản xuất liên quan
        if (inputOrderCode) {
          if (data.isApprovedOrder) {
            inputOrderCode.value = data.productionOrderCode;
            inputOrderCode.style.color = '#1e40af';
            inputOrderCode.style.fontWeight = '700';
          } else {
            inputOrderCode.value = `(Chưa duyệt LSX - Trạng thái: ${data.status})`;
            inputOrderCode.style.color = '#d97706';
            inputOrderCode.style.fontWeight = '600';
          }
        }

        // Cập nhật cảnh báo phiếu đã lập nếu có
        if (data.hasExistingSlip) {
          if (existingSlipAlert) existingSlipAlert.style.display = 'flex';
          if (existingSlipLink) {
            existingSlipLink.href = `/Productions/Home/MaterialRequisitionCreate?id=${data.existingReceiptId}`;
            existingSlipLink.textContent = `Xem phiếu ${data.existingReceiptCode} (${data.existingReceiptStatus})`;
          }
        } else {
          if (existingSlipAlert) existingSlipAlert.style.display = 'none';
        }

        // Cập nhật danh sách BOM
        updateBomDropdown(data.boms, data.hasBom);

        if (inputNote) {
          inputNote.value = data.isApprovedOrder
            ? `Cấp phát nguyên vật liệu cho ${data.productionOrderCode} (${data.productName})`
            : `Tính nhu cầu nguyên vật liệu cho YCSX-${data.requestId} (${data.productName})`;
        }

        // Tự động tính toán lại
        if (data.hasBom) {
          calculateRequirement();
        }
      } catch (err) {
        console.error('Lỗi khi tải yêu cầu:', err);
      }
    });
  }

  // 4. Sự kiện thay đổi Sản phẩm
  if (selectProduct) {
    selectProduct.addEventListener('change', async function () {
      const productId = parseInt(this.value, 10);
      markOutdated();

      if (!productId || isNaN(productId) || productId <= 0) return;

      const selectedOpt = selectProduct.options[selectProduct.selectedIndex];
      const unit = selectedOpt ? selectedOpt.getAttribute('data-unit') : 'Hộp';
      if (spanProductUnit) spanProductUnit.textContent = unit;
      if (bomUnitLabel) bomUnitLabel.textContent = unit;

      try {
        const response = await fetch(`/Productions/Home/GetProductBoms?productId=${productId}`);
        if (!response.ok) throw new Error('Không thể tải định mức BOM.');
        const data = await response.json();

        updateBomDropdown(data.boms, data.hasBom);

        if (data.hasBom) {
          calculateRequirement();
        }
      } catch (err) {
        console.error('Lỗi khi tải BOM của sản phẩm:', err);
      }
    });
  }

  // Cập nhật danh sách BOM trong thẻ select
  function updateBomDropdown(boms, hasBom) {
    if (!selectBom) return;
    selectBom.innerHTML = '';

    if (!hasBom || !boms || boms.length === 0) {
      const opt = document.createElement('option');
      opt.value = '';
      opt.textContent = '-- Sản phẩm này chưa có BOM --';
      selectBom.appendChild(opt);

      if (noBomAlert) noBomAlert.style.display = 'flex';
      if (btnCalculate) btnCalculate.disabled = true;
      if (btnCreateSlip) btnCreateSlip.disabled = true;
      if (inputStandardOutput) inputStandardOutput.value = '1';
      return;
    }

    if (noBomAlert) noBomAlert.style.display = 'none';
    if (btnCalculate) btnCalculate.disabled = false;

    boms.forEach((b, idx) => {
      const opt = document.createElement('option');
      opt.value = b.bomCode;
      opt.textContent = `${b.bomCode} - ${b.bomName}`;
      opt.setAttribute('data-output', b.standardOutput);
      opt.setAttribute('data-unit', b.unit);
      if (idx === 0) opt.selected = true;
      selectBom.appendChild(opt);
    });

    if (boms.length > 0 && inputStandardOutput) {
      inputStandardOutput.value = 1;
    }
  }

  // 5. Sự kiện thay đổi BOM
  if (selectBom) {
    selectBom.addEventListener('change', function () {
      markOutdated();
      const opt = selectBom.options[selectBom.selectedIndex];
      if (opt && opt.getAttribute('data-output') && inputStandardOutput) {
        inputStandardOutput.value = 1;
      }
    });
  }

  // Sự kiện khi người dùng sửa số lượng hoặc BOM output
  if (inputTargetQuantity) {
    inputTargetQuantity.addEventListener('input', markOutdated);
  }
  if (inputStandardOutput) {
    inputStandardOutput.addEventListener('input', markOutdated);
  }
  if (inputNote) {
    inputNote.addEventListener('input', function () {});
  }

  // 6. Nút Tính nhu cầu
  if (btnCalculate) {
    btnCalculate.addEventListener('click', function () {
      calculateRequirement();
    });
  }

  // Nút Kiểm tra lại tồn kho
  if (btnRefreshStock) {
    btnRefreshStock.addEventListener('click', function () {
      calculateRequirement(true);
    });
  }

  // Nút Xem BOM
  if (btnViewBom) {
    btnViewBom.addEventListener('click', function () {
      const productId = selectProduct ? selectProduct.value : '';
      if (productId) {
        window.open(`/Productions/Home/BomList`, '_blank');
      } else {
        window.open(`/Productions/Home/BomList`, '_blank');
      }
    });
  }

  // 7. Hàm gửi AJAX tính nhu cầu nguyên vật liệu trên Server
  async function calculateRequirement(isRefreshNotice = false) {
    const productId = parseInt(selectProduct?.value || '0', 10);
    const targetQty = parseFloat(inputTargetQuantity?.value || '0');
    const bomCode = selectBom?.value || '';
    const stdOutput = 1;
    const requestId = parseInt(selectRequest?.value || '0', 10);
    const orderCode = inputOrderCode?.value || '';

    if (productId <= 0) {
      alert('Vui lòng chọn sản phẩm cần sản xuất.');
      return;
    }

    if (isNaN(targetQty) || targetQty <= 0) {
      alert('Số lượng thành phẩm cần sản xuất phải lớn hơn 0.');
      inputTargetQuantity?.focus();
      return;
    }

    if (isNaN(stdOutput) || stdOutput <= 0) {
      alert('Sản lượng chuẩn của BOM phải lớn hơn 0.');
      inputStandardOutput?.focus();
      return;
    }

    if (!bomCode) {
      alert('Sản phẩm chưa có BOM. Vui lòng chọn sản phẩm có BOM hoặc tạo BOM trước.');
      return;
    }

    // Hiệu ứng loading
    if (btnCalculate) {
      btnCalculate.disabled = true;
      btnCalculate.innerHTML = `
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
          <circle cx="12" cy="12" r="10" stroke-opacity="0.25"></circle>
          <path d="M12 2a10 10 0 0 1 10 10"></path>
        </svg> Đang tính...`;
    }

    try {
      const payload = {
        requestId: requestId > 0 ? requestId : null,
        productionOrderCode: orderCode && !orderCode.includes('(Chưa') ? orderCode : null,
        productId: productId,
        targetQuantity: targetQty,
        bomCode: bomCode,
        bomStandardOutput: stdOutput,
        requiredDate: inputRequiredDate?.value || null,
        note: inputNote?.value || null
      };

      const response = await fetch('/Productions/Home/CalculateRequirementApi', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': getVerificationToken()
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.message || 'Lỗi khi tính toán trên máy chủ.');
      }

      const data = await response.json();

      if (data.errorMessage) {
        alert(data.errorMessage);
        return;
      }

      // Cập nhật kết quả lên giao diện
      renderCalculationResults(data);
      clearOutdated();

      if (isRefreshNotice) {
        showToast('Đã đối chiếu tồn kho mới nhất từ cơ sở dữ liệu.');
      }
    } catch (err) {
      alert(`Lỗi tính toán: ${err.message}`);
    } finally {
      if (btnCalculate) {
        btnCalculate.disabled = false;
        btnCalculate.innerHTML = `
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10"></polyline>
            <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
          </svg> Tính nhu cầu`;
      }
    }
  }

  // 8. Hàm hiển thị kết quả tính toán lên DOM
  function renderCalculationResults(data) {
    if (bomUnitLabel) bomUnitLabel.textContent = data.productUnit || 'Hộp';
    // 1. Cập nhật thẻ KPI
    if (kpiTotalTypes) kpiTotalTypes.textContent = data.totalMaterialTypes;
    if (kpiSufficient) kpiSufficient.textContent = data.sufficientCount;
    if (kpiShortage) kpiShortage.textContent = data.shortageCount;

    if (kpiConclusionBadge) {
      if (data.shortageCount === 0 && data.totalMaterialTypes > 0) {
        kpiConclusionBadge.className = 'mrp-conclusion-badge sufficient';
        kpiConclusionBadge.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="20 6 9 17 4 12"></polyline>
          </svg> ${data.conclusion}`;
      } else {
        kpiConclusionBadge.className = 'mrp-conclusion-badge shortage';
        kpiConclusionBadge.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg> ${data.conclusion}`;
      }
    }

    // 2. Thời điểm kiểm tra
    if (timestampText) {
      const now = new Date(data.checkedAt || Date.now());
      timestampText.textContent = now.toLocaleDateString('vi-VN') + ' ' + now.toLocaleTimeString('vi-VN');
    }

    // 3. Render bảng chi tiết nguyên vật liệu
    if (tableBody) {
      tableBody.innerHTML = '';

      if (!data.lines || data.lines.length === 0) {
        tableBody.innerHTML = `
          <tr>
            <td colspan="9" class="text-center" style="padding: 24px; color: #64748b;">
              Không có dữ liệu nguyên vật liệu nào trong định mức đã chọn.
            </td>
          </tr>`;
        return;
      }

      data.lines.forEach((line) => {
        const isShortage = line.shortage > 0;
        const isMismatch = line.isUnitMismatch;

        let badgeHtml = '';
        if (isMismatch) {
          badgeHtml = `<span class="badge-status-mismatch">Lệch ĐVT</span>`;
        } else if (isShortage) {
          badgeHtml = `<span class="badge-status-shortage">Thiếu tồn kho</span>`;
        } else {
          badgeHtml = `<span class="badge-status-sufficient">Đủ tồn kho</span>`;
        }

        const tr = document.createElement('tr');
        tr.className = 'mrp-data-row';
        tr.setAttribute('data-shortage', isShortage ? 'true' : 'false');

        tr.innerHTML = `
          <td class="text-center">${line.stt}</td>
          <td><strong style="color: #1e293b;">${line.ingrCode}</strong></td>
          <td>
            <strong>${line.ingrName}</strong>
            ${line.isDiscrete ? '<div style="font-size:11px; color:#64748b;">(Vật tư đếm rời - đã làm tròn lên)</div>' : ''}
          </td>
          <td>${line.unit}</td>
          <td class="text-right" style="color: #475569;">${formatNumber(line.bomNorm)} ${line.unit} / 1 ${data.productUnit || 'Hộp'}</td>
          <td class="text-right"><strong style="color: #0f172a; font-size: 13.5px;">${formatNumber(line.totalRequired)}</strong></td>
          <td class="text-right" style="color: ${isShortage ? '#dc2626' : '#16a34a'}; font-weight: 600;">${formatNumber(line.availableStock)}</td>
          <td class="text-right"><strong style="color: ${isShortage ? '#dc2626' : '#64748b'}; font-size: 13.5px;">${formatNumber(line.shortage)}</strong></td>
          <td class="text-center">${badgeHtml}</td>
        `;

        tableBody.appendChild(tr);
      });

      applyTableFilter();
    }

    // 4. Kích hoạt nút tạo phiếu nếu đã liên kết Lệnh sản xuất
    const orderCode = inputOrderCode?.value || '';
    const isLinkedOrder = orderCode && !orderCode.includes('(Chưa') && !orderCode.includes('Tính thử');

    if (btnCreateSlip) {
      if (isLinkedOrder) {
        btnCreateSlip.disabled = false;
        btnCreateSlip.title = 'Lập phiếu yêu cầu cấp nguyên vật liệu gửi kho';
      } else {
        btnCreateSlip.disabled = true;
        btnCreateSlip.title = 'Cần liên kết với Lệnh sản xuất đã duyệt trước khi tạo phiếu cấp chính thức';
      }
    }
  }

  // 9. Bộ lọc bảng (Tất cả / Chỉ thiếu)
  if (filterAllBtn) {
    filterAllBtn.addEventListener('click', function () {
      currentFilter = 'all';
      filterAllBtn.classList.add('active');
      if (filterShortageOnlyBtn) filterShortageOnlyBtn.classList.remove('active');
      applyTableFilter();
    });
  }

  if (filterShortageOnlyBtn) {
    filterShortageOnlyBtn.addEventListener('click', function () {
      currentFilter = 'shortage';
      filterShortageOnlyBtn.classList.add('active');
      if (filterAllBtn) filterAllBtn.classList.remove('active');
      applyTableFilter();
    });
  }

  function applyTableFilter() {
    const rows = document.querySelectorAll('.mrp-data-row');
    rows.forEach(row => {
      const isShortage = row.getAttribute('data-shortage') === 'true';
      if (currentFilter === 'shortage') {
        row.style.display = isShortage ? '' : 'none';
      } else {
        row.style.display = '';
      }
    });
  }

  // 10. Tạo phiếu yêu cầu cấp nguyên vật liệu gửi kho
  if (btnCreateSlip) {
    btnCreateSlip.addEventListener('click', async function () {
      const orderCode = inputOrderCode?.value || '';
      const isLinkedOrder = orderCode && !orderCode.includes('(Chưa') && !orderCode.includes('Tính thử');

      if (!isLinkedOrder) {
        alert('Chỉ được tạo phiếu cấp nguyên vật liệu chính thức khi đã liên kết với Lệnh sản xuất (Production Order) đã được duyệt.');
        return;
      }

      if (isOutdated) {
        alert('Dữ liệu đầu vào đã bị sửa đổi. Vui lòng bấm "Tính nhu cầu" để cập nhật lại trước khi tạo phiếu.');
        return;
      }

      const productId = parseInt(selectProduct?.value || '0', 10);
      const targetQty = parseFloat(inputTargetQuantity?.value || '0');
      const bomCode = selectBom?.value || '';
      const stdOutput = 1;
      const requestId = parseInt(selectRequest?.value || '0', 10);

      const confirmMsg = `Bạn có chắc chắn muốn lập Phiếu yêu cầu cấp nguyên vật liệu gửi kho cho lệnh: ${orderCode}?\n\nLưu ý: Phiếu sẽ yêu cầu cấp TOÀN BỘ lượng cần cho sản xuất. Tồn kho chỉ được trừ khi kho xác nhận xuất thực tế.`;
      if (!confirm(confirmMsg)) return;

      btnCreateSlip.disabled = true;
      btnCreateSlip.innerHTML = `
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
          <circle cx="12" cy="12" r="10" stroke-opacity="0.25"></circle>
          <path d="M12 2a10 10 0 0 1 10 10"></path>
        </svg> Đang lưu phiếu...`;

      try {
        const payload = {
          requestId: requestId > 0 ? requestId : null,
          productionOrderCode: orderCode,
          productId: productId,
          targetQuantity: targetQty,
          bomCode: bomCode,
          bomStandardOutput: stdOutput,
          requiredDate: inputRequiredDate?.value || null,
          note: inputNote?.value || null
        };

        const response = await fetch('/Productions/Home/CreateMaterialRequisitionFromMRP', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getVerificationToken()
          },
          body: JSON.stringify(payload)
        });

        const resData = await response.json();

        if (!response.ok || !resData.success) {
          if (resData.existingReceiptId) {
            if (confirm(`${resData.message}\n\nBạn có muốn mở xem phiếu này ngay không?`)) {
              window.location.href = `/Productions/Home/MaterialRequisitionCreate?id=${resData.existingReceiptId}`;
              return;
            }
          } else {
            alert(resData.message || 'Lỗi khi tạo phiếu yêu cầu cấp nguyên vật liệu.');
          }
          return;
        }

        alert(`✅ ${resData.message}\n\nSố phiếu: ${resData.receiptCode}`);
        if (resData.redirectUrl) {
          window.location.href = resData.redirectUrl;
        }
      } catch (err) {
        alert(`Lỗi hệ thống: ${err.message}`);
      } finally {
        btnCreateSlip.disabled = false;
        btnCreateSlip.innerHTML = `
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
            <polyline points="14 2 14 8 20 8"></polyline>
            <line x1="12" y1="18" x2="12" y2="12"></line>
            <line x1="9" y1="15" x2="15" y2="15"></line>
          </svg> Tạo phiếu yêu cầu cấp nguyên vật liệu`;
      }
    });
  }

  // 11. Hàm định dạng số
  function formatNumber(num) {
    if (num === null || num === undefined || isNaN(num)) return '0';
    return Number(num).toLocaleString('vi-VN', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 12
    });
  }

  // 12. Toast thông báo nhanh
  function showToast(message) {
    let toast = document.getElementById('mrpToast');
    if (!toast) {
      toast = document.createElement('div');
      toast.id = 'mrpToast';
      toast.style.cssText = 'position:fixed; bottom:24px; right:24px; background:#1e293b; color:#fff; padding:10px 18px; border-radius:6px; font-size:13px; z-index:9999; box-shadow:0 4px 12px rgba(0,0,0,0.15); transition:all 0.3s ease; opacity:0; transform:translateY(10px);';
      document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.style.opacity = '1';
    toast.style.transform = 'translateY(0)';
    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateY(10px)';
    }, 3000);
  }
});
