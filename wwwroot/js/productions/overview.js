/**
 * PRODUCTIONS - OVERVIEW JS
 * Khởi tạo dữ liệu biểu đồ, sự kiện lọc, refresh số liệu cho dashboard
 */

document.addEventListener('DOMContentLoaded', () => {
  initBanner();
  initRefreshButtons();
  initCharts();
});

/**
 * Đóng banner chào mừng
 */
function initBanner() {
  const dismissBtn = document.getElementById('btnDismissBanner');
  const banner = document.getElementById('welcomeBanner');

  if (dismissBtn && banner) {
    dismissBtn.addEventListener('click', () => {
      banner.style.opacity = '0';
      banner.style.transform = 'translateY(-10px)';
      setTimeout(() => {
        banner.classList.add('dismissed');
      }, 250);
    });
  }
}

/**
 * Sự kiện làm mới số liệu (Refresh)
 */
function initRefreshButtons() {
  const refreshBtns = document.querySelectorAll('.btn-refresh-stat');

  refreshBtns.forEach((btn) => {
    btn.addEventListener('click', () => {
      btn.classList.add('spinning');
      window.location.reload();
    });
  });
}

/**
 * Khởi tạo biểu đồ (sử dụng Chart.js nếu có, hoặc Canvas Engine tích hợp)
 */
function initCharts() {
  renderTopProductsChart();
  renderOrderStatusChart();
}

/**
 * Biểu đồ 1: Top sản phẩm có sản lượng cao nhất (Cột dọc)
 */
function renderTopProductsChart() {
  const canvas = document.getElementById('topProductsChart');
  if (!canvas) return;

  if (!window.dbTopProducts || window.dbTopProducts.length === 0) {
    const parent = canvas.parentElement;
    if (parent) {
      parent.innerHTML = '<div style="display:flex; height:240px; align-items:center; justify-content:center; color:#94a3b8; font-size:14px; font-weight:500;">Chưa có dữ liệu tồn kho sản phẩm</div>';
    }
    return;
  }

  const labels = window.dbTopProducts.map(p => p.name.length > 18 ? p.name.substring(0, 16) + '...' : p.name);
  const values = window.dbTopProducts.map(p => p.qty);
  const data = { labels, values };

  // Nếu có Chart.js từ CDN
  if (typeof Chart !== 'undefined') {
    new Chart(canvas, {
      type: 'bar',
      data: {
        labels: data.labels,
        datasets: [{
          data: data.values,
          backgroundColor: '#34a853',
          hoverBackgroundColor: '#2b8a44',
          borderRadius: 4,
          barThickness: 42
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (context) => `Tồn kho thành phẩm: ${context.parsed.y.toLocaleString()} hộp`
            }
          }
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: {
              color: '#64748b',
              font: { size: 12 }
            }
          },
          y: {
            min: 0,
            ticks: {
              color: '#94a3b8',
              font: { size: 11 }
            },
            grid: {
              color: '#f1f5f9'
            }
          }
        }
      }
    });
  } else {
    // Fallback vẽ bằng Canvas thuần nếu không có mạng
    drawFallbackBarChart(canvas, data);
  }
}

/**
 * Biểu đồ 2: Lệnh sản xuất theo trạng thái (Donut Chart)
 */
function renderOrderStatusChart() {
  const canvas = document.getElementById('orderStatusChart');
  if (!canvas) return;

  if (!window.dbStatusCounts || window.dbStatusCounts.length === 0) {
    const parent = canvas.closest('.donut-chart-container') || canvas.parentElement;
    if (parent) {
      parent.innerHTML = '<div style="display:flex; height:240px; align-items:center; justify-content:center; color:#94a3b8; font-size:14px; font-weight:500;">Chưa có dữ liệu lệnh sản xuất</div>';
    }
    return;
  }

  const statusMap = {
    'Pending': { name: 'Chờ duyệt (Pending)', color: '#f59e0b' },
    'Approved': { name: 'Đã duyệt (Approved)', color: '#3b82f6' },
    'In Production': { name: 'Đang sản xuất', color: '#10b981' },
    'Completed': { name: 'Hoàn thành', color: '#34a853' },
    'Cancelled': { name: 'Đã hủy', color: '#ef4444' }
  };

  const chartData = {
    labels: window.dbStatusCounts.map(s => statusMap[s.Status]?.name || s.Status),
    values: window.dbStatusCounts.map(s => s.Count),
    colors: window.dbStatusCounts.map(s => statusMap[s.Status]?.color || '#6366f1')
  };

  if (typeof Chart !== 'undefined') {
    new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels: chartData.labels,
        datasets: [{
          data: chartData.values,
          backgroundColor: chartData.colors,
          borderWidth: 2,
          borderColor: '#ffffff',
          hoverOffset: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '66%',
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (context) => ` ${context.label}: ${context.parsed.toLocaleString()} lệnh`
            }
          }
        }
      }
    });
  } else {
    drawFallbackDonutChart(canvas, chartData);
  }
}

