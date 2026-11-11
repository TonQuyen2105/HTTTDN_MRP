(() => {
  'use strict';
  const normalize = value => String(value ?? '').normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'd').toLowerCase().replace(/\s+/g, ' ').trim();
  const compact = value => value.replace(/[^a-z0-9]/g, '');
  const matches = (text, query) => {
    const haystack = normalize(text);
    return normalize(query).split(' ').filter(Boolean).every(term =>
      haystack.includes(term) || (compact(term).length > 0 && compact(haystack).includes(compact(term))));
  };
  const filters = new Map();
  const configurations = [
    { input:'bomSearchInput', body:'bomListTableBody', rows:'[data-bom-row]', count:'bomVisibleCount', empty:'bomEmptyRow' },
    { input:'statsSearchInput', body:'statsListBody', rows:'[data-stat-row]', count:'statsVisibleCount', empty:'statsEmptyRow' },
    { input:'mrSearchInput', body:'mrListBody', rows:'[data-mr-list-row]', count:'mrVisibleCount', status:'mrStatusFilter' },
    { input:'orderSearchInput', body:'orderListTableBody', rows:'.order-row', count:'lblTotalRowCount', status:'statusFilterSelect' },
    { input:'handoverSearchInput', body:'pendingHandoverBody', rows:'.pending-row', count:'handoverPendingCount' },
    { input:'handoverSearchInput', body:'handoverSlipsBody', rows:'.slip-row', count:'handoverSlipCount', status:'slipStatusFilter', handover:true }
  ];
  const show = (row, visible) => {
    row.hidden = !visible;
    row.style.setProperty('display', visible ? 'table-row' : 'none', 'important');
  };
  function init(config) {
    const input = document.getElementById(config.input);
    const body = document.getElementById(config.body);
    if (!input || !body) return;
    const rows = [...body.querySelectorAll(config.rows)];
    const status = document.getElementById(config.status);
    const count = document.getElementById(config.count);
    let empty = document.getElementById(config.empty);
    // Preserve the existing empty/load-error row where the server rendered one.
    if (!empty) empty = [...body.children].find(row => row.querySelector('td[colspan]'));
    if (!empty) {
      empty = document.createElement('tr');
      const cell = document.createElement('td');
      cell.setAttribute('colspan', body.closest('table').querySelectorAll('thead th').length);
      cell.style.cssText = 'text-align:center;padding:32px;color:#64748b;';
      cell.textContent = 'Không có kết quả phù hợp. Thử từ khóa hoặc trạng thái khác.';
      empty.appendChild(cell);
      body.appendChild(empty);
    }
    const index = rows.map(row => ({ row, text: [...row.querySelectorAll('td')].map(cell => cell.textContent).join(' ') + ' ' + (row.dataset.search || '') + ' ' + (row.dataset.code || '') + ' ' + (row.dataset.ycsx || '') }));
    function apply() {
      const selected = normalize(status?.value || 'all');
      let visible = 0;
      index.forEach(({row, text}) => {
        let rowStatus = normalize(row.dataset.status);
        if (config.handover && ['completed','received'].includes(rowStatus)) rowStatus = 'approved';
        const match = matches(text, input.value) && (!selected || selected === 'all' || selected === rowStatus);
        show(row, match);
        if (match) visible++;
      });
      show(empty, visible === 0);
      if (count) { count.textContent = visible; count.setAttribute('aria-live', 'polite'); }
    }
    const handlers = filters.get(config.input) || [];
    handlers.push(apply); filters.set(config.input, handlers);
    input.addEventListener('input', apply);
    input.addEventListener('search', apply);
    status?.addEventListener('change', apply);
    apply();
  }
  window.ProductionListSearch = {
    refresh: id => (filters.get(id) || []).forEach(apply => apply()),
    matches
  };
  const start = () => configurations.forEach(init);
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
  else start();
})();
