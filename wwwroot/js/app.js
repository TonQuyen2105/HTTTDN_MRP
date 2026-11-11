/**
 * APP.JS
 * Xử lý tương tác giao diện dùng chung: Sidebar, Menu Accordion, Tooltips
 */

document.addEventListener('DOMContentLoaded', () => {
  initSidebar();
  initSubmenus();
});

/**
 * Khởi tạo tương tác đóng/mở thanh Sidebar
 */
function initSidebar() {
  const sidebar = document.getElementById('appSidebar');
  const toggleBtn = document.getElementById('sidebarToggleBtn');
  const collapseBtn = document.getElementById('sidebarCollapseBtn');

  if (toggleBtn && sidebar) {
    toggleBtn.addEventListener('click', () => {
      sidebar.classList.toggle('collapsed');
    });
  }

  if (collapseBtn && sidebar) {
    collapseBtn.addEventListener('click', () => {
      sidebar.classList.toggle('collapsed');
      const collapseText = collapseBtn.querySelector('.collapse-text');
      if (collapseText) {
        collapseText.textContent = sidebar.classList.contains('collapsed') 
          ? 'Mở rộng menu' 
          : 'Thu gọn menu';
      }
    });
  }
}

/**
 * Xử lý click đóng/mở menu con (Accordion)
 */
function initSubmenus() {
  const navItemsWithSub = document.querySelectorAll('.nav-item.has-sub > .nav-link');

  navItemsWithSub.forEach((link) => {
    link.addEventListener('click', (e) => {
      e.preventDefault();
      const parentItem = link.parentElement;
      parentItem.classList.toggle('open');
    });
  });
}