/* ==========================================================================
   CANVAS FALLBACK ENGINES (Đảm bảo luôn hiển thị đẹp khi offline)
   ========================================================================== */

function drawFallbackBarChart(canvas, data) {
  if (!data || !data.values || data.values.length === 0) return;
  const ctx = canvas.getContext('2d');
  const dpr = window.devicePixelRatio || 1;
  const rect = canvas.getBoundingClientRect();
  
  canvas.width = (rect.width || 480) * dpr;
  canvas.height = (rect.height || 320) * dpr;
  ctx.scale(dpr, dpr);

  const width = canvas.width / dpr;
  const height = canvas.height / dpr;
  const paddingBottom = 40;
  const paddingLeft = 45;
  const paddingTop = 20;
  const paddingRight = 20;

  const chartHeight = height - paddingBottom - paddingTop;
  const chartWidth = width - paddingLeft - paddingRight;
  const maxVal = 450;

  // Lưới ngang
  ctx.strokeStyle = '#f1f5f9';
  ctx.lineWidth = 1;
  ctx.font = '11px sans-serif';
  ctx.fillStyle = '#94a3b8';

  for (let val = 0; val <= maxVal; val += 50) {
    const y = paddingTop + chartHeight - (val / maxVal) * chartHeight;
    ctx.beginPath();
    ctx.moveTo(paddingLeft, y);
    ctx.lineTo(width - paddingRight, y);
    ctx.stroke();
    ctx.fillText(String(val), 10, y + 4);
  }

  // Cột
  const barWidth = 46;
  const totalSlots = data.values.length;
  const slotWidth = chartWidth / totalSlots;

  data.values.forEach((val, idx) => {
    const barHeight = (val / maxVal) * chartHeight;
    const x = paddingLeft + idx * slotWidth + (slotWidth - barWidth) / 2;
    const y = paddingTop + chartHeight - barHeight;

    ctx.fillStyle = '#34a853';
    ctx.fillRect(x, y, barWidth, barHeight);

    // Nhãn X
    ctx.fillStyle = '#64748b';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(data.labels[idx], x + barWidth / 2, height - 15);
  });
}

function drawFallbackDonutChart(canvas, chartData) {
  if (!chartData || !chartData.values || chartData.values.length === 0) return;
  const total = chartData.values.reduce((acc, v) => acc + v, 0);
  if (total === 0) return;

  const ctx = canvas.getContext('2d');
  const dpr = window.devicePixelRatio || 1;
  const rect = canvas.getBoundingClientRect();

  canvas.width = (rect.width || 240) * dpr;
  canvas.height = (rect.height || 240) * dpr;
  ctx.scale(dpr, dpr);

  const cx = (canvas.width / dpr) / 2;
  const cy = (canvas.height / dpr) / 2;
  const radius = Math.min(cx, cy) - 10;
  const innerRadius = radius * 0.66;
  let startAngle = -Math.PI / 2;

  chartData.values.forEach((val, idx) => {
    const sliceAngle = (val / total) * 2 * Math.PI;
    const endAngle = startAngle + sliceAngle;

    ctx.beginPath();
    ctx.arc(cx, cy, radius, startAngle, endAngle);
    ctx.arc(cx, cy, innerRadius, endAngle, startAngle, true);
    ctx.closePath();

    ctx.fillStyle = chartData.colors[idx];
    ctx.fill();

    // Border ngăn cách
    ctx.lineWidth = 2;
    ctx.strokeStyle = '#ffffff';
    ctx.stroke();

    startAngle = endAngle;
  });
}
