document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('bomEditorForm');
  if (!form) return;
  const body = document.getElementById('bomEditorLines');
  const product = document.getElementById('bomProduct');
  const message = document.getElementById('bomFormMessage');
  const save = document.getElementById('bomSaveButton');
  let dirty = false;
  const priceInput = document.getElementById('bomProductPrice');
  let originalPrice = form.dataset.originalPrice || '';
  const shelfLifeInput = document.getElementById('bomShelfLifeMonths');
  const descriptionInput = document.getElementById('bomDescription');
  const descriptions = JSON.parse(document.getElementById('bomProductDescriptions').textContent);
  let originalDescription = descriptions.find(p => p.id === Number(product.value))?.description ?? null;
  let originalShelfLife = form.dataset.originalShelfLife === '' ? null : Number(form.dataset.originalShelfLife);
  const money = value => value.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' đ';
  function updateMaterialTotal() {
    let total = 0, complete = body.rows.length > 0;
    [...body.rows].forEach(row => {
      const raw = row.querySelector('.bom-ingredient').selectedOptions[0]?.dataset.price;
      const price = raw == null || raw === '' ? null : Number(raw);
      const qty = Number(row.querySelector('.bom-quantity').value);
      const validPrice = price !== null && Number.isFinite(price) && price >= 0;
      row.querySelector('[data-material-price]').textContent = validPrice ? money(price) : 'Chưa có giá';
      const amount = validPrice && Number.isFinite(qty) && qty > 0 ? Math.round((price * qty + Number.EPSILON) * 100) / 100 : null;
      row.querySelector('[data-material-amount]').textContent = amount !== null ? money(amount) : '—';
      if (amount === null) complete = false; else total += amount;
    });
    document.getElementById('bomMaterialTotal').textContent = complete ? money(total) : 'Chưa đủ định mức hoặc đơn giá NVL';
  }
  const showError = text => { message.textContent = text; message.hidden = false; };
  function updateProductUnit() { document.getElementById('bomProductUnit').textContent = product.selectedOptions[0]?.dataset.unit || 'thành phẩm'; }
  function renumber() { [...body.rows].forEach((row, i) => row.querySelector('[data-row-number]').textContent = i + 1); document.getElementById('bomLineCount').textContent = body.rows.length; updateMaterialTotal(); }
  function addLine(line = {}) {
    const fragment = document.getElementById('bomLineTemplate').content.cloneNode(true);
    const row = fragment.querySelector('tr');
    const select = row.querySelector('.bom-ingredient');
    const quantity = row.querySelector('.bom-quantity');
    select.value = line.IngredientId || '';
    // Keep the original decimal text until the user changes it to avoid needless numeric rounding.
    quantity.value = line.Quantity ?? '';
    function syncUnit() { row.querySelector('[data-ingredient-unit]').textContent = select.selectedOptions[0]?.dataset.unit || '—'; }
    select.addEventListener('change', () => { syncUnit(); updateMaterialTotal(); });
    quantity.addEventListener('input', updateMaterialTotal);
    row.querySelector('.bom-remove-line').addEventListener('click', () => { if (save.disabled) return; row.remove(); renumber(); dirty = true; });
    syncUnit(); body.appendChild(fragment); renumber();
  }
  const initial = JSON.parse(document.getElementById('bomInitialLines').textContent);
  initial.forEach(addLine);
  if (!initial.length) addLine();
  updateProductUnit();
  product.addEventListener('change', () => { updateProductUnit(); originalPrice = product.selectedOptions[0]?.dataset.price || ''; priceInput.value = originalPrice;
    const previous = product.selectedOptions[0]?.dataset.shelfLife;
    originalShelfLife = previous == null || previous === '' ? null : Number(previous);
    shelfLifeInput.value = originalShelfLife > 0 ? originalShelfLife : '';
    originalDescription = descriptions.find(p => p.id === Number(product.value))?.description ?? null;
    descriptionInput.value = originalDescription ?? '';
  });
  document.getElementById('bomAddLine').addEventListener('click', () => { if (save.disabled) return; if (body.rows.length >= 200) { showError('Tối đa 200 nguyên liệu trong một công thức.'); return; } addLine(); dirty = true; });
  form.addEventListener('input', () => { dirty = true; });
  window.addEventListener('beforeunload', e => { if (dirty) { e.preventDefault(); e.returnValue = ''; } });
  form.addEventListener('submit', async e => {
    e.preventDefault();
    if (save.disabled || !form.reportValidity()) return;
    const lines = [...body.rows].map(row => ({ ingredientId: Number(row.querySelector('.bom-ingredient').value), quantity: row.querySelector('.bom-quantity').value }));
    if (!lines.length || lines.some(l => !l.ingredientId || !Number.isFinite(Number(l.quantity)) || Number(l.quantity) <= 0)) { showError('Chọn ít nhất một nguyên liệu và nhập định mức lớn hơn 0.'); return; }
    if (new Set(lines.map(l => l.ingredientId)).size !== lines.length) { showError('Một nguyên liệu chỉ được chọn một lần.'); return; }
    if (priceInput.value !== '' && (!Number.isFinite(Number(priceInput.value)) || Number(priceInput.value) < 0)) { showError('Giá thành phẩm không được âm.'); return; }
    const shelfLife = shelfLifeInput.value === '' ? null : Number(shelfLifeInput.value);
    if (shelfLife !== null && (!Number.isInteger(shelfLife) || shelfLife <= 0 || shelfLife > 2147483647)) { showError('Thời hạn sử dụng phải là số tháng nguyên lớn hơn 0.'); return; }
    const payload = { description: descriptionInput.value === '' ? null : descriptionInput.value, originalDescription, shelfLifeMonths: shelfLife, originalShelfLifeMonths: originalShelfLife, productPrice: priceInput.value === '' ? null : priceInput.value, originalProductPrice: originalPrice === '' ? null : originalPrice, id: form.dataset.id ? Number(form.dataset.id) : null, version: form.dataset.version, code: document.getElementById('bomCode').value.trim(), name: document.getElementById('bomName').value.trim(), productId: Number(product.value), lines };
    save.disabled = true; save.textContent = 'Đang lưu...'; message.hidden = true;
    try {
      const response = await fetch(form.dataset.saveUrl, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': form.querySelector('[name="__RequestVerificationToken"]').value }, body: JSON.stringify(payload) });
      const isJson = (response.headers.get('content-type') || '').includes('application/json');
      const result = isJson ? await response.json() : null;
      if (response.ok && result?.success) {
        dirty = false;
        const destination = new URL(form.dataset.detailUrl, window.location.origin);
        destination.searchParams.set('id', result.id);
        window.location.assign(destination.href);
      } else if (response.status === 401 || response.status === 403) {
        showError(result?.message || 'Phiên đăng nhập đã hết hạn hoặc bạn không có quyền. Vui lòng đăng nhập lại.');
      } else if (response.status === 400 && !result) {
        showError('Yêu cầu không hợp lệ hoặc phiên xác thực đã hết hạn. Vui lòng tải lại trang trước khi lưu.');
      } else {
        showError(result?.message || `Máy chủ không xử lý được yêu cầu (mã ${response.status}). Thông tin trên form vẫn được giữ lại.`);
      }
    } catch { showError('Không nhận được phản hồi. Hãy kiểm tra kết nối và tải lại danh sách trước khi thử lưu lại.'); }
    finally { save.disabled = false; save.textContent = 'Lưu công thức'; }
  });
});
